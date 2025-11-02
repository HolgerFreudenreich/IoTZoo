// --------------------------------------------------------------------------------------------------------------------
//      ____    ______   _____
//     /  _/___/_  __/  /__  / ____  ____
//     / // __ \/ /       / / / __ \/ __ \
//   _/ // /_/ / /       / /_/ /_/ / /_/ /
//  /___/\____/_/       /____|____/\____/ 
// --------------------------------------------------------------------------------------------------------------------
// (c) 2025 Holger Freudenreich under MIT license
// --------------------------------------------------------------------------------------------------------------------
// The MQTT Client to publish messages/topics in special cases like MailReceiver, Philips Hue-Events, Cron-Jobs,
// Sunrise and Sunset
// --------------------------------------------------------------------------------------------------------------------

using Domain.Interfaces;
using Domain.Interfaces.MQTT;
using Microsoft.Extensions.Logging;
using MQTTnet;
using System.Reflection;

namespace Domain.Services.MQTT;

public class MqttPublisher : IMqttPublisher, IDisposable
{
    public ILogger Logger { get; } = null!;

    public IMqttClient MqttClient { get; private set; } = null!;

    protected IDataTransferService DataTransferService { get; } = null!;

    public MqttPublisher(ILogger<MqttPublisher> logger,
                         IDataTransferService dataTransferService)
    {
        Logger = logger;
        DataTransferService = dataTransferService;
        _ = InitMqttClientAsync();
    }

    protected async Task InitMqttClientAsync()
    {
        try
        {
            var factory = new MqttClientFactory();
            MqttClient = factory.CreateMqttClient();

            var mqttClientOptions = new MqttClientOptionsBuilder().WithTcpServer(DataTransferService.MqttBrokerSettings.Ip,
                                                                                 DataTransferService.MqttBrokerSettings.Port).Build();
            MqttClient.DisconnectedAsync -= MqttClient_DisconnectedAsync;
            MqttClient.DisconnectedAsync += MqttClient_DisconnectedAsync;
            MqttClientConnectResult connectionResult = await MqttClient.ConnectAsync(mqttClientOptions);
            if (connectionResult.ResultCode == MqttClientConnectResultCode.Success)
            {
                Logger.LogInformation("MQTT connected!");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, $"{MethodBase.GetCurrentMethod()} failed!");
        }
    }

    private async Task MqttClient_DisconnectedAsync(MqttClientDisconnectedEventArgs args)
    {
        try
        {
            Logger.LogWarning("MQTT disconnected! Try to reconnect...");
            await Task.Delay(2000);
            await MqttClient.ReconnectAsync();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, $"{MethodBase.GetCurrentMethod()} failed!");
        }
    }

    public void Dispose()
    {
        MqttClient.DisconnectedAsync -= MqttClient_DisconnectedAsync;
    }
}
