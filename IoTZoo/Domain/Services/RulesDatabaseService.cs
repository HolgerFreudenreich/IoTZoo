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

using Dapper;
using Domain.Interfaces.Crud;
using Domain.Pocos;
using Domain.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Reflection;

namespace DataAccess.Services;


public class RulesDatabaseService : DataServiceBase, IRulesCrudService
{
   public RulesDatabaseService(IOptions<AppSettings> options,
                       ILogger<DataServiceBase> logger) : base(options,
                                                               logger)
   {
      Initialize(typeof(Rule), "cfg", "rule");
   }

   public async Task<List<Rule>> GetRules(bool onlyEnabledRules)
   {
      IEnumerable<Rule> rules = null!;
      try
      {
         string sql = $"select {FieldListSelect} from {FullQualifiedTableName} ";
         if (onlyEnabledRules)
         {
            sql += "where enabled = true ";
         }
         sql += "order by priority;";
         rules = await Db.QueryAsync<Rule>(sql);
      }
      catch (Exception ex)
      {
         Logger.LogError(ex, $"{MethodBase.GetCurrentMethod()} failed!");
         throw;
      }
      return rules.AsList();
   }

   public async Task<List<Rule>> GetRulesBySourceTopic(KnownTopic sourceTopic, bool onlyEnabledRules)
   {
      IEnumerable<Rule> rules = null!;
      try
      {
         string sql = $"select {FieldListSelect} from {FullQualifiedTableName} where project_name = @ProjectName and source_topic = @SourceTopic ";
         if (onlyEnabledRules)
         {
            sql += "and enabled = true ";
         }
         sql += "order by priority;";
         rules = await Db.QueryAsync<Rule>(sql,
                               new { ProjectName = sourceTopic.ProjectName, SourceTopic = sourceTopic.Topic });
      }
      catch (Exception ex)
      {
         Logger.LogError(ex, $"{MethodBase.GetCurrentMethod()} failed!");
         throw;
      }
      return rules.AsList();
   }

   public async Task Save(Rule rule)
   {
      if (rule.RuleId > 0)
      {
         await Update(rule);
      }
      else
      {
         await Insert(rule);
      }
   }

   public async Task Insert(Rule rule)
   {
      try
      {
         int createdRuleId = await Db.QuerySingleAsync<int>(InsertSql, rule);
         rule.RuleId = createdRuleId;
      }
      catch (Exception ex)
      {
         Logger.LogError(ex, $"{MethodBase.GetCurrentMethod()} failed!");
         throw;
      }
   }

   public async Task Update(Rule rule)
   {
      try
      {
         int rowsProcessed = await Db.ExecuteAsync(UpdateSql,
                                                   rule);
      }
      catch (Exception ex)
      {
         Logger.LogError(ex, $"{MethodBase.GetCurrentMethod()} failed!");
         throw;
      }
   }

   public async Task Delete(Rule rule)
   {
      try
      {
         int rowsProcessed = await Db.ExecuteAsync(DeleteSql,
                                                   rule);
      }
      catch (Exception ex)
      {
         Logger.LogError(ex, $"{MethodBase.GetCurrentMethod()} failed!");
         throw;
      }
   }

   public async Task<List<Rule>?> GetRulesByProject(Project? project)
   {
      if (null == project)
      {
         return await GetRules(false);
      }
      IEnumerable<Rule> rules = null!;
      try
      {
         string sql = $"select {FieldListSelect} from {FullQualifiedTableName} where project_name = @ProjectName ";

         sql += "order by priority;";
         rules = await Db.QueryAsync<Rule>(sql,
                               new { ProjectName = project.ProjectName });
      }
      catch (Exception ex)
      {
         Logger.LogError(ex, $"{MethodBase.GetCurrentMethod()} failed!");
         throw;
      }
      return rules.AsList();
   }
}