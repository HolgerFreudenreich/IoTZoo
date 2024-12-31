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

public class StorageBin : BasePoco
{
   public int StorageBinId
   {
      get;
      init;
   }

   public string BoxNr
   {
      get;
      set;
   } = string.Empty;

   /// <summary>
   /// For Sorting
   /// </summary>
   public int BoxNrNumeric
   {
      get
      {
         if (!int.TryParse(BoxNr,
                           out var number))
         {
            return -1;
         }

         return number;
      }
   }

   public string? ParentBoxNr
   {
      get;
      init;
   }
}