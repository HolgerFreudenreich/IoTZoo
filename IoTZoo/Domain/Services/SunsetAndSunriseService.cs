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

using DataAccess.Services;
using Domain.Interfaces;
using Domain.Interfaces.Crud;
using Domain.Interfaces.MQTT;
using Domain.Pocos;
using Domain.Services.Timer;
using MQTTnet.Protocol;
using SunriseAndSunset;

namespace Domain.Services;

public interface ISunsetAndSunriseService
{
    public Task PublishIsDayMode();
}

public class SunsetAndSunriseService : ISunsetAndSunriseService
{
    public ISettingsCrudService SettingsService { get; }
    public IProjectCrudService ProjectService { get; }
    public IIoTZooMqttClient IotZooMqttClient { get; }
    public IDataTransferService DataTransferService { get; }

    public SunsetAndSunriseService(ISettingsCrudService settingsService,
                                   IProjectCrudService projectService,
                                   IIoTZooMqttClient iotZooMqttClient,
                                   IDataTransferService dataTransferService)
    {
        this.SettingsService = settingsService;
        this.ProjectService = projectService;
        this.IotZooMqttClient = iotZooMqttClient;
        this.DataTransferService = dataTransferService;
        StartSunriseTimer();
        StartSunsetTimer();
    }

    protected async void StartSunriseTimer()
    {
        double latitude = await SettingsService.GetSettingDouble(SettingCategory.Location, SettingKey.Latitude);
        double longitude = await SettingsService.GetSettingDouble(SettingCategory.Location, SettingKey.Longitude);
        SunriseCalc homeLocationSun = new SunriseCalc(latitude,
                                                      longitude)
        {
            Day = DateTime.Today // Midnight
        };
        // Get today's sunrise time.
        homeLocationSun.GetSunrise(out DateTime todaysSunriseUtc);

        double millisecondsUntilSunrise = (todaysSunriseUtc - DateTime.UtcNow).TotalMilliseconds;
        if (millisecondsUntilSunrise < 0)
        {
            // The sun has already risen. Take the next sunrise!
            homeLocationSun.Day = DateTime.Today.AddDays(1);
            homeLocationSun.GetSunrise(out DateTime tomorrowsSunriseUtc);
            millisecondsUntilSunrise = (tomorrowsSunriseUtc - DateTime.UtcNow).TotalMilliseconds;
        }

        // millisecondsUntilSunrise = 10000;
        TimerService timerServiceToSunrise = new TimerService(millisecondsUntilSunrise, start: true);
        timerServiceToSunrise.OnElapsed -= TimerServiceToSunrise_OnElapsed;
        timerServiceToSunrise.OnElapsed += TimerServiceToSunrise_OnElapsed;
    }

    protected async void StartSunsetTimer()
    {
        double latitude = await SettingsService.GetSettingDouble(SettingCategory.Location, SettingKey.Latitude);
        double longitude = await SettingsService.GetSettingDouble(SettingCategory.Location, SettingKey.Longitude);
        SunriseCalc homeLocationSun = new SunriseCalc(latitude,
            longitude)
        {
            Day = DateTime.Today // Midnight
        };

        // Get today's sunset time.
        homeLocationSun.GetSunset(out DateTime todaysSunsetUtc);

        double millisecondsUntilSunset = (todaysSunsetUtc.ToLocalTime() - DateTime.Now).TotalMilliseconds;
        if (millisecondsUntilSunset < 0)
        {
            // The sun has already set. Take the next sunset!
            homeLocationSun.Day = DateTime.Today.AddDays(1);
            homeLocationSun.GetSunset(out DateTime tomorrowsSunsetUtc);
            millisecondsUntilSunset = (tomorrowsSunsetUtc.ToLocalTime() - DateTime.Now).TotalMilliseconds;
        }

        // millisecondsUntilSunset = 10000;
        TimerService timerServiceToSunset = new TimerService(millisecondsUntilSunset, start: true);
        timerServiceToSunset.OnElapsed -= TimerServiceToSunset_OnElapsed;
        timerServiceToSunset.OnElapsed += TimerServiceToSunset_OnElapsed;
    }

    private async void TimerServiceToSunrise_OnElapsed(System.Timers.Timer timer, TimerServiceEventArgs elapsedEventArgs)
    {
        var projects = await ProjectService.LoadProjects();
        foreach (var project in projects)
        {
            await IotZooMqttClient.PublishTopic($"{project.ProjectName}/{TopicConstants.SUNRISE_NOW}", "When the sun rises...");
            await IotZooMqttClient.PublishTopic($"{project.ProjectName}/{TopicConstants.IS_DAY_MODE}", 0.ToString());
        }
        timer.Dispose();
        StartSunriseTimer();
    }

    private async void TimerServiceToSunset_OnElapsed(System.Timers.Timer timer, TimerServiceEventArgs elapsedEventArgs)
    {
        var projects = await ProjectService.LoadProjects();
        foreach (var project in projects)
        {
            await IotZooMqttClient.PublishTopic($"{project.ProjectName}/{TopicConstants.SUNSET_NOW}", "When the sun goes down...");
            await IotZooMqttClient.PublishTopic($"{project.ProjectName}/{TopicConstants.IS_DAY_MODE}", 1.ToString());
        }
        timer.Dispose();
        StartSunsetTimer();
    }

    public async Task PublishIsDayMode()
    {
        double latitude = await SettingsService.GetSettingDouble(SettingCategory.Location, SettingKey.Latitude);
        double longitude = await SettingsService.GetSettingDouble(SettingCategory.Location, SettingKey.Longitude);
        SunriseCalc homeLocationSun = new SunriseCalc(latitude, longitude)
        {
            Day = DateTime.Today // Midnight
        };
        homeLocationSun.GetSunrise(out DateTime todaysSunriseUtc);
        homeLocationSun.GetSunset(out DateTime todaysSununsetUtc);
        bool isDay = DateTime.UtcNow >= todaysSunriseUtc && DateTime.UtcNow <= todaysSununsetUtc;
        var projects = await ProjectService.LoadProjects();
        foreach (var project in projects)
        {
            await IotZooMqttClient.PublishTopic($"{DataTransferService.NamespaceName}/{project.ProjectName}/{TopicConstants.IS_DAY_MODE}", isDay ? "1" : "0", MqttQualityOfServiceLevel.ExactlyOnce);
        }
    }
}