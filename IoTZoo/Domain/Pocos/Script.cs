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

public class Script : BasePoco
{
   public int ScriptId { get; set; }

   public string ScriptName { get; set; } = string.Empty;

   public int ScriptLanguageId { get; set; } = 0;

   public string? Author { get; set; } = string.Empty;

   public string Description { get; set; } = string.Empty;

   public string SourceCode { get; set; } = string.Empty;
}
