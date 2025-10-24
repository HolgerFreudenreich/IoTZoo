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
using Domain.Interfaces.Timer;
using Domain.Pocos;
using Microsoft.Extensions.Logging;
using Quartz;
using Quartz.Impl;
using Quartz.Spi;
using System.Reflection;

namespace Domain.Services.Timer;

public class CronService : ICronService
{
    private IScheduler scheduler = null!;
    private readonly IJobFactory jobFactory = null!;

    public ICronCrudService CronCrudService { get; }

    protected IDataTransferService DataTransferService { get; }
    public ILogger<CronService> Logger { get; }

    public CronService(ILogger<CronService> logger, IJobFactory jobFactory,
                       ICronCrudService cronCrudService,
                       IDataTransferService dataTransferService)
    {
        CronCrudService = cronCrudService;
        Logger = logger;
        this.jobFactory = jobFactory;
        this.DataTransferService = dataTransferService;
        _ = InitCronScheduler();
    }

    protected async Task InitCronJob(CronJob cronJob)
    {
        try
        {
            IJobDetail jobDetail = JobBuilder.Create<PublishTimeJob>()
                                        .WithIdentity(nameof(PublishTimeJob), cronJob.CronId.ToString())
                                        .Build();

            jobDetail.JobDataMap.Add("Topic", value: $"{DataTransferService.NamespaceName}/{cronJob.ProjectName}/{cronJob.Topic}");

            var trigger = TriggerBuilder.Create()
                               .WithIdentity("trigger-" + nameof(PublishTimeJob), cronJob.CronId.ToString())
                               .StartAt(DateTime.Now.AddSeconds(5)) // Give mqtt client time to connect to the broker before firing.
                               .WithSchedule(CronScheduleBuilder.CronSchedule(cronJob.ToString())
                               .WithMisfireHandlingInstructionIgnoreMisfires())
                               .Build();

            await scheduler.ScheduleJob(jobDetail, trigger);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, $"{MethodBase.GetCurrentMethod()} failed!");
        }
    }

    protected async Task InitCronScheduler()
    {
        scheduler = await StdSchedulerFactory.GetDefaultScheduler();
        scheduler.JobFactory = jobFactory;

        var cronJobs = await CronCrudService.Load(onlyEnabledJobs: true);
        foreach (CronJob cronJob in cronJobs)
        {
            await InitCronJob(cronJob);
        }
        // Job to calculate the time until the next sunrise and sunset.

        await InitCalcSunriseAndSunsetJob();

        await scheduler.Start();
    }

    private async Task InitCalcSunriseAndSunsetJob()
    {
        try
        {
            IJobDetail jobDetail = JobBuilder.Create<CalculateNextSunriseAndSunsetJob>()
                                        .WithIdentity(nameof(CalculateNextSunriseAndSunsetJob))
                                        .Build();


            var trigger = TriggerBuilder.Create()
                               .WithIdentity("trigger-" + nameof(CalculateNextSunriseAndSunsetJob))
                               .StartAt(DateTime.Now.AddSeconds(5)) // Give mqtt client time to connect to the broker before firing.
                               .WithSchedule(CronScheduleBuilder.CronSchedule("0 */1 * ? * *")
                               .WithMisfireHandlingInstructionIgnoreMisfires())
                               .Build();

            await scheduler.ScheduleJob(jobDetail, trigger);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, $"{MethodBase.GetCurrentMethod()} failed!");
        }
    }

    public async Task StartProjectCronJobs(Project project)
    {
        var cronJobs = await CronCrudService.LoadByProject(project, onlyEnabledJobs: true);
        foreach (CronJob cronJob in cronJobs)
        {
            try
            {
                await InitCronJob(cronJob);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"{MethodBase.GetCurrentMethod()} failed!");
                continue;
            }
        }
    }

    public async Task StopProjectCronJobs(Project project)
    {
        var cronJobs = await CronCrudService.LoadByProject(project, onlyEnabledJobs: true);
        foreach (CronJob cronJob in cronJobs)
        {
            bool deleted = await scheduler.DeleteJob(new JobKey(nameof(PublishTimeJob), cronJob.CronId.ToString()));
        }
    }

}
