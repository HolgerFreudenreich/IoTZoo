using Domain.Interfaces;
using Domain.Interfaces.Crud;
using Domain.Pocos;
using Domain.Services;
using Microsoft.Extensions.Logging;
using MimeKit;
using Moq;

namespace UnitTests;

public class MailSenderTests
{
    private static OutgoingEmail CreateEmail() => new()
    {
        Id = "mail-1",
        Priority = "high",
        CreatedAt = DateTimeOffset.Parse("2026-11-14T10:30:00+01:00"),
        Sender = new MailAddressData { Name = "Max", Address = "max@example.com" },
        Recipients = new MailRecipients
        {
            To = [new MailAddressData { Name = "Peter", Address = "peter@example.com" }],
            Cc = [new MailAddressData { Address = "support@example.com" }],
            Bcc = [new MailAddressData { Address = "archive@example.com" }]
        },
        Content = new MailContent { Subject = "Status", Format = "html", Text = "Hello", Html = "<p>Hello</p>" },
        Options = new MailOptions { AllowHtml = true, RequestReadReceipt = true }
    };

    [Fact]
    public void CreatesMessageWithAddressesBodiesAndMetadata()
    {
        var message = MailSenderService.CreateMessage(CreateEmail(), new MailSenderConfig());
        Assert.Equal("max@example.com", Assert.Single(message.From.Mailboxes).Address);
        Assert.Equal("peter@example.com", Assert.Single(message.To.Mailboxes).Address);
        Assert.Equal("support@example.com", Assert.Single(message.Cc.Mailboxes).Address);
        Assert.Equal("archive@example.com", Assert.Single(message.Bcc.Mailboxes).Address);
        Assert.Equal("Status", message.Subject);
        Assert.Equal("Hello", message.TextBody);
        Assert.Equal("<p>Hello</p>", message.HtmlBody);
        Assert.Equal(MessagePriority.Urgent, message.Priority);
        Assert.Equal("mail-1", message.Headers["X-IoTZoo-Mail-Id"]);
        Assert.Contains("max@example.com", message.Headers["Disposition-Notification-To"]);
        Assert.Equal(DateTimeOffset.Parse("2026-11-14T10:30:00+01:00"), message.Date);
    }

    [Fact]
    public void HtmlRequiresExplicitPermission()
    {
        var email = CreateEmail();
        email.Options!.AllowHtml = false;
        Assert.Throws<ArgumentException>(() => MailSenderService.CreateMessage(email, new MailSenderConfig()));
    }

    [Fact]
    public void AttachmentMustStayInConfiguredDirectory()
    {
        var email = CreateEmail();
        email.Attachments = [new MailAttachment { Path = Path.Combine("..", "secret.txt") }];
        Assert.Throws<ArgumentException>(() => MailSenderService.CreateMessage(email, new MailSenderConfig { AttachmentDirectory = Path.GetTempPath() }));
    }

    [Fact]
    public void MissingRecipientsAreRejected()
    {
        var email = CreateEmail();
        email.Recipients = new MailRecipients();
        Assert.Throws<ArgumentException>(() => MailSenderService.CreateMessage(email, new MailSenderConfig()));
    }

    [Fact]
    public async Task RetainedCommandDoesNotLoadSettings()
    {
        var settings = new Mock<ISettingsCrudService>();
        var sender = new MailSenderService(settings.Object, Mock.Of<IKnownTopicsCrudService>(), Mock.Of<IDataTransferService>(), Mock.Of<ILogger<MailSenderService>>());
        await sender.HandleAsync("{}", true);
        settings.Verify(x => x.GetObject(It.IsAny<SettingCategory>(), It.IsAny<SettingKey>()), Times.Never);
    }

    [Fact]
    public async Task RejectsPayloadThatOverridesConfiguredServer()
    {
        var settings = new Mock<ISettingsCrudService>();
        settings.Setup(x => x.GetObject(SettingCategory.Mail, SettingKey.MailSenderConfig))
            .ReturnsAsync(new MailSenderConfig { Enabled = true, HostName = "configured.example.com" });
        var logger = new Mock<ILogger<MailSenderService>>();
        var sender = new MailSenderService(settings.Object, Mock.Of<IKnownTopicsCrudService>(), Mock.Of<IDataTransferService>(), logger.Object);
        await sender.HandleAsync("{\"email\":{\"smtp\":{\"host\":\"other.example.com\"}}}", false);
        logger.Verify(x => x.Log(LogLevel.Error, It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((state, _) => Convert.ToString(state)!.Contains("Mail sender command failed")),
            It.Is<ArgumentException>(exception => exception != null && exception.Message.Contains("do not match")),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public void AddsAttachmentAndChecksDeclaredSize()
    {
        string path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, "abc");
            var email = CreateEmail();
            email.Attachments = [new MailAttachment { Path = path, Filename = "status.txt", MimeType = "text/plain", Size = 3 }];
            var config = new MailSenderConfig { AttachmentDirectory = Path.GetTempPath() };
            var message = MailSenderService.CreateMessage(email, config);
            var attachment = Assert.IsAssignableFrom<MimePart>(Assert.Single(message.Attachments));
            Assert.Equal("status.txt", attachment.FileName);
            Assert.Equal("text/plain", attachment.ContentType.MimeType);
            email.Attachments[0].Size = 4;
            Assert.Throws<ArgumentException>(() => MailSenderService.CreateMessage(email, config));
        }
        finally
        {
            File.Delete(path);
        }
    }
}
