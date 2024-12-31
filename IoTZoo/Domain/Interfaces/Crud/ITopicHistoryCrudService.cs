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

public interface ITopicHistoryCrudService
{
   public Task Insert(TopicHistory topicHistory);

   Task<List<TopicHistory>> LoadTopicHistory(Project? project);

   Task DeleteAll();

   Task DeleteTopicHistoryEntry(TopicHistory topicHistory);
}
