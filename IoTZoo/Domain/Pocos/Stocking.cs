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

public class Stocking : BasePoco
{
   public Stocking()
   {
      StorageBin = new StorageBin();
      Component = new Component();
   }

   public int StockingId
   {
      get;
      init;
   }

   public int StorageBinId
   {
      get
      {
         return StorageBin.StorageBinId;
      }
   }

   public string Sku
   {
      get
      {
         if (null == Component || null == Component.Sku)
         {
            return string.Empty;
         }
         return Component.Sku;
      }

      set
      {
         Component.Sku = value;
      }
   }

   public StorageBin StorageBin
   {
      get;
      set;
   }

   public Component Component
   {
      get;
      set;
   }

   public int Quantity
   {
      get;
      set;
   }
}