using System.Buffers;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using DataAccess.Interfaces;
using Domain.Interfaces;
using Domain.Interfaces.Crud;
using Domain.Interfaces.RuleEngine;
using Domain.Interfaces.Timer;
using Domain.Pocos;
using Domain.Services;
using Domain.Services.MQTT;
using Domain.Services.RuleEngine;
using Domain.Services.Timer;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using MQTTnet.Server;

namespace UnitTests;

public class CountDownTests
{
    [Theory]
    [InlineData("countdown_start")]
    [InlineData("start_countdown")]
    public async Task StartTopicsCreateCountdown(string topic)
    {
        var dataTransfer = new Mock<IDataTransferService>();
        dataTransfer.SetupGet(x => x.NamespaceName).Returns("iotzoo");
        var factory = new Mock<ICountDownFactory>();
        var rules = new Mock<IRulesCrudService>();
        rules.Setup(x => x.GetRules(It.IsAny<bool>())).ReturnsAsync(new List<Rule>());
        rules.Setup(x => x.GetRulesBySourceTopic(It.IsAny<KnownTopic>(), It.IsAny<bool>()))
            .ReturnsAsync(new List<Rule>());
        var client = new IotZooMqttClient(
            Options.Create(new AppSettings()), Mock.Of<IKnownMicrocontrollerCrudService>(), dataTransfer.Object,
            Mock.Of<IKnownTopicsCrudService>(), Mock.Of<IHueBridgeService>(), Mock.Of<IProjectCrudService>(),
            NullLogger<IotZooMqttClient>.Instance, rules.Object, Mock.Of<ISettingsCrudService>(),
            Mock.Of<ITopicHistoryCrudService>(), Mock.Of<IExpressionEvaluationService>(),
            Mock.Of<IPrepareTargetPayload>(), Mock.Of<ICronCrudService>(), factory.Object);

        await client.HandleMqttMessage(new TopicEntry
        {
            NamespaceName = "iotzoo",
            ProjectName = "picea",
            Topic = topic,
            Payload = "{\"ProjectName\":\"picea\",\"CounterName\":\"ClearVorne\",\"Seconds\":20,\"ReportProgress\":true}"
        });

        factory.Verify(x => x.Create(It.Is<CountDownData>(data =>
            data.ProjectName == "picea" && data.CounterName == "ClearVorne" &&
            data.Seconds == 20 && data.ReportProgress)), Times.Once);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task CountdownPublishesStatusAndFinishedOnce(bool reportProgress)
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();

        var server = new MqttServerFactory().CreateMqttServer(
            new MqttServerOptionsBuilder().WithDefaultEndpoint().WithDefaultEndpointPort(port).Build());
        var messages = new ConcurrentQueue<(string Topic, string Payload)>();
        var finishedReceived = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var markerReceived = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        server.InterceptingPublishAsync += args =>
        {
            var message = args.ApplicationMessage;
            messages.Enqueue((message.Topic, Encoding.UTF8.GetString(message.Payload.ToArray())));
            if (message.Topic == "iotzoo/picea/countdown_finished")
            {
                finishedReceived.TrySetResult();
            }
            else if (message.Topic == "iotzoo/picea/test_marker")
            {
                markerReceived.TrySetResult();
            }
            return Task.CompletedTask;
        };
        await server.StartAsync();
        try
        {
            var dataTransfer = new Mock<IDataTransferService>();
            dataTransfer.SetupGet(x => x.NamespaceName).Returns("iotzoo");
            dataTransfer.SetupGet(x => x.MqttBrokerSettings).Returns(new MqttBrokerSettings { Ip = "127.0.0.1", Port = port });
            var completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var countdown = new CountDownService(NullLogger<CountDownService>.Instance, dataTransfer.Object,
                new CountDownData { ProjectName = "picea", CounterName = "ClearVorne", Seconds = 1, ReportProgress = reportProgress });
            countdown.Completed += _ => completed.TrySetResult();

            await Task.WhenAll(finishedReceived.Task, completed.Task).WaitAsync(TimeSpan.FromSeconds(10));
            await countdown.MqttClient.PublishAsync("iotzoo/picea/test_marker", "done");
            await markerReceived.Task.WaitAsync(TimeSpan.FromSeconds(10));

            var statuses = messages.Where(x => x.Topic == "iotzoo/picea/countdown_status").ToArray();
            var status = Assert.Single(statuses);
            var statusData = JsonSerializer.Deserialize<CountDownData>(status.Payload);
            Assert.NotNull(statusData);
            Assert.Equal(0, statusData.Seconds);
            Assert.Equal("ClearVorne", statusData.CounterName);
            var finished = Assert.Single(messages.Where(x => x.Topic == "iotzoo/picea/countdown_finished"));
            Assert.Equal("ClearVorne", finished.Payload);
        }
        finally
        {
            await server.StopAsync();
            server.Dispose();
        }
    }
}
