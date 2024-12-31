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
using MQTTnet.Client;
using MQTTnet.Protocol;
using Quartz;
using System.Reflection;
using System.Text.Json;

namespace Domain.Services.Timer;

public class PublishTimeJob : IJob
{
   protected IMqttClient MqttClient
   {
      get;
      set;
   } = null!;

   protected IDataTransferService DataTransferService { get; set; }
   private ILogger Logger { get; }

   public PublishTimeJob(IDataTransferService dataTransferService, ILogger<PublishTimeJob> logger)
   {
      Logger = logger;
      DataTransferService = dataTransferService;
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
      var factory = new MqttFactory();
      MqttClient = factory.CreateMqttClient();

      var mqttClientOptions = new MqttClientOptionsBuilder().WithTcpServer(DataTransferService.MqttBrokerSettings.Ip,
                                                                           DataTransferService.MqttBrokerSettings.Port).Build();
      MqttClient.DisconnectedAsync -= MqttClient_DisconnectedAsync;
      MqttClient.DisconnectedAsync += MqttClient_DisconnectedAsync;
      MqttClientConnectResult connectionResult = MqttClient.ConnectAsync(mqttClientOptions).Result;
      if (connectionResult.ResultCode == MqttClientConnectResultCode.Success)
      {
         Logger.LogInformation("MQTT connected!");
      }
   }

   public async Task Execute(IJobExecutionContext context)
   {
      try
      {
         if (null == MqttClient)
         {
            return;
         }

         object topicData = context.MergedJobDataMap.Get("Topic");

         if (null != topicData)
         {
            string? strTopic = topicData.ToString();
            if (null != strTopic)
            {
               //string dateTimeString = DateTime.Now.ToString(DataTransferService.DateTimeFormat);
               DateTime dateTime = DateTime.Now;

               //if (DateTime.TryParse(dateTimeString, out dateTime))

               string jsonPayload = string.Empty;
               try
               {
                  jsonPayload = JsonSerializer.Serialize(new
                  {
                     DateTime = dateTime.ToString($"{DataTransferService.DateTimeFormat}"),
                     Date = dateTime.Date.ToShortDateString(),
                     Time = dateTime.ToString("HH:mm:ss"),
                     TimeShort = dateTime.ToString("HH:mm")
                  });
               }
               catch (Exception ex)
               {
                  jsonPayload = ex.GetBaseException().Message;
               }

               var applicationMessage = new MqttApplicationMessageBuilder()
                                       .WithTopic(strTopic)
                                       .WithPayload(jsonPayload)
                                       .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtMostOnce)
                                       .Build();

               await MqttClient.PublishAsync(applicationMessage);
            }
         }
      }
      catch (Exception ex)
      {
         Logger.LogError(ex, $"{MethodBase.GetCurrentMethod()} failed!");
      }
   }
}
