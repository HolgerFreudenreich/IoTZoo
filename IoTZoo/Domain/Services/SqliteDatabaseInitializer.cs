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

using Domain.Pocos;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.RegularExpressions;

namespace Domain.Services;

public class SqliteDatabaseInitializer(IOptions<AppSettings> options, ILogger<SqliteDatabaseInitializer> logger)
{
   private static readonly Regex LineCommentRegex = new(@"--[^\r\n]*", RegexOptions.Compiled);

   private readonly AppSettings _appSettings = options.Value;

   public void Initialize()
   {
      if (_appSettings.ConfigurationDatabaseType != DatabaseType.Sqlite)
      {
         return;
      }

      InitializeDatabase(_appSettings.ConnectionStringSqlite, "CreateDatabaseIotZoo.txt");
      InitializeDatabase(_appSettings.ConnectionStringSqliteDatabaseTopicHistory, "CreateDatabaseTopicHistory.txt");
      InitializeDatabase(_appSettings.ConnectionStringSqliteDatabaseComponentManagement, "CreateDatabaseComponentManagement.txt");
      InitializeDatabase(_appSettings.ConnectionStringSqliteDatabaseScripting, "CreateDatabaseScripting.txt");
      InitializeDatabase(_appSettings.ConnectionStringSqliteDatabaseSettings, "CreateDatabaseSettings.txt");
   }

   public static string ToAbsoluteConnectionString(string connectionString)
   {
      SqliteConnectionStringBuilder builder = new(connectionString);
      builder.DataSource = Path.GetFullPath(builder.DataSource, AppContext.BaseDirectory);
      return builder.ConnectionString;
   }

   private void InitializeDatabase(string connectionString, string scriptFileName)
   {
      string scriptPath = Path.Combine(AppContext.BaseDirectory, "Database", "Scripts", scriptFileName);
      if (string.IsNullOrWhiteSpace(connectionString) || !File.Exists(scriptPath))
      {
         logger.LogWarning("Database initialization skipped, script or connection string missing: {Script}", scriptFileName);
         return;
      }

      string absoluteConnectionString = ToAbsoluteConnectionString(connectionString);
      Directory.CreateDirectory(Path.GetDirectoryName(new SqliteConnectionStringBuilder(absoluteConnectionString).DataSource)!);

      using SqliteConnection connection = new(absoluteConnectionString);
      connection.Open();

      using SqliteCommand existsCommand = connection.CreateCommand();
      existsCommand.CommandText = "select count(*) from sqlite_master where type = 'table' and name not like 'sqlite_%';";
      long tableCount = (long)(existsCommand.ExecuteScalar() ?? 0L);
      if (tableCount > 0)
      {
         return;
      }

      string script = LineCommentRegex.Replace(File.ReadAllText(scriptPath), string.Empty);
      using SqliteTransaction transaction = connection.BeginTransaction();
      foreach (string statement in script.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
      {
         using SqliteCommand command = connection.CreateCommand();
         command.Transaction = transaction;
         command.CommandText = statement + ";";
         command.ExecuteNonQuery();
      }
      transaction.Commit();

      logger.LogInformation("Database initialized from {Script}", scriptFileName);
   }
}
