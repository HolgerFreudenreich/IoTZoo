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
using Microsoft.JSInterop;

namespace IotZoo.Pages;

public class InstructionsRunOnDockerPageBase : PageBase
{
   [Inject]
   protected IJSRuntime JsRuntime { get; set; } = null!;

   protected override void OnInitialized()
   {
      DataTransferService.CurrentScreen = ScreenMode.InstructionsRunOnDocker;
      base.OnInitialized();
   }

   protected void CopyToClipboard(string text)
   {
      JsRuntime.InvokeVoidAsync("clipboardCopy.copyText", text);
      Snackbar.Add("Copied to clipboard", MudBlazor.Severity.Info);
   }
}
