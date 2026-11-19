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

using Domain.Interfaces;
using Domain.Pocos;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;

namespace Domain.Services;

public class SqliteDatabaseInitializer(IOptions<AppSettings> options,
                                       IDatabaseVersionService databaseVersionService,
                                       ILogger<SqliteDatabaseInitializer> logger)
{
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
        if (tableCount == 0)
        {
            ExecuteScript(connection, scriptPath);
            logger.LogInformation("Database initialized from {Script}", scriptFileName);
        }

        ApplyMigrations(connectionString, scriptFileName);
    }

    private void ApplyMigrations(string connectionString, string scriptFileName)
    {
        int? currentVersion = databaseVersionService.GetVersion(connectionString);
        if (currentVersion is null)
        {
            // Database created before versioning was introduced: start with version 0, so that all migrations are applied.
            EnsureVersionTables(connectionString);
            logger.LogWarning("db_version was missing and has been added with version 0: {Script}", scriptFileName);
            currentVersion = databaseVersionService.GetVersion(connectionString);
            if (currentVersion is null)
            {
                logger.LogWarning("Database migrations skipped, db_version missing: {Script}", scriptFileName);
                return;
            }
        }

        IEnumerable<SqliteMigration> pendingMigrations = SqliteMigrationRegistry.Migrations
           .Where(migration => migration.DatabaseScriptName == scriptFileName && migration.DatabaseVersionId > currentVersion)
           .OrderBy(migration => migration.DatabaseVersionId);

        foreach (SqliteMigration migration in pendingMigrations)
        {
            string migrationScriptPath = Path.Combine(AppContext.BaseDirectory, "Database", "Scripts", migration.MigrationScriptName);
            using (SqliteConnection connection = new(ToAbsoluteConnectionString(connectionString)))
            {
                connection.Open();
                ExecuteScript(connection, migrationScriptPath);
            }

            databaseVersionService.AddMigration(connectionString, migration.DatabaseVersionId, migration.MigrationScriptName);
            logger.LogInformation("Database migrated to version {Version} with {Script}", migration.DatabaseVersionId, migration.MigrationScriptName);
        }
    }

    private static void EnsureVersionTables(string connectionString)
    {
        using SqliteConnection connection = new(ToAbsoluteConnectionString(connectionString));
        connection.Open();
        using SqliteTransaction transaction = connection.BeginTransaction();
        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            "create table if not exists db_version (db_version_id integer primary key, version_number integer not null);" +
            "insert or ignore into db_version (db_version_id, version_number) values (1, 0);" +
            "create table if not exists db_migration (" +
            "db_migration_id integer primary key autoincrement, " +
            "db_version_id integer not null, " +
            "migration_script_name character varying(200) not null, " +
            "migration_date datetime not null default current_timestamp, " +
            "constraint uk_db_migration unique (migration_script_name));";
        command.ExecuteNonQuery();
        transaction.Commit();
    }

    private static void ExecuteScript(SqliteConnection connection, string scriptPath)
    {
        using SqliteTransaction transaction = connection.BeginTransaction();
        foreach (string statement in SplitStatements(File.ReadAllText(scriptPath)))
        {
            string commandText = statement.Trim();
            if (commandText.Length == 0)
            {
                continue;
            }

            using SqliteCommand command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = commandText;
            command.ExecuteNonQuery();
        }
        transaction.Commit();
    }

    private static IEnumerable<string> SplitStatements(string script)
    {
        StringBuilder statement = new();
        bool inString = false;
        for (int i = 0; i < script.Length; i++)
        {
            char c = script[i];
            if (inString)
            {
                statement.Append(c);
                if (c == '\'')
                {
                    if (i + 1 < script.Length && script[i + 1] == '\'')
                    {
                        statement.Append(script[++i]);
                    }
                    else
                    {
                        inString = false;
                    }
                }
            }
            else if (c == '\'')
            {
                inString = true;
                statement.Append(c);
            }
            else if (c == '-' && i + 1 < script.Length && script[i + 1] == '-')
            {
                while (i + 1 < script.Length && script[i + 1] != '\n' && script[i + 1] != '\r')
                {
                    i++;
                }
            }
            else if (c == ';')
            {
                yield return statement.ToString();
                statement.Clear();
            }
            else
            {
                statement.Append(c);
            }
        }

        yield return statement.ToString();
    }
}
