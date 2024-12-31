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
using Domain.Services.ComponentManagement;
using Microsoft.AspNetCore.Components;

namespace IotZoo.Components;

public class ElectricalComponentsBase : ComponentBase
{
   [Inject]
   IStockingService StockingService
   {
      get;
      set;
   } = null!;

   [Parameter]

   public StorageBin StorageBin { get; set; } = null!;

   protected override async Task OnInitializedAsync()
   {
      await LoadData(StorageBin);
      await base.OnInitializedAsync();
   }

   protected async Task LoadData(StorageBin storageBinLocal)
   {
      this.Stockings = await StockingService.GetStockings(storageBinLocal);
   }

   protected List<Stocking>? Stockings
   {
      get;
      set;
   }
}

