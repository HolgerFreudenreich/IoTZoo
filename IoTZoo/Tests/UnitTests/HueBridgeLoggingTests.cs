// --------------------------------------------------------------------------------------------------------------------
//      ____    ______   _____
//     /  _/___/_  __/  /__  / ____  ____
//     / // __ \/ /       / / / __ \/ __ \
//   _/ // /_/ / /       / /_/ /_/ / /_/ /
//  /___/\____/_/       /____|____/\____/   P L A Y G R O U N D
// --------------------------------------------------------------------------------------------------------------------
// Connect «Things» with microcontrollers in a simple way.
// --------------------------------------------------------------------------------------------------------------------
// (c) 2025 - 2026 Holger Freudenreich under the MIT license
// --------------------------------------------------------------------------------------------------------------------

using System.Collections.Concurrent;
using System.Text.Json;
using DataAccess.Services;
using Domain.Interfaces;
using Domain.Interfaces.Crud;
using Domain.Pocos;
using HueApi.Models;
using HueApi.Models.Responses;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace UnitTests;

public class HueBridgeLoggingTests
{
    [Fact]
    public async Task RepeatedInvalidResponsesAreRateLimitedAndRecoveryResetsLimit()
    {
        var clock = new TestTimeProvider();
        var logger = new RecordingLogger();
        var service = CreateService(logger, clock);
        service.Failure = new JsonException("Invalid response");

        await Task.WhenAll(Enumerable.Range(0, 50).Select(_ => Task.Run(() => service.SetLightBrightness(1, 50))));
        Assert.Empty(logger.Errors.Where(e => e.Exception is JsonException));
        Assert.Equal(50, service.FetchCount);
    }

    [Fact]
    public async Task OtherFailuresAreNotRateLimited()
    {
        var logger = new RecordingLogger();
        var service = CreateService(logger, new TestTimeProvider());
        service.Failure = new InvalidOperationException("Other Hue failure");

        await service.SetLightBrightness(1, 50);
        await service.SetLightBrightness(1, 50);

        Assert.True(logger.Errors.Count(e => e.Exception is InvalidOperationException && e.Exception.Message == "Other Hue failure") >= 2);
    }

    [Fact]
    public async Task DirectGetLightsStillLogsAndPropagatesInvalidResponse()
    {
        var logger = new RecordingLogger();
        var service = CreateService(logger, new TestTimeProvider());
        service.Failure = new JsonException("Invalid response");

        await Assert.ThrowsAsync<JsonException>(() => service.GetLights());
        await Assert.ThrowsAsync<JsonException>(() => service.GetLights());
        Assert.Equal(2, logger.Errors.Count(e => e.Exception is JsonException));
    }

    private static TestHueBridgeService CreateService(RecordingLogger logger, TestTimeProvider clock)
    {
        var transfer = new Mock<IDataTransferService>();
        transfer.SetupGet(x => x.PhilipsHueBridgeSettings).Returns(new PhilipsHueBridgeSettings { Ip = "", Key = "" });
        transfer.SetupGet(x => x.MqttBrokerSettings).Returns(new MqttBrokerSettings { Ip = "127.0.0.1", Port = 1 });
        return new TestHueBridgeService(logger, transfer.Object, clock);
    }

    private sealed class TestHueBridgeService(ILogger<HueBridgeService> logger, IDataTransferService transfer, TimeProvider clock)
        : HueBridgeService(logger, Options.Create(new AppSettings()), transfer,
            Mock.Of<IProjectCrudService>(), Mock.Of<IKnownTopicsCrudService>(), clock)
    {
        private int fetchCount;
        public int FetchCount => fetchCount;
        public Exception? Failure { get; set; }

        protected override Task<HueResponse<Light>> FetchLightsAsync()
        {
            Interlocked.Increment(ref fetchCount);
            if (Failure is Exception failure)
            {
                return Task.FromException<HueResponse<Light>>(failure);
            }
            var response = JsonSerializer.Deserialize<HueResponse<Light>>("{\"data\":[],\"errors\":[]}", JsonSerializerOptions.Web)!;
            return Task.FromResult(response);
        }
    }

    private sealed class TestTimeProvider : TimeProvider
    {
        private long timestamp;
        public override long TimestampFrequency => TimeSpan.TicksPerSecond;
        public override long GetTimestamp() => Interlocked.Read(ref timestamp);
        public void Advance(TimeSpan duration) => Interlocked.Add(ref timestamp, duration.Ticks);
    }

    private sealed class RecordingLogger : ILogger<HueBridgeService>
    {
        public ConcurrentQueue<(Exception? Exception, string Message)> Errors { get; } = new();
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (logLevel == LogLevel.Error)
            {
                Errors.Enqueue((exception, formatter(state, exception)));
            }
        }
    }
}
