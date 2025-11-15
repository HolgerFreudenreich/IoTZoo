// --------------------------------------------------------------------------------------------------------------------
//      ____    ______   _____
//     /  _/___/_  __/  /__  / ____  ____
//     / // __ \/ /       / / / __ \/ __ \
//   _/ // /_/ / /       / /_/ /_/ / /_/ /
//  /___/\____/_/       /____|____/\____/   P L A Y G R O U N D
// --------------------------------------------------------------------------------------------------------------------
// Connect «Things» with microcontrollers in a simple way.
// --------------------------------------------------------------------------------------------------------------------
// (c) 2025 Holger Freudenreich under the MIT license
// --------------------------------------------------------------------------------------------------------------------

namespace Domain.Services;

using Dapper;
using Domain.Interfaces.Crud;
using Domain.Interfaces.Timer;
using Domain.Pocos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Reflection;

public class ProjectDatabaseService : DataServiceBase, IProjectCrudService
{
   protected IKnownTopicsCrudService KnownTopicsCrudService { get; }

   protected ICronService CronService { get; }

   public ProjectDatabaseService(IOptions<AppSettings> options,
                                 ILogger<DataServiceBase> logger,
                                 IKnownTopicsCrudService knownTopicsCrudService,
                                 ICronService cronService) : base(options, logger)
   {
      Initialize(typeof(Project), "cfg", "project");
      KnownTopicsCrudService = knownTopicsCrudService;
      CronService = cronService;
   }

   public async Task Delete(Project project)
   {
      try
      {
         await Db.ExecuteAsync(DeleteSql, project);
      }
      catch (Exception ex)
      {
         Logger.LogError(ex, $"{MethodBase.GetCurrentMethod()} failed!");
      }
   }

   public async Task Insert(Project project)
   {
      try
      {
         project.ProjectId = await Db.QueryFirstAsync<int>(InsertSql, project);
         if (project.ProjectId > 0)
         {
            // Create known topics
            await KnownTopicsCrudService.RegisterProjectDefaultKnownTopics(project);
            // and start the project cron jobs.
            await CronService.StartProjectCronJobs(project);
         }
      }
      catch (Exception ex)
      {
         Logger.LogError(ex, $"{MethodBase.GetCurrentMethod()} failed!");
      }
   }

   public async Task Update(Project project)
   {
      try
      {
         await Db.ExecuteAsync(UpdateSql, project);

         // update the dependent objects (KnownTopics, Rules).

         //var knownTopics = await KnownTopicsCrudService.GetKnownTopicsByProjectName(project.ProjectName, null);
         //foreach (var knownTopic in knownTopics)
         //{
         //   //knownTopic.Topic = 
         //}

      }
      catch (Exception ex)
      {
         Logger.LogError(ex, $"{MethodBase.GetCurrentMethod()} failed!");
      }
   }

   public async Task Save(Project project)
   {
      if (project.ProjectId > 0)
      {
         await Update(project);
      }
      else
      {
         await Insert(project);
      }
   }

   public async Task<List<Project>> LoadProjects()
   {
      IEnumerable<Project>? projects = null!;
      try
      {
         projects = await Db.QueryAsync<Project>($"select {FieldListSelect} from {FullQualifiedTableName} order by project_name;");
      }
      catch (Exception exception)
      {
         Logger.LogError(exception, $"{MethodBase.GetCurrentMethod()} failed!");
      }
      return projects.AsList();
   }

   public async Task<Project?> LoadProjectByName(string projectName)
   {
      Project? project = null;
      try
      {
         project = await Db.QuerySingleOrDefaultAsync<Project?>($"select {FieldListSelect} from {FullQualifiedTableName} where project_name = @ProjectName;",
            new { ProjectName = projectName });

      }
      catch (Exception exception)
      {
         Logger.LogError(exception, $"{MethodBase.GetCurrentMethod()} failed!");
      }
      return project;
   }
}
