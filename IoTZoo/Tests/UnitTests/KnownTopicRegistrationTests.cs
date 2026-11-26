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
using Domain.Interfaces;
using Domain.Interfaces.Timer;
using Domain.Pocos;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace UnitTests;

public class KnownTopicRegistrationTests
{
    [Fact]
    public async Task RegisterProjectDefaultKnownTopics_CreatesExceptionTopicWithHistoryPerProject()
    {
        using var database = new TestDatabase();
        var firstProject = new Project { ProjectName = "one" };
        var secondProject = new Project { ProjectName = "two" };

        await database.Service.RegisterProjectDefaultKnownTopics(firstProject);
        await database.Service.RegisterProjectDefaultKnownTopics(secondProject);

        var first = await database.Service.GetKnownTopicByTopicName("one", "Exception");
        var second = await database.Service.GetKnownTopicByTopicName("two", "Exception");
        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.Equal("zoo/one/Exception", first.FullQualifiedTopic);
        Assert.Equal("zoo/two/Exception", second.FullQualifiedTopic);
        Assert.True(first.KeepHistory);
        Assert.True(second.KeepHistory);
        Assert.Equal(MessageDirection.Inbound, first.MessageDirection);
        var parent = await database.Service.GetKnownTopicByTopicName("one", TopicConstants.INIT);
        Assert.NotNull(parent);
        Assert.Equal(parent.KnownTopicId, first.ParentKnownTopicId);
    }

    [Fact]
    public async Task RegisterProjectDefaultKnownTopics_RepeatedRegistrationPreservesExistingExceptionSettings()
    {
        using var database = new TestDatabase();
        var project = new Project { ProjectName = "one" };
        await database.Service.RegisterProjectDefaultKnownTopics(project);
        var original = await database.Service.GetKnownTopicByTopicName("one", "Exception");
        Assert.NotNull(original);
        original.KeepHistory = false;
        original.Description = "custom description";
        await database.Service.Save(original);

        await database.Service.RegisterProjectDefaultKnownTopics(project);

        var exceptions = (await database.Service.GetKnownTopicsByProjectName("one", null))
            .Where(topic => topic.Topic == "Exception").ToList();
        var existing = Assert.Single(exceptions);
        Assert.Equal(original.KnownTopicId, existing.KnownTopicId);
        Assert.False(existing.KeepHistory);
        Assert.Equal("custom description", existing.Description);
    }

    private sealed class TestDatabase : IDisposable
    {
        private readonly SqliteConnection connection;
        public TestKnownTopicsService Service { get; }

        public TestDatabase()
        {
            string connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()),
                Mode = SqliteOpenMode.Memory,
                Cache = SqliteCacheMode.Shared,
                Pooling = false
            }.ToString();
            connection = new SqliteConnection(connectionString);
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "create table known_topic (known_topic_id integer primary key autoincrement, " +
                "parent_known_topic_id integer, allow_delete boolean, allow_edit boolean, namespace_name text not null, " +
                "project_name text, topic text not null, description text, friendly_name text, keep_history boolean, " +
                "retained boolean, message_direction integer, last_payload text, sender text, payload_updated_at datetime, " +
                "unique(project_name, topic));";
            command.ExecuteNonQuery();
            var data = new Mock<IDataTransferService>();
            data.SetupGet(x => x.NamespaceName).Returns("zoo");
            Service = new TestKnownTopicsService(Options.Create(new AppSettings { ConnectionStringSqlite = connectionString }),
                Mock.Of<ICronCrudService>(), data.Object);
        }

        public void Dispose()
        {
            Service.Dispose();
            connection.Dispose();
        }
    }

    private sealed class TestKnownTopicsService(IOptions<AppSettings> options, ICronCrudService cron, IDataTransferService data)
        : KnownTopicsDatabaseService(options, NullLogger<KnownTopicsDatabaseService>.Instance, cron, data), IDisposable
    {
        public void Dispose() => Db.Dispose();
    }
}
