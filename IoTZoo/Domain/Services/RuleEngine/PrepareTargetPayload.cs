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
using Domain.Interfaces.RuleEngine;
using Domain.Pocos;
using Json.Path;
using Microsoft.Extensions.Logging;
using System.Text.Json.Nodes;

namespace Domain.Services.RuleEngine;


/// <summary>
/// Transformation from raw target payload data to data to be sent.
/// </summary>
public class PrepareTargetPayload : IPrepareTargetPayload
{
   public ILogger<PrepareTargetPayload> Logger { get; }

   protected IExpressionParser ExpressionParser { get; } = null!;

   protected ICalculationService CalculationService { get; set; }

   public PrepareTargetPayload(ILogger<PrepareTargetPayload> logger,
                               ICalculationService calculationService,
                               IExpressionParser expressionParser)
   {
      Logger = logger;
      CalculationService = calculationService;
      ExpressionParser = expressionParser;
   }

   public bool HasMathOps(string expression)
   {
      char[] mathops = { '+', '-', '*', '/', '^', '=', '%' };
      return expression.IndexOfAny(mathops) >= 0;
   }

   public bool IsCalculation(string expression)
   {
      bool hasMathOps = HasMathOps(expression);
      if (!hasMathOps)
      {
         if (expression.Contains("NOT", StringComparison.OrdinalIgnoreCase))
         {
            return true;
         }
      }
      return hasMathOps;
      //return expression.Contains(FunctionNames.Calc, StringComparison.OrdinalIgnoreCase);
   }

   public async Task<string?> Execute(string? expression, string? payload)
   {
      if (null != expression)
      {
         expression = expression.Replace(FunctionNames.Input, payload, StringComparison.OrdinalIgnoreCase);
         expression = await ExpressionParser.ResolveExpression(expression);

         if (expression.Contains("$["))
         {
            expression = GetJsonPropertyData(expression, payloadJson: payload);
         }

         if (IsCalculation(expression))
         {
            expression = Convert.ToString(CalculationService.Calculate(expression));
         }
      }

      if (!string.IsNullOrEmpty(expression))
      {
         expression = System.Text.RegularExpressions.Regex.Unescape(expression);
      }
      return expression;
   }

   public async Task<string?> Execute(Rule rule, TopicEntry topicEntry)
   {
      if (null == rule)
      {
         return null;
      }
      if (null == topicEntry)
      {
         return null;
      }
      return await Execute(expression: rule.TargetPayload, topicEntry.Payload);
   }

   /// <summary>
   /// Extracts data from the payload.
   /// </summary>
   /// <param name="payloadJson">Example: {"DateTime":"14.10.2024 16:30:45","Time":"16:30:45","TimeShort":"16:30"}</param>
   /// <param name="targetProperty">Example: $['TimeShort']</param>
   /// <returns></returns>
   private string GetJsonPropertyData(string targetProperty, string? payloadJson)
   {
      if (string.IsNullOrEmpty(payloadJson))
      {
         return string.Empty;
      }
      int index = targetProperty.LastIndexOf("]");
      int startIndex = targetProperty.LastIndexOf("[");
      string location = targetProperty.Substring(startIndex - 1, index - startIndex + 2);
      var path = JsonPath.Parse(location);
      JsonNode? jsonNode = JsonNode.Parse(payloadJson);
      PathResult? pathResult = path.Evaluate(jsonNode);

      var firstMatch = pathResult.Matches?.FirstOrDefault();
      if (firstMatch != null)
      {
         if (firstMatch.Value != null)
         {
            targetProperty = targetProperty.Replace(location, firstMatch.Value.ToString());
         }
      }

      return targetProperty;
   }
}
