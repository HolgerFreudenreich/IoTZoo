// --------------------------------------------------------------------------------------------------------------------
//      ____    ______   _____
//     /  _/___/_  __/  /__  / ____  ____
//     / // __ \/ /       / / / __ \/ __ \
//   _/ // /_/ / /       / /_/ /_/ / /_/ /
//  /___/\____/_/       /____|____/\____/ 
// --------------------------------------------------------------------------------------------------------------------
// Connect «Things» with microcontrollers without programming knowledge.
// --------------------------------------------------------------------------------------------------------------------
// (c) 2025 Holger Freudenreich under MIT license
// --------------------------------------------------------------------------------------------------------------------

using DataAccess.Interfaces;
using Domain.Interfaces.Crud;
using Domain.Interfaces.MQTT;
using Domain.Pocos;
using Microsoft.AspNetCore.Components;
using System.Net;
using System.Net.Sockets;

namespace IotZoo.Pages;

public class SettingsPageBase : MqttPageBase
{
   [Inject]
   protected ISettingsCrudService SettingsService
   {
      get;
      set;
   } = null!;

   [Inject]
   protected IHueBridgeService HueBridgeService
   {
      get;
      set;
   } = null!;

   [Inject]
   protected IIotZooMqttBroker InternalBroker
   {
      get;
      set;
   } = null!;

   [Inject]
   protected INamespaceCrudService NamespaceService
   {
      get;
      set;
   } = null!;

   protected MqttBrokerSettings MqttBrokerSettings
   {
      get;
      set;
   } = new();

   public string HueBridgeIp
   {
      get;
      set;
   } = null!;

   public string? HueBridgeAppKey
   {
      get;
      set;
   }

   protected string MqttNamespaceName { get; set; } = string.Empty;

   protected override void OnInitialized()
   {
      MqttBrokerSettings.UseInternalMqttBroker = DataTransferService.MqttBrokerSettings.UseInternalMqttBroker;
      MqttBrokerSettings.Ip = DataTransferService.MqttBrokerSettings.Ip;
      HueBridgeIp = DataTransferService.PhilipsHueBridgeSettings.Ip;
      HueBridgeAppKey = DataTransferService.PhilipsHueBridgeSettings.Key;
      DataTransferService.CurrentScreen = ScreenMode.Settings;
      MqttNamespaceName = DataTransferService.NamespaceName;
      base.OnInitialized();
   }

   protected async Task Save()
   {
      if (!Validate())
      {
         return;
      }
      //await SettingsService.Update(SettingCategory.General, SettingKey.Namespace, "iot_zoo");

      await SettingsService.Update(SettingCategory.General, SettingKey.DateAndTimeFormat, DataTransferService.DateTimeFormat);
      if (1 == await SettingsService.Update(SettingCategory.MqttBrokerSettings,
                                            SettingKey.MqttBrokerSettings,
                                            MqttBrokerSettings))
      {
         DataTransferService.MqttBrokerSettings = MqttBrokerSettings;
      }
      else
      {
         Snackbar.Add("Unable to save Mqtt Broker Settings!");
      }

      if (1 == await SettingsService.Update(SettingCategory.PhilipsHue, SettingKey.Ip, HueBridgeIp))
      {
         DataTransferService.PhilipsHueBridgeSettings.Ip = HueBridgeIp;
      }

      if (1 == await SettingsService.Update(SettingCategory.PhilipsHue, SettingKey.AppKey, HueBridgeAppKey!))
      {
         DataTransferService.PhilipsHueBridgeSettings.Key = HueBridgeAppKey!;
      }

      await SettingsService.Update(SettingCategory.UiSettings, SettingKey.IsDarkMode, DataTransferService.IsDarkMode);
      if (MqttBrokerSettings.UseInternalMqttBroker)
      {
         await InternalBroker.StartServer();
      }
      else
      {
         await InternalBroker.StopServer();
      }

      if (MqttNamespaceName != DataTransferService.NamespaceName)
      {
         DataTransferService.NamespaceName = MqttNamespaceName;
         await NamespaceService.Update(DataTransferService.NamespaceName);
      }

      AppStatus.NotifyStateChanged();
      Snackbar.Add("Settings saved.", MudBlazor.Severity.Success);
   }

   public void DarkModeChanged(bool isDarkMode)
   {
      DataTransferService.IsDarkMode = isDarkMode;
      AppStatus.NotifyStateChanged();
   }

   public void UseInternalMqttBrokerChanged(bool useInternalMqttBroker)
   {
      MqttBrokerSettings.UseInternalMqttBroker = useInternalMqttBroker;

      if (useInternalMqttBroker)
      {
         IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
         IPAddress? ipAddress = host.AddressList.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork);
         if (ipAddress != null)
         {
            MqttBrokerSettings.Ip = ipAddress.ToString(); // Tools.GetLocalIPAddress();//Dns.GetHostName();
         }
         MqttBrokerSettings.Ip = Dns.GetHostName();
         MqttBrokerSettings.Port = 1883;
         //Snackbar.Add("Starting internal MQTT Broker...");
         //await InternalBroker.StartServer();
         //Snackbar.Add("Internal MQTT Broker is running.");
      }
      else
      {
         //Snackbar.Add("Stopping internal MQTT Broker...");
         //await InternalBroker.StopServer();
      }
      AppStatus.NotifyStateChanged();
   }

   protected async Task RegisterAtPhilipsHueBridgeAsync()
   {
      try
      {
         HueBridgeAppKey = await HueBridgeService.RegisterAppAtHueBridgeAsync(HueBridgeIp,
                                                                              "IotZoo",
                                                                              "IotZoo");
         if (!string.IsNullOrEmpty(HueBridgeAppKey))
         {
            await Save();
         }
         HueBridgeService.ApplySettings();
      }
      catch (Exception exception)
      {
         // Make sure the user has pressed the button on the bridge before calling RegisterAsync
         // It will throw an LinkButtonNotPressedException if the user did not press the button
         Snackbar.Add(exception.GetBaseException().Message, MudBlazor.Severity.Error);
      }
   }

   private bool Validate()
   {
      // ToDo: Validate fields.
      // https://developers.meethue.com/develop/application-design-guidance/hue-bridge-discovery/
      // http://192.168.178.34/api/0/config
      return true;
   }
}
