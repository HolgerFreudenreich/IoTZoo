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
using Domain.Services.ComponentManagement;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using System.Reflection;


public class ComponentEditorBase : EditorBase
{
   [Parameter]

   public Component Component { get; set; } = null!;

   protected override async Task OnInitializedAsync()
   {
      if (Component.ComponentId <= 0)
      {
         DialogTitle = "Add Component";
         IsNewRecord = true;
      }
      else
      {
         DialogTitle = "Edit Component";
         IsNewRecord = false;
      }

      HashCode = GetHashCodeBase64(Component);
      await base.OnInitializedAsync();
   }

   [Inject]
   private IComponentService ComponentService
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

   protected override async Task Cancel()
   {
      await base.Cancel(Component);
   }

   protected override async Task Save()
   {
      try
      {
         Snackbar.Clear();

         TrimTextFields(Component);
         await ComponentService.SaveComponent(Component);
      }
      catch (Exception ex)
      {
         Logger.LogError(ex, $"{MethodBase.GetCurrentMethod()} failed!");
         Snackbar.Add("Unable to save component!", Severity.Error);
      }
      MudDialog.Close(DialogResult.Ok(Component));
   }
}