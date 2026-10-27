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
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Reflection;

namespace Domain.Services;

public class TopicHistoryDatabaseService : DataServiceBase, ITopicHistoryCrudService
{
    public TopicHistoryDatabaseService(IOptions<AppSettings> options,
                                       ILogger<DataServiceBase> logger) : base(options,
                                                                               logger)
    {
        Initialize(typeof(TopicHistory), "th", "topic_history");
    }

    public async Task Insert(TopicHistory topicHistory)
    {
        try
        {
            int rowsProcessed = await Db.ExecuteAsync(InsertSql, topicHistory);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex,
                            $"{MethodBase.GetCurrentMethod()} failed!");
        }
    }

    public async Task<List<TopicHistory>> LoadTopicHistory(Project? project)
    {
        IEnumerable<TopicHistory> data = null!;
        try
        {
            if (project == null)
            {
                data =
                  await Db.QueryAsync<TopicHistory>($"select {FieldListSelect} from {FullQualifiedTableName} order by topic_history_id desc;");
            }
            else
            {
                data = await Db.QueryAsync<TopicHistory>($"select {FieldListSelect} from {FullQualifiedTableName} where project_name = @ProjectName order by topic_history_id desc;",
                                                         new { project.ProjectName });
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, $"{MethodBase.GetCurrentMethod()} failed!");
        }

        return data.AsList();
    }

    public async Task DeleteAll()
    {
        try
        {
            var transaction = Db.BeginTransaction();
            int rowsProcessed = await Db.ExecuteAsync("delete from topic_history;"); // SQLite does not support TRUNCATE TABLE.
            await Db.ExecuteAsync("delete from sqlite_sequence where name = 'topic_history';"); // reset the sequence for the id.
            transaction.Commit();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, $"{MethodBase.GetCurrentMethod()} failed!");
        }
    }

    public async Task DeleteTopicHistoryEntry(TopicHistory topicHistoryEntry)
    {
        try
        {
            int rowsProcessed = await Db.ExecuteAsync("delete from topic_history where topic_history_id = @Topic;", new { Topic = topicHistoryEntry.TopicHistoryId });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, $"{MethodBase.GetCurrentMethod()} failed!");
        }
    }

    public async Task DeleteTopicHistoryEntries(string? topic, DateTime from, DateTime to)
    {
        try
        {
            if (string.IsNullOrEmpty(topic))
            {
                int rowsProcessed = await Db.ExecuteAsync("delete from topic_history where date_received >= @From and date_received <= @To;", new { From = from, To = to });
            }
            else
            {
                int rowsProcessed = await Db.ExecuteAsync("delete from topic_history where topic = @Topic and date_received >= @From and date_received <= @To;", new { Topic = topic, From = from, To = to });
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, $"{MethodBase.GetCurrentMethod()} failed!");
        }
    }
}