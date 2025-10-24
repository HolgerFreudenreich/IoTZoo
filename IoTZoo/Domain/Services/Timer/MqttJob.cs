// --------------------------------------------------------------------------------------------------------------------
//      ____    ______   _____
//     /  _/___/_  __/  /__  / ____  ____
//     / // __ \/ /       / / / __ \/ __ \
//   _/ // /_/ / /       / /_/ /_/ / /_/ /
//  /___/\____/_/       /____|____/\____/   P L A Y G R O U N D
// --------------------------------------------------------------------------------------------------------------------
// Connect «Things» with microcontrollers in a simple way.
// --------------------------------------------------------------------------------------------------------------------
// (c) 2025 Holger Freudenreich under the MIT license
// --------------------------------------------------------------------------------------------------------------------

using Domain.Interfaces;
using Microsoft.Extensions.Logging;
using MQTTnet;
using Quartz;

namespace Domain.Services.Timer;

public class MqttJob : IJob
{
    protected IDataTransferService DataTransferService { get; set; } = null!;

    protected ILogger Logger { get; }

    protected IMqttClient MqttClient
    {
        get;
        set;
    } = null!;

    public virtual async Task Execute(IJobExecutionContext context)
    {
        await Task.CompletedTask;
    }

    public MqttJob(IDataTransferService dataTransferService, ILogger<MqttJob> logger)
    {
        DataTransferService = dataTransferService;
        Logger = logger;
        InitMqttClient();
    }

    private async Task MqttClient_DisconnectedAsync(MqttClientDisconnectedEventArgs arg)
    {
        Logger.LogWarning("MQTT disconnected! Try to reconnect...");
        await Task.Delay(3000);
        await MqttClient.ReconnectAsync();
    }

    private void InitMqttClient()
    {
        var factory = new MqttClientFactory();
        MqttClient = factory.CreateMqttClient();

        var mqttClientOptions = new MqttClientOptionsBuilder().WithTcpServer(DataTransferService.MqttBrokerSettings.Ip,
                                                                             DataTransferService.MqttBrokerSettings.Port).Build();
        MqttClient.DisconnectedAsync -= MqttClient_DisconnectedAsync;
        MqttClient.DisconnectedAsync += MqttClient_DisconnectedAsync;
        MqttClientConnectResult connectionResult = MqttClient.ConnectAsync(mqttClientOptions).Result;
        if (connectionResult.ResultCode != MqttClientConnectResultCode.Success)
        {
            Logger.LogWarning("MQTT not connected!");
        }
    }

}
