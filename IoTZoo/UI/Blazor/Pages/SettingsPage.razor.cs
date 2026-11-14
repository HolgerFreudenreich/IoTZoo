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

using DataAccess.Interfaces;
using Domain.Interfaces;
using Domain.Interfaces.Crud;
using Domain.Interfaces.MQTT;
using Domain.Pocos;
using Domain.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MQTTnet;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
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

    [Inject]
    protected IProjectCrudService ProjectService
    {
        get;
        set;
    } = null!;

    [Inject]
    protected IKnownTopicsCrudService KnownTopicsService
    {
        get;
        set;
    } = null!;

    [Inject]
    protected IAppLockService AppLock
    {
        get;
        set;
    } = null!;

    [Inject]
    protected IJSRuntime JSRuntime
    {
        get;
        set;
    } = null!;

    [Inject]
    protected IIoTZooMqttClient IotZooMqttClient
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

    protected string? UnlockCodeInput
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

    protected override async Task OnInitializedAsync()
    {
        var storedMqttBrokerSettings = await SettingsService.GetObject(SettingCategory.MqttBrokerSettings, SettingKey.MqttBrokerSettings) as MqttBrokerSettings;
        if (null != storedMqttBrokerSettings)
        {
            MqttBrokerSettings = storedMqttBrokerSettings;
        }
        HueBridgeIp = DataTransferService.PhilipsHueBridgeSettings.Ip;
        HueBridgeAppKey = DataTransferService.PhilipsHueBridgeSettings.Key;
        DataTransferService.CurrentScreen = ScreenMode.Settings;
        MqttNamespaceName = DataTransferService.NamespaceName;
        var maxReceivedTopicsSetting = await SettingsService.GetSetting(SettingCategory.UiSettings.ToString(), SettingKey.MqttExplorerMaxReceivedTopics.ToString());
        if (int.TryParse(maxReceivedTopicsSetting?.SettingValue, out var maxReceivedTopics) && maxReceivedTopics > 0)
        {
            DataTransferService.MqttExplorerMaxReceivedTopics = maxReceivedTopics;
        }
        var jsonMailSettings = await SettingsService.GetSettingString(SettingCategory.Mail, SettingKey.MailReceiverConfigs);
        if (!string.IsNullOrEmpty(jsonMailSettings))
        {
            try
            {
                var config = JsonSerializer.Deserialize<List<MailReceiverConfig>>(jsonMailSettings);
                if (null != config)
                {
                    MailReceiverConfigs = config;
                }
            }
            catch (JsonException exception)
            {
                Snackbar.Add($"Mail settings are not valid JSON: {exception.Message}", MudBlazor.Severity.Error);
            }
        }
        await base.OnInitializedAsync();
    }

    protected async Task TestMqttConnectionAsync()
    {
        using var client = new MqttClientFactory().CreateMqttClient();
        var options = new MqttClientOptionsBuilder().WithTcpServer(MqttBrokerSettings.Ip, MqttBrokerSettings.Port)
            .WithBrokerCredentials(MqttBrokerSettings).Build();
        try
        {
            await client.ConnectAsync(options);
            if (client.IsConnected)
            {
                await client.DisconnectAsync();
                Snackbar.Add("MQTT connection successful.", MudBlazor.Severity.Success);
            }
            else
            {
                Snackbar.Add("MQTT connection failed.", MudBlazor.Severity.Error);
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"MQTT connection failed: {ex.Message}", MudBlazor.Severity.Error);
        }
    }

    private async Task CreateMailReceiverKnownTopics()
    {
        List<Project> projects = await ProjectService.LoadProjects();
        foreach (MailReceiverConfig mailReceiverConfig in MailReceiverConfigs.Where(c => !string.IsNullOrWhiteSpace(c.UserName)))
        {
            foreach (Project project in projects)
            {
                await KnownTopicsService.Save(new KnownTopic
                {
                    NamespaceName = DataTransferService.NamespaceName,
                    ProjectName = project.ProjectName,
                    Topic = $"mail_receiver/{mailReceiverConfig.UserName}/mail_count",
                    Description = $"Number of mails received by {mailReceiverConfig.UserName}.",
                    MessageDirection = MessageDirection.Inbound
                }, allowUpdate: false);

                await KnownTopicsService.Save(new KnownTopic
                {
                    NamespaceName = DataTransferService.NamespaceName,
                    ProjectName = project.ProjectName,
                    Topic = $"mail_receiver/{mailReceiverConfig.UserName}/subject",
                    Description = $"Subject of the last mail received by {mailReceiverConfig.UserName}.",
                    MessageDirection = MessageDirection.Inbound
                }, allowUpdate: false);
            }
        }
    }

    protected async Task Save()
    {
        if (!Validate())
        {
            return;
        }
        //await SettingsService.Update(SettingCategory.General, SettingKey.Namespace, "iot_zoo");

        await SettingsService.Update(SettingCategory.General, SettingKey.DateAndTimeFormat, DataTransferService.DateTimeFormat);
        try
        {
            if (1 == await SettingsService.Update(SettingCategory.MqttBrokerSettings,
                                                  SettingKey.MqttBrokerSettings,
                                                  MqttBrokerSettings))
            {
                DataTransferService.MqttBrokerSettings = MqttBrokerSettings;
            }
            else
            {
                Snackbar.Add("Unable to save Mqtt Broker Settings!", MudBlazor.Severity.Error);
            }
        }
        catch (CryptographicException exception)
        {
            Snackbar.Add($"Mqtt password encryption failed: {exception.Message}", MudBlazor.Severity.Error);
            return;
        }

        int rowsUpdated = await SettingsService.Update(SettingCategory.Mail,
                                     SettingKey.MailReceiverConfigs,
                                     MailReceiverConfigs);
        await CreateMailReceiverKnownTopics();

        bool hueSettingsChanged = HueBridgeIp != DataTransferService.PhilipsHueBridgeSettings.Ip ||
                                  HueBridgeAppKey != DataTransferService.PhilipsHueBridgeSettings.Key;

        if (1 == await SettingsService.Update(SettingCategory.PhilipsHue, SettingKey.Ip, HueBridgeIp))
        {
            DataTransferService.PhilipsHueBridgeSettings.Ip = HueBridgeIp;
        }

        if (1 == await SettingsService.Update(SettingCategory.PhilipsHue, SettingKey.AppKey, HueBridgeAppKey!))
        {
            DataTransferService.PhilipsHueBridgeSettings.Key = HueBridgeAppKey!;
        }

        if (hueSettingsChanged)
        {
            HueBridgeService.ApplySettings();
        }

        await SettingsService.Update(SettingCategory.UiSettings, SettingKey.IsDarkMode, DataTransferService.IsDarkMode);
        await SettingsService.Update(SettingCategory.UiSettings, SettingKey.MqttExplorerMaxReceivedTopics, DataTransferService.MqttExplorerMaxReceivedTopics);
        if (!string.IsNullOrEmpty(UnlockCodeInput))
        {
            await AppLock.SetUnlockCodeAsync(UnlockCodeInput);
            UnlockCodeInput = null;
        }
        await InternalBroker.StopServer();
        if (MqttBrokerSettings.UseInternalMqttBroker)
        {
            await InternalBroker.StartServer();
        }
        await IotZooMqttClient.Reconnect();

        if (MqttNamespaceName != DataTransferService.NamespaceName)
        {
            DataTransferService.NamespaceName = MqttNamespaceName;
            await NamespaceService.Update(DataTransferService.NamespaceName);
        }

        AppStatus.NotifyStateChanged();
        Snackbar.Add("Settings saved.", MudBlazor.Severity.Success);
    }

    protected async Task RemoveUnlockCode()
    {
        await AppLock.RemoveUnlockCodeAsync();
        UnlockCodeInput = null;
        Snackbar.Add("Unlock code removed. The app runs without login.", MudBlazor.Severity.Success);
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

    protected async Task ExportSettingsToClipboard()
    {
        try
        {
            List<Setting> settings = await SettingsService.GetAllSettings();
            string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            await JSRuntime.InvokeVoidAsync("copyToClipboard", json);
            Snackbar.Add($"{settings.Count} settings exported to clipboard.", MudBlazor.Severity.Success);
        }
        catch (Exception exception)
        {
            Snackbar.Add($"Export failed: {exception.GetBaseException().Message}", MudBlazor.Severity.Error);
        }
    }

    protected async Task ImportSettingsFromClipboard()
    {
        try
        {
            string? json = await JSRuntime.InvokeAsync<string?>("readClipboardText");
            List<Setting>? settings = string.IsNullOrWhiteSpace(json)
                ? null
                : JsonSerializer.Deserialize<List<Setting>>(json);
            if (settings == null || settings.Count == 0)
            {
                Snackbar.Add("Clipboard does not contain valid settings.", MudBlazor.Severity.Warning);
                return;
            }

            int imported = 0;
            foreach (Setting setting in settings)
            {
                var importedSetting = new Setting
                {
                    Category = setting.Category,
                    SettingKey = setting.SettingKey,
                    SettingValue = setting.SettingValue
                };
                if (await SettingsService.Update(importedSetting) == 0)
                {
                    await SettingsService.Insert(importedSetting);
                }
                imported++;
            }

            Snackbar.Add($"{imported} settings imported. Restart the application to apply all changes.", MudBlazor.Severity.Success);
        }
        catch (Exception exception)
        {
            Snackbar.Add($"Import failed: {exception.GetBaseException().Message}", MudBlazor.Severity.Error);
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
