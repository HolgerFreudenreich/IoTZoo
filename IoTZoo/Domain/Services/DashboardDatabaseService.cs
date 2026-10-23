// --------------------------------------------------------------------------------------------------------------------
//      ____    ______   _____
//     /  _/___/_  __/  /__  / ____  ____
//     / // __ \\/ /       / / / __ \\/ __ \\
//   _/ // /_/ / /       / /_/ /_/ / /_/ /
//  /___/\\____/_/       /____|____/\\____/   P L A Y G R O U N D
// --------------------------------------------------------------------------------------------------------------------
// Connect «Things» with microcontrollers in a simple way.
// --------------------------------------------------------------------------------------------------------------------
// (c) 2025 - 2026 Holger Freudenreich under the MIT license
// --------------------------------------------------------------------------------------------------------------------

using Dapper;
using Domain.Interfaces.Crud;
using Domain.Pocos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Reflection;

namespace Domain.Services;

public class DashboardDatabaseService : DataServiceBase, IDashboardCrudService
{
   public DashboardDatabaseService(IOptions<AppSettings> options,
                                   ILogger<DataServiceBase> logger) : base(options, logger)
   {
      Initialize(typeof(Dashboard), "cfg", "dashboard");
   }

   public async Task<List<Dashboard>> GetDashboards()
   {
      try
      {
         var dashboards = await Db.QueryAsync<Dashboard>($"select {FieldListSelect} from {FullQualifiedTableName} order by index_in_project, dashboard_id;");
         return dashboards.AsList();
      }
      catch (Exception exception)
      {
         Logger.LogError(exception, $"{MethodBase.GetCurrentMethod()} failed!");
         throw;
      }
   }

   public async Task<Dashboard?> GetDashboardById(int dashboardId)
   {
      try
      {
         return await Db.QueryFirstOrDefaultAsync<Dashboard?>($"select {FieldListSelect} from {FullQualifiedTableName} where dashboard_id = @DashboardId;",
                                                              new { DashboardId = dashboardId });
      }
      catch (Exception exception)
      {
         Logger.LogError(exception, $"{MethodBase.GetCurrentMethod()} failed!");
         throw;
      }
   }

   public async Task<int> Insert(Dashboard dashboard)
   {
      try
      {
         dashboard.DashboardId = await Db.QuerySingleAsync<int>(InsertSql, dashboard);
         return dashboard.DashboardId;
      }
      catch (Exception exception)
      {
         Logger.LogError(exception, $"{MethodBase.GetCurrentMethod()} failed!");
         throw;
      }
   }

   public async Task<bool> Update(Dashboard dashboard)
   {
      try
      {
         int rows = await Db.ExecuteAsync(UpdateSql, dashboard);
         return rows == 1;
      }
      catch (Exception exception)
      {
         Logger.LogError(exception, $"{MethodBase.GetCurrentMethod()} failed!");
         throw;
      }
   }

   public async Task<bool> Delete(Dashboard dashboard)
   {
      try
      {
         int rows = await Db.ExecuteAsync(DeleteSql, dashboard);
         return rows == 1;
      }
      catch (Exception exception)
      {
         Logger.LogError(exception, $"{MethodBase.GetCurrentMethod()} failed!");
         throw;
      }
   }
}
