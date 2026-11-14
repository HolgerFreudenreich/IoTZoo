using System.Security.Cryptography;
using Domain.Interfaces;
using Domain.Interfaces.Crud;
using Domain.Interfaces.MQTT;
using Domain.Pocos;
using Domain.Services;
using IotZoo.Pages;
using MailKit.Net.Imap;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MudBlazor;

namespace UnitTests;

public class SettingsMailSenderTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UnreadableSenderPasswordDoesNotBlockPageOrOverwriteSavedSettings(bool replace)
    {
        var settings = new Mock<ISettingsCrudService>();
        settings.Setup(x => x.GetObject(SettingCategory.Mail, SettingKey.MailSenderConfig))
            .ThrowsAsync(new CryptographicException("Password authentication failed"));
        settings.Setup(x => x.GetSetting("Mail", "MailSenderConfig"))
            .ReturnsAsync(new Setting { Category = "Mail", SettingKey = "MailSenderConfig", SettingValue = "{\"Enabled\":true,\"HostName\":\"smtp.example.com\",\"UserName\":\"user\",\"Password\":\"unreadable-ciphertext\"}" });
        settings.Setup(x => x.GetSettingString(SettingCategory.Mail, SettingKey.MailReceiverConfigs)).ReturnsAsync("");
        settings.Setup(x => x.Update(It.IsAny<SettingCategory>(), It.IsAny<SettingKey>(), It.IsAny<object>())).ReturnsAsync(1);
        var snackbar = new Mock<ISnackbar>();
        var page = new TestSettingsPage(settings.Object, snackbar.Object);

        await page.InitializeAsync();
        Assert.True(page.Unreadable);
        Assert.True(page.Sender.Enabled);
        Assert.Equal("smtp.example.com", page.Sender.HostName);
        Assert.Equal("user", page.Sender.UserName);
        Assert.Equal(string.Empty, page.Sender.Password);
        snackbar.Verify(x => x.Add(It.Is<string>(text => text.Contains("could not be read")), Severity.Error,
            It.IsAny<Action<SnackbarOptions>>(), It.IsAny<string>()), Times.Once);

        if (replace)
        {
            page.Replace = true;
            page.Sender.Password = "new-password";
        }
        await page.SaveAsync();
        settings.Verify(x => x.Update(SettingCategory.Mail, SettingKey.MailSenderConfig,
            It.Is<object>(value => value is MailSenderConfig && ((MailSenderConfig)value).Password == "new-password")), replace ? Times.Once() : Times.Never());
        if (!replace)
        {
            settings.Verify(x => x.Update(SettingCategory.Mail, SettingKey.MailSenderConfig, It.IsAny<object>()), Times.Never);
            snackbar.Verify(x => x.Add(It.Is<string>(text => text.Contains("were not changed")), Severity.Warning,
                It.IsAny<Action<SnackbarOptions>>(), It.IsAny<string>()), Times.Once);
        }
        Assert.Equal(!replace, page.Unreadable);
    }

    [Fact]
    public async Task InvalidSenderJsonDoesNotPreventOpeningPage()
    {
        var settings = new Mock<ISettingsCrudService>();
        settings.Setup(x => x.GetObject(SettingCategory.Mail, SettingKey.MailSenderConfig))
            .ThrowsAsync(new System.Text.Json.JsonException("Invalid JSON"));
        settings.Setup(x => x.GetSettingString(SettingCategory.Mail, SettingKey.MailReceiverConfigs)).ReturnsAsync("");
        var snackbar = new Mock<ISnackbar>();
        var page = new TestSettingsPage(settings.Object, snackbar.Object);
        await page.InitializeAsync();
        Assert.True(page.Unreadable);
        Assert.False(page.Sender.Enabled);
        snackbar.Verify(x => x.Add(It.Is<string>(text => text.Contains("could not be read")), Severity.Error,
            It.IsAny<Action<SnackbarOptions>>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task ReplacingUnreadableEnabledSettingsRequiresPassword()
    {
        var settings = new Mock<ISettingsCrudService>();
        settings.Setup(x => x.GetObject(SettingCategory.Mail, SettingKey.MailSenderConfig))
            .ThrowsAsync(new CryptographicException("Password authentication failed"));
        settings.Setup(x => x.GetSetting("Mail", "MailSenderConfig"))
            .ReturnsAsync(new Setting { Category = "Mail", SettingKey = "MailSenderConfig", SettingValue = "{\"Enabled\":true,\"HostName\":\"smtp.example.com\",\"UserName\":\"user\",\"Password\":\"unreadable\"}" });
        settings.Setup(x => x.GetSettingString(SettingCategory.Mail, SettingKey.MailReceiverConfigs)).ReturnsAsync("");
        var snackbar = new Mock<ISnackbar>();
        var page = new TestSettingsPage(settings.Object, snackbar.Object);
        await page.InitializeAsync();
        page.Replace = true;
        await page.SaveAsync();
        settings.Verify(x => x.Update(SettingCategory.Mail, SettingKey.MailSenderConfig, It.IsAny<object>()), Times.Never);
        Assert.True(page.Unreadable);
        snackbar.Verify(x => x.Add(It.Is<string>(text => text.Contains("password when a username is configured")), Severity.Error,
            It.IsAny<Action<SnackbarOptions>>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task RejectedSmtpCredentialsDoNotPreventSettingsFromOpeningOrBeingCorrected()
    {
        var config = new MailSenderConfig { Enabled = true, HostName = "smtp.example.com", UserName = "user", Password = "wrong" };
        var settings = new Mock<ISettingsCrudService>();
        settings.Setup(x => x.GetObject(SettingCategory.Mail, SettingKey.MailSenderConfig)).ReturnsAsync(() => config.Copy());
        settings.Setup(x => x.DisableMailSenderIfMatches(It.IsAny<MailSenderConfig>()))
            .Callback<MailSenderConfig>(_ => config.Enabled = false).ReturnsAsync(true);
        settings.Setup(x => x.GetSettingString(SettingCategory.Mail, SettingKey.MailReceiverConfigs)).ReturnsAsync("");
        settings.Setup(x => x.Update(It.IsAny<SettingCategory>(), It.IsAny<SettingKey>(), It.IsAny<object>())).ReturnsAsync(1);
        var smtp = CreateRejectingSmtp();
        var logger = new Mock<ILogger<MailSenderService>>();
        var sender = new TestMailSender(settings.Object, smtp.Object, logger.Object);
        await sender.HandleAsync("""{"email":{"sender":{"address":"a@example.com"},"recipients":{"to":[{"address":"b@example.com"}]},"content":{"text":"hello"}}}""", false);
        logger.Verify(x => x.Log(LogLevel.Error, It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((state, _) => Convert.ToString(state)!.Contains("authentication failed")),
            It.Is<MailKit.Security.AuthenticationException>(exception => exception != null && exception.Message.Contains("rejected")),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
        smtp.Verify(x => x.AuthenticateAsync(It.IsAny<System.Text.Encoding>(), It.IsAny<System.Net.ICredentials>(), It.IsAny<CancellationToken>()), Times.Once);
        smtp.Verify(x => x.SendAsync(It.IsAny<MailKit.ISendRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        Assert.False(config.Enabled);
        settings.Verify(x => x.DisableMailSenderIfMatches(It.Is<MailSenderConfig>(c => c.Enabled && c.Password == "wrong")), Times.Once);
        await sender.HandleAsync("{}", false);
        smtp.Verify(x => x.AuthenticateAsync(It.IsAny<System.Text.Encoding>(), It.IsAny<System.Net.ICredentials>(), It.IsAny<CancellationToken>()), Times.Once);

        var snackbar = new Mock<ISnackbar>();
        var testSmtp = CreateRejectingSmtp();
        var page = new TestSettingsPage(settings.Object, snackbar.Object) { Smtp = testSmtp.Object };
        await page.InitializeAsync();
        Assert.False(page.Unreadable);
        Assert.False(page.Sender.Enabled);
        await page.TestSmtpAsync();
        snackbar.Verify(x => x.Add(It.Is<string>(text => text.Contains("SMTP authentication failed") && text.Contains("rejected")), Severity.Error,
            It.IsAny<Action<SnackbarOptions>>(), It.IsAny<string>()), Times.Once);
        page.Sender.Password = "corrected";
        page.Sender.Enabled = true;
        await page.SaveAsync();
        settings.Verify(x => x.Update(SettingCategory.Mail, SettingKey.MailSenderConfig,
            It.Is<object>(value => value is MailSenderConfig && ((MailSenderConfig)value).Password == "corrected")), Times.Once);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task SettingsSmtpTestReflectsWhetherSavedSenderWasDisabled(bool disabled)
    {
        var config = new MailSenderConfig { Enabled = true, HostName = "smtp", UserName = "user", Password = "bad" };
        var settings = new Mock<ISettingsCrudService>();
        settings.Setup(x => x.GetObject(SettingCategory.Mail, SettingKey.MailSenderConfig)).ReturnsAsync(() => config.Copy());
        settings.Setup(x => x.GetSettingString(SettingCategory.Mail, SettingKey.MailReceiverConfigs)).ReturnsAsync("");
        settings.Setup(x => x.DisableMailSenderIfMatches(It.IsAny<MailSenderConfig>())).ReturnsAsync(disabled);
        var snackbar = new Mock<ISnackbar>();
        var page = new TestSettingsPage(settings.Object, snackbar.Object) { Smtp = CreateRejectingSmtp().Object };
        await page.InitializeAsync();
        await page.TestSmtpAsync();
        Assert.Equal(!disabled, page.Sender.Enabled);
        settings.Verify(x => x.DisableMailSenderIfMatches(It.Is<MailSenderConfig>(c => c.Password == "bad")), Times.Once);
        snackbar.Verify(x => x.Add(It.Is<string>(text => disabled ? text.Contains("Mail sender disabled") : text.Contains("not disabled")), Severity.Error,
            It.IsAny<Action<SnackbarOptions>>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task RejectedImapCredentialsDisableSenderBeforeSmtpSend()
    {
        var config = new MailSenderConfig { Enabled = true, HostName = "smtp", UserName = "user", Password = "bad", ImapHostName = "imap" };
        var settings = new Mock<ISettingsCrudService>();
        settings.Setup(x => x.GetObject(SettingCategory.Mail, SettingKey.MailSenderConfig)).ReturnsAsync(config);
        settings.Setup(x => x.DisableMailSenderIfMatches(It.IsAny<MailSenderConfig>())).ReturnsAsync(true);
        var smtp = new Mock<SmtpClient>();
        smtp.Setup(x => x.ConnectAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<SecureSocketOptions>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        smtp.Setup(x => x.AuthenticateAsync(It.IsAny<System.Text.Encoding>(), It.IsAny<System.Net.ICredentials>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var imap = new Mock<ImapClient>();
        imap.Setup(x => x.ConnectAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<SecureSocketOptions>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        imap.Setup(x => x.AuthenticateAsync(It.IsAny<System.Text.Encoding>(), It.IsAny<System.Net.ICredentials>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new MailKit.Security.AuthenticationException("IMAP rejected"));
        var sender = new TestMailSender(settings.Object, smtp.Object, NullLogger<MailSenderService>.Instance) { Imap = imap.Object };
        await sender.HandleAsync("""{"email":{"sender":{"address":"a@example.com"},"recipients":{"to":[{"address":"b@example.com"}]},"content":{"text":"hello"},"options":{"saveToSentFolder":true}}}""", false);
        settings.Verify(x => x.DisableMailSenderIfMatches(It.Is<MailSenderConfig>(c => c.ImapHostName == "imap" && c.Password == "bad")), Times.Once);
        smtp.Verify(x => x.SendAsync(It.IsAny<MailKit.ISendRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task NetworkFailureDoesNotDisableSender()
    {
        var settings = new Mock<ISettingsCrudService>();
        settings.Setup(x => x.GetObject(SettingCategory.Mail, SettingKey.MailSenderConfig))
            .ReturnsAsync(new MailSenderConfig { Enabled = true, HostName = "smtp" });
        var smtp = new Mock<SmtpClient>();
        smtp.Setup(x => x.ConnectAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<SecureSocketOptions>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IOException("Network unavailable"));
        var sender = new TestMailSender(settings.Object, smtp.Object, NullLogger<MailSenderService>.Instance);
        await sender.HandleAsync("""{"email":{"sender":{"address":"a@example.com"},"recipients":{"to":[{"address":"b@example.com"}]},"content":{"text":"hello"}}}""", false);
        settings.Verify(x => x.DisableMailSenderIfMatches(It.IsAny<MailSenderConfig>()), Times.Never);
    }

    [Fact]
    public async Task StaleSettingsPageDoesNotReenableAutomaticallyDisabledSender()
    {
        var config = new MailSenderConfig { Enabled = true, HostName = "smtp", UserName = "user", Password = "wrong" };
        var settings = new Mock<ISettingsCrudService>();
        settings.Setup(x => x.GetObject(SettingCategory.Mail, SettingKey.MailSenderConfig)).ReturnsAsync(() => config.Copy());
        settings.Setup(x => x.GetSettingString(SettingCategory.Mail, SettingKey.MailReceiverConfigs)).ReturnsAsync("");
        var snackbar = new Mock<ISnackbar>();
        var page = new TestSettingsPage(settings.Object, snackbar.Object);
        await page.InitializeAsync();
        config.Enabled = false;
        await page.SaveAsync();
        Assert.False(page.Sender.Enabled);
        settings.Verify(x => x.Update(SettingCategory.Mail, SettingKey.MailSenderConfig, It.IsAny<object>()), Times.Never);
        snackbar.Verify(x => x.Add(It.Is<string>(text => text.Contains("disabled after these settings were opened")), Severity.Warning,
            It.IsAny<Action<SnackbarOptions>>(), It.IsAny<string>()), Times.Once);
    }

    private static Mock<SmtpClient> CreateRejectingSmtp()
    {
        var smtp = new Mock<SmtpClient>();
        smtp.Setup(x => x.ConnectAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<SecureSocketOptions>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        smtp.Setup(x => x.AuthenticateAsync(It.IsAny<System.Text.Encoding>(), It.IsAny<System.Net.ICredentials>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new MailKit.Security.AuthenticationException("Credentials rejected"));
        return smtp;
    }

    private sealed class TestMailSender(ISettingsCrudService settings, SmtpClient smtp, ILogger<MailSenderService> logger)
        : MailSenderService(settings, Mock.Of<IKnownTopicsCrudService>(), Mock.Of<IDataTransferService>(), logger)
    {
        public ImapClient? Imap { get; set; }
        protected override SmtpClient CreateSmtpClient() => smtp;
        protected override ImapClient CreateImapClient() => Imap ?? base.CreateImapClient();
    }

    private sealed class TestSettingsPage : SettingsPageBase
    {
        public TestSettingsPage(ISettingsCrudService settings, ISnackbar snackbar)
        {
            SettingsService = settings;
            Snackbar = snackbar;
            MailSenderService = new MailSenderService(settings, Mock.Of<IKnownTopicsCrudService>(), Mock.Of<IDataTransferService>(), NullLogger<MailSenderService>.Instance);
            Logger = NullLogger<PageBase>.Instance;
            var transfer = new Mock<IDataTransferService>();
            transfer.SetupProperty(x => x.CurrentScreen);
            transfer.SetupGet(x => x.NamespaceName).Returns("iotzoo");
            transfer.SetupGet(x => x.PhilipsHueBridgeSettings).Returns(new PhilipsHueBridgeSettings { Ip = "", Key = "" });
            DataTransferService = transfer.Object;
            InternalBroker = Mock.Of<IIotZooMqttBroker>();
            IotZooMqttClient = Mock.Of<IIoTZooMqttClient>();
            var projects = new Mock<IProjectCrudService>();
            projects.Setup(x => x.LoadProjects()).ReturnsAsync(new List<Project>());
            ProjectService = projects.Object;
            KnownTopicsService = Mock.Of<IKnownTopicsCrudService>();
            NamespaceService = Mock.Of<INamespaceCrudService>();
        }

        public SmtpClient? Smtp { get; set; }
        protected override SmtpClient CreateMailSenderClient() => Smtp ?? base.CreateMailSenderClient();
        public MailSenderConfig Sender => MailSenderConfig;
        public bool Unreadable => MailSenderSettingsUnreadable;
        public bool Replace { set => ReplaceUnreadableMailSenderSettings = value; }
        public Task InitializeAsync() => OnInitializedAsync();
        public Task TestSmtpAsync() => TestMailSenderAsync();
        public Task SaveAsync() => Save();
    }
}
