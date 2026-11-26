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

using DataAccess.Services;
using Domain.Pocos;
using Domain.Services;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace UnitTests;

public class MailSenderMigrationTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void InitializesSenderSettingsForNewAndExistingDatabases(bool existing, bool senderAlreadyExists)
    {
        string path = Path.GetTempFileName();
        string connectionString = new SqliteConnectionStringBuilder { DataSource = path, Pooling = false }.ToString();
        try
        {
            if (existing)
            {
                using var connection = new SqliteConnection(connectionString);
                connection.Open();
                using var command = connection.CreateCommand();
                command.CommandText = "create table setting (setting_id integer primary key, category text, setting_key text, setting_value text, unique(category, setting_key));" +
                    "create table db_version (db_version_id integer primary key, version_number integer);" +
                    "insert into db_version values (1, 3);" +
                    "create table db_migration (db_migration_id integer primary key, db_version_id integer, migration_script_name text, migration_date text);";
                command.ExecuteNonQuery();
                if (senderAlreadyExists)
                {
                    command.CommandText = "insert into setting(category, setting_key, setting_value) values ('Mail', 'MailSenderConfig', '{\"HostName\":\"existing.example.com\"}')";
                    command.ExecuteNonQuery();
                }
            }
            var options = Options.Create(new AppSettings { ConnectionStringSqliteDatabaseSettings = connectionString });
            var versionService = new DatabaseVersionService(options);
            new SqliteDatabaseInitializer(options, versionService, NullLogger<SqliteDatabaseInitializer>.Instance).Initialize();
            Assert.Equal(4, versionService.GetVersion(connectionString));
            using var check = new SqliteConnection(connectionString);
            check.Open();
            using var query = check.CreateCommand();
            query.CommandText = "select setting_value from setting where category = 'Mail' and setting_key = 'MailSenderConfig'";
            Assert.Equal(senderAlreadyExists ? "{\"HostName\":\"existing.example.com\"}" : "{}", query.ExecuteScalar());
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void MissingDbVersionIsAddedWithVersionZeroAndAllMigrationsAreApplied()
    {
        string path = Path.GetTempFileName();
        string connectionString = new SqliteConnectionStringBuilder { DataSource = path, Pooling = false }.ToString();
        try
        {
            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();
                using var command = connection.CreateCommand();
                command.CommandText = "create table setting (setting_id integer primary key, category text, setting_key text, setting_value text, unique(category, setting_key));";
                command.ExecuteNonQuery();
            }
            var options = Options.Create(new AppSettings { ConnectionStringSqliteDatabaseSettings = connectionString });
            var versionService = new DatabaseVersionService(options);
            Assert.Null(versionService.GetVersion(connectionString));

            new SqliteDatabaseInitializer(options, versionService, NullLogger<SqliteDatabaseInitializer>.Instance).Initialize();

            Assert.Equal(4, versionService.GetVersion(connectionString));
            using var check = new SqliteConnection(connectionString);
            check.Open();
            using var query = check.CreateCommand();
            query.CommandText = "select count(*) from db_migration";
            Assert.Equal(3L, query.ExecuteScalar());
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task MissingSenderSettingIsInsertedWithEncryptedPasswordAndCanBeUpdated()
    {
        string connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()),
            Mode = SqliteOpenMode.Memory,
            Cache = SqliteCacheMode.Shared,
            Pooling = false
        }.ToString();
        using var connection = new SqliteConnection(connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "create table setting (setting_id integer primary key, category text, setting_key text, setting_value text, unique(category, setting_key));";
        command.ExecuteNonQuery();
        var options = Options.Create(new AppSettings
        {
            ConnectionStringSqliteDatabaseSettings = connectionString,
            MqttPasswordEncryptionKey = Convert.ToBase64String(new byte[32])
        });
        using var settings = new TestSettingsService(options);
        var config = new MailSenderConfig { HostName = "smtp.example.com", Password = "secret" };
        Assert.Equal(1, await settings.Update(SettingCategory.Mail, SettingKey.MailSenderConfig, config));
        var stored = await settings.GetSetting("Mail", "MailSenderConfig");
        Assert.NotNull(stored);
        Assert.DoesNotContain("secret", stored.SettingValue);
        var loaded = Assert.IsType<MailSenderConfig>(await settings.GetObject(SettingCategory.Mail, SettingKey.MailSenderConfig));
        Assert.Equal("secret", loaded.Password);
        loaded.HostName = "updated.example.com";
        Assert.Equal(1, await settings.Update(SettingCategory.Mail, SettingKey.MailSenderConfig, loaded));
        Assert.Equal("updated.example.com", Assert.IsType<MailSenderConfig>(await settings.GetObject(SettingCategory.Mail, SettingKey.MailSenderConfig)).HostName);
    }

    [Fact]
    public async Task AuthenticationFailureDisablesOnlyMatchingSavedConfigurationWithoutReencryptingPassword()
    {
        string connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()),
            Mode = SqliteOpenMode.Memory,
            Cache = SqliteCacheMode.Shared,
            Pooling = false
        }.ToString();
        using var connection = new SqliteConnection(connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "create table setting (setting_id integer primary key, category text, setting_key text, setting_value text, unique(category, setting_key));";
        command.ExecuteNonQuery();
        var options = Options.Create(new AppSettings
        {
            ConnectionStringSqliteDatabaseSettings = connectionString,
            MqttPasswordEncryptionKey = Convert.ToBase64String(new byte[32])
        });
        using var settings = new TestSettingsService(options);
        var original = new MailSenderConfig { Enabled = true, HostName = "smtp.example.com", UserName = "user", Password = "secret", AttachmentDirectory = "files" };
        Assert.Equal(1, await settings.Update(SettingCategory.Mail, SettingKey.MailSenderConfig, original));
        var before = await settings.GetSetting("Mail", "MailSenderConfig");
        Assert.NotNull(before);
        string encryptedPassword = System.Text.Json.JsonDocument.Parse(before.SettingValue).RootElement.GetProperty("Password").GetString()!;
        var different = original.Copy();
        different.Password = "corrected";
        Assert.False(await settings.DisableMailSenderIfMatches(different));
        Assert.True(await settings.DisableMailSenderIfMatches(original));
        Assert.False(await settings.DisableMailSenderIfMatches(original));
        var after = await settings.GetSetting("Mail", "MailSenderConfig");
        Assert.NotNull(after);
        using var json = System.Text.Json.JsonDocument.Parse(after.SettingValue);
        Assert.Equal(encryptedPassword, json.RootElement.GetProperty("Password").GetString());
        var restored = Assert.IsType<MailSenderConfig>(await settings.GetObject(SettingCategory.Mail, SettingKey.MailSenderConfig));
        Assert.False(restored.Enabled);
        Assert.Equal("secret", restored.Password);
        Assert.Equal("files", restored.AttachmentDirectory);
        restored.Password = "corrected";
        restored.Enabled = true;
        Assert.Equal(1, await settings.Update(SettingCategory.Mail, SettingKey.MailSenderConfig, restored));
        Assert.False(await settings.DisableMailSenderIfMatches(original));
        Assert.True(Assert.IsType<MailSenderConfig>(await settings.GetObject(SettingCategory.Mail, SettingKey.MailSenderConfig)).Enabled);
    }

    private sealed class TestSettingsService(IOptions<AppSettings> options)
        : SettingsDatabaseService(options, NullLogger<DataServiceBase>.Instance), IDisposable
    {
        public void Dispose() => Db.Dispose();
    }
}
