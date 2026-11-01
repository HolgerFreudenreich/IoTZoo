// --------------------------------------------------------------------------------------------------------------------
//      ____    ______   _____
//     /  _/___/_  __/  /__  / ____  ____
//     / // __ \/ /       / / / __ \/ __ \
//   _/ // /_/ / /       / /_/ /_/ / /_/ /
//  /___/\____/_/       /____|____/\____/   P L A Y G R O U N D
// --------------------------------------------------------------------------------------------------------------------
// Connect «Things» with microcontrollers in a simple way.
// --------------------------------------------------------------------------------------------------------------------
// (c) 2025 - 2026 Holger Freudenreich under the MIT license
// --------------------------------------------------------------------------------------------------------------------

using DataAccess.Services;
using Domain.Interfaces;
using Domain.Interfaces.Crud;
using Domain.Pocos;
using Json.Path;
using Microsoft.Extensions.Logging;
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
        int count = 0;
        int index = text.IndexOf(value, StringComparison.Ordinal);
        while (index != -1)
        {
            count++;
            index = text.IndexOf(value, index + value.Length, StringComparison.Ordinal);
        }
        return count;
    }

    /// <summary>
    /// Replaces all variables of the expression with the persisted value.
    /// <param name="expression"></param>
    /// <returns></returns>
    public async Task<string> ResolveVariables(string expression)
    {
        if (IsPlainExpression(expression))
        {
            return await ResolvePlainExpression(expression);
        }

        string output = expression;
        int count = Count(expression, FunctionNames.ReadFromMemory);

        for (int i = 0; i < count; i++)
        {
            output = await ReplaceVariableNameWithVariableContent(output);
        }

        return output;
    }

    private static bool IsPlainExpression(string expression)
    {
        return !expression.StartsWith("{") &&
               !new[] { FunctionNames.ReadFromMemory, FunctionNames.Calc, FunctionNames.Script }
                   .Any(function => expression.Contains(function, StringComparison.OrdinalIgnoreCase));
    }

    private async Task<string> ResolvePlainExpression(string expression)
    {
        expression = expression.Replace(FunctionNames.Calc, string.Empty, StringComparison.OrdinalIgnoreCase);
        string output = string.Empty;

        foreach (string item in expression.Split(" "))
        {
            string? value = char.IsLetter(item[0]) ? await GetTopicPayload(item) : item;
            if (value == null)
            {
                return expression;
            }
            output += value + " ";
        }

        return output.TrimEnd();
    }

    private async Task<string?> GetTopicPayload(string topic)
    {
        TopicEntry? topicFromMemory = (from data in DataTransferService.ReceivedTopicsQueue where data.Topic == topic select data).SingleOrDefault();
        if (topicFromMemory != null)
        {
            return topicFromMemory.Payload ?? string.Empty;
        }

        KnownTopic? knownTopic = await KnownTopicsDatabaseService.GetKnownTopicByTopicName("", topic); // fixme Holger
        return knownTopic == null ? null : knownTopic.LastPayload ?? string.Empty;
    }

    /// <summary>
    /// </summary>
    /// <param name="input">Something like Read('colors')['green']</param>
    /// <param name="payload"></param>
    /// <param name="indexEnd"></param>
    /// <returns></returns>
    private string HandleJsonPathArrayIndexer(string input, string? payload, int startIndex, int indexEnd)
    {
        if (string.IsNullOrEmpty(payload))
        {
            return input;
        }

        try
        {
            int arrayIndexerStartIndex = indexEnd + 3;
            int arrayIndexerEndIndex = input.IndexOf("]", arrayIndexerStartIndex);
            string arrayIndexer = input.Substring(arrayIndexerStartIndex, arrayIndexerEndIndex - arrayIndexerStartIndex - 1);

            string? value = EvaluateArrayIndexer(payload, arrayIndexer);
            if (value != null)
            {
                string segment = input.Substring(startIndex, arrayIndexerEndIndex - startIndex + 1);
                return input.Replace(segment, value);
            }
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, $"{MethodBase.GetCurrentMethod()} failed!");
        }
        return input;
    }

    private static string? EvaluateArrayIndexer(string payload, string arrayIndexer)
    {
        JsonNode? jsonNode = JsonNode.Parse(payload);
        PathResult? pathResult = JsonPath.Parse($"$.*.{arrayIndexer}").Evaluate(jsonNode);
        if (pathResult.Matches is not { Count: > 0 })
        {
            pathResult = JsonPath.Parse($"$.{arrayIndexer}").Evaluate(jsonNode);
        }
        return pathResult.Matches?.FirstOrDefault()?.Value?.ToString();
    }

    protected async Task<string> ReplaceVariableNameWithVariableContent(string input)
    {
        int startIndex = input.IndexOf(FunctionNames.ReadFromMemory, StringComparison.Ordinal);
        if (-1 == startIndex)
        {
            return input;
        }

        ReadCall call = ParseReadCall(input, startIndex);

        TopicEntry? topicFromMemory = (from data in DataTransferService.ReceivedTopicsQueue where data.FullQualifiedTopic == call.NettoTopicName select data).SingleOrDefault();
        if (null != topicFromMemory && !string.IsNullOrEmpty(topicFromMemory.Payload))
        {
            return ResolveFromMemory(input, call, topicFromMemory.Payload);
        }

        return await ResolveFromDatabase(input, call);
    }

    private static ReadCall ParseReadCall(string input, int startIndex)
    {
        int indexEnd = input.IndexOf(")", StringComparison.Ordinal);
        bool hasBrackets = true;
        if (-1 == indexEnd)
        {
            indexEnd = input.IndexOf(" ", StringComparison.Ordinal);
            hasBrackets = false;
        }

        string formula = input.Substring(startIndex, indexEnd - startIndex + 1).Trim();
        string topicName = hasBrackets
            ? formula.Substring(FunctionNames.ReadFromMemory.Length + 1, formula.Length - FunctionNames.ReadFromMemory.Length - 2)
            : formula.Substring(FunctionNames.ReadFromMemory.Length, formula.Length - FunctionNames.ReadFromMemory.Length);

        string[] topicArguments = topicName.Split(',');
        string nettoTopicName = CleanArgument(topicArguments[0]);
        string? defaultValue = topicArguments.Length > 1 ? CleanArgument(topicArguments[1]) : null;

        bool isArrayIndexer = input.Length > indexEnd + 1 && input[indexEnd + 1] == '[';

        return new ReadCall(formula, topicName, nettoTopicName, defaultValue, hasBrackets, isArrayIndexer, startIndex, indexEnd);
    }

    private static string CleanArgument(string argument)
    {
        return argument.Replace("'", string.Empty).Replace("\\\"", string.Empty).Trim();
    }

    private string ResolveFromMemory(string input, ReadCall call, string payload)
    {
        if (call.IsArrayIndexer)
        {
            return HandleJsonPathArrayIndexer(input, payload, call.StartIndex, call.IndexEnd);
        }

        string key = call.HasBrackets
            ? $"{FunctionNames.ReadFromMemory}({call.TopicName})"
            : $"{FunctionNames.ReadFromMemory}{call.TopicName}";
        return input.Replace(key, payload);
    }

    private async Task<string> ResolveFromDatabase(string input, ReadCall call)
    {
        KnownTopic? topicFromDatabase = await KnownTopicsDatabaseService.GetKnownTopicByTopicName("", call.NettoTopicName); // fixme Holger
        if (null != topicFromDatabase && !string.IsNullOrEmpty(topicFromDatabase.LastPayload))
        {
            if (call.IsArrayIndexer)
            {
                return HandleJsonPathArrayIndexer(input, topicFromDatabase.LastPayload, call.StartIndex, call.IndexEnd);
            }
            return input.Replace($"{FunctionNames.ReadFromMemory}({call.TopicName})", topicFromDatabase.LastPayload);
        }

        if (null != call.DefaultValue)
        {
            return input.Replace(call.Formula, call.DefaultValue);
        }

        return string.Empty;
    }

    private sealed record ReadCall(string Formula, string TopicName, string NettoTopicName, string? DefaultValue, bool HasBrackets, bool IsArrayIndexer, int StartIndex, int IndexEnd);
}
