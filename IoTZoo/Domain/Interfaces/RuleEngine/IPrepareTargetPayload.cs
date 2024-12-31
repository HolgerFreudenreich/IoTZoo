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

using Domain.Pocos;

namespace Domain.Interfaces.RuleEngine
{
   public interface IPrepareTargetPayload
   {
      Task<string?> Execute(Rule rule, TopicEntry currentTopic);

      /// <summary>
      /// Resolves the expression.
      /// </summary>
      /// <param name="expression">Example: $"{FunctionNames.Script}(TemperatureToColor({FunctionNames.Input}));";</param>
      /// <param name="payload">12.5</param>
      /// <returns></returns>
      Task<string?> Execute(string? expression, string? payload);
   }
}