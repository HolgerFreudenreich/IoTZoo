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

using Domain.Interfaces.RuleEngine;
using Microsoft.Extensions.Logging;

namespace Domain.Services.RuleEngine
{
   public interface IExpressionParser
   {
      public Task<string> ResolveExpression(string expression);
   }

   public interface IVariablesResolver
   {
      public Task<string> ResolveVariables(string expression);
   }

   public class ExpressionParser : IExpressionParser
   {
      protected ILogger<ExpressionParser> Logger { get; }

      protected IVariablesResolver VariablesResolver { get; }

      protected IScriptsResolver ScriptsResolver { get; }

      public ExpressionParser(ILogger<ExpressionParser> logger,
                              IVariablesResolver variablesParser,
                              IScriptsResolver scriptExecutor)
      {
         Logger = logger;
         VariablesResolver = variablesParser;
         ScriptsResolver = scriptExecutor;
      }

      public async Task<string> ResolveExpression(string expression)
      {
         if (string.IsNullOrEmpty(expression))
         {
            return expression;
         }
         // 1. Replace variable names with variable values.
         expression = await VariablesResolver.ResolveVariables(expression);

         // 2. Replace function names with function results.
         expression = await ScriptsResolver.ResolveScripts(expression);

         return expression;
      }
   }
}
