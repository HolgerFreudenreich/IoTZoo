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

namespace Infrastructure;

using Microsoft.Extensions.Logging;
using Quartz;
using Quartz.Spi;

public class JobFactory : IJobFactory
{
  private readonly IServiceProvider serviceProvider;
  private readonly ILogger<JobFactory> logger;

  public JobFactory(IServiceProvider serviceProvider, ILogger<JobFactory> logger)
  {
    this.serviceProvider = serviceProvider;
    this.logger = logger;
  }

  public IJob NewJob(TriggerFiredBundle bundle, IScheduler scheduler)
  {
    var jobType = bundle.JobDetail.JobType;

    if (serviceProvider.GetService(jobType) is IJob job)
    {
      return job;
    }
    string msg = $"No job found for jobType {jobType}! Do not forget to AddSingleton!";
    logger.LogCritical(msg);
    throw new Exception(msg);
  }

  public void ReturnJob(IJob job)
  {
    var disposable = job as IDisposable;
    disposable?.Dispose();
  }
}