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

using Domain.Interfaces.Crud;
using Domain.Pocos;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using FieldInfo = Domain.Pocos.FieldInfo;

namespace Domain.Services;

public enum SaveResult
{
   Inserted,
   Updated,
   NothingDone,
}

public class DataServiceBase(IOptions<AppSettings> options, ILogger<DataServiceBase> logger)
   : IDataServiceBase
{
   protected IDbConnection Db = null!;

   protected string? FullQualifiedTableName
   {
      get;
      set;
   }

   protected string TableName { get; set; } = null!;

   protected string FieldListSelect = null!;

   protected string InsertSql = null!;
   protected string UpdateSql = null!;
   protected string DeleteSql = null!;

   protected string ConnectionString = null!;

   public string GetSelectFieldList()
   {
      return FieldListSelect;
   }

   protected string GetTableName(string schemaName, string tableName)
   {
      if (AppSettings.ConfigurationDatabaseType == DatabaseType.Sqlite)
      {
         return tableName;
      }
      return $"{schemaName}.{tableName}";
   }

   /// <summary>
   /// All Fields that the Service can deliver. Represents the fields in the sql database.
   /// </summary>
   public List<FieldInfo> FieldInfos
   {
      get;
      set;
   } = null!;

   protected Type ServiceType
   {
      get;
      set;
   } = null!;

   protected ILogger<DataServiceBase> Logger
   {
      get;
   } = logger;

   protected AppSettings AppSettings
   {
      get;
   } = options.Value;

   protected PropertyInfo[]? PropertyInfos
   {
      get;
      set;
   }

   protected void SetConnectionString(string schemaName)
   {
      if (AppSettings.ConfigurationDatabaseType == DatabaseType.Sqlite)
      {
         if (schemaName == "th")
         {
            ConnectionString = AppSettings.ConnectionStringSqliteDatabaseTopicHistory;
         }
         else if (schemaName == "component")
         {
            ConnectionString = AppSettings.ConnectionStringSqliteDatabaseComponentManagement;
         }
         else if (schemaName == "script")
         {
            ConnectionString = AppSettings.ConnectionStringSqliteDatabaseScripting;
         }
         else if (schemaName == "setting")
         {
            ConnectionString = AppSettings.ConnectionStringSqliteDatabaseSettings;
         }
         else if (string.IsNullOrEmpty(schemaName))
         {
            ConnectionString = "Data Source=:memory:;";
         }
         else
         {
            ConnectionString = AppSettings.ConnectionStringSqlite;
         }
      }
      else if (AppSettings?.ConfigurationDatabaseType == DatabaseType.Postgres)
      {
         ConnectionString = AppSettings.ConnectionStringPostgres;
         throw new Exception("Postgres needs to be revised, see Postgres.plsql");
      }
      else
      {
         throw new Exception("unsupported database type!");
      }
   }

   [MemberNotNull(nameof(Db))]
   protected void Initialize(Type type, string schemaName, string tableName)
   {
      SetConnectionString(schemaName);
      TableName = tableName;
      string fullQualifiedTableName = GetTableName(schemaName,
                                                   tableName);
      Db = GetNewOpenedConnection();
      ServiceType = type;
      PropertyInfos = type.GetProperties();

      if (string.IsNullOrEmpty(fullQualifiedTableName))
      {
         FullQualifiedTableName = TableName.ToLower();
      }
      else
      {
         FullQualifiedTableName = fullQualifiedTableName;
      }

      Initialize();
   }

   private void Initialize()
   {
      if (string.IsNullOrEmpty(FullQualifiedTableName))
      {
         throw new ArgumentNullException("FullQualifiedTableName must not be null!");
      }
      FieldInfos = GetFieldInfosFromDatabase();
      if (string.IsNullOrEmpty(FieldListSelect))
      {
         FieldListSelect = GetFieldsForSelectClause(FieldInfos,
                                                    ServiceType,
                                                    true,
                                                    true);
      }

      if (string.IsNullOrEmpty(InsertSql))
      {
         InsertSql = $"insert into {FullQualifiedTableName} ({GetFieldsForInsertClause(FieldInfos)}) values " +
                                             $"({GetInsertValuesClause(FieldInfos, ServiceType)}) returning " + // {TableName} Dapper unfortunately does not map back the whole table
                                             $"{GetFieldsForSelectClause(FieldInfos, ServiceType, false, true)}"; // ... so wie only take the primary key.
      }

      if (string.IsNullOrEmpty(UpdateSql))
      {
         UpdateSql = $"update {FullQualifiedTableName} set {GetStandardFieldsForUpdateClause(FieldInfos, ServiceType)} where " +
                                             $"{GetKeyFields(FieldInfos, ServiceType)};";
      }

      if (string.IsNullOrEmpty(DeleteSql))
      {
         DeleteSql = $"delete from {FullQualifiedTableName} where {GetKeyFields(FieldInfos, ServiceType)}";
      }
   }

   protected DbCommand CreateCommand(string sql)
   {
      var connection = GetNewOpenedConnection();

      if (AppSettings.ConfigurationDatabaseType == DatabaseType.Sqlite)
      {
         return new SqliteCommand(sql,
                                  connection as SqliteConnection);
      }

      if (AppSettings.ConfigurationDatabaseType == DatabaseType.Postgres)
      {
         return new NpgsqlCommand(sql,
                                  connection as NpgsqlConnection);
      }

      throw new NotSupportedException($"Unsupported database type {AppSettings.ConfigurationDatabaseType}");
   }

   protected List<FieldInfo> GetFieldInfosFromDatabase()
   {
      List<FieldInfo> fieldInfos = new List<FieldInfo>();

      using DbCommand command = CreateCommand($"select * from {FullQualifiedTableName};");
      using var reader = command.ExecuteReader(CommandBehavior.KeyInfo);
      var table = reader.GetSchemaTable();
      if (null == table)
      {
         return fieldInfos;
      }
      foreach (DataRow dataRow in table.Rows)
      {
         FieldInfo fieldInfo = new FieldInfo
         {
            DatabaseName = Convert.ToString(dataRow[table.Columns["BaseCatalogName"]!]),
            TableName = Convert.ToString(dataRow[table.Columns["BaseTableName"]!]),
            SchemaName = Convert.ToString(dataRow[table.Columns["BaseSchemaName"]!])!,
            ColumnName = Convert.ToString(dataRow[table.Columns["ColumnName"]!])!,
            Ordinal = Convert.ToInt32(dataRow[table.Columns["ColumnOrdinal"]!]),
            IsKey = Convert.ToBoolean(dataRow[table.Columns["IsKey"]!]),
            IsNullable = Convert.ToBoolean(dataRow[table.Columns["AllowDBNull"]!]),
            IsUnique = Convert.ToBoolean(dataRow[table.Columns["IsUnique"]!]),
            IsAutoIncrement = Convert.ToBoolean(dataRow[table.Columns["IsAutoIncrement"]!]),
            DataType = Type.GetType(Convert.ToString(dataRow[table.Columns["DataType"]!])!)
         };
         fieldInfos.Add(fieldInfo);
      }

      Logger.LogDebug($"Table {FullQualifiedTableName} has {fieldInfos.Count} fields.");


      //foreach (var fieldInfo in fieldInfos)
      //{
      //  FieldInfo detailedColumnInformations = GetDetailedColumnInformations(fieldInfo.SchemaName,
      //                                                                       fieldInfo.TableName,
      //                                                                       fieldInfo.ColumnName);

      //  fieldInfo.FieldLength = detailedColumnInformations.FieldLength;
      //}


      return fieldInfos;
   }

   protected IDbConnection GetNewOpenedConnection()
   {
      if (AppSettings.ConfigurationDatabaseType == DatabaseType.Sqlite)
      {
         SqliteConnection connection = new SqliteConnection(ConnectionString);
         connection.Open();
         return connection;
      }

      if (AppSettings.ConfigurationDatabaseType == DatabaseType.Postgres)
      {
         NpgsqlConnection connection = new NpgsqlConnection(ConnectionString);
         connection.Open();
         return connection;
      }

      throw new Exception("unsupported database type!");
   }

   protected string? GetPocoPropertyName(Type type, string databaseFieldName)
   {
      string simplifiedName = databaseFieldName.Replace("_", string.Empty);

      PropertyInfo? propertyInfo = (from data in PropertyInfos
                                    where data.Name.Equals(simplifiedName,
                                                           StringComparison.OrdinalIgnoreCase)
                                    select data).FirstOrDefault();
      if (propertyInfo == null)
      {
         return null;
      }
      return propertyInfo.Name;
   }

   protected string GetFieldsForSelectClause(List<FieldInfo> fieldInfos,
                                             Type type,
                                             bool includeDefaultFields = true,
                                             bool includeKeyFields = true,
                                             bool addTableName = true)
   {
      string sqlFields = string.Empty;

      foreach (FieldInfo fieldInfo in fieldInfos)
      {
         if (!includeDefaultFields)
         {
            if (!fieldInfo.IsKey)
            {
               break;
            }
         }
         if (!includeKeyFields)
         {
            if (fieldInfo.IsKey)
            {
               break;
            }
         }
         var pocoPropertyName = GetPocoPropertyName(type, fieldInfo.ColumnName);
         if (null != pocoPropertyName)
         {
            if (addTableName)
            {
               sqlFields += $"{fieldInfo.TableName}.";
            }
            sqlFields += $"{fieldInfo.ColumnName} as {pocoPropertyName}, ";
         }
      }

      if (!string.IsNullOrEmpty(sqlFields))
      {
         if (sqlFields.Length > 2)
         {
            sqlFields = sqlFields.Remove(sqlFields.Length - 2);
         }
      }

      return sqlFields;
   }

   protected string GetFieldsForInsertClause(List<FieldInfo> fieldInfos)
   {
      string sqlFields = string.Empty;

      foreach (FieldInfo fieldInfo in fieldInfos)
      {
         if (!fieldInfo.IsKey)
         {
            if (IsAutoValueField(fieldInfo))
            {
               continue;
            }

            sqlFields += $"{fieldInfo.ColumnName}, ";
         }
      }

      if (!string.IsNullOrEmpty(sqlFields))
      {
         if (sqlFields.Length > 2)
         {
            sqlFields = sqlFields.Remove(sqlFields.Length - 2);
         }
      }

      return sqlFields;
   }

   private bool IsAutoValueField(FieldInfo fieldInfo)
   {
      if (fieldInfo.ColumnName == "created_at" ||
                  fieldInfo.ColumnName == "updated_at")
      {
         return true;
      }

      return false;
   }

   protected string GetInsertValuesClause(List<FieldInfo> fieldInfos, Type type)
   {
      string parameters = string.Empty;

      foreach (FieldInfo fieldInfo in fieldInfos)
      {
         if (!fieldInfo.IsKey)
         {
            // skip auto values.
            if (IsAutoValueField(fieldInfo))
            {
               continue;
            }

            var pocoPropertyName = GetPocoPropertyName(type,
                                                       fieldInfo.ColumnName);
            if (null != pocoPropertyName)
            {
               parameters += $"@{pocoPropertyName}, ";
            }
         }
      }

      if (!string.IsNullOrEmpty(parameters))
      {
         if (parameters.Length > 2)
         {
            parameters = parameters.Remove(parameters.Length - 2);
         }
      }

      return parameters;
   }

   protected string GetStandardFieldsForUpdateClause(List<FieldInfo> fieldInfos,
                                                     Type type)
   {
      string sqlFields = string.Empty;

      foreach (FieldInfo fieldInfo in fieldInfos)
      {
         if (!fieldInfo.IsKey)
         {
            if (fieldInfo.ColumnName == "created_at" ||
                        fieldInfo.ColumnName == "updated_at")
            {
               continue;
            }
            var pocoPropertyName = GetPocoPropertyName(type, fieldInfo.ColumnName);
            if (null != pocoPropertyName)
            {
               sqlFields += $"{fieldInfo.ColumnName} = @{pocoPropertyName}, ";
            }
         }
      }

      if (!string.IsNullOrEmpty(sqlFields))
      {
         if (sqlFields.Length > 2)
         {
            sqlFields = sqlFields.Remove(sqlFields.Length - 2);
         }
      }

      return sqlFields;
   }

   protected string GetKeyFields(List<FieldInfo> fieldInfos,
                                 Type type)
   {
      string sqlFields = string.Empty;

      foreach (FieldInfo fieldInfo in fieldInfos)
      {
         if (fieldInfo.IsKey)
         {
            var pocoPropertyName = GetPocoPropertyName(type, fieldInfo.ColumnName);
            if (null != pocoPropertyName)
            {
               sqlFields += $"{fieldInfo.ColumnName} = @{pocoPropertyName}, ";
            }
         }
      }

      if (!string.IsNullOrEmpty(sqlFields))
      {
         if (sqlFields.Length > 2)
         {
            sqlFields = sqlFields.Remove(sqlFields.Length - 2);
         }
      }

      return sqlFields;
   }
}