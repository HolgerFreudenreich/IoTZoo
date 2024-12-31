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

using Domain.Pocos;
using Domain.Services;

namespace Domain.Interfaces.Crud;

public interface IKnownTopicsCrudService
{
   public Task<bool> ExistsByTopicName(string topicName);

   public Task<SaveResult> Save(KnownTopic knownTopic, bool allowUpdate = true);

   public Task<int> Insert(KnownTopic knownTopic);

   public Task RegisterProjectDefaultKnownTopics(Project project);

   public Task Delete(KnownTopic knownTopic);

   public Task<List<KnownTopic>> GetKnownTopicsByProjectName(string projectName, List<MessageDirection>? messageDirections);

   public Task<KnownTopic?> GetKnownTopicByTopicName(string projectName, string topic);

   public Task<List<KnownTopic>> GetKnownTopics();

   Task<KnownTopic?> LoadByTopicName(string topic);

   Task<string?> LoadProjectNameByTopicName(string topic);
}
