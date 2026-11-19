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

using Serilog;
using Serilog.Configuration;
using Serilog.Events;

namespace IotZoo.Logging;

public static class SqliteLogSinkExtensions
{
    /// <summary>
    /// Writes log events to a SQLite database. Can be configured in appsettings.json with
    /// { "Name": "SqliteLog", "Args": { "connectionString": "...", "retentionDays": 14 } }
    /// (add "IotZoo" to Serilog:Using).
    /// </summary>
    /// <param name="retentionDays">Entries older than this are deleted. 0 or less keeps everything.</param>
    public static LoggerConfiguration SqliteLog(this LoggerSinkConfiguration sinkConfiguration,
                                                string connectionString = SqliteLogSink.DefaultConnectionString,
                                                int retentionDays = 14,
                                                LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum)
    {
        ArgumentNullException.ThrowIfNull(sinkConfiguration);
        BatchingOptions options = new()
        {
            BatchSizeLimit = 100,
            BufferingTimeLimit = TimeSpan.FromSeconds(2),
            EagerlyEmitFirstEvent = true
        };
        return sinkConfiguration.Sink(new SqliteLogSink(connectionString, retentionDays), options, restrictedToMinimumLevel);
    }
}
