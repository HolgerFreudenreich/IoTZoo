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

namespace DataAccess.Services;

using Dapper;
using Domain.Interfaces.Crud;
using Domain.Pocos;
using System.Collections.Generic;
using System.Threading.Tasks;

public class RulesServiceMock : IRulesCrudService
{
   private readonly List<Rule> rules = new()
                                      {
                                        new Rule
                                        {
                                          RuleId       = 1,
                                          Priority     = 10,
                                          SourceTopic  = "sensor/temperature/0001",
                                          TargetTopic  = "traffic_light/0001",
                                          Expression   = "input1 <= 23",
                                          TargetPayload = "green"
                                        },
                                        new Rule
                                        {
                                          RuleId       = 2,
                                          Priority     = 5,
                                          SourceTopic  = "sensor/temperature/0001",
                                          TargetTopic  = "traffic_light/0001",
                                          Expression   = "input1 > 23",
                                          TargetPayload = "yellow"
                                        },
                                        new Rule
                                        {
                                          RuleId       = 3,
                                          Priority     = 0,
                                          SourceTopic  = "sensor/temperature/0001",
                                          TargetTopic  = "traffic_light/0001",
                                          Expression   = "input1 > 27",
                                          TargetPayload = "red"
                                        }
                                      };

   public List<Rule> GetRulesFromMemory()
   {
      var sortedList = rules.OrderBy(rule => rule.Priority).AsList();
      return sortedList;
   }

   public List<Rule> GetRulesFromMemory(string sourceTopic)
   {
      var sortedList = (from data in rules where data.SourceTopic == sourceTopic select data).OrderBy(x => x.Priority).AsList();
      return sortedList;
   }

   public Task Save(Rule rule)
   {
      var existingRule = (from data in rules where data.RuleId == rule.RuleId select data).SingleOrDefault();
      if (existingRule != null)
      {
         existingRule = rule;
      }
      else
      {
         rules.Add(rule);
      }
      return Task.CompletedTask;
   }

   public Task Update(Rule rule)
   {
      var existingRule = (from data in rules where data.RuleId == rule.RuleId select data).SingleOrDefault();
      if (null != existingRule)
      {
         existingRule.Expression = rule.Expression;
         existingRule.SourceTopic = rule.SourceTopic;
         existingRule.Priority = rule.Priority;
         existingRule.TargetTopic = rule.TargetTopic;
         existingRule.DelayMs = rule.DelayMs;
         existingRule.Enabled = rule.Enabled;
      }
      return Task.CompletedTask;
   }

   public Task Delete(Rule rule)
   {
      var existingRule = (from data in rules where data.RuleId == rule.RuleId select data).SingleOrDefault();
      if (existingRule != null)
      {
         rules.Remove(existingRule);
      }
      return Task.CompletedTask;
   }

   public string GetSelectFieldList()
   {
      return "*";
   }

   public Task<List<Rule>> GetRulesBySourceTopic(string sourceTopic)
   {
      List<Rule> rules = GetRulesFromMemory(sourceTopic);
      Task<List<Rule>> task = Task.FromResult(rules);
      return task;
   }

   public Task<List<Rule>> GetRules(bool onlyEnabledRules = false)
   {
      List<Rule> rules = GetRulesFromMemory();
      Task<List<Rule>> task = Task.FromResult(rules);
      return task;
   }

   public Task<List<Rule>?> GetRulesByProject(Project? selectedProject)
   {
      throw new NotImplementedException();
   }

   Task IRulesCrudService.Insert(Rule rule)
   {
      throw new NotImplementedException();
   }

   public Task<List<Rule>> GetRulesBySourceTopic(KnownTopic topic, bool onlyEnabledRules = false)
   {
      List<Rule> rules = GetRulesFromMemory();
      Task<List<Rule>> task = Task.FromResult(rules);
      return task;
   }
}