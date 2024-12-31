// --------------------------------------------------------------------------------------------------------------------
//      ____    ______   _____
//     /  _/___/_  __/  /__  / ____  ____
//     / // __ \/ /       / / / __ \/ __ \
//   _/ // /_/ / /       / /_/ /_/ / /_/ /
//  /___/\____/_/       /____|____/\____/ 
// --------------------------------------------------------------------------------------------------------------------
// (c) 2025 Holger Freudenreich under MIT license
// --------------------------------------------------------------------------------------------------------------------
// Management of the available devices.
// --------------------------------------------------------------------------------------------------------------------

using Dapper;
using Domain.Pocos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Reflection;

namespace Domain.Services.ComponentManagement;

public class ComponentDatabaseService : DataServiceBase,
                                        IComponentService
{
   public ComponentDatabaseService(IOptions<AppSettings> options,
                                   ILogger<DataServiceBase> logger) : base(options, logger)
   {
      Initialize(typeof(Component),
                 "component",
                 "component");
   }

   public async Task<Component?> GetComponent(string sku)
   {
      Component? component = null;
      try
      {
         component = await Db.ExecuteScalarAsync<Component>($"select {FieldListSelect} from {FullQualifiedTableName} where sku = @sku", sku);
      }
      catch (Exception ex)
      {
         Logger.LogError(ex, $"{MethodBase.GetCurrentMethod()} failed!");
      }
      return component;
   }

   public async Task<List<Component>?> GetComponents()
   {
      IEnumerable<Component>? components = null!;
      try
      {
         components = await Db.QueryAsync<Component>($"select {FieldListSelect} from {FullQualifiedTableName} order by sku;");
      }
      catch (Exception ex)
      {
         Logger.LogError(ex, $"{MethodBase.GetCurrentMethod()} failed!");
      }
      return components.AsList();
   }

   public async Task AddComponent(Component component)
   {
      try
      {
         await Db.QueryAsync(InsertSql, component);
      }
      catch (Exception ex)
      {
         Logger.LogError(ex, $"{MethodBase.GetCurrentMethod()} failed!");
      }
   }

   public async Task DeleteComponent(string sku)
   {
      try
      {
         int rowsProcessed = await Db.ExecuteAsync(DeleteSql,
                                                   sku);
      }
      catch (Exception ex)
      {
         Logger.LogError(ex, $"{MethodBase.GetCurrentMethod()} failed!");
      }
   }

   public async Task UpdateComponent(Component component)
   {
      try
      {
         int rowsProcessed = await Db.ExecuteAsync(UpdateSql,
                                                   component);
      }
      catch (Exception ex)
      {
         Logger.LogError(ex, $"{MethodBase.GetCurrentMethod()} failed!");
      }
   }

   public async Task SaveComponent(Component component)
   {
      if (component.ComponentId > 0)
      {
         await UpdateComponent(component);
      }
      else
      {
         await AddComponent(component);
      }
   }

   public async Task DeleteComponent(Component component)
   {
      try
      {
         int rowsProcessed = await Db.ExecuteAsync(DeleteSql,
                                                   new { component.ComponentId });
      }
      catch (Exception ex)
      {
         Logger.LogError(ex, $"{MethodBase.GetCurrentMethod()} failed!");
      }
   }
}