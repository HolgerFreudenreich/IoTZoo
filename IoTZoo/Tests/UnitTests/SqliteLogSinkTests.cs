using IotZoo.Logging;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace UnitTests;

public class SqliteLogSinkTests : IDisposable
{
    private readonly string databasePath = Path.Combine(Path.GetTempPath(), $"iotzoo-log-{Guid.NewGuid():N}.db");

    private string ConnectionString => $"Data Source={databasePath}";

    [Fact]
    public async Task WrittenEntriesCanBeReadFilteredAndDeleted()
    {
        using (Logger logger = new LoggerConfiguration()
                   .MinimumLevel.Debug()
                   .WriteTo.SqliteLog(ConnectionString, retentionDays: 14)
                   .CreateLogger())
        {
            logger.Information("info 100%_done");
            logger.Error(new InvalidOperationException("boom"), "failed {Name}", "x");
        }

        LogEntryService service = CreateService();

        List<LogEntry> all = await service.Load(LogEventLevel.Debug, null, 100);
        Assert.Equal(2, all.Count);

        List<LogEntry> errors = await service.Load(LogEventLevel.Error, null, 100);
        LogEntry error = Assert.Single(errors);
        Assert.Equal("failed \"x\"", error.Message);
        Assert.Contains("boom", error.Exception);

        // % and _ must be matched literally
        Assert.Single(await service.Load(LogEventLevel.Debug, "100%_done", 100));
        Assert.Empty(await service.Load(LogEventLevel.Debug, "100x_done", 100));

        await service.DeleteAll();
        Assert.Empty(await service.Load(LogEventLevel.Debug, null, 100));
    }

    [Fact]
    public void OldEntriesArePurgedOnStartup()
    {
        using (Logger logger = new LoggerConfiguration().WriteTo.SqliteLog(ConnectionString, retentionDays: 1).CreateLogger())
        {
            logger.Warning("fresh");
        }
        using (SqliteConnection connection = new(ConnectionString))
        {
            connection.Open();
            using SqliteCommand command = connection.CreateCommand();
            command.CommandText = "INSERT INTO LogEntries (Timestamp, Level, Message) VALUES ($t, 3, 'old')";
            command.Parameters.AddWithValue("$t", DateTime.UtcNow.AddDays(-5).ToString("O"));
            command.ExecuteNonQuery();
        }

        _ = new SqliteLogSink(ConnectionString, retentionDays: 1);

        using SqliteConnection check = new(ConnectionString);
        check.Open();
        using SqliteCommand count = check.CreateCommand();
        count.CommandText = "SELECT COUNT(*) FROM LogEntries WHERE Message = 'old'";
        Assert.Equal(0L, count.ExecuteScalar());
    }

    [Fact]
    public async Task SinkCanBeConfiguredFromConfiguration()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Serilog:Using:0"] = "IotZoo",
                ["Serilog:MinimumLevel"] = "Warning",
                ["Serilog:WriteTo:0:Name"] = "SqliteLog",
                ["Serilog:WriteTo:0:Args:connectionString"] = ConnectionString,
                ["Serilog:WriteTo:0:Args:retentionDays"] = "3"
            })
            .Build();

        using (Logger logger = new LoggerConfiguration().ReadFrom.Configuration(configuration).CreateLogger())
        {
            logger.Information("ignored");
            logger.Warning("kept");
        }

        LogEntry entry = Assert.Single(await new LogEntryService(configuration).Load(LogEventLevel.Verbose, null, 10));
        Assert.Equal("kept", entry.Message);
    }

    [Fact]
    public void ServiceFindsConnectionStringInConfiguration()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Serilog:WriteTo:0:Name"] = "Console",
                ["Serilog:WriteTo:1:Name"] = "SqliteLog",
                ["Serilog:WriteTo:1:Args:connectionString"] = "Data Source=./x.db"
            })
            .Build();

        Assert.Equal("Data Source=./x.db", LogEntryService.FindConnectionString(configuration));
        Assert.Equal(SqliteLogSink.DefaultConnectionString,
                     LogEntryService.FindConnectionString(new ConfigurationBuilder().Build()));
    }

    private LogEntryService CreateService()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Serilog:WriteTo:0:Name"] = "SqliteLog",
                ["Serilog:WriteTo:0:Args:connectionString"] = ConnectionString
            })
            .Build();
        return new LogEntryService(configuration);
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        foreach (string file in new[] { databasePath, databasePath + "-wal", databasePath + "-shm" })
        {
            if (File.Exists(file))
            {
                File.Delete(file);
            }
        }
    }
}
