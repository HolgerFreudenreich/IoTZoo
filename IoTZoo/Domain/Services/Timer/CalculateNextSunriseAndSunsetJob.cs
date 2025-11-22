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
using Domain.Pocos;
using Domain.Services.MQTT;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Protocol;
using Quartz;
using SunriseAndSunset;
using System.Reflection;

namespace Domain.Services.Timer;

public class CalculateNextSunriseAndSunsetJob : MqttPublisher, IJob
{
    protected ISettingsCrudService SettingsCrudService { get; set; } = null!;

    protected IProjectCrudService ProjectCrudService { get; set; } = null!;

    public CalculateNextSunriseAndSunsetJob(ILogger<MqttPublisher> logger, IDataTransferService dataTransferService,
        ISettingsCrudService settingsCrudService,
        IProjectCrudService projectsCrudService) : base(logger, dataTransferService)
    {
        SettingsCrudService = settingsCrudService;
        ProjectCrudService = projectsCrudService;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            if (null == MqttClient)
            {
                return;
            }
            if (!MqttClient.IsConnected)
            {
                return;
            }

            double latitude = await SettingsCrudService.GetSettingDouble(SettingCategory.Location, SettingKey.Latitude);
            double longitude = await SettingsCrudService.GetSettingDouble(SettingCategory.Location, SettingKey.Longitude);

            var projects = await ProjectCrudService.LoadProjects();

            SunriseCalc homeLocationSun = new SunriseCalc(latitude,
                                                          longitude)
            {
                Day = DateTime.Today // Midnight
            };
            // Get today's sunrise time.
            homeLocationSun.GetSunrise(out DateTime todaysSunriseUtc);

            double minutesUntilSunrise = (todaysSunriseUtc - DateTime.UtcNow).TotalMinutes;
            if (minutesUntilSunrise < 0)
            {
                double minutsAfterSunrise = Math.Abs(minutesUntilSunrise);

                await MqttClient.PublishAsync($"{DataTransferService.NamespaceName}/{TopicConstants.MINUTES_AFTER_SUNRISE}",
                                            minutsAfterSunrise.ToString("F0"));

                if (null != projects)
                {
                    foreach (var project in projects)
                    {
                        await MqttClient.PublishAsync($"{DataTransferService.NamespaceName}/{project.ProjectName}/{TopicConstants.MINUTES_AFTER_SUNRISE}",
                                                             minutsAfterSunrise.ToString("F0"));
                    }
                }

                // The sun has already risen. Take the next sunrise!
                homeLocationSun.Day = DateTime.Today.AddDays(1);
                homeLocationSun.GetSunrise(out DateTime tomorrowsSunriseUtc);
                minutesUntilSunrise = (tomorrowsSunriseUtc - DateTime.UtcNow).TotalMinutes;
            }

            var applicationMessage = new MqttApplicationMessageBuilder()
                                           .WithTopic($"{DataTransferService.NamespaceName}/{TopicConstants.MINUTES_NEXT_SUNRISE}")
                                           .WithPayload(minutesUntilSunrise.ToString("F0"))
                                           .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtMostOnce)
                                           .Build();
            MqttClientPublishResult result = await MqttClient.PublishAsync(applicationMessage);

            if (null != projects)
            {
                foreach (var project in projects)
                {
                    applicationMessage = new MqttApplicationMessageBuilder()
                                  .WithTopic($"{DataTransferService.NamespaceName}/{project.ProjectName}/{TopicConstants.MINUTES_NEXT_SUNRISE}")
                                  .WithPayload(minutesUntilSunrise.ToString("F0"))
                                  .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtMostOnce)
                                  .Build();
                    result = await MqttClient.PublishAsync(applicationMessage);
                }


                if (minutesUntilSunrise <= 1 && minutesUntilSunrise > 0)
                {
                    // The sun rises now.
                    foreach (var project in projects)
                    {
                        result = await MqttClient.PublishAsync($"{DataTransferService.NamespaceName}/{project.ProjectName}/{TopicConstants.SUNRISE_NOW}", "When the sun rises...)");
                    }
                }
            }

            // ---------------------
            // sunset

            // Get today's sunset time.
            homeLocationSun.Day = DateTime.Today;

            homeLocationSun.GetSunset(out DateTime todaysSunsetUtc);

            double minutesUntilSunset = (todaysSunsetUtc - DateTime.UtcNow).TotalMinutes;

            if (minutesUntilSunset < 0)
            {
                double minutsAfterSunset = Math.Abs(minutesUntilSunset);

                result = await MqttClient.PublishAsync($"{DataTransferService.NamespaceName}/{TopicConstants.MINUTES_AFTER_SUNSET}",
                                              minutsAfterSunset.ToString("F0"));

                if (null != projects)
                {
                    foreach (var project in projects)
                    {
                        result = await MqttClient.PublishAsync($"{DataTransferService.NamespaceName}/{project.ProjectName}/{TopicConstants.MINUTES_AFTER_SUNSET}",
                                                               minutsAfterSunset.ToString("F0"));
                    }
                }

                // The sun has already set. Take the next sunset!
                homeLocationSun.Day = DateTime.Today.AddDays(1);
                homeLocationSun.GetSunset(out DateTime tomorrowsSunsetUtc);
                minutesUntilSunset = (tomorrowsSunsetUtc - DateTime.UtcNow).TotalMinutes;
            }

            result = await MqttClient.PublishAsync($"{DataTransferService.NamespaceName}/{TopicConstants.MINUTES_NEXT_SUNSET}",
                                                   minutesUntilSunset.ToString("F0"));
            if (null != projects)
            {
                foreach (var project in projects)
                {
                    result = await MqttClient.PublishAsync($"{DataTransferService.NamespaceName}/{project.ProjectName}/{TopicConstants.MINUTES_NEXT_SUNSET}",
                                                           minutesUntilSunset.ToString("F0"));

                    result = await MqttClient.PublishAsync($"{DataTransferService.NamespaceName}/{project.ProjectName}/{TopicConstants.IS_DAY_MODE}", (minutesUntilSunset < minutesUntilSunrise).ToString());
                }

                if (minutesUntilSunset <= 1 && minutesUntilSunset > 0)
                {
                    // The sun sets now.
                    foreach (var project in projects)
                    {
                        result = await MqttClient.PublishAsync($"{DataTransferService.NamespaceName}/{project.ProjectName}/{TopicConstants.SUNSET_NOW}",
                                                               "When the sun goes down...");
                   }
                }
            }





        }
        catch (Exception ex)
        {
            Logger.LogError(ex, $"{MethodBase.GetCurrentMethod()} failed!");
        }
    }
}
