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

using DataAccess.Interfaces;
using Domain.Interfaces.Crud;
using Domain.Interfaces.MQTT;
using Domain.Pocos;
using Microsoft.AspNetCore.Components;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;

namespace IotZoo.Pages;

public class SettingsPageBase : PageBase
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

    protected List<MailReceiverConfig> MailReceiverConfigs
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

    // Helper method to compute floating DST transition dates
    static DateTime TransitionDate(int year, TimeZoneInfo.TransitionTime transition)
    {
        DateTime firstDayOfMonth = new(year, transition.Month, 1);
        DayOfWeek firstDayOfWeek = firstDayOfMonth.DayOfWeek;

        int daysOffset = (transition.Week - 1) * 7 + (7 + transition.DayOfWeek - firstDayOfWeek) % 7;

        if (daysOffset >= DateTime.DaysInMonth(year, transition.Month)) // Handle cases where the transition is the "last" occurrence
        {
            daysOffset -= 7;
        }

        return firstDayOfMonth.AddDays(daysOffset).Add(transition.TimeOfDay.TimeOfDay);
    }

    protected DateTime? SummertimeStart
    {
        get
        {
            TimeZoneInfo.AdjustmentRule[] rules = TimeZoneInfo.Local.GetAdjustmentRules();
            DateTime? summertimeStart = null;

            foreach (var rule in rules)
            {
                if (DateTime.Today.Year >= rule.DateStart.Year && DateTime.Today.Year <= rule.DateEnd.Year)
                {
                    summertimeStart = rule.DaylightTransitionStart.IsFixedDateRule
                        ? new DateTime(DateTime.Today.Year, rule.DaylightTransitionStart.Month, rule.DaylightTransitionStart.Day)
                        : TransitionDate(DateTime.Today.Year, rule.DaylightTransitionStart);

                    break;
                }
            }

            if (summertimeStart == null)
            {
                return null;
            }
            return summertimeStart.Value;
        }
    }

    protected DateTime? WintertimeStart
    {
        get
        {
            TimeZoneInfo.AdjustmentRule[] rules = TimeZoneInfo.Local.GetAdjustmentRules();
            DateTime? wintertimeStart = null;

            foreach (var rule in rules)
            {
                if (DateTime.Today.Year >= rule.DateStart.Year && DateTime.Today.Year <= rule.DateEnd.Year)
                {
                    wintertimeStart = rule.DaylightTransitionEnd.IsFixedDateRule
                        ? new DateTime(DateTime.Today.Year, rule.DaylightTransitionEnd.Month, rule.DaylightTransitionEnd.Day)
                        : TransitionDate(DateTime.Today.Year, rule.DaylightTransitionEnd);

                    break;
                }
            }

            if (wintertimeStart == null)
            {
                return null;
            }
            return wintertimeStart.Value;
        }
    }

    protected string MqttNamespaceName { get; set; } = string.Empty;

    protected override async void OnInitialized()
    {
        MqttBrokerSettings.UseInternalMqttBroker = DataTransferService.MqttBrokerSettings.UseInternalMqttBroker;
        MqttBrokerSettings.Ip = DataTransferService.MqttBrokerSettings.Ip;
        HueBridgeIp = DataTransferService.PhilipsHueBridgeSettings.Ip;
        HueBridgeAppKey = DataTransferService.PhilipsHueBridgeSettings.Key;
        DataTransferService.CurrentScreen = ScreenMode.Settings;
        MqttNamespaceName = DataTransferService.NamespaceName;
        var jsonMailSettings = await SettingsService.GetSettingString(SettingCategory.Mail, SettingKey.MailReceiverConfigs);
        if (!string.IsNullOrEmpty(jsonMailSettings))
        {
            var config = JsonSerializer.Deserialize<List<MailReceiverConfig>>(jsonMailSettings);
            if (null != config)
            {
                MailReceiverConfigs = config;
            }
        }
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

        int rowsUpdated = await SettingsService.Update(SettingCategory.Mail,
                                     SettingKey.MailReceiverConfigs,
                                     MailReceiverConfigs);

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
            await HueBridgeService.ApplySettingsAsync();
        }
        catch (Exception exception)
        {
            // Make sure the user has pressed the button on the bridge before calling RegisterAsync
            // It will throw an LinkButtonNotPressedException if the user did not press the button
            Snackbar.Add(exception.GetBaseException().Message, MudBlazor.Severity.Error);
        }
    }

    protected async Task AddMailReceiver()
    {
        MailReceiverConfigs.Add(new());
        await InvokeAsync(StateHasChanged);
    }

    protected async Task RemoveEmailConfig(MailReceiverConfig config)
    {
        MailReceiverConfigs.Remove(config);
        await InvokeAsync(StateHasChanged);
    }

    private bool Validate()
    {
        // ToDo: Validate fields.
        // https://developers.meethue.com/develop/application-design-guidance/hue-bridge-discovery/
        // http://192.168.178.34/api/0/config
        return true;
    }
}
