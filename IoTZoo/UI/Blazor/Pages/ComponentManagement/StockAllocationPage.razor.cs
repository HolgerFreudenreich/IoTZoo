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
using IotZoo.Dialogs;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace IotZoo.Pages.ComponentManagement
{
   public class StockAllocationPageBase : PageBase
   {
      [Inject]
      private IStockingService StockingService
      {
         get;
         set;
      } = null!;

      protected List<StorageBin>? StorageBins { get; set; } = new();

      protected IEnumerable<Component>? ComponentsCatalog
      {
         get;
         set;
      } = new List<Component>();


      private Component? selectedComponent;
      protected Component? SelectedComponent
      {
         get => selectedComponent;
         set
         {
            if (selectedComponent != value)
            {
               selectedComponent = value;
               _ = OnComponentSelected(value);
            }
         }
      }

      private async Task OnComponentSelected(Component? component)
      {
         if (component == null)
         {
            StorageBins = await StockingService.GetStorageBins();
         }
         else
         {
            StorageBins = await StockingService.GetStorageBins(component);
         }
      }

      protected override async Task OnAfterRenderAsync(bool firstRender)
      {
         base.OnAfterRender(firstRender);
         if (firstRender)
         {
            await OnComponentSelected(SelectedComponent);
            await LoadData();
         }
      }

      protected override void OnInitialized()
      {
         DataTransferService.CurrentScreen = ScreenMode.StorageAllocation;
         base.OnInitialized();
      }

      protected override async Task LoadData()
      {
         ComponentsCatalog = await StockingService.GetComponents();

         await InvokeAsync(StateHasChanged);
      }

      protected async Task OpenAssignComponentEditor(StorageBin storageBin)
      {
         try
         {
            var options = GetDialogOptions();

            var parameters = new DialogParameters { ["StorageBin"] = storageBin };
            IsEditorOpen = true;
            var dialog = await DialogService.ShowAsync<AssignComponentEditor>("Assign Component Editor",
                                                                                   parameters,
                                                                                   options);
            var result = await dialog.Result;
         }
         finally
         {
            IsEditorOpen = false;
            await LoadData();
         }
      }
   }
}
