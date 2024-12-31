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

using IotZoo.Controller;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using System.Reflection;

namespace IotZoo.Dialogs;

public class AboutDialogBase : DialogBase
{
   [CascadingParameter]
   protected MudDialogInstance MudDialog { get; set; } = null!;

   [Inject]
   protected NavigationManager NavigationManager
   {
      get;
      set;
   } = null!;

   protected string Version { get; set; } = null!;

   public void OkBtnPress()
   {
      MudDialog.Close();
   }

   protected override async Task OnAfterRenderAsync(bool firstRender)
   {
      await base.OnAfterRenderAsync(firstRender);
      if (firstRender)
      {
         Snackbar.Add("😜 Greetings to all makers!", Severity.Info);
         Version = VersionController.Version;

         if (string.IsNullOrEmpty(Version))
         {
            Version = "unknown";
         }
         await InvokeAsync(StateHasChanged);
      }
   }
}