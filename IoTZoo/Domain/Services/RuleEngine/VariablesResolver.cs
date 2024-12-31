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

using DataAccess.Services;
using Domain.Interfaces;
using Domain.Interfaces.Crud;
using Domain.Pocos;
using Json.Path;
using Microsoft.Extensions.Logging;
using MQTTnet.Client;
using System.Reflection;
using System.Text.Json.Nodes;

namespace Domain.Services.RuleEngine;

public class VariablesResolver : IVariablesResolver
{
   ILogger<VariablesResolver> Logger { get; }
   protected IKnownTopicsCrudService KnownTopicsDatabaseService { get; }
   public IDataTransferService DataTransferService { get; }

   public VariablesResolver(ILogger<VariablesResolver> logger,
                          IKnownTopicsCrudService knownTopicsService,
                          IDataTransferService dataTransferService)
   {
      KnownTopicsDatabaseService = knownTopicsService;
      DataTransferService = dataTransferService;
      Logger = logger;
   }

   public int Count(string text, string value)
   {
      int count = 0, minIndex = text.IndexOf(value, 0);
      while (minIndex != -1)
      {
         minIndex = text.IndexOf(value, minIndex + value.Length);
         count++;
      }
      return count;
   }

   /// <summary>
   /// Replaces all variables of the expression with the persisted value.
   /// <param name="expression"></param>
   /// <returns></returns>
   public async Task<string> ResolveVariables(string expression)
   {
      if (!expression.StartsWith("{") &&
          !expression.Contains(FunctionNames.ReadFromMemory, StringComparison.OrdinalIgnoreCase) &&
          !expression.Contains(FunctionNames.Calc, StringComparison.OrdinalIgnoreCase) &&
          !expression.Contains(FunctionNames.Script, StringComparison.OrdinalIgnoreCase))
      {
         expression = expression.Replace(FunctionNames.Calc, string.Empty, StringComparison.OrdinalIgnoreCase);
         string output1 = string.Empty;
         var splitted = expression.Split(" ");
         foreach (var item in splitted)
         {
            if (char.IsLetter(item[0]))
            {
               TopicEntry? topicFromMemory = (from data in DataTransferService.ReceivedTopicsQueue where data.Topic == item select data).SingleOrDefault();
               if (topicFromMemory != null)
               {
                  output1 += topicFromMemory.Payload;
               }
               else
               {
                  var knownTopic = await KnownTopicsDatabaseService.GetKnownTopicByTopicName("", item); // fixme Holger
                  if (knownTopic != null)
                  {
                     output1 += knownTopic.LastPayload;
                  }
                  else
                  {
                     return expression;
                  }
               }
            }
            else
            {
               output1 += item;
            }
            output1 += " ";
         }
         return output1.TrimEnd();
      }

      string output = expression;
      int count = Count(expression, FunctionNames.ReadFromMemory); // Regex.Count(expression, FunctionNames.ReadFromMemory);

      for (int i = 0; i < count; i++)
      {
         output = await ReplaceVariableNameWithVariableContent(output);
      }

      return output;
   }

   /// <summary>
   /// </summary>
   /// <param name="input">Something like Read('colors')['green']</param>
   /// <param name="payload"></param>
   /// <param name="indexEnd"></param>
   /// <returns></returns>
   private string HandleJsonPathArrayIndexer(string input, string? payload, int indexEnd)
   {
      try
      {
         if (string.IsNullOrEmpty(payload))
         {
            return input;
         }
         string indexAccessor = input.Substring(indexEnd + 1);
         int arrayIndexerStartIndex = indexEnd + 3;
         int arrayIndexerEndIndex = input.IndexOf("]", arrayIndexerStartIndex);
         string arrayIndexer = input.Substring(arrayIndexerStartIndex, arrayIndexerEndIndex - arrayIndexerStartIndex - 1);

         string source = $"$.*.{arrayIndexer}";

         var path = JsonPath.Parse(source);
         JsonNode? jsonNode = JsonNode.Parse(payload);
         PathResult? pathResult = path.Evaluate(jsonNode);

         var firstMatch = pathResult.Matches?.FirstOrDefault();
         JsonNode? jsonNode1 = JsonNode.Parse(payload);
         PathResult? pathResult1 = path.Evaluate(jsonNode);

         Node? firstMatch1 = pathResult.Matches?.FirstOrDefault();
         if (firstMatch1 != null && firstMatch1.Value != null)
         {
            return firstMatch1.Value.ToString();
         }
      }
      catch (Exception exception)
      {
         Logger.LogError(exception, $"{MethodBase.GetCurrentMethod()} failed!");
      }
      return input;
   }

   protected async Task<string> ReplaceVariableNameWithVariableContent(string input)
   {
      // Are there many ReadFromMemory occurrences?
      int nextStartIndex = input.IndexOf(FunctionNames.ReadFromMemory);
      if (-1 == nextStartIndex)
      {
         return input;
      }
      int indexEnd = input.IndexOf(")");
      bool hasBrackets = true;
      if (-1 == indexEnd)
      {
         indexEnd = input.IndexOf(" ");
         hasBrackets = false;
      }

      string formula = input.Substring(nextStartIndex - 0, indexEnd - nextStartIndex + 1).Trim();
      string topicName = string.Empty;

      if (hasBrackets)
      {
         topicName = formula.Substring(FunctionNames.ReadFromMemory.Length + 1, formula.Length - FunctionNames.ReadFromMemory.Length - 2);
      }
      else
      {
         topicName = formula.Substring(FunctionNames.ReadFromMemory.Length, formula.Length - FunctionNames.ReadFromMemory.Length);
      }

      string nettoTopicName = topicName.Replace("'", string.Empty).Replace("\"", string.Empty).Trim();

      string output = string.Empty;

      bool isArrayIndexer = false;

      if (input.Length > indexEnd + 1)
      {
         if (input.Substring(indexEnd + 1, 1) == "[")
         {
            // Something like ReadFromMemory('colors')['green']
            isArrayIndexer = true;
         }
      }

      TopicEntry? topicFromMemory = (from data in DataTransferService.ReceivedTopicsQueue where data.FullQualifiedTopic == nettoTopicName select data).SingleOrDefault();
      if (null != topicFromMemory)
      {
         if (hasBrackets)
         {
            output = input.Replace($"{FunctionNames.ReadFromMemory}({topicName})", topicFromMemory.Payload); // ToDo: handle null == topicFromMemory.Payload
         }
         else
         {
            output = input.Replace($"{FunctionNames.ReadFromMemory}{topicName}", topicFromMemory.Payload); // ToDo: handle null == topicFromMemory.Payload
         }
         if (isArrayIndexer)
         {
            output = HandleJsonPathArrayIndexer(input, topicFromMemory.Payload, indexEnd);
         }
      }
      else
      {
         // Read from Database
         KnownTopic? topicFromDatabase = await KnownTopicsDatabaseService.GetKnownTopicByTopicName("", nettoTopicName); // fixme Holger
         if (null != topicFromDatabase)
         {
            output = input.Replace($"{FunctionNames.ReadFromMemory}({topicName})", topicFromDatabase.LastPayload);

            if (isArrayIndexer)
            {
               output = HandleJsonPathArrayIndexer(input, topicFromDatabase.LastPayload, indexEnd);
            }
         }
      }
      return output;
   }
}
