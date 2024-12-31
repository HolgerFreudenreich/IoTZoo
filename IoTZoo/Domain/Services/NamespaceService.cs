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
using Domain.Interfaces.Crud;
using Domain.Pocos;
using Domain.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Reflection;

namespace DataAccess.Services;

public class NamespaceService : DataServiceBase, INamespaceCrudService
{
   public NamespaceService(IOptions<AppSettings> options, ILogger<DataServiceBase> logger) : base(options, logger)
   {
      Initialize(typeof(KnownTopicPrefix), "cfg", "known_topic_prefix");
   }

   public string GetNamespaceName()
   {
      return Convert.ToString(Db.ExecuteScalar($"select topic_prefix from {FullQualifiedTableName}"))!;
   }

   public async Task Update(string namespaceName)
   {
      try
      {
         int rows = await Db.ExecuteAsync("update known_topic_prefix set topic_prefix = @TopicPrefix;", new { TopicPrefix = namespaceName });
      }
      catch (Exception ex)
      {
         Logger.LogError(ex, $"{MethodBase.GetCurrentMethod()} failed!");
         throw;
      }
   }
}
