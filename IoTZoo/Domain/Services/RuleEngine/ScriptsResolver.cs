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
using System.Text.RegularExpressions;

namespace Domain.Services.RuleEngine;


public class ScriptsResolver : IScriptsResolver
{
   IScriptService ScriptService { get; set; }

   public ScriptsResolver(IScriptService scriptService)
   {
      ScriptService = scriptService;
   }

   protected IEnumerable<int> GetAllIndexes(string source, string matchString)
   {
      matchString = Regex.Escape(matchString);
      foreach (Match match in Regex.Matches(source, matchString))
      {
         yield return match.Index;
      }
   }

   public async Task<string> ResolveScripts(string expression)
   {
      var indexes = GetAllIndexes(expression, FunctionNames.Script).ToList();
      int count = indexes.Count;

      for (int index = 0; index < count; index++)
      {
         int startIndexScript = expression.IndexOf($"{FunctionNames.Script}");
         startIndexScript += FunctionNames.Script.Length;

         bool considerRoundBrackets = expression[FunctionNames.Script.Length] == '(';

         int endIndexScriptName = expression.IndexOf("(", startIndexScript + 1);
         string scriptName = string.Empty;

         if (!considerRoundBrackets)
         {
            scriptName = expression.Substring(startIndexScript, endIndexScriptName - startIndexScript);
         }
         else
         {
            scriptName = expression.Substring(startIndexScript + 1, endIndexScriptName - startIndexScript - 1);
         }

         int startIndexData = expression.IndexOf("(", endIndexScriptName);
         int endIndexData = expression.IndexOf(")", endIndexScriptName + 1);
         string data = expression.Substring(startIndexData + 1, endIndexData - startIndexData - 1);

         string scriptResult = await ScriptService.ProcessScript(scriptName, data);
         expression = expression.Remove(expression.IndexOf($"{FunctionNames.Script}")) + scriptResult + expression.Substring(endIndexData + 1);
      }
      return expression;
   }
}
