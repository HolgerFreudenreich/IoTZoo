// --------------------------------------------------------------------------------------------------------------------
//      ____    ______   _____
//     /  _/___/_  __/  /__  / ____  ____
//     / // __ \/ /       / / / __ \/ __ \
//   _/ // /_/ / /       / /_/ /_/ / /_/ /
//  /___/\____/_/       /____|____/\____/ 
// --------------------------------------------------------------------------------------------------------------------
// (c) 2025 - 2026 Holger Freudenreich under MIT license
// --------------------------------------------------------------------------------------------------------------------
// Internal MQTT Broker. It is totally independent from the MQTTClient. You can use it if you do not want to setup
// an external MQTT Broker like Mosquitto or use a public MQTT Broker.
// --------------------------------------------------------------------------------------------------------------------

namespace Domain.Services.MQTT;

using Domain.Interfaces;
using Domain.Interfaces.Crud;
using Domain.Interfaces.MQTT;
using Domain.Pocos;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Protocol;
using MQTTnet.Server;
using System.Net;
using System.Net.Sockets;


/// <summary>
/// Internal MQTT Broker. It is totally independent from the client.
/// </summary>
public class IotZooMqttBroker : IIotZooMqttBroker
{
   protected MqttServer MqttServer
   {
      get;
      set;
   } = null!;

   protected IDataTransferService DataTransferService { get; set; }

   public async Task StartServer()
   {
      if (!DataTransferService.MqttBrokerSettings.UseInternalMqttBroker)
      {
         return;
      }

      if (null == MqttServer)
      {
            var mqttServerOptions = new MqttServerOptionsBuilder().WithDefaultEndpoint().Build();
            MqttServer = new MqttServerFactory().CreateMqttServer(mqttServerOptions);
            MqttServer.ValidatingConnectionAsync += ValidateConnectionAsync;
         }

      if (!MqttServer.IsStarted)
      {
         await MqttServer.StartAsync();
      }
   }

   public async Task StopServer()
   {
      if (null != MqttServer && MqttServer.IsStarted)
      {
         await MqttServer.StopAsync();
      }
   }

   private Task ValidateConnectionAsync(ValidatingConnectionEventArgs context)
   {
      var configuredUser = DataTransferService.MqttBrokerSettings.User;
      if (string.IsNullOrEmpty(configuredUser))
      {
         return Task.CompletedTask;
      }

      var configuredPassword = DataTransferService.MqttBrokerSettings.Password ?? string.Empty;
      if (context.UserName != configuredUser || context.Password != configuredPassword)
      {
         context.ReasonCode = MqttConnectReasonCode.BadUserNameOrPassword;
      }

      return Task.CompletedTask;
   }

   public IotZooMqttBroker(IOptions<AppSettings> options,
                           IDataTransferService dataTransferService,
                           ISettingsCrudService settingsService)
   {
      DataTransferService = dataTransferService;
      // Read settings from database
      var mqttBrokerSettings = settingsService.GetObject(SettingCategory.MqttBrokerSettings,
                                                         SettingKey.MqttBrokerSettings).Result;
      if (null != mqttBrokerSettings)
      {
         dataTransferService.MqttBrokerSettings = (MqttBrokerSettings)mqttBrokerSettings;

         if (dataTransferService.MqttBrokerSettings.UseInternalMqttBroker)
         {
            IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
            IPAddress? ipAddress = host.AddressList.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork);
            if (ipAddress != null)
            {
               dataTransferService.MqttBrokerSettings.Ip = ipAddress.ToString(); // Tools.GetLocalIPAddress(); // System.Net.Dns.GetHostName(); // "localhost"; // todo: should be Dns.GetHostName()?
            }
         }
      }
   }
}