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

using Domain.Interfaces.Crud;
using Domain.Pocos;
using Domain.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace UnitTests;

public class KnownTopicsServiceMock : DataServiceBase, IKnownTopicsCrudService
{
   List<KnownTopic> knownTopics = new List<KnownTopic>();

   public KnownTopicsServiceMock(IOptions<AppSettings> options, ILogger<DataServiceBase> logger) : base(options, logger)
   {
   }

   public Task<KnownTopic?> GetKnownTopicByTopicNameAsync(string topicName)
   {
      return Task.FromResult((from data in knownTopics where data.Topic == topicName select data).FirstOrDefault());
   }

   public Task<List<KnownTopic>> GetKnownTopics()
   {
      return Task.FromResult(knownTopics);
   }

   public Task Delete(KnownTopic knownTopic)
   {
      return Task.FromResult(knownTopics.Remove(knownTopic));
   }

   public Task<KnownTopic?> LoadByTopicName(string topic)
   {
      throw new NotImplementedException();
   }

   public Task<string?> LoadProjectNameByTopicName(string topic)
   {
      throw new NotImplementedException();
   }

   Task IKnownTopicsCrudService.RegisterProjectDefaultKnownTopics(Project project)
   {
      throw new NotImplementedException();
   }

   public Task<int> Insert(KnownTopic knownTopic)
   {
      throw new NotImplementedException();
   }

   Task<SaveResult> IKnownTopicsCrudService.Save(KnownTopic knownTopic, bool allowUpdate)
   {
      knownTopics.Add(knownTopic);
      return Task.FromResult(SaveResult.Inserted);
   }

   async Task<bool> IKnownTopicsCrudService.ExistsByTopicName(string topicName)
   {
      return knownTopics.Select(x => x.Topic == topicName).Any();
   }

   public Task<List<KnownTopic>> GetKnownTopicsByProjectName(string? projectName, List<MessageDirection>? messageDirections)
   {
      throw new NotImplementedException();
   }

   public Task<KnownTopic?> GetKnownTopicByTopicName(string projectName, string topicName)
   {
      KnownTopic? item = knownTopics.FirstOrDefault(x => x.Topic == topicName);
      return Task.FromResult(item);
   }
}