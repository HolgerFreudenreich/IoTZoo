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
using Microsoft.Extensions.Options;

namespace Domain.Services;

public class DatabaseVersionService(IOptions<AppSettings> options) : IDatabaseVersionService
{
    private readonly AppSettings appSettings = options.Value;

    public int? GetVersion(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return null;
        }

        string absoluteConnectionString = SqliteDatabaseInitializer.ToAbsoluteConnectionString(connectionString);
        if (!File.Exists(new SqliteConnectionStringBuilder(absoluteConnectionString).DataSource))
        {
            return null;
        }

        using SqliteConnection connection = new(absoluteConnectionString);
        connection.Open();

        using SqliteCommand existsCommand = connection.CreateCommand();
        existsCommand.CommandText = "select count(*) from sqlite_master where type = 'table' and name = 'db_version';";
        if ((long)(existsCommand.ExecuteScalar() ?? 0L) == 0L)
        {
            return null;
        }

        using SqliteCommand versionCommand = connection.CreateCommand();
        versionCommand.CommandText = "select version_number from db_version where db_version_id = 1;";
        object? result = versionCommand.ExecuteScalar();
        return result is null or DBNull ? null : Convert.ToInt32(result);
    }

    public IReadOnlyDictionary<string, int?> GetAllVersions()
    {
        return new Dictionary<string, int?>
        {
            ["IotZoo"] = GetVersion(appSettings.ConnectionStringSqlite),
            ["TopicHistory"] = GetVersion(appSettings.ConnectionStringSqliteDatabaseTopicHistory),
            ["ComponentManagement"] = GetVersion(appSettings.ConnectionStringSqliteDatabaseComponentManagement),
            ["Scripting"] = GetVersion(appSettings.ConnectionStringSqliteDatabaseScripting),
            ["Settings"] = GetVersion(appSettings.ConnectionStringSqliteDatabaseSettings),
        };
    }

    public IReadOnlyList<DatabaseMigration> GetMigrations(string connectionString)
    {
        List<DatabaseMigration> migrations = [];
        if (string.IsNullOrWhiteSpace(connectionString) || !File.Exists(new SqliteConnectionStringBuilder(ToAbsolute(connectionString)).DataSource))
        {
            return migrations;
        }

        using SqliteConnection connection = new(ToAbsolute(connectionString));
        connection.Open();

        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "select db_migration_id, db_version_id, migration_script_name, migration_date from db_migration where migration_date is null order by db_migration_id;";
        using SqliteDataReader reader = command.ExecuteReader();
        while (reader.Read())
        {
            migrations.Add(new DatabaseMigration
            {
                DatabaseMigrationId = reader.GetInt64(0),
                DatabaseVersionId = reader.GetInt32(1),
                MigrationScriptName = reader.GetString(2),
                MigrationDate = reader.GetDateTime(3),
            });
        }

        return migrations;
    }

    public void AddMigration(string connectionString, int databaseVersionId, string migrationScriptName)
    {
        using SqliteConnection connection = new(ToAbsolute(connectionString));
        connection.Open();

        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "insert into db_migration (db_version_id, migration_script_name) values (@DatabaseVersionId, @MigrationScriptName);";
        command.Parameters.AddWithValue("@DatabaseVersionId", databaseVersionId);
        command.Parameters.AddWithValue("@MigrationScriptName", migrationScriptName);
        command.ExecuteNonQuery();
    }

    private static string ToAbsolute(string connectionString)
    {
        return SqliteDatabaseInitializer.ToAbsoluteConnectionString(connectionString);
    }
}
