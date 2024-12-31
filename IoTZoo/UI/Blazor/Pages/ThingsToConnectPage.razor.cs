// --------------------------------------------------------------------------------------------------------------------
//      ____    ______   _____
//     /  _/___/_  __/  /__  / ____  ____
//     / // __ \/ /       / / / __ \/ __ \
//   _/ // /_/ / /       / /_/ /_/ / /_/ /
//  /___/\____/_/       /____|____/\____/ 
// --------------------------------------------------------------------------------------------------------------------
// Connect «Things» with microcontrollers without programming knowledge.
// --------------------------------------------------------------------------------------------------------------------
// (c) 2025 Holger Freudenreich under MIT license
// --------------------------------------------------------------------------------------------------------------------

using Domain.Pocos;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;


namespace IotZoo.Pages;

public class ThingsToConnectPageBase : PageBase
{
   protected override void OnInitialized()
   {
      DataTransferService.CurrentScreen = ScreenMode.ThingsToConnect;
   }


}
