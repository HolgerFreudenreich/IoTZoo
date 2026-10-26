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

public class DashboardItemsDatabaseService : DataServiceBase, IDashboardItemsCrudService
{
   public DashboardItemsDatabaseService(IOptions<AppSettings> options,
                                        ILogger<DataServiceBase> logger) : base(options, logger)
   {
      Initialize(typeof(DashboardItem), "cfg", "dashboard_item");
   }

   public async Task<List<DashboardItem>> GetDashboardItems()
   {
      try
      {
         var dashboardItems = await Db.QueryAsync<DashboardItem>($"select {FieldListSelect} from {FullQualifiedTableName} order by dashboard_item_id;");
         return dashboardItems.AsList();
      }
      catch (Exception exception)
      {
         Logger.LogError(exception, $"{MethodBase.GetCurrentMethod()} failed!");
         throw;
      }
   }

   public async Task<List<DashboardItem>> GetDashboardItemsByDashboardId(int dashboardId)
   {
      try
      {
         var dashboardItems = await Db.QueryAsync<DashboardItem>($"select {FieldListSelect} from {FullQualifiedTableName} where dashboard_id = @DashboardId order by dashboard_item_id;",
                                                                 new { DashboardId = dashboardId });
         return dashboardItems.AsList();
      }
      catch (Exception exception)
      {
         Logger.LogError(exception, $"{MethodBase.GetCurrentMethod()} failed!");
         throw;
      }
   }

   public async Task<List<DashboardItem>> GetDashboardItemsByKnownTopicId(int knownTopicId)
   {
      try
      {
         var dashboardItems = await Db.QueryAsync<DashboardItem>($"select {FieldListSelect} from {FullQualifiedTableName} where known_topic_id = @KnownTopicId order by dashboard_item_id;",
                                                                 new { KnownTopicId = knownTopicId });
         return dashboardItems.AsList();
      }
      catch (Exception exception)
      {
         Logger.LogError(exception, $"{MethodBase.GetCurrentMethod()} failed!");
         throw;
      }
   }

    public async Task SaveDashboardsForKnownTopic(int knownTopicId, IReadOnlyDictionary<int, DashboardItem> dashboardItems)
   {
      try
      {
         var existing = await GetDashboardItemsByKnownTopicId(knownTopicId);

         foreach (var item in existing.Where(item => !dashboardItems.ContainsKey(item.DashboardId)))
         {
            await Delete(item);
         }

         foreach (var item in existing.Where(item => dashboardItems.ContainsKey(item.DashboardId)))
         {
            var wanted = dashboardItems[item.DashboardId];
            item.DisplayType = wanted.DisplayType;
            item.SettingsJson = wanted.SettingsJson;
            await Update(item);
         }

         foreach (var (dashboardId, wanted) in dashboardItems.Where(entry => !existing.Any(item => item.DashboardId == entry.Key)))
         {
            await Insert(new DashboardItem
            {
               DashboardId = dashboardId,
               KnownTopicId = knownTopicId,
               DisplayType = wanted.DisplayType,
               SettingsJson = wanted.SettingsJson,
               IndexInDashboard = wanted.IndexInDashboard
            });
         }
      }
      catch (Exception exception)
      {
         Logger.LogError(exception, $"{MethodBase.GetCurrentMethod()} failed!");
         throw;
      }
   }

   public async Task<DashboardItem?> GetDashboardItemById(int dashboardItemId)
   {
      try
      {
         return await Db.QueryFirstOrDefaultAsync<DashboardItem?>($"select {FieldListSelect} from {FullQualifiedTableName} where dashboard_item_id = @DashboardItemId;",
                                                                  new { DashboardItemId = dashboardItemId });
      }
      catch (Exception exception)
      {
         Logger.LogError(exception, $"{MethodBase.GetCurrentMethod()} failed!");
         throw;
      }
   }

   public async Task<int> Insert(DashboardItem dashboardItem)
   {
      try
      {
         dashboardItem.DashboardItemId = await Db.QuerySingleAsync<int>(InsertSql, dashboardItem);
         return dashboardItem.DashboardItemId;
      }
      catch (Exception exception)
      {
         Logger.LogError(exception, $"{MethodBase.GetCurrentMethod()} failed!");
         throw;
      }
   }

   public async Task<bool> Update(DashboardItem dashboardItem)
   {
      try
      {
         int rows = await Db.ExecuteAsync(UpdateSql, dashboardItem);
         return rows == 1;
      }
      catch (Exception exception)
      {
         Logger.LogError(exception, $"{MethodBase.GetCurrentMethod()} failed!");
         throw;
      }
   }

   public async Task<bool> Delete(DashboardItem dashboardItem)
   {
      try
      {
         int rows = await Db.ExecuteAsync(DeleteSql, dashboardItem);
         return rows == 1;
      }
      catch (Exception exception)
      {
         Logger.LogError(exception, $"{MethodBase.GetCurrentMethod()} failed!");
         throw;
      }
   }
}
