// --------------------------------------------------------------------------------------------------------------------
//      ____    ______   _____
//     /  _/___/_  __/  /__  / ____  ____
//     / // __ \/ /       / / / __ \/ __ \
//   _/ // /_/ / /       / /_/ /_/ / /_/ /
//  /___/\____/_/       /____|____/\____/ 
// --------------------------------------------------------------------------------------------------------------------
// Connect «Things» with microcontrollers without programming knowledge.
// MIT License
// --------------------------------------------------------------------------------------------------------------------
// (c) 2025 - 2026 Holger Freudenreich under MIT license
// --------------------------------------------------------------------------------------------------------------------

using Microsoft.Data.Sqlite;
using Serilog.Core;
using Serilog.Debugging;
using Serilog.Events;

namespace IotZoo.Logging;

/// <summary>
/// Serilog sink that stores log events in a SQLite database. Events are written in batches.
/// The sink never logs through ILogger; failures are reported to Serilog's SelfLog.
/// </summary>
public sealed class SqliteLogSink : IBatchedLogEventSink
{
    public const string DefaultConnectionString = "Data Source=./Database/Logs.db";
    public const string TimestampFormat = "O";

    private static readonly TimeSpan PurgeInterval = TimeSpan.FromHours(1);

    private readonly string connectionString;
    private readonly int retentionDays;
    private DateTime lastPurgeUtc = DateTime.MinValue;

    public SqliteLogSink(string connectionString, int retentionDays)
    {
        this.connectionString = connectionString;
        this.retentionDays = retentionDays;
        try
        {
            CreateDatabase();
        }
        catch (Exception ex)
        {
            SelfLog.WriteLine("SqliteLogSink: unable to initialize database: {0}", ex);
        }
    }

    public Task EmitBatchAsync(IReadOnlyCollection<LogEvent> batch)
    {
        try
        {
            using SqliteConnection connection = new(connectionString);
            connection.Open();
            using SqliteTransaction transaction = connection.BeginTransaction();
            using SqliteCommand command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText =
                "INSERT INTO LogEntries (Timestamp, Level, Message, Exception, SourceContext) " +
                "VALUES ($timestamp, $level, $message, $exception, $sourceContext)";
            SqliteParameter timestamp = command.Parameters.Add("$timestamp", SqliteType.Text);
            SqliteParameter level = command.Parameters.Add("$level", SqliteType.Integer);
            SqliteParameter message = command.Parameters.Add("$message", SqliteType.Text);
            SqliteParameter exception = command.Parameters.Add("$exception", SqliteType.Text);
            SqliteParameter sourceContext = command.Parameters.Add("$sourceContext", SqliteType.Text);

            foreach (LogEvent logEvent in batch)
            {
                timestamp.Value = logEvent.Timestamp.UtcDateTime.ToString(TimestampFormat);
                level.Value = (int)logEvent.Level;
                message.Value = logEvent.RenderMessage();
                exception.Value = (object?)logEvent.Exception?.ToString() ?? DBNull.Value;
                sourceContext.Value = logEvent.Properties.TryGetValue(Constants.SourceContextPropertyName, out LogEventPropertyValue? value)
                    && value is ScalarValue { Value: string text }
                        ? text
                        : DBNull.Value;
                command.ExecuteNonQuery();
            }
            transaction.Commit();

            PurgeIfDue(connection);
        }
        catch (Exception ex)
        {
            SelfLog.WriteLine("SqliteLogSink: unable to write log events: {0}", ex);
        }
        return Task.CompletedTask;
    }

    public Task OnEmptyBatchAsync()
    {
        return Task.CompletedTask;
    }

    private void CreateDatabase()
    {
        SqliteConnectionStringBuilder builder = new(connectionString);
        string? directory = Path.GetDirectoryName(Path.GetFullPath(builder.DataSource));
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using SqliteConnection connection = new(connectionString);
        connection.Open();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText =
            "PRAGMA journal_mode=WAL;" +
            "CREATE TABLE IF NOT EXISTS LogEntries (" +
            "Id INTEGER PRIMARY KEY AUTOINCREMENT, " +
            "Timestamp TEXT NOT NULL, " +
            "Level INTEGER NOT NULL, " +
            "Message TEXT NOT NULL, " +
            "Exception TEXT NULL, " +
            "SourceContext TEXT NULL);" +
            "CREATE INDEX IF NOT EXISTS IX_LogEntries_Timestamp ON LogEntries (Timestamp);" +
            "CREATE INDEX IF NOT EXISTS IX_LogEntries_Level ON LogEntries (Level);";
        command.ExecuteNonQuery();
        Purge(connection);
    }

    private void PurgeIfDue(SqliteConnection connection)
    {
        if (DateTime.UtcNow - lastPurgeUtc >= PurgeInterval)
        {
            Purge(connection);
        }
    }

    private void Purge(SqliteConnection connection)
    {
        lastPurgeUtc = DateTime.UtcNow;
        if (retentionDays <= 0)
        {
            return;
        }
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "DELETE FROM LogEntries WHERE Timestamp < $limit";
        command.Parameters.AddWithValue("$limit", DateTime.UtcNow.AddDays(-retentionDays).ToString(TimestampFormat));
        command.ExecuteNonQuery();
    }
}
