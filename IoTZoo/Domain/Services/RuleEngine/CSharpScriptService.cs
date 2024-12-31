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
using Domain.Interfaces.RuleEngine;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Domain.Services.RuleEngine;

public class CSharpScriptService : IScriptService
{
   protected ILogger<CSharpScriptService> Logger { get; }
   protected IScriptCrudService ScriptCrudService { get; }

   public CSharpScriptService(ILogger<CSharpScriptService> logger, IScriptCrudService scriptCrudService)
   {
      Logger = logger;
      ScriptCrudService = scriptCrudService;
   }

   public async Task<string> ProcessScript(string scriptName, string inputData)
   {
      var script = await ScriptCrudService.LoadScript(scriptName);
      if (script == null)
      {
         Logger.LogError($"Script {scriptName} not found!");
         return string.Empty;
      }
      string? result = await ProcessScript(script, inputData);
      if (result == null)
      {
         return string.Empty;
      }
      return result;
   }

   public async Task<string> ProcessScript(Pocos.Script script, string inputData)
   {
      string? baseCode = script.SourceCode;

      string sourceCode = string.Empty;

      if (!string.IsNullOrEmpty(inputData))
      {
         sourceCode = baseCode + $"{Environment.NewLine}var input = {inputData};{Environment.NewLine}return {script.ScriptName}(input);";
      }
      else
      {
         sourceCode = baseCode + $"{Environment.NewLine}return {script.ScriptName}();";
      }
      ScriptOptions scriptOptions = GetScriptOptions();

      if (string.IsNullOrEmpty(sourceCode))
      {
         Logger.LogError($"Script {script.ScriptName} has no code!");
      }

      var result = await CSharpScript.EvaluateAsync(sourceCode, scriptOptions);
      if (null == result)
      {
         return string.Empty;
      }
      return result.ToString()!;
   }

   public async Task<string?> EvaluateScript(Pocos.Script script)
   {
      ScriptOptions scriptOptions = GetScriptOptions();
      object result = null!;
      try
      {
         result = await CSharpScript.EvaluateAsync(script.SourceCode, scriptOptions);
      }
      catch (CompilationErrorException compilationErrorException)
      {
         return compilationErrorException.GetBaseException().Message;
      }
      catch (Exception ex)
      {
         return ex.GetBaseException().Message;
      }

      return null; // no error!
   }

   private ScriptOptions GetScriptOptions()
   {
      ScriptOptions scriptOptions = ScriptOptions.Default.AddReferences(typeof(JsonSerializer).Assembly).
                                                                        AddReferences(typeof(MudBlazor.Color).Assembly).
                                                                        AddImports("System.Text.Json").
                                                                        AddImports("MudBlazor");
      return scriptOptions;
   }
}
