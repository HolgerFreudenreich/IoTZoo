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
using Domain.Interfaces.RuleEngine;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Domain.Services.RuleEngine;


public class ScriptsResolver : IScriptsResolver
{
   IScriptService ScriptService { get; set; }

   public ScriptsResolver(IScriptService scriptService)
   {
      ScriptService = scriptService;
   }

   public async Task<string> ResolveScripts(string expression)
   {
      int occurrences = Regex.Matches(expression, Regex.Escape(FunctionNames.Script)).Count;

      for (int index = 0; index < occurrences; index++)
      {
         ScriptCall call = ParseScriptCall(expression);
         string scriptResult = await ScriptService.ProcessScript(call.Name, call.Data);

         int endOfPath = FindPathEnd(expression, call.EndOfCall);
         if (endOfPath > call.EndOfCall)
         {
            scriptResult = ResolveJsonPath(scriptResult, expression.Substring(call.EndOfCall, endOfPath - call.EndOfCall));
         }

         expression = expression.Substring(0, call.StartOfCall) + scriptResult + expression.Substring(endOfPath);
      }
      return expression;
   }

   private static ScriptCall ParseScriptCall(string expression)
   {
      int startOfCall = expression.IndexOf(FunctionNames.Script, StringComparison.Ordinal);
      int startOfName = startOfCall + FunctionNames.Script.Length;
      int startOfData = expression.IndexOf('(', startOfName + 1);
      int endOfData = expression.IndexOf(')', startOfData + 1);

      string name = expression.Substring(startOfName, startOfData - startOfName).Trim('(', ')');
      string data = expression.Substring(startOfData + 1, endOfData - startOfData - 1);

      return new ScriptCall(name, data, startOfCall, endOfData + 1);
   }

   private static int FindPathEnd(string expression, int start)
   {
      int end = start;
      while (end < expression.Length && (expression[end] == '[' || expression[end] == '.'))
      {
         end = expression[end] == '['
            ? expression.IndexOf(']', end) + 1
            : SkipIdentifier(expression, end + 1);
      }
      return end;
   }

   private static int SkipIdentifier(string expression, int position)
   {
      while (position < expression.Length && (char.IsLetterOrDigit(expression[position]) || expression[position] == '_'))
      {
         position++;
      }
      return position;
   }

   private sealed record ScriptCall(string Name, string Data, int StartOfCall, int EndOfCall);

   protected static string ResolveJsonPath(string json, string path)
   {
      using JsonDocument document = JsonDocument.Parse(json);
      JsonElement element = document.RootElement;
      foreach (Match match in Regex.Matches(path, @"\[(\d+)\]|\.(\w+)"))
      {
         element = match.Groups[1].Success ? element[int.Parse(match.Groups[1].Value)] : element.GetProperty(match.Groups[2].Value);
      }
      return element.ValueKind == JsonValueKind.String ? element.GetString()! : element.GetRawText();
   }
}
