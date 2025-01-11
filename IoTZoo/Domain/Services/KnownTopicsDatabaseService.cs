// --------------------------------------------------------------------------------------------------------------------
//      ____    ______   _____
//     /  _/___/_  __/  /__  / ____  ____
//     / // __ \/ /       / / / __ \/ __ \
//   _/ // /_/ / /       / /_/ /_/ / /_/ /
//  /___/\____/_/       /____|____/\____/ 
// --------------------------------------------------------------------------------------------------------------------
// (c) 2025 Holger Freudenreich under MIT license
// --------------------------------------------------------------------------------------------------------------------
// Known topics are stored in a table. You can save the history of known topics if you set the property
// KnownTopic.Persist to true.
// --------------------------------------------------------------------------------------------------------------------

using Dapper;
using Domain.Interfaces;
using Domain.Interfaces.Crud;
using Domain.Interfaces.Timer;
using Domain.Pocos;
using Domain.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Reflection;

namespace DataAccess.Services;

public class KnownTopicsDatabaseService : DataServiceBase, IKnownTopicsCrudService
{
   public ICronCrudService CronCrudService { get; }

   protected IDataTransferService DataTransferService { get; set; }

   public KnownTopicsDatabaseService(IOptions<AppSettings> options,
                                     ILogger<KnownTopicsDatabaseService> logger,
                                     ICronCrudService cronCrudService,
                                     IDataTransferService dataTransferService) : base(options,
                                                                                      logger)
   {
      Initialize(typeof(KnownTopic),
                 "cfg",
                 "known_topic");
      CronCrudService = cronCrudService;
      DataTransferService = dataTransferService;
   }

   public async Task<bool> ExistsByTopicName(string topicName)
   {
      string sql = $"select count(*) from {FullQualifiedTableName} where topic = @Topic;";
      int count = await Db.QuerySingleAsync<int>(sql,
                                                 new { Topic = topicName });
      return count > 0;
   }

   public async Task<SaveResult> Save(KnownTopic knownTopic, bool allowUpdate = true)
   {
      SaveResult saveResult = SaveResult.NothingDone;
      if (knownTopic.KnownTopicId > 0 && allowUpdate)
      {
         // Update
         if (await Update(knownTopic))
         {
            saveResult = SaveResult.Updated;
         }
      }
      else
      {
         // Since the Device does not know the KnownTopicId we have to check if the record already exists.
         var existing = await GetKnownTopicByTopicName(knownTopic.ProjectName, knownTopic.Topic);
         if (existing == null)
         {
            knownTopic.KnownTopicId = await Insert(knownTopic);
            if (knownTopic.KnownTopicId > 0)
            {
               saveResult = SaveResult.Inserted;
            }
         }
         else
         {
            knownTopic.KnownTopicId = existing.KnownTopicId;
            if (allowUpdate)
            {
               if (await Update(knownTopic))
               {
                  saveResult = SaveResult.Updated;
               }
            }
         }
      }
      return saveResult;
   }

   public async Task<int> Insert(KnownTopic knownTopic)
   {
      int createdKnownTopicId = -1;
      try
      {
         createdKnownTopicId = await Db.QuerySingleOrDefaultAsync<int>(InsertSql,
                                                                       knownTopic);
      }
      catch (Exception ex)
      {
         Logger.LogError(ex, $"{MethodBase.GetCurrentMethod()} failed!");
         throw;
      }
      return createdKnownTopicId;
   }

   protected async Task<bool> Update(KnownTopic knownTopic)
   {
      try
      {
         int rows = await Db.ExecuteAsync(UpdateSql, knownTopic);
         return rows == 1;
      }
      catch (Exception ex)
      {
         Logger.LogError(ex, $"{MethodBase.GetCurrentMethod()} failed!");
         throw;
      }
   }

   public async Task Delete(KnownTopic knownTopic)
   {
      try
      {
         string sql = $"delete from {this.FullQualifiedTableName} where topic = @Topic;";
         int rowsProcessed = await Db.ExecuteAsync(sql, new { Topic = knownTopic.Topic });
      }
      catch (Exception ex)
      {
         Logger.LogError(ex, $"{MethodBase.GetCurrentMethod()} failed!");
         throw;
      }
   }

   public async Task<KnownTopic?> GetKnownTopicByTopicName(string projectName, string topic)
   {
      KnownTopic? knownTopic = null;

      try
      {
         if (!string.IsNullOrEmpty(projectName))
         {
            knownTopic = await Db.QueryFirstOrDefaultAsync<KnownTopic?>($"select {FieldListSelect} from {FullQualifiedTableName} where project_name = @ProjectName and topic = @Topic;",
                                                                      new { ProjectName = projectName, Topic = topic });
         }
         else
         {
            knownTopic = await Db.QueryFirstOrDefaultAsync<KnownTopic?>($"select {FieldListSelect} from {FullQualifiedTableName} where topic = @Topic;",
                                                                        new { Topic = topic });
         }
      }
      catch (Exception exception)
      {
         Logger.LogError(exception, $"{MethodBase.GetCurrentMethod()} failed!");
         throw;
      }
      return knownTopic;
   }

   public async Task<List<KnownTopic>> GetKnownTopics()
   {
      IEnumerable<KnownTopic> result = null!;
      try
      {
         result = await Db.QueryAsync<KnownTopic>($"select {FieldListSelect} from {FullQualifiedTableName} order by topic;");
      }
      catch (Exception exception)
      {
         Logger.LogError(exception, $"{MethodBase.GetCurrentMethod()} failed!");
         throw;
      }
      return result.AsList();
   }

   public async Task<List<KnownTopic>> GetKnownTopicsByProjectName(string? projectName, List<MessageDirection>? messageDirections)
   {
      if (string.IsNullOrEmpty(projectName))
      {
         return await GetKnownTopics();
      }
      IEnumerable<KnownTopic> result = null!;
      try
      {
         string sql = $"select {FieldListSelect} from {FullQualifiedTableName} where project_name = @ProjectName ";
         if (null != messageDirections && messageDirections.Any())
         {
            sql += $"and message_direction in (";
            foreach (var direction in messageDirections)
            {
               sql += (int)direction;
               sql += ", ";
            }
            sql = sql.Remove(sql.Length - 2);
            sql += ") ";
         }

         sql += "order by topic;";
         result = await Db.QueryAsync<KnownTopic>(sql,
                                                  new { ProjectName = projectName });
      }
      catch (Exception exception)
      {
         Logger.LogError(exception, $"{MethodBase.GetCurrentMethod()} failed!");
         throw;
      }
      return result.AsList();
   }

   private async Task RegisterProjectKnownTopicAndCreateCronJob(int? parentKnownTopicId,
                                                                Project project,
                                                                CronJob cronJob,
                                                                string description)
   {
      try
      {
         if (await Save(new KnownTopic
                        {
                           ParentKnownTopicId = parentKnownTopicId,
                           NamespaceName = DataTransferService.NamespaceName,
                           ProjectName = project.ProjectName,
                           Topic = cronJob.Topic,
                           Description = description,
                           MessageDirection = MessageDirection.Inbound
                        },
                        allowUpdate: false) == SaveResult.Inserted)
         {
            cronJob.NamespaceName = DataTransferService.NamespaceName;
            cronJob.ProjectName = project.ProjectName;
            await CronCrudService.Insert(cronJob);
         }
      }
      catch (Exception exception)
      {
         Logger.LogError(exception, $"{MethodBase.GetCurrentMethod()} failed!");
      }
   }

   public async Task RegisterProjectDefaultKnownTopics(Project project)
   {
      var parentKnownTopic = new KnownTopic
      {
         NamespaceName = DataTransferService.NamespaceName,
         ProjectName = project.ProjectName,
         MessageDirection = MessageDirection.Internal,
         Topic = TopicConstants.INIT,
         Description = $"topic to setup your rules for the project '{project.ProjectName}'. It is triggert when the IotZoo-Client is starting."
      };

      await Save(parentKnownTopic);

      // CRON
      await RegisterProjectKnownTopicAndCreateCronJob(parentKnownTopic.KnownTopicId, project,
                                                      new CronJob(CronExpression.FromString("* * * ? * *"), $"{TopicConstants.TIMER}/{TopicConstants.EVERY_SECOND}", true),
                                                                  "Fires every second.");
      await RegisterProjectKnownTopicAndCreateCronJob(parentKnownTopic.KnownTopicId, project,
                                                      new CronJob(CronExpression.FromString("*/5 * * ? * *"), $"{TopicConstants.TIMER}/{TopicConstants.EVERY_05_SECONDS}", true),
                                                                  "Fires every 5 seconds.");
      await RegisterProjectKnownTopicAndCreateCronJob(parentKnownTopic.KnownTopicId, project,
                                                      new CronJob(CronExpression.FromString("*/10 * * ? * *"), $"{TopicConstants.TIMER}/{TopicConstants.EVERY_10_SECONDS}", true),
                                                                  "Fires every 10 seconds.");
      await RegisterProjectKnownTopicAndCreateCronJob(parentKnownTopic.KnownTopicId, project,
                                                      new CronJob(CronExpression.FromString("*/15 * * ? * *"), $"{TopicConstants.TIMER}/{TopicConstants.EVERY_15_SECONDS}", true),
                                                                  "Fires every 15 seconds.");
      await RegisterProjectKnownTopicAndCreateCronJob(parentKnownTopic.KnownTopicId, project,
                                                      new CronJob(CronExpression.FromString("*/30 * * ? * *"), $"{TopicConstants.TIMER}/{TopicConstants.EVERY_30_SECONDS}", true),
                                                                  "Fires every 30 seconds.");
      await RegisterProjectKnownTopicAndCreateCronJob(parentKnownTopic.KnownTopicId, project,
                                                      new CronJob(CronExpression.FromString("0 */1 * ? * *"), $"{TopicConstants.TIMER}/{TopicConstants.EVERY_MINUTE}", true),
                                                                  "Fires every minute.");
      await RegisterProjectKnownTopicAndCreateCronJob(parentKnownTopic.KnownTopicId, project,
                                                      new CronJob(CronExpression.FromString("0 */15 * ? * *"), $"{TopicConstants.TIMER}/{TopicConstants.EVERY_15_MINUTES}", true),
                                                                  "Fires every 15 minutes.");
      await RegisterProjectKnownTopicAndCreateCronJob(parentKnownTopic.KnownTopicId, project,
                                                      new CronJob(CronExpression.FromString("0 */30 * ? * *"), $"{TopicConstants.TIMER}/{TopicConstants.EVERY_30_MINUTES}", true),
                                                                  "Fires every 30 minutes.");
      await RegisterProjectKnownTopicAndCreateCronJob(parentKnownTopic.KnownTopicId, project,
                                                      new CronJob(CronExpression.FromString("0 0 * ? * *"), $"{TopicConstants.TIMER}/{TopicConstants.EVERY_HOUR}", true),
                                                                  "Fires every hour.");
      // Sunrise and sunset
      await Save(new KnownTopic
      {
         ParentKnownTopicId = parentKnownTopic.KnownTopicId,
         NamespaceName = DataTransferService.NamespaceName,
         ProjectName = project.ProjectName,
         Topic = $"{TopicConstants.SUNRISE_NOW}",
         Description = "When the sun rises...",
         MessageDirection = MessageDirection.Inbound,
         KeepHistory = true
      }, allowUpdate: false);
      await Save(new KnownTopic
      {
         ParentKnownTopicId = parentKnownTopic.KnownTopicId,
         NamespaceName = DataTransferService.NamespaceName,
         ProjectName = project.ProjectName,
         Topic = $"{TopicConstants.SUNSET_NOW}",
         Description = "When the sun goes down...",
         MessageDirection = MessageDirection.Inbound,
         KeepHistory = true
      }, allowUpdate: false);
      await Save(new KnownTopic
      {
         ParentKnownTopicId = parentKnownTopic.KnownTopicId,
         NamespaceName = DataTransferService.NamespaceName,
         ProjectName = project.ProjectName,
         Topic = $"{TopicConstants.IS_DAY_MODE}",
         Description = "1 = day, 0 = night",
         MessageDirection = MessageDirection.Inbound
      }, allowUpdate: false);
      // Philps Hue
      await Save(new KnownTopic
      {
         ParentKnownTopicId = parentKnownTopic.KnownTopicId,
         NamespaceName = DataTransferService.NamespaceName,
         ProjectName = project.ProjectName,
         Topic = $"{TopicConstants.HUE}/{TopicConstants.LIGHT}/{TopicConstants.COLOR}/{TopicConstants.GREEN}",
         MessageDirection = MessageDirection.Outbound,
         Description = "Color change to green. Payload: <LightId>."
      }, allowUpdate: false);
      await Save(new KnownTopic
      {
         ParentKnownTopicId = parentKnownTopic.KnownTopicId,
         NamespaceName = DataTransferService.NamespaceName,
         ProjectName = project.ProjectName,
         Topic = $"{TopicConstants.HUE}/{TopicConstants.LIGHT}/{TopicConstants.COLOR}/{TopicConstants.YELLOW}",
         MessageDirection = MessageDirection.Outbound,
         Description = "Color change to yellow. Payload: <LightId>."
      }, allowUpdate: false);
      await Save(new KnownTopic
      {
         ParentKnownTopicId = parentKnownTopic.KnownTopicId,
         NamespaceName = DataTransferService.NamespaceName,
         ProjectName = project.ProjectName,
         Topic = $"{TopicConstants.HUE}/{TopicConstants.LIGHT}/{TopicConstants.COLOR}/{TopicConstants.ORANGE}",
         MessageDirection = MessageDirection.Outbound,
         Description = "Color change to orange. Payload: <LightId>."
      }, allowUpdate: false);
      await Save(new KnownTopic
      {
         ParentKnownTopicId = parentKnownTopic.KnownTopicId,
         NamespaceName = DataTransferService.NamespaceName,
         ProjectName = project.ProjectName,
         Topic = $"{TopicConstants.HUE}/{TopicConstants.LIGHT}/{TopicConstants.COLOR}/{TopicConstants.RED}",
         MessageDirection = MessageDirection.Outbound,
         Description = "Color change to red. Payload: <LightId>."
      }, allowUpdate: false);
      await Save(new KnownTopic
      {
         ParentKnownTopicId = parentKnownTopic.KnownTopicId,
         NamespaceName = DataTransferService.NamespaceName,
         ProjectName = project.ProjectName,
         Topic = $"{TopicConstants.HUE}/{TopicConstants.LIGHT}/{TopicConstants.COLOR}/{TopicConstants.BLUE}",
         MessageDirection = MessageDirection.Outbound,
         Description = "Color change to blue. Payload: <LightId>."
      }, allowUpdate: false);
      await Save(new KnownTopic
      {
         ParentKnownTopicId = parentKnownTopic.KnownTopicId,
         NamespaceName = DataTransferService.NamespaceName,
         ProjectName = project.ProjectName,
         Topic = $"{TopicConstants.HUE}/{TopicConstants.LIGHT}/{TopicConstants.COLOR}/{TopicConstants.PURPLE}",
         MessageDirection = MessageDirection.Outbound,
         Description = "Color change to purple. Payload: <LightId>."
      }, allowUpdate: false);
      await Save(new KnownTopic
      {
         ParentKnownTopicId = parentKnownTopic.KnownTopicId,
         NamespaceName = DataTransferService.NamespaceName,
         ProjectName = project.ProjectName,
         Topic = $"{TopicConstants.HUE}/{TopicConstants.LIGHT}/{TopicConstants.COLOR}/{TopicConstants.GOLD}",
         MessageDirection = MessageDirection.Outbound,
         Description = "Color change to gold. Payload: <LightId>."
      }, allowUpdate: false);
      await Save(new KnownTopic
      {
         ParentKnownTopicId = parentKnownTopic.KnownTopicId,
         NamespaceName = DataTransferService.NamespaceName,
         ProjectName = project.ProjectName,
         Topic = $"{TopicConstants.HUE}/{TopicConstants.LIGHT}/{TopicConstants.COLOR}/{TopicConstants.WHITE_COLD}",
         MessageDirection = MessageDirection.Outbound,
         Description = "Color change to cold white. Payload: <LightId>."
      }, allowUpdate: false);
      await Save(new KnownTopic
      {
         ParentKnownTopicId = parentKnownTopic.KnownTopicId,
         NamespaceName = DataTransferService.NamespaceName,
         ProjectName = project.ProjectName,
         Topic = $"{TopicConstants.HUE}/{TopicConstants.LIGHT}/{TopicConstants.COLOR}/{TopicConstants.WHITE_WARM}",
         MessageDirection = MessageDirection.Outbound,
         Description = "Color change to warm white. Payload: <LightId>."
      }, allowUpdate: false);
      await Save(new KnownTopic
      {
         ParentKnownTopicId = parentKnownTopic.KnownTopicId,
         NamespaceName = DataTransferService.NamespaceName,
         ProjectName = project.ProjectName,
         Topic = $"{TopicConstants.HUE}/{TopicConstants.LIGHT}/{TopicConstants.ON}",
         MessageDirection = MessageDirection.Outbound,
         Description = "Turns the light on. Payload: <LightId>."
      }, allowUpdate: false);
      await Save(new KnownTopic
      {
         ParentKnownTopicId = parentKnownTopic.KnownTopicId,
         NamespaceName = DataTransferService.NamespaceName,
         ProjectName = project.ProjectName,
         Topic = $"{TopicConstants.HUE}/{TopicConstants.LIGHT}/{TopicConstants.OFF}",
         MessageDirection = MessageDirection.Outbound,
         Description = "Turns the light off. Payload: <LightId>."
      }, allowUpdate: false);
      await Save(new KnownTopic
      {
         ParentKnownTopicId = parentKnownTopic.KnownTopicId,
         NamespaceName = DataTransferService.NamespaceName,

         ProjectName = project.ProjectName,
         Topic = $"{TopicConstants.HUE}/{TopicConstants.LIGHT}/{TopicConstants.TOGGLE}",
         MessageDirection = MessageDirection.Outbound,
         Description = "Turns the light off if on and on if off. Payload: <LightId>."
      }, allowUpdate: false);
      await Save(new KnownTopic
      {
         ParentKnownTopicId = parentKnownTopic.KnownTopicId,
         NamespaceName = DataTransferService.NamespaceName,
         ProjectName = project.ProjectName,
         Topic = $"{TopicConstants.HUE}/{TopicConstants.LIGHT}/{TopicConstants.DARKER}",
         MessageDirection = MessageDirection.Outbound,
         Description = "Dims the light. Payload: <LightId>."
      }, allowUpdate: false);
      await Save(new KnownTopic
      {
         ParentKnownTopicId = parentKnownTopic.KnownTopicId,
         NamespaceName = DataTransferService.NamespaceName,
         ProjectName = project.ProjectName,
         Topic = $"{TopicConstants.HUE}/{TopicConstants.LIGHT}/{TopicConstants.BRIGHTER}",
         MessageDirection = MessageDirection.Outbound,
         Description = "Increases the light level. Payload: <LightId>."
      }, allowUpdate: false);
      await Save(new KnownTopic
      {
         ParentKnownTopicId = parentKnownTopic.KnownTopicId,
         NamespaceName = DataTransferService.NamespaceName,
         ProjectName = project.ProjectName,
         Topic = $"{TopicConstants.HUE}/{TopicConstants.LIGHT}/{TopicConstants.BRIGHTNESS}",
         MessageDirection = MessageDirection.Outbound,
         Description = "Adjusts the light level. Payload: { \"LightId\": <LightId>, Brightness\": \"input\" }"
      }, allowUpdate: false);
   }

   public async Task<KnownTopic?> LoadByTopicName(string topic)
   {
      KnownTopic? knownTopic = null;
      string sql = $"select {this.FieldListSelect} from {FullQualifiedTableName} where topic = @Topic;";
      knownTopic = await Db.QueryFirstOrDefaultAsync<KnownTopic?>(sql, new { Topic = topic });
      return knownTopic;
   }

   public async Task<string?> LoadProjectNameByTopicName(string topic)
   {
      string? projectName = null;
      string sql = $"select project_name from {FullQualifiedTableName} where topic = @Topic;";
      projectName = await Db.ExecuteScalarAsync<string>(sql, new { Topic = topic });
      return projectName;
   }
}