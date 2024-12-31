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

using System.Text.Json.Serialization;

namespace Domain.Pocos;

public class Project : BasePoco
 {
   public int ProjectId { get; set; }

   public string ProjectName { get; set; } = string.Empty;

   public string? Description { get; set; }

   /// <summary>
   /// true, if it is a new created item; otherwise false.
   /// </summary>
   [JsonIgnore]
   public bool IsNew { get; set; } = false;

   public override int GetHashCode() => ProjectName.GetHashCode();

   // To display correctly in MudSelect, MutAutocomplete.
   public override string ToString() => ProjectName;
}
