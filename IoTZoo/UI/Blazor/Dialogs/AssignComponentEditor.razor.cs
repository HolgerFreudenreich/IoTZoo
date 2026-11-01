// --------------------------------------------------------------------------------------------------------------------
//      ____    ______   _____
//     /  _/___/_  __/  /__  / ____  ____
//     / // __ \/ /       / / / __ \/ __ \
//   _/ // /_/ / /       / /_/ /_/ / /_/ /
//  /___/\____/_/       /____|____/\____/   P L A Y G R O U N D
// --------------------------------------------------------------------------------------------------------------------
// Connect «Things» with microcontrollers in a simple way.
// --------------------------------------------------------------------------------------------------------------------
// (c) 2025 - 2026 Holger Freudenreich under the MIT license
// --------------------------------------------------------------------------------------------------------------------

namespace IotZoo.Dialogs;

using Dapper;
using Domain.Pocos;
using Domain.Services.ComponentManagement;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using System.Reflection;

public class AssignComponentEditorBase : EditorBase
{
   protected IEnumerable<Component>? ComponentsCatalog
   {
      get;
      set;
   } = new List<Component>();

   protected Stocking Stocking
   {
      get;
      set;
   } = null!;

   [Inject]
   protected IStockingService StockingService
   {
      get;
      set;
   } = null!;

   [Parameter]
   public StorageBin? StorageBin { get; set; }

   protected override async Task OnInitializedAsync()
   {
      DialogTitle = "Insert";
      IsNewRecord = true;

      Stocking = new()
      {
         StorageBin = this.StorageBin ?? new(),
         Quantity = 1
      };

      HashCode = GetHashCodeBase64(StorageBin);
      await base.OnInitializedAsync();
   }

   [Inject]
   public IComponentService ComponentService
   {
      get;
      set;
   } = null!;

   protected override void OnInitialized()
   {
      try
      {
         base.OnInitialized();
      }
      catch (Exception ex)
      {
         Logger.LogError(ex, $"{MethodBase.GetCurrentMethod()} failed!");
         throw;
      }
   }

   protected override async Task OnAfterRenderAsync(bool firstRender)
   {
      await base.OnAfterRenderAsync(firstRender);
      if (firstRender)
      {
         await LoadData();
         await InvokeAsync(StateHasChanged);
      }
   }

   protected async Task<IEnumerable<Component?>> SearchComponent(string search, CancellationToken cancellationToken)
   {
      if (string.IsNullOrEmpty(search))
      {
         return ComponentsCatalog!;
      }
      var result = await Task.FromResult(ComponentsCatalog!.Where(x => x.Sku.Contains(search, StringComparison.OrdinalIgnoreCase)).AsList());

      if (1 == result.Count && ComponentsCatalog!.Any(data => data.Sku == search))
      {
         return ComponentsCatalog!;
      }
      return result;
   }

   protected async Task LoadData()
   {
      ComponentsCatalog = await ComponentService.GetComponents();
   }

   protected override Task Cancel()
   {
      return base.Cancel(Stocking);
   }

   protected override async Task Save()
   {
      try
      {
         Snackbar.Clear();

         TrimTextFields(Stocking);

         await this.StockingService.SaveStocking(Stocking);
      }
      catch (Exception ex)
      {
         Logger.LogError(ex, $"{MethodBase.GetCurrentMethod()} failed!");
         Snackbar.Add("Unable to save!", Severity.Error);
      }
      MudDialog.Close(DialogResult.Ok(Stocking));
   }
}