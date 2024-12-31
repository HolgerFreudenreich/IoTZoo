// --------------------------------------------------------------------------------------------------------------------
//      ____    ______   _____
//     /  _/___/_  __/  /__  / ____  ____
//     / // __ \/ /       / / / __ \/ __ \
//   _/ // /_/ / /       / /_/ /_/ / /_/ /
//  /___/\____/_/       /____|____/\____/ 
// --------------------------------------------------------------------------------------------------------------------
// (c) 2025 Holger Freudenreich under MIT license
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
      if (null != MqttServer)
      {
         if (MqttServer.IsStarted)
         {
            return;
         }
      }

      if (null == MqttServer)
      {
         var mqttFactory = new MqttFactory();

         //IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
         //IPAddress? ipAddress = host.AddressList.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork);

         // var mqttServerOptions = new MqttServerOptionsBuilder().WithDefaultEndpointBoundIPAddress(ipAddress).Build();

         var mqttServerOptions = new MqttServerOptionsBuilder().WithDefaultEndpoint().Build();

         MqttServer = mqttFactory.CreateMqttServer(mqttServerOptions);

      }
      if (null != MqttServer)
      {
         if (!MqttServer.IsStarted)
         {
            await MqttServer.StartAsync();
         }
      }
   }

   public async Task StopServer()
   {
      if (null != MqttServer)
      {
         if (MqttServer.IsStarted)
         {
            await MqttServer.StopAsync();
         }
      }
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