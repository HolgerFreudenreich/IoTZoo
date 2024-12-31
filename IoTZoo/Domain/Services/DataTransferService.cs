// --------------------------------------------------------------------------------------------------------------------
//      ____    ______   _____
//     /  _/___/_  __/  /__  / ____  ____
//     / // __ \/ /       / / / __ \/ __ \
//   _/ // /_/ / /       / /_/ /_/ / /_/ /
//  /___/\____/_/       /____|____/\____/ 
// --------------------------------------------------------------------------------------------------------------------
// (c) 2025 Holger Freudenreich under MIT license
// --------------------------------------------------------------------------------------------------------------------
// Service to transfer data from page to page.
// --------------------------------------------------------------------------------------------------------------------

namespace Domain.Services;

using Domain.Interfaces;
using Domain.Interfaces.Crud;
using Domain.Pocos;
using System.Net;

public class DataTransferService : IDataTransferService
{
   public DataTransferService(ISettingsCrudService settingsService, INamespaceCrudService namespaceCrudService)
   {
      SettingsService = settingsService;

      // Read settings from database
      var mqttBrokerSettings = settingsService.GetObject(SettingCategory.MqttBrokerSettings,
                                                         SettingKey.MqttBrokerSettings).Result;
      if (null != mqttBrokerSettings)
      {
         MqttBrokerSettings = (MqttBrokerSettings)mqttBrokerSettings;
         if (MqttBrokerSettings.UseInternalMqttBroker)
         {
            MqttBrokerSettings.Ip = Dns.GetHostName(); // "localhost" does not work!
         }
      }

      NamespaceName = namespaceCrudService.GetNamespaceName(); // SettingsService.GetSettingString(SettingCategory.MqttClientSettings, SettingKey.Namespace).Result;
      DateTimeFormat = settingsService.GetSettingString(SettingCategory.General, SettingKey.DateAndTimeFormat).Result;
      PhilipsHueBridgeSettings.Ip = settingsService.GetSettingString(SettingCategory.PhilipsHue, SettingKey.Ip).Result;
      PhilipsHueBridgeSettings.Key = settingsService.GetSettingString(SettingCategory.PhilipsHue, SettingKey.AppKey).Result;
      IsDarkMode = settingsService.GetSettingBool(SettingCategory.UiSettings, SettingKey.IsDarkMode).Result;
   }

   public ISettingsCrudService SettingsService { get; set; }

   public MqttBrokerSettings MqttBrokerSettings
   {
      get;
      set;
   } = null!;

   /// <summary>
   /// Hold the received messages in a queue to have faster read-access.
   /// </summary>
   public int MqttMessagesQueueSize { get; set; } = 200;

   public string NamespaceName { get; set; }

   public ScreenMode CurrentScreen { get; set; }

   public PhilipsHueBridgeSettings PhilipsHueBridgeSettings { get; set; } = new PhilipsHueBridgeSettings();

   public string DateTimeFormat { get; set; } = "dd.MM.yyyy HH:mm:ss";

   /// <summary>
   /// Queue of the received topics.
   /// </summary>
   public Queue<TopicEntry> ReceivedTopicsQueue
   {
      get;
      set;
   } = new();

   public long TopicsPerSecond
   {
      get;
      set;
   }

   public bool AlwaysEnqueueTopic
   {
      get;
      set;
   } = false;

   public bool Pause
   {
      get;
      set;
   }

   public bool IsDarkMode { get; set; }
   public Project? SelectedProject { get; set; } = null!;

}