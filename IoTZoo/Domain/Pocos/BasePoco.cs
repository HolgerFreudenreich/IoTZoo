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

public class BasePoco
{
   /// <summary>
   /// Creation DateTime on database. This field is managed by the database!
   /// </summary>
   [JsonIgnore]
   public DateTime? CreatedAt
   {
      get;
      set;
   }

   /// <summary>
   /// Modified DateTime on database. This field is managed by the database!
   /// </summary>
   [JsonIgnore]
   public DateTime? UpdatedAt
   {
      get;
      set;
   }
}