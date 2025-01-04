// --------------------------------------------------------------------------------------------------------------------
//      ____    ______   _____
//     /  _/___/_  __/  /__  / ____  ____
//     / // __ \/ /       / / / __ \/ __ \
//   _/ // /_/ / /       / /_/ /_/ / /_/ /
//  /___/\____/_/       /____|____/\____/   P L A Y G R O U N D
//
// --------------------------------------------------------------------------------------------------------------------
// (c) 2025 Holger Freudenreich under MIT license
// --------------------------------------------------------------------------------------------------------------------
// Editor for boxes/totes. Each box can contain one or more components.
// It would also be conceivable to put boxes in boxes.
// --------------------------------------------------------------------------------------------------------------------

namespace IotZoo.Dialogs;

using Domain.Interfaces.Crud;
using Domain.Pocos;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using System.Reflection;

public class BoxEditorBase : EditorBase
{
   [Parameter]
   public StorageBin Box { get; set; } = null!;

   protected override async Task OnInitializedAsync()
   {
      if (Box.StorageBinId <= 0)
      {
         DialogTitle = "Add Box";
         IsNewRecord = true;
      }
      else
      {
         DialogTitle = "Edit Box";
         IsNewRecord = false;
      }

      HashCode = GetHashCodeBase64(Box);
      await base.OnInitializedAsync();
   }

   [Inject]
   private IStorageBinCrudService BoxService
   {
      get;
      set;
   } = null!;

   protected override async Task Cancel()
   {
      await Cancel(Box);
   }

   protected override async Task Save()
   {
      try
      {
         Snackbar.Clear();

         TrimTextFields(Box);
         await BoxService.SaveStorageBin(Box);
      }
      catch (Exception exception)
      {
         Logger.LogError(exception, $"{MethodBase.GetCurrentMethod()} failed!");
         Snackbar.Add("Unable to save!", Severity.Error);
      }
      MudDialog.Close(DialogResult.Ok(Box));
   }
}