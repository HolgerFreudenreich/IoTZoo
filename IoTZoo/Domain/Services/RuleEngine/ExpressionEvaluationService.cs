// --------------------------------------------------------------------------------------------------------------------
//      ____    ______   _____
//     /  _/___/_  __/  /__  / ____  ____
//     / // __ \/ /       / / / __ \/ __ \
//   _/ // /_/ / /       / /_/ /_/ / /_/ /
//  /___/\____/_/       /____|____/\____/ 
// --------------------------------------------------------------------------------------------------------------------
// (c) 2025 - 2026 Holger Freudenreich under MIT license
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
using System.Text.RegularExpressions;

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

            if (null == expressionPoco || null == payload || string.IsNullOrEmpty(expressionPoco.Value) || string.IsNullOrEmpty(expressionPoco.Operator))
            {
                return expressionEvaluationResult;
            }

            string value = expressionPoco.Value;
            string operatorSymbol = expressionPoco.Operator;

            if (operatorSymbol.Equals("Contains", StringComparison.OrdinalIgnoreCase))
            {
                return CreateResult("contains matches", payload.Contains(value));
            }
            if (operatorSymbol.Equals("Contains not", StringComparison.OrdinalIgnoreCase))
            {
                return CreateResult("contains NOT matches", !payload.Contains(value));
            }
            if (operatorSymbol == "=" || operatorSymbol == "==")
            {
                return EvaluateEquality(payload, value);
            }

            return operatorSymbol switch
            {
                ">" => CompareNumeric("> matches", payload, value, (left, right) => left > right),
                "<" => CompareNumeric("< matches", payload, value, (left, right) => left < right),
                ">=" => CompareNumeric(">= matches", payload, value, (left, right) => left >= right),
                "<=" => CompareNumeric("<= matches", payload, value, (left, right) => left <= right),
                "%" => CompareNumeric("% matches", payload, value, (left, right) => left % right != 0),
                _ => expressionEvaluationResult
            };
        }

        private static ExpressionEvaluationResult CreateResult(string protocol, bool matches)
        {
            return new ExpressionEvaluationResult { Protocol = protocol, Matches = matches };
        }

        private static ExpressionEvaluationResult CompareNumeric(string protocol, string payload, string value, Func<double, double, bool> comparison)
        {
            return CreateResult(protocol, comparison(Convert.ToDouble(payload, CultureInfo.InvariantCulture), Convert.ToDouble(value)));
        }

        private static ExpressionEvaluationResult EvaluateEquality(string payload, string value)
        {
            if (double.TryParse(value, out double number))
            {
                return CreateResult("== matches", Convert.ToDouble(payload, CultureInfo.InvariantCulture) == number);
            }
            return CreateResult("== matches", payload == value);
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

            ExpressionEvaluationResult? andResult = await EvaluateAndAsync(expressions, payload);
            if (andResult != null)
            {
                return andResult;
            }

            ExpressionEvaluationResult? orResult = await EvaluateOrAsync(expressions, payload);
            if (orResult != null)
            {
                return orResult;
            }

            return await EvaluateAsync(expressions, payload);
        }

        private async Task<ExpressionEvaluationResult?> EvaluateAndAsync(string expressions, string? payload)
        {
            string[] separatingStringsAnd = { " && ", " and " };
            var splittedByAnd = expressions.Split(separatingStringsAnd, StringSplitOptions.TrimEntries);
            if (splittedByAnd.Length <= 1)
            {
                return null;
            }

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
            }

            expressionEvaluationResult.Matches = true;
            return expressionEvaluationResult;
        }

        private async Task<ExpressionEvaluationResult?> EvaluateOrAsync(string expressions, string? payload)
        {
            string[] separatingStringsOr = { " || ", " or " };
            var splittedByOr = expressions.Split(separatingStringsOr, StringSplitOptions.None);
            if (splittedByOr.Length <= 1)
            {
                return null;
            }

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

            return null;
        }


        private async Task<ExpressionEvaluationResult> EvaluateAsync(string expression, string? payload)
        {
            object? result = true;
            if (!string.IsNullOrEmpty(expression))
            {
                if (expression.Contains("$[") && payload != null)
                {
                    expression = ReplaceJsonPathWithValue(expression, payload);
                }
                else
                {
                    if (expression.StartsWith("["))
                    {
                        return EvaluateJsonArrayExpression(expression, payload);
                    }
                    if (expression.StartsWith("{"))
                    {
                        return EvaluateJsonExpression(expression, payload);
                    }
                    expression = await ResolvePlainExpression(expression, payload);
                }

                // Datenbank ist schneller!
                //result = await CSharpScript.EvaluateAsync(expression);
                result = CalculationService.Calculate(expression);

                Debug.WriteLine($"{expression} ➔ {result}");
            }
            return new ExpressionEvaluationResult { Result = result, Matches = Convert.ToBoolean(result), Protocol = $"{expression} ➔ {result}" };
        }

        private static string ReplaceJsonPathWithValue(string expression, string payload)
        {
            int index = expression.LastIndexOf("]");
            string location = expression.Substring(0, index + 1);
            JsonPath jsonPath = JsonPath.Parse(location);

            JsonNode? jsonNode = JsonNode.Parse(payload);
            PathResult? pathResult = jsonPath.Evaluate(jsonNode);

            var firstMatch = pathResult.Matches?.FirstOrDefault();
            if (firstMatch == null)
            {
                return expression;
            }

            var value = firstMatch.Value?.ToString();
            var isNumeric = double.TryParse(value, out _);
            if (!isNumeric)
            {
                value = $"'{value}'";
            }

            if (location == firstMatch.Location?.ToString())
            {
                expression = expression.Replace(location, value);
            }
            return expression;
        }

        private ExpressionEvaluationResult EvaluateJsonArrayExpression(string expression, string? payload)
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

        private async Task<string> ResolvePlainExpression(string expression, string? payload)
        {
            string inputValue = payload ?? string.Empty;
            if (!double.TryParse(inputValue, NumberStyles.Float, CultureInfo.InvariantCulture, out _) && !inputValue.StartsWith('\'') && !inputValue.StartsWith('"'))
            {
                inputValue = $"'{inputValue}'";
            }
            expression = Regex.Replace(expression, $@"(?<![\w/']){FunctionNames.Input}(?![\w'/])", _ => inputValue, RegexOptions.IgnoreCase);

            return await ExpressionParser.ResolveExpression(expression);
        }
    }
}
