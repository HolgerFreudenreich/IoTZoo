// --------------------------------------------------------------------------------------------------------------------
//      ____    ______   _____
//     /  _/___/_  __/  /__  / ____  ____
//     / // __ \/ /       / / / __ \/ __ \
//   _/ // /_/ / /       / /_/ /_/ / /_/ /
//  /___/\____/_/       /____|____/\____/  (c) 2025 Holger Freudenreich under MIT license 
//
// --------------------------------------------------------------------------------------------------------------------
// Perform CRUD operations on boxes/totes. Each box can contain one or more components.
// It would also be conceivable to put boxes in boxes.
// --------------------------------------------------------------------------------------------------------------------

using Dapper;
using Domain.Interfaces.Crud;
using Domain.Pocos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Reflection;

namespace Domain.Services.ComponentManagement;

public class StorageBinService : DataServiceBase, IStorageBinCrudService
{
   public StorageBinService(IOptions<AppSettings> options,
                         ILogger<DataServiceBase> logger) : base(options,
                                                                 logger)
   {
      Initialize(typeof(StorageBin),
                 "component",
                 "storage_bin");
   }
   public async Task AddStorageBin(StorageBin storageBin)
   {
      try
      {
         await Db.ExecuteAsync(InsertSql, storageBin);
      }
      catch (Exception ex)
      {
         Logger.LogError(ex, $"{MethodBase.GetCurrentMethod()} failed!");
      }
   }

   public async Task RemoveStorageBin(StorageBin storageBin)
   {
      try
      {
         int rowsProcessed = await Db.ExecuteAsync(DeleteSql,
                                                   new { storageBin.StorageBinId });
      }
      catch (Exception ex)
      {
         Logger.LogError(ex, $"{MethodBase.GetCurrentMethod()} failed!");
      }
   }

   public async Task UpdateStorageBin(StorageBin storageBin)
   {
      try
      {
         int rowsProcessed = await Db.ExecuteAsync(UpdateSql,
                                                   storageBin);
      }
      catch (Exception ex)
      {
         Logger.LogError(ex, $"{MethodBase.GetCurrentMethod()} failed!");
      }
   }

   public async Task DeleteStorageBin(StorageBin storageBin)
   {
      try
      {
         int rowsProcessed = await Db.ExecuteAsync(DeleteSql,
                                                   new { storageBin.StorageBinId });
      }
      catch (Exception ex)
      {
         Logger.LogError(ex, $"{MethodBase.GetCurrentMethod()} failed!");
      }
   }

   public async Task SaveStorageBin(StorageBin storageBin)
   {
      if (storageBin.StorageBinId > 0)
      {
         await UpdateStorageBin(storageBin);
      }
      else
      {
         await AddStorageBin(storageBin);
      }
   }

   public async Task<List<StorageBin>?> GetStorageBins()
   {
      IEnumerable<StorageBin>? storageBins = null;
      try
      {
         storageBins = await Db.QueryAsync<StorageBin>($"select {FieldListSelect} from {FullQualifiedTableName} order by storage_bin_id;");
      }
      catch (Exception ex)
      {
         Logger.LogError(ex, $"{MethodBase.GetCurrentMethod()} failed!");
      }
      return storageBins.AsList();
   }

   public async Task<List<StorageBin>?> GetStorageBins(Component component)
   {
      IEnumerable<StorageBin>? storageBins = null;
      try
      {
         storageBins = await Db.QueryAsync<StorageBin>($"select {FieldListSelect} from {FullQualifiedTableName} " +
                                                       "inner join stocking on storage_bin.storage_bin_id = stocking.storage_bin_id " +
                                                       "inner join component on stocking.sku = component.sku where " +
                                                       "stocking.sku = @Sku " +
                                                       "order by storage_bin.storage_bin_id;", new { component.Sku });
      }
      catch (Exception ex)
      {
         Logger.LogError(ex, $"{MethodBase.GetCurrentMethod()} failed!");
      }
      return storageBins.AsList();
   }
}