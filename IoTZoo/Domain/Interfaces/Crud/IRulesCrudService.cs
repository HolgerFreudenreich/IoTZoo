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

using Domain.Pocos;

namespace Domain.Interfaces.Crud;

public interface IRulesCrudService : IDataServiceBase
{
   public Task<List<Rule>> GetRules(bool onlyEnabledRules = false);

   public Task<List<Rule>> GetRulesBySourceTopic(KnownTopic topic, bool onlyEnabledRules = false);

   /// <summary>
   /// Saves a rule (insert or update).
   /// </summary>
   /// <param name="rule"></param>
   public Task Save(Rule rule);

   public Task Insert(Rule rule);

   public Task Update(Rule rule);

   public Task Delete(Rule rule);

   Task<List<Rule>?> GetRulesByProject(Project? selectedProject);
}