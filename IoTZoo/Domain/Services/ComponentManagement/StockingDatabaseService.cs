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
using Domain.Interfaces.Crud;
using Domain.Pocos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Reflection;
using Component = Domain.Pocos.Component;

namespace Domain.Services.ComponentManagement;

/// <summary>
/// An electrical component like HW-040
/// </summary>
public interface IComponentService
{
   public Task<Component?> GetComponent(string sku);

   Task<List<Component>?> GetComponents();

   public Task AddComponent(Component component);

   public Task DeleteComponent(string sku);

   public Task DeleteComponent(Component component);

   public Task UpdateComponent(Component component);

   public Task SaveComponent(Component component);
}


/// <summary>
/// Which item is in which storage bin with which stock?
/// </summary>
public interface IStockingService : IComponentService, IStorageBinCrudService
{
   public Task AddStocking(Stocking stocking);

   public Task<Stocking?> GetStocking(StorageBin storageBin);

   public Task SaveStocking(Stocking stocking);

   public Task<List<Stocking>?> GetStockings(StorageBin storageBin);

   public Task UpdateStocking(Stocking stocking);

   public Task DeleteStocking(Stocking stocking);
}

public class StockingDatabaseService : DataServiceBase, IStockingService
{
   public StockingDatabaseService(IOptions<AppSettings> options,
                                  ILogger<DataServiceBase> logger,
                                  IComponentService componentService,
                                  IStorageBinCrudService storageBinService) : base(options, logger)
   {
      Initialize(typeof(Stocking),
                 "component",
                 "stocking");
      ComponentService = componentService;
      StorageBinService = storageBinService;
   }

   protected IComponentService ComponentService
   {
      get;
      set;
   }
   public IStorageBinCrudService StorageBinService { get; private set; }

   public async Task<Component?> GetComponent(string sku)
   {
      return await ComponentService.GetComponent(sku);
   }

   public async Task<List<Component>?> GetComponents()
   {
      return await ComponentService.GetComponents();
   }

   public async Task AddComponent(Component component)
   {
      await ComponentService.AddComponent(component);
   }

   public async Task DeleteComponent(string sku)
   {
      await ComponentService.DeleteComponent(sku);
   }

   public async Task UpdateComponent(Component component)
   {
      await ComponentService.UpdateComponent(component);
   }

   public async Task SaveComponent(Component component)
   {
      await ComponentService.SaveComponent(component);
   }

   public async Task DeleteComponent(Component component)
   {
      await ComponentService.DeleteComponent(component);
   }

   public async Task AddStorageBin(StorageBin storageBin)
   {
      await StorageBinService.AddStorageBin(storageBin);
   }

   public async Task RemoveStorageBin(StorageBin storageBin)
   {
      await StorageBinService.RemoveStorageBin(storageBin);
   }

   public async Task UpdateStorageBin(StorageBin storageBin)
   {
      await StorageBinService.UpdateStorageBin(storageBin);
   }

   public async Task DeleteStorageBin(StorageBin storageBin)
   {
      await StorageBinService.DeleteStorageBin(storageBin);
   }

   public async Task SaveStorageBin(StorageBin storageBin)
   {
      await StorageBinService.SaveStorageBin(storageBin);
   }

   public async Task AddStocking(Stocking stocking)
   {
      try
      {
         int rowsProcessed = await Db.ExecuteAsync(InsertSql,
                                        new { stocking.StorageBin.StorageBinId, stocking.Component.Sku, stocking.Quantity });
      }
      catch (Exception ex)
      {
         Logger.LogError(ex, $"{MethodBase.GetCurrentMethod()} failed!");
      }
   }

   public async Task UpdateStocking(Stocking stocking)
   {
      try
      {
         const string Sql = "update stocking set quantity = @Quantity where stocking_id = @StockingId;";
         int rowsProcessed = await Db.ExecuteAsync(Sql,
                                                   new { stocking.Quantity, stocking.StockingId });
      }
      catch (Exception ex)
      {
         Logger.LogError(ex, $"{MethodBase.GetCurrentMethod()} failed!");
      }
   }

   public async Task DeleteStocking(Stocking stocking)
   {
      try
      {
         int rowsProcessed = await Db.ExecuteAsync(DeleteSql,
                                                   new { stocking.StockingId });
      }
      catch (Exception ex)
      {
         Logger.LogError(ex, $"{MethodBase.GetCurrentMethod()} failed!");
      }
   }

   public async Task<List<StorageBin>?> GetStorageBins()
   {
      return await StorageBinService.GetStorageBins();
   }

   public async Task<List<StorageBin>?> GetStorageBins(Component component)
   {
      return await StorageBinService.GetStorageBins(component);
   }

   public async Task<Stocking?> GetStocking(StorageBin storageBin)
   {
      Stocking? stocking = null;
      try
      {
         stocking = await Db.QueryFirstOrDefaultAsync<Stocking>($"select {FieldListSelect} from {FullQualifiedTableName} where storage_bin_id = @StorageBinId;",
                                                                new { storageBin.StorageBinId });
      }
      catch (Exception ex)
      {
         Logger.LogError(ex, $"{MethodBase.GetCurrentMethod()} failed!");
      }
      return stocking;
   }

   public async Task SaveStocking(Stocking stocking)
   {
      // Gibt es bereits einen Lagerplatz mit dieser BinLocationId für die Komponente?
      var tmpStocking = await GetStocking(stocking.StorageBin,
                                          stocking.Component);
      if (null != tmpStocking)
      {
         tmpStocking.Quantity += stocking.Quantity;
         await UpdateStocking(tmpStocking);
      }
      else
      {
         await AddStocking(stocking);
      }
   }

   private async Task<Stocking?> GetStocking(StorageBin storageBin, Component component)
   {
      Stocking? stocking = null;
      try
      {
         stocking = await Db.QueryFirstOrDefaultAsync<Stocking>($"select {FieldListSelect} from {FullQualifiedTableName} where storage_bin_id = @StorageBinId and sku = @Sku;",
                                                                new { storageBin.StorageBinId, component.Sku });
      }
      catch (Exception ex)
      {
         Logger.LogError(ex, $"{MethodBase.GetCurrentMethod()} failed!");
      }
      return stocking;
   }

   public async Task<List<Stocking>?> GetStockings(StorageBin storageBin)
   {
      IEnumerable<Stocking>? stockings = null;
      try
      {
         stockings = await Db.QueryAsync<Stocking>($"select {FieldListSelect} from {FullQualifiedTableName} where storage_bin_id = @StorageBinId order by sku;",
                                                  new { storageBin.StorageBinId });
         foreach (var stocking in stockings)
         {
            stocking.StorageBin = storageBin;
         }
      }
      catch (Exception ex)
      {
         Logger.LogError(ex, $"{MethodBase.GetCurrentMethod()} failed!");
      }
      return stockings.AsList();
   }
}