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
using Microsoft.AspNetCore.Components;
using MudBlazor;
using System.Reflection;

namespace IotZoo.Dialogs;

public class HueLightEditorBase : EditorBase
{
   [Parameter]

   public HueComponent HueLight { get; set; } = null!;

   protected override async Task OnInitializedAsync()
   {
      DialogTitle = "Edit Hue Light";
      IsNewRecord = false;

      HashCode = GetHashCodeBase64(HueLight);
      await base.OnInitializedAsync();
   }

   protected override async Task Cancel()
   {
      // Gibt es nicht gespeicherte Änderungen?
      var hashCodeTmp = GetHashCodeBase64(HueLight);
      if (hashCodeTmp != HashCode)
      {
         bool? result = await DialogService.ShowMessageBox("Warning!",
                                                           $"There are unsaved changes! Do you want to cancel without saving?",
                                                           yesText: "Yes", cancelText: "No");
         if (!result.HasValue)
         {
            return;
         }
      }
      MudDialog.Cancel();
   }

   protected override Task Save()
   {
      try
      {
         Snackbar.Clear();
         // todo: call Service to save the data.
      }
      catch (Exception ex)
      {
         Logger.LogError(ex, $"{MethodBase.GetCurrentMethod()} failed!");
         Snackbar.Add("Unable to save!", Severity.Error);
      }
      MudDialog.Close(DialogResult.Ok(HueLight));
      return Task.CompletedTask;
   }
}