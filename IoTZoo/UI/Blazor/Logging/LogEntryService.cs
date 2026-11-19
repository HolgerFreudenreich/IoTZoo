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
using Serilog.Events;

namespace IotZoo.Logging;

public sealed record LogEntry(long Id,
                              DateTime Timestamp,
                              LogEventLevel Level,
                              string Message,
                              string? Exception,
                              string? SourceContext);

public interface ILogEntryService
{
    Task<List<LogEntry>> Load(LogEventLevel minimumLevel, string? searchText, int maxRows);

    Task DeleteAll();
}

/// <summary>
/// Reads the entries written by <see cref="SqliteLogSink"/>. The database is located
/// via the "SqliteLog" entry in the Serilog:WriteTo section of the configuration.
/// </summary>
public sealed class LogEntryService : ILogEntryService
{
    private readonly string connectionString;

    public LogEntryService(IConfiguration configuration)
    {
        connectionString = FindConnectionString(configuration);
    }

    public static string FindConnectionString(IConfiguration configuration)
    {
        foreach (IConfigurationSection sink in configuration.GetSection("Serilog:WriteTo").GetChildren())
        {
            if (string.Equals(sink["Name"], "SqliteLog", StringComparison.OrdinalIgnoreCase))
            {
                string? configured = sink["Args:connectionString"];
                return string.IsNullOrWhiteSpace(configured) ? SqliteLogSink.DefaultConnectionString : configured;
            }
        }
        return SqliteLogSink.DefaultConnectionString;
    }

    public async Task<List<LogEntry>> Load(LogEventLevel minimumLevel, string? searchText, int maxRows)
    {
        List<LogEntry> entries = new();
        if (!DatabaseExists())
        {
            return entries;
        }

        await using SqliteConnection connection = new(connectionString);
        await connection.OpenAsync();
        await using SqliteCommand command = connection.CreateCommand();
        string sql = "SELECT Id, Timestamp, Level, Message, Exception, SourceContext FROM LogEntries WHERE Level >= $level";
        command.Parameters.AddWithValue("$level", (int)minimumLevel);
        if (!string.IsNullOrWhiteSpace(searchText))
        {
            sql += " AND (Message LIKE $search ESCAPE '\\' OR Exception LIKE $search ESCAPE '\\' OR SourceContext LIKE $search ESCAPE '\\')";
            command.Parameters.AddWithValue("$search", "%" + EscapeLike(searchText.Trim()) + "%");
        }
        sql += " ORDER BY Timestamp DESC, Id DESC LIMIT $limit";
        command.Parameters.AddWithValue("$limit", Math.Max(1, maxRows));
        command.CommandText = sql;

        await using SqliteDataReader reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            DateTime timestamp = DateTime.Parse(reader.GetString(1),
                                                System.Globalization.CultureInfo.InvariantCulture,
                                                System.Globalization.DateTimeStyles.RoundtripKind).ToLocalTime();
            entries.Add(new LogEntry(reader.GetInt64(0),
                                     timestamp,
                                     (LogEventLevel)reader.GetInt32(2),
                                     reader.GetString(3),
                                     reader.IsDBNull(4) ? null : reader.GetString(4),
                                     reader.IsDBNull(5) ? null : reader.GetString(5)));
        }
        return entries;
    }

    public async Task DeleteAll()
    {
        if (!DatabaseExists())
        {
            return;
        }
        await using SqliteConnection connection = new(connectionString);
        await connection.OpenAsync();
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "DELETE FROM LogEntries";
        await command.ExecuteNonQueryAsync();
    }

    private bool DatabaseExists()
    {
        SqliteConnectionStringBuilder builder = new(connectionString);
        return File.Exists(builder.DataSource);
    }

    private static string EscapeLike(string text)
    {
        return text.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
    }
}
