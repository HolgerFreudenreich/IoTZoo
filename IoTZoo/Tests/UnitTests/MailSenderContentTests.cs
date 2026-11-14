using System.Text.Json;
using Domain.Interfaces;
using Domain.Interfaces.Crud;
using Domain.Pocos;
using Domain.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace UnitTests;

public class MailSenderContentTests
{
    private static MailSenderService CreateSender(Mock<IKnownTopicsCrudService> topics)
    {
        var dataTransfer = new Mock<IDataTransferService>();
        dataTransfer.SetupGet(x => x.NamespaceName).Returns("iotzoo");
        return new MailSenderService(Mock.Of<ISettingsCrudService>(), topics.Object, dataTransfer.Object,
            NullLogger<MailSenderService>.Instance);
    }

    [Fact]
    public async Task ResolvesExamplePayloadAndKeepsJsonCharactersIntact()
    {
        const string payload = """
            {"email":{"sender":{"address":"camera@example.com"},"recipients":{"to":[{"address":"holger@example.com"}]},"content":{"subject":"Wettervorhersage","format":"text","text":"Read('iotzoo/picea/weather_forecast')"}}}
            """;
        var email = JsonSerializer.Deserialize<MailSenderRequest>(payload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!.Email!;
        var topics = new Mock<IKnownTopicsCrudService>();
        topics.Setup(x => x.GetKnownTopicByTopicName("picea", "weather_forecast"))
            .ReturnsAsync(new KnownTopic { LastPayload = "Regen: \"stark\"\n22 °C \\ Nord" });
        await CreateSender(topics).ResolveContentAsync(email.Content!);
        Assert.Equal("Regen: \"stark\"\n22 °C \\ Nord", email.Content!.Text);
        var message = MailSenderService.CreateMessage(email, new MailSenderConfig());
        Assert.Equal("Wettervorhersage", message.Subject);
        Assert.Equal("Regen: \"stark\"\r\n22 °C \\ Nord", message.TextBody);
        topics.Verify(x => x.GetKnownTopicByTopicName("picea", "weather_forecast"), Times.Once);
    }

    [Fact]
    public async Task ResolvesMultipleReferencesAcrossFieldsWithoutRecursivelyEvaluatingValues()
    {
        var topics = new Mock<IKnownTopicsCrudService>();
        topics.Setup(x => x.GetKnownTopicByTopicName("picea", "weather/forecast"))
            .ReturnsAsync(new KnownTopic { LastPayload = "Read('iotzoo/picea/not-evaluated')" });
        topics.Setup(x => x.GetKnownTopicByTopicName("other", "temperature"))
            .ReturnsAsync(new KnownTopic { LastPayload = "22 °C" });
        var content = new MailContent
        {
            Subject = "Status: Read('iotzoo/other/temperature')",
            Text = "Forecast (Read('iotzoo/picea/weather/forecast')), temperature: Read('iotzoo/other/temperature')!",
            Html = "<p>Read('iotzoo/other/temperature')</p>"
        };
        await CreateSender(topics).ResolveContentAsync(content);
        Assert.Equal("Status: 22 °C", content.Subject);
        Assert.Equal("Forecast (Read('iotzoo/picea/not-evaluated')), temperature: 22 °C!", content.Text);
        Assert.Equal("<p>22 °C</p>", content.Html);
        topics.Verify(x => x.GetKnownTopicByTopicName("other", "temperature"), Times.Once);
        topics.Verify(x => x.GetKnownTopicByTopicName("picea", "not-evaluated"), Times.Never);
    }

    [Fact]
    public async Task EmptyPayloadIsValidAndOrdinaryContentIsUnchanged()
    {
        var topics = new Mock<IKnownTopicsCrudService>();
        topics.Setup(x => x.GetKnownTopicByTopicName("picea", "empty"))
            .ReturnsAsync(new KnownTopic { LastPayload = string.Empty });
        var content = new MailContent { Subject = "Ordinary subject", Text = "before Read('iotzoo/picea/empty') after" };
        await CreateSender(topics).ResolveContentAsync(content);
        Assert.Equal("Ordinary subject", content.Subject);
        Assert.Equal("before  after", content.Text);
        Assert.Null(content.Html);
    }

    [Theory]
    [InlineData("Read('other/picea/weather')")]
    [InlineData("Read('iotzoo/picea')")]
    [InlineData("Read(iotzoo/picea/weather)")]
    [InlineData("Read('iotzoo/picea/weather'")]
    public async Task InvalidReferenceIsRejectedWithoutDatabaseLookup(string text)
    {
        var topics = new Mock<IKnownTopicsCrudService>();
        var content = new MailContent { Text = text };
        await Assert.ThrowsAsync<ArgumentException>(() => CreateSender(topics).ResolveContentAsync(content));
        Assert.Equal(text, content.Text);
        topics.Verify(x => x.GetKnownTopicByTopicName(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MissingTopicOrPayloadIsRejected(bool existing)
    {
        var topics = new Mock<IKnownTopicsCrudService>();
        topics.Setup(x => x.GetKnownTopicByTopicName("picea", "weather"))
            .ReturnsAsync(existing ? new KnownTopic { LastPayload = null } : null);
        var content = new MailContent { Subject = "Subject", Text = "Read('iotzoo/picea/weather')" };
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => CreateSender(topics).ResolveContentAsync(content));
        Assert.Contains("iotzoo/picea/weather", exception.Message);
        Assert.Equal("Read('iotzoo/picea/weather')", content.Text);
    }

    [Fact]
    public async Task HandlerLogsMissingReferenceBeforeAttemptingSmtp()
    {
        var settings = new Mock<ISettingsCrudService>();
        settings.Setup(x => x.GetObject(SettingCategory.Mail, SettingKey.MailSenderConfig))
            .ReturnsAsync(new MailSenderConfig { Enabled = true, HostName = "invalid host" });
        var topics = new Mock<IKnownTopicsCrudService>();
        var dataTransfer = new Mock<IDataTransferService>();
        dataTransfer.SetupGet(x => x.NamespaceName).Returns("iotzoo");
        var logger = new Mock<ILogger<MailSenderService>>();
        var sender = new MailSenderService(settings.Object, topics.Object, dataTransfer.Object, logger.Object);
        await sender.HandleAsync("""{"email":{"content":{"text":"Read('iotzoo/picea/weather')"}}}""", false);
        topics.Verify(x => x.GetKnownTopicByTopicName("picea", "weather"), Times.Once);
        logger.Verify(x => x.Log(LogLevel.Error, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(),
            It.Is<InvalidOperationException>(exception => exception != null && exception.Message.Contains("No LastPayload")),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }
}
