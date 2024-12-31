// --------------------------------------------------------------------------------------------------------------------
//      ____    ______   _____
//     /  _/___/_  __/  /__  / ____  ____
//     / // __ \/ /       / / / __ \/ __ \
//   _/ // /_/ / /       / /_/ /_/ / /_/ /
//  /___/\____/_/       /____|____/\____/ 
// --------------------------------------------------------------------------------------------------------------------
// (c) 2025 Holger Freudenreich under MIT license
// --------------------------------------------------------------------------------------------------------------------
// Service that determines whether one or more expressions are true.
// --------------------------------------------------------------------------------------------------------------------

using DataAccess.Services;
using Domain.Interfaces.Crud;
using Domain.Interfaces.RuleEngine;
using Domain.Pocos;
using Json.Path;
using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Domain.Services.RuleEngine
{
   public interface IExpressionEvaluationService
   {
      Task<ExpressionEvaluationResult> EvaluateExpressionAsync(string? expression, string? payload);
   }

   public class ExpressionEvaluationService : IExpressionEvaluationService
   {
      protected IPrepareTargetPayload PrepareTargetPayload;

      protected ICalculationService CalculationService { get; set; }

      protected IScriptCrudService ScriptCrudService { get; set; } = null!;
      public IScriptService ScriptService { get; private set; } = null!;

      protected IExpressionParser ExpressionParser { get; } = null!;

      public ExpressionEvaluationService(IPrepareTargetPayload prepareTargetPayload,
                                         ICalculationService calculationService,
                                         IScriptCrudService scriptCrudService,
                                         IScriptService scriptService,
                                         IExpressionParser expressionParser)
      {
         PrepareTargetPayload = prepareTargetPayload;
         CalculationService = calculationService;
         ScriptCrudService = scriptCrudService;
         ScriptService = scriptService;
         ExpressionParser = expressionParser;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="expressionPoco"></param>
      /// <param name="payload"></param>
      /// <returns>true, if the expression evaluates to true; otherwise false</returns>
      protected ExpressionEvaluationResult Evaluate(ExpressionPoco expressionPoco, string? payload)
      {
         ExpressionEvaluationResult expressionEvaluationResult = new();

         if (null != expressionPoco && null != payload)
         {
            if (string.IsNullOrEmpty(expressionPoco.Value))
            {
               return expressionEvaluationResult;
            }
            if (string.IsNullOrEmpty(expressionPoco.Operator))
            {
               return expressionEvaluationResult;
            }
            if (expressionPoco.Operator.Equals("Contains", StringComparison.OrdinalIgnoreCase))
            {
               expressionEvaluationResult.Protocol = "contains matches";
               expressionEvaluationResult.Matches = payload.Contains(expressionPoco.Value);
               return expressionEvaluationResult;
            }
            else if (expressionPoco.Operator.Equals("Contains not", StringComparison.OrdinalIgnoreCase))
            {
               expressionEvaluationResult.Protocol = "contains NOT matches";

               expressionEvaluationResult.Matches = !payload.Contains(expressionPoco.Value);
               return expressionEvaluationResult;
            }
            else if (expressionPoco.Operator == ">")
            {
               expressionEvaluationResult.Protocol = "> matches";
               expressionEvaluationResult.Matches = Convert.ToDouble(payload, CultureInfo.InvariantCulture) > Convert.ToDouble(expressionPoco.Value);
               return expressionEvaluationResult;
            }
            else if (expressionPoco.Operator == "<")
            {
               expressionEvaluationResult.Protocol = "< matches";

               expressionEvaluationResult.Matches = Convert.ToDouble(payload, CultureInfo.InvariantCulture) < Convert.ToDouble(expressionPoco.Value);
               return expressionEvaluationResult;
            }
            else if (expressionPoco.Operator == ">=")
            {
               expressionEvaluationResult.Protocol = ">= matches";

               expressionEvaluationResult.Matches = Convert.ToDouble(payload, CultureInfo.InvariantCulture) >= Convert.ToDouble(expressionPoco.Value);
               return expressionEvaluationResult;
            }
            else if (expressionPoco.Operator == "<=")
            {
               expressionEvaluationResult.Protocol = "<= matches";

               expressionEvaluationResult.Matches = Convert.ToDouble(payload, CultureInfo.InvariantCulture) <= Convert.ToDouble(expressionPoco.Value);
               return expressionEvaluationResult;
            }
            else if (expressionPoco.Operator == "=" || expressionPoco.Operator == "==")
            {
               expressionEvaluationResult.Protocol = "== matches";
               double val;
               if (double.TryParse(expressionPoco.Value, out val))
               {
                  expressionEvaluationResult.Matches = Convert.ToDouble(payload, CultureInfo.InvariantCulture) == val;
                  return expressionEvaluationResult;
               }
               expressionEvaluationResult.Matches = payload == expressionPoco.Value;
               return expressionEvaluationResult;
            }
            else if (expressionPoco.Operator == "%")
            {
               expressionEvaluationResult.Protocol = "% matches";
               expressionEvaluationResult.Matches = Convert.ToDouble(payload, CultureInfo.InvariantCulture) % Convert.ToDouble(expressionPoco.Value) != 0;
               return expressionEvaluationResult;
            }
         }
         return expressionEvaluationResult;
      }

      protected ExpressionEvaluationResult EvaluateJsonExpression(string expression, string? payload)
      {
         // An Expressions in JSON format.
         ExpressionPoco? expressionPoco = null;

         try
         {
            expressionPoco = JsonSerializer.Deserialize<ExpressionPoco>(expression);
         }
         catch (Exception exception)
         {
            return new ExpressionEvaluationResult { Protocol = exception.GetBaseException().Message };
         }

         if (expressionPoco != null)
         {
            return Evaluate(expressionPoco, payload);
         }
         return new ExpressionEvaluationResult();
      }

      public async Task<ExpressionEvaluationResult> EvaluateExpressionAsync(string? expressions,
                                                                            string? payload)
      {
         if (string.IsNullOrEmpty(expressions))
         {
            ExpressionEvaluationResult expressionEvaluationResult = new ExpressionEvaluationResult { Matches = true };
            expressionEvaluationResult.Protocol = "no expressions ➔ treat as positiv evaluated";
            return expressionEvaluationResult; // treat as evaluated.
         }

         //if (string.IsNullOrEmpty(payload))
         //{
         //   return false;
         //}
         // does expression contain multiple expressions?

         // check for AND
         string[] separatingStringsAnd = { " && ", " and " };
         var splittedByAnd = expressions.Split(separatingStringsAnd, StringSplitOptions.TrimEntries);
         if (splittedByAnd.Length > 1)
         {
            bool evaluated = false;
            var expressionEvaluationResult = new ExpressionEvaluationResult();
            foreach (string expression in splittedByAnd)
            {
               var expressionEvaluationResultTmp = await EvaluateAsync(expression.Trim(), payload);
               expressionEvaluationResult.Protocol += expressionEvaluationResultTmp.Protocol + Environment.NewLine;

               if (!expressionEvaluationResultTmp.Matches)
               {
                  expressionEvaluationResult.Matches = false;
                  return expressionEvaluationResult;
               }
               evaluated = true;
            }
            if (evaluated)
            {
               expressionEvaluationResult.Matches = true;
               return expressionEvaluationResult;
            }
         }

         // check for OR
         string[] separatingStringsOr = { " || ", " or " };
         var splittedByOr = expressions.Split(separatingStringsOr, StringSplitOptions.None);
         if (splittedByOr.Length > 1)
         {
            var expressionEvaluationResult = new ExpressionEvaluationResult();

            foreach (string expression in splittedByOr)
            {
               var expressionEvaluationResultTmp = await EvaluateAsync(expression.Trim(), payload);
               expressionEvaluationResult.Protocol += expressionEvaluationResultTmp.Protocol + Environment.NewLine;

               if (expressionEvaluationResultTmp.Matches)
               {
                  expressionEvaluationResult.Matches = true;
                  return expressionEvaluationResult;
               }
            }
         }

         return await EvaluateAsync(expressions, payload);
      }


      private async Task<ExpressionEvaluationResult> EvaluateAsync(string expression, string? payload)
      {
         object? result = true;
         if (!string.IsNullOrEmpty(expression))
         {
            if (expression.Contains("$[") && payload != null)
            {
               int index = expression.LastIndexOf("]");
               string location = expression.Substring(0, index + 1);
               JsonPath jsonPath = JsonPath.Parse(location);

               JsonNode? jsonNode = JsonNode.Parse(payload);
               PathResult? pathResult = jsonPath.Evaluate(jsonNode);

               var firstMatch = pathResult.Matches?.FirstOrDefault();
               if (firstMatch != null)
               {
                  var value = firstMatch.Value?.ToString();
                  var isNumeric = double.TryParse(value, out double number);
                  if (!isNumeric)
                  {
                     value = $"'{value}'";
                  }
                  if (location == firstMatch.Location?.ToString())
                  {
                     expression = expression.Replace(location,
                                                     value);
                  }
               }
            }
            else
            {
               if (expression.StartsWith("["))
               {
                  // Mehrere Expressions im JSON Format. Werden zunächst mit "OR" verknüpft
                  var expressionPocos = JsonSerializer.Deserialize<List<ExpressionPoco>>(expression);
                  if (null != expressionPocos)
                  {
                     foreach (var expressionPoco in expressionPocos)
                     {
                        var expressionEvaluationResultTmp = Evaluate(expressionPoco, payload);
                        if (expressionEvaluationResultTmp.Matches)
                        {
                           return new ExpressionEvaluationResult { Matches = true, Protocol = "at least one matches" }; // at least one matches
                        }
                     }
                  }
                  return new ExpressionEvaluationResult { Protocol = "no match" };
               }
               else if (expression.StartsWith("{"))
               {
                  return EvaluateJsonExpression(expression, payload);
               }
               else
               {
                  expression = expression.Replace(FunctionNames.Input,
                                                  payload);

                  expression = await ExpressionParser.ResolveExpression(expression);
               }
            }

            // Datenbank ist schneller!
            //result = await CSharpScript.EvaluateAsync(expression);
            result = CalculationService.Calculate(expression);

            Debug.WriteLine($"{expression} ➔ {result}");
         }
         return new ExpressionEvaluationResult { Result = result, Matches = Convert.ToBoolean(result), Protocol = $"{expression} ➔ {result}" };
      }
   }
}
