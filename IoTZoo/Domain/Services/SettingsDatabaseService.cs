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
using Domain.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Reflection;
using System.Text.Json;

namespace DataAccess.Services;

public class SettingsDatabaseService : DataServiceBase, ISettingsCrudService
{
   public SettingsDatabaseService(IOptions<AppSettings> options,
                                  ILogger<DataServiceBase> logger) : base(options,
                                                                          logger)
   {
      Initialize(typeof(Setting), "setting", "setting");
   }

   public async Task<Setting?> GetSetting(string category, string settingsKey)
   {
      Setting? setting = await Db.QueryFirstOrDefaultAsync<Setting?>($"select {FieldListSelect} from {FullQualifiedTableName} where category = @Category and setting_key = @SettingKey;",
                                    new { Category = category, SettingKey = settingsKey });
      return setting;
   }

   public async Task<bool> GetSettingBool(SettingCategory category, SettingKey settingsKey)
   {
      return await GetSettingBool(category.ToString(), settingsKey.ToString());
   }

   public async Task<bool> GetSettingBool(string category, string settingsKey)
   {
      var setting = await GetSetting(category, settingsKey);
      if (null != setting)
      {
         return Convert.ToBoolean(setting.SettingValue, CultureInfo.InvariantCulture);
      }

      throw new Exception("unknown settingsKey!");
   }

   public async Task<double> GetSettingDouble(SettingCategory category, SettingKey settingsKey)
   {
      return await GetSettingDouble(category.ToString(), settingsKey.ToString());
   }

   public async Task<double> GetSettingDouble(string category, string settingsKey)
   {
      var setting = await GetSetting(category, settingsKey);
      if (null != setting)
      {
         return Convert.ToDouble(setting.SettingValue, CultureInfo.InvariantCulture);
      }

      throw new Exception("unknown settingsKey!");
   }

   public async Task<string> GetSettingString(string category, string settingsKey)
   {
      var setting = await GetSetting(category, settingsKey);
      if (null != setting)
      {
         return setting.SettingValue;
      }

      throw new Exception("unknown settingsKey!");
   }

   public async Task<string> GetSettingString(SettingCategory category, SettingKey settingsKey)
   {
      return await GetSettingString(category.ToString(), settingsKey.ToString());
   }

   public async Task<object?> GetObject(SettingCategory category, SettingKey settingsKey)
   {
      var setting = await GetSetting(category.ToString(), settingsKey.ToString());
      if (null == setting)
      {
         return null;
      }

      var type = Type.GetType($"Domain.Pocos.{setting.SettingKey}");
      if (null == type)
      {
         throw new Exception($"{setting.SettingKey} is not a valid type!");
      }
      return JsonSerializer.Deserialize(setting.SettingValue, type);
   }

   public async Task<int> Update(SettingCategory category,
                                 SettingKey key,
                                 object value)
   {
      string json = System.Text.Json.JsonSerializer.Serialize(value);
      return await Update(category,
                          key,
                          json);
   }

   public async Task<int> Update(SettingCategory category,
                                 SettingKey key,
                                 string value)
   {
      return await Update(new Setting { Category = category.ToString(), SettingKey = key.ToString(), SettingValue = value });
   }

   public async Task<int> Update(SettingCategory category,
                                 SettingKey key,
                                 double value)
   {
      return await Update(new Setting
      {
         Category = category.ToString(),
         SettingKey = key.ToString(),
         SettingValue = Convert.ToString(value, CultureInfo.InvariantCulture)
      });
   }

   public async Task<int> Update(Setting setting)
   {
      int rowsProcessed = 0;
      try
      {
         if (setting.SettingId == 0)
         {
            var settingTmp = await GetSetting(setting.Category, setting.SettingKey);
            if (null != settingTmp)
            {
               setting.SettingId = settingTmp.SettingId;
            }
         }

         rowsProcessed = await Db.ExecuteAsync(UpdateSql, setting);
      }
      catch (Exception ex)
      {
         Logger.LogError(ex,
                         $"{MethodBase.GetCurrentMethod()} failed!");
      }

      return rowsProcessed;
   }

   public async Task<int> Insert(Setting setting)
   {
      int rowsProcessed = 0;
      try
      {
         rowsProcessed = await Db.ExecuteAsync(InsertSql, setting);
      }
      catch (Exception ex)
      {
         Logger.LogError(ex, $"{MethodBase.GetCurrentMethod()} failed!");
      }

      return rowsProcessed;
   }

   public async Task<int> Delete(Setting setting)
   {
      int rowsProcessed = 0;
      try
      {
         rowsProcessed = await Db.ExecuteAsync(DeleteSql, setting);
      }
      catch (Exception ex)
      {
         Logger.LogError(ex, $"{MethodBase.GetCurrentMethod()} failed!");
      }

      return rowsProcessed;
   }
}