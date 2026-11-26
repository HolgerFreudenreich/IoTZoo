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

using Domain.Interfaces;
using Domain.Interfaces.Crud;
using Domain.Interfaces.MQTT;
using Domain.Interfaces.RuleEngine;
using Domain.Interfaces.Timer;
using Domain.Services;
using Domain.Services.MQTT;
using Domain.Services.RuleEngine;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;

namespace UnitTests;

public class DeveloperTestsServiceRegistrationTests
{
    [Fact]
    public async Task MqttClientResolvesWithMailSenderWithoutConnectingToExternalServices()
    {
        var services = new ServiceCollection();
        DeveloperTests.Program.ConfigureServices(services, new ConfigurationBuilder().Build());

        services.RemoveAll<IDataTransferService>();
        services.AddSingleton(Mock.Of<IDataTransferService>());
        services.RemoveAll<IKnownMicrocontrollerCrudService>();
        services.AddSingleton(Mock.Of<IKnownMicrocontrollerCrudService>());
        services.RemoveAll<IRulesCrudService>();
        services.AddSingleton(Mock.Of<IRulesCrudService>());
        services.RemoveAll<ISettingsCrudService>();
        services.AddSingleton(Mock.Of<ISettingsCrudService>());
        services.RemoveAll<IKnownTopicsCrudService>();
        services.AddSingleton(Mock.Of<IKnownTopicsCrudService>());
        services.RemoveAll<IProjectCrudService>();
        services.AddSingleton(Mock.Of<IProjectCrudService>());
        services.RemoveAll<ICronCrudService>();
        services.AddSingleton(Mock.Of<ICronCrudService>());
        services.RemoveAll<ICountDownFactory>();
        services.AddSingleton(Mock.Of<ICountDownFactory>());
        services.RemoveAll<IExpressionEvaluationService>();
        services.AddSingleton(Mock.Of<IExpressionEvaluationService>());
        services.RemoveAll<IPrepareTargetPayload>();
        services.AddSingleton(Mock.Of<IPrepareTargetPayload>());

        await using var provider = services.BuildServiceProvider();
        var sender = provider.GetRequiredService<MailSenderService>();
        Assert.Same(sender, provider.GetRequiredService<MailSenderService>());
        var client = Assert.IsType<IotZooMqttClient>(provider.GetRequiredService<IIoTZooMqttClient>());
        Assert.Same(client, provider.GetRequiredService<IIoTZooMqttClient>());
    }
}
