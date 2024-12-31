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

using Dapper;
using Domain.Interfaces.Timer;
using Domain.Pocos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Reflection;

namespace Domain.Services;

public class CronDatabaseService : DataServiceBase, ICronCrudService
{
   public CronDatabaseService(IOptions<AppSettings> options, ILogger<DataServiceBase> logger) : base(options, logger)
   {
      Initialize(typeof(CronJob), "cfg", "cron");
   }

   public async Task Delete(CronJob cronJob)
   {
      try
      {
         int rowsProcessed = await Db.ExecuteAsync(DeleteSql,
                                                   cronJob);
      }
      catch (Exception ex)
      {
         Logger.LogError(ex, $"{MethodBase.GetCurrentMethod()} failed!");
      }
   }

   public async Task Insert(CronJob cronJob)
   {
      try
      {
         int createdCronId = await Db.QuerySingleAsync<int>(InsertSql,
                                                            cronJob);
         cronJob.CronId = createdCronId;
      }
      catch (Exception ex)
      {
         Logger.LogError(ex, $"{MethodBase.GetCurrentMethod()} failed!");
      }
   }

   public async Task<List<CronJob>> Load(bool onlyEnabledJobs = true)
   {
      IEnumerable<CronJob> cronJobs = null!;
      try
      {
         string sql = $"select {FieldListSelect} from {FullQualifiedTableName}";
         if (onlyEnabledJobs)
         {
            sql += " where enabled = true;";
         }
         cronJobs = await Db.QueryAsync<CronJob>(sql);
      }
      catch (Exception ex)
      {
         Logger.LogError(ex, $"{MethodBase.GetCurrentMethod()} failed!");
      }
      return cronJobs.AsList();
   }

   public async Task<List<CronJob>> LoadByProject(Project project, bool onlyEnabledJobs = true)
   {
      IEnumerable<CronJob> cronJobs = null!;
      try
      {
         string sql = $"select {FieldListSelect} from {FullQualifiedTableName} where project_name = @ProjectName ";
         if (onlyEnabledJobs)
         {
            sql += " and enabled = true;";
         }
         cronJobs = await Db.QueryAsync<CronJob>(sql, new { ProjectName = project.ProjectName });
      }
      catch (Exception ex)
      {
         Logger.LogError(ex, $"{MethodBase.GetCurrentMethod()} failed!");
      }
      return cronJobs.AsList();
   }

   public async Task Save(CronJob cronJob)
   {
      if (cronJob.CronId > 0)
      {
         await Update(cronJob);
      }
      else
      {
         await Insert(cronJob);
      }
   }

   public async Task Update(CronJob cronJob)
   {
      try
      {
         int rowsProcessed = await Db.ExecuteAsync(UpdateSql,
                                                   cronJob);
      }
      catch (Exception ex)
      {
         Logger.LogError(ex, $"{MethodBase.GetCurrentMethod()} failed!");
      }
   }
}
