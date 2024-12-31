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

namespace DataAccess.Services
{
   public static class FunctionNames
   {
      public const string ReadFromMemory = "Read"; // Example: Read('Variable1')
      public const string Calc = "Calc";           // Example: Calc(Read('Variable1') + 1)
      public const string Input = "input";         // Example: input > 5; input1 == "Some Text"
      public const string Script = "::";           // Example: ::GetCalendarWeek();
   }
}

