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

namespace Domain.Pocos;

public class ExpressionEvaluationResult
{
   public bool Matches { get; set; }
   public string? Protocol { get; set; }
   public object? Result { get; internal set; }
}

public class ExpressionPoco
{
   /// <summary>
   /// Operator like >, <, <=, >=, Contains
   /// </summary>
   public string? Operator { get; set; }

   /// <summary>
   /// The value to be checked against.
   /// </summary>
   public string? Value { get; set; }
}
