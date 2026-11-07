// --------------------------------------------------------------------------------------------------------------------
//      ____    ______   _____
//     /  _/___/_  __/  /__  / ____  ____
//     / // __ \/ /       / / / __ \/ __ \
//   _/ // /_/ / /       / /_/ /_/ / /_/ /
//  /___/\____/_/       /____|____/\____/   P L A Y G R O U N D
// --------------------------------------------------------------------------------------------------------------------
// Connect «Things» with microcontrollers in a simple way.
// --------------------------------------------------------------------------------------------------------------------
// (c) 2025 - 2026 Holger Freudenreich under the MIT license
// --------------------------------------------------------------------------------------------------------------------

using Dapper;
using Domain.Interfaces.Crud;
using Domain.Pocos;
using Domain.Services;
using Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Reflection;
using System.Security.Cryptography;
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

   public async Task<int> GetSettingInt(SettingCategory category, SettingKey settingsKey)
   {
      return await GetSettingInt(category.ToString(), settingsKey.ToString());
   }

   public async Task<int> GetSettingInt(string category, string settingsKey)
   {
      var setting = await GetSetting(category, settingsKey);
      if (null != setting)
      {
         return Convert.ToInt32(setting.SettingValue, CultureInfo.InvariantCulture);
      }

      throw new Exception("unknown settingsKey!");
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
         if (category == SettingCategory.Mail.ToString() && settingsKey == SettingKey.MailReceiverConfigs.ToString())
         {
            return DecryptMailReceiverConfigs(setting.SettingValue);
         }
         return setting.SettingValue;
      }

      throw new Exception("unknown settingsKey!");
   }

   private string DecryptMailReceiverConfigs(string json)
   {
      if (string.IsNullOrEmpty(json))
      {
         return json;
      }

      List<MailReceiverConfig>? configs;
      try
      {
         configs = JsonSerializer.Deserialize<List<MailReceiverConfig>>(json);
      }
      catch (JsonException)
      {
         return json;
      }
      if (null == configs)
      {
         return json;
      }

      foreach (var config in configs.Where(config => !string.IsNullOrEmpty(config.Password)))
      {
         try
         {
            config.Password = PasswordEncryptor.Decrypt(config.Password, AppSettings.MqttPasswordEncryptionKey);
         }
         catch (Exception exception) when (exception is CryptographicException or FormatException)
         {
            config.Password = string.Empty;
         }
      }
      return JsonSerializer.Serialize(configs);
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
         var value = JsonSerializer.Deserialize(setting.SettingValue, type);
         if (value is MqttBrokerSettings mqttBrokerSettings && !string.IsNullOrEmpty(mqttBrokerSettings.Password))
         {
            try
            {
               mqttBrokerSettings.Password = PasswordEncryptor.Decrypt(mqttBrokerSettings.Password, AppSettings.MqttPasswordEncryptionKey);
            }
            catch (Exception exception) when (exception is CryptographicException or FormatException)
            {
               mqttBrokerSettings.Password = string.Empty;
            }
         }
         return value;
      }

      public async Task<List<Setting>> GetAllSettings()
      {
         var settings = await Db.QueryAsync<Setting>($"select {FieldListSelect} from {FullQualifiedTableName} order by category, setting_key;");
         return settings.ToList();
      }

   public async Task<int> Update(SettingCategory category,
                                 SettingKey key,
                                 object value)
   {
      if (value is MqttBrokerSettings mqttBrokerSettings)
      {
         var storedSettings = new MqttBrokerSettings
         {
            UseInternalMqttBroker = mqttBrokerSettings.UseInternalMqttBroker,
            Ip = mqttBrokerSettings.Ip,
            Port = mqttBrokerSettings.Port,
            User = mqttBrokerSettings.User,
            Password = string.IsNullOrEmpty(mqttBrokerSettings.Password)
               ? mqttBrokerSettings.Password
               : PasswordEncryptor.Encrypt(mqttBrokerSettings.Password, AppSettings.MqttPasswordEncryptionKey)
         };
            value = storedSettings;
         }
         if (value is List<MailReceiverConfig> mailReceiverConfigs)
         {
            value = mailReceiverConfigs.Select(config => new MailReceiverConfig
            {
               Enabled = config.Enabled,
               HostName = config.HostName,
               Password = string.IsNullOrEmpty(config.Password)
                  ? config.Password
                  : PasswordEncryptor.Encrypt(config.Password, AppSettings.MqttPasswordEncryptionKey),
               UserName = config.UserName,
               Port = config.Port
            }).ToList();
         }
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