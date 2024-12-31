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

namespace IotZoo.Dialogs;

using Domain.Pocos;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using System.Reflection;

public class PinEditorBase : EditorBase
{
   [Parameter]

   public ConnectedDevice ConnectedDevice { get; set; } = null!;

   protected override async Task OnInitializedAsync()
   {
      DialogTitle = "Edit Pins";
      IsNewRecord = false;

      HashCode = GetHashCodeBase64(ConnectedDevice);
      await base.OnInitializedAsync();
   }

   protected override async Task Cancel()
   {
      await base.Cancel(ConnectedDevice);
   }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
   protected override async Task Save()
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
   {
      try
      {
         Snackbar.Clear();
         TrimTextFields(ConnectedDevice);
      }
      catch (Exception ex)
      {
         Logger.LogError(ex, $"{MethodBase.GetCurrentMethod()} failed!");
         Snackbar.Add("Unable to save!", Severity.Error);
      }
      MudDialog.Close(DialogResult.Ok(ConnectedDevice));
   }
}