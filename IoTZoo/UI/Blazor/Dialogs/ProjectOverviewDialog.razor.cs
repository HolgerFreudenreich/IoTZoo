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

using Domain.Interfaces;
using Domain.Pocos;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using System.Text;

namespace IotZoo.Dialogs;

public class ProjectOverviewDialogBase : ComponentBase
{
   [CascadingParameter]
   protected MudDialogInstance MudDialog { get; set; } = null!;

   [Parameter]
   public Project Project { get; set; } = null!;

   [Inject]
   protected IMicrocontrollerService MicrocontrollerService { get; set; } = null!;

   [Inject]
   protected IDataTransferService DataTransferService { get; set; } = null!;

   protected string HtmlContent { get; set; } = string.Empty;

   protected override async Task OnAfterRenderAsync(bool firstRender)
   {
      await base.OnAfterRenderAsync(firstRender);
      if (firstRender)
      {
         var microcontrollers = await MicrocontrollerService.GetMicrocontrollers(Project);

         StringBuilder sb = new StringBuilder();
         sb.AppendLine("<pre class=\"mermaid\">");
         sb.AppendLine("%%{init:{'theme': 'base','themeVariables': {'fontSize': '16px'}}}%%");
         sb.AppendLine("mindmap");
         sb.AppendLine($")MQTT Broker {this.DataTransferService.MqttBrokerSettings.ToString()}(");
         sb.AppendLine("  (IoT Zoo)");
         foreach (var microcontroller in microcontrollers)
         {
            sb.AppendLine($"  ESP㉜ Mac {microcontroller.MacAddress}");
            try
            {
               var connectedDevices = await MicrocontrollerService.GetDeviceConfig(microcontroller);
               foreach (var connectedDevice in connectedDevices)
               {
                  sb.AppendLine($"    {connectedDevice.DeviceType}");
                  if (connectedDevice.Pins != null)
                  {
                     string pins = "      ";
                     foreach (var pin in connectedDevice.Pins)
                     {
                        pins += $"{pin.PinName} → PIN {pin.MicrocontrollerGpoPin} ";
                     }
                     sb.AppendLine(pins);
                  }
               }
            }
            catch (Exception)
            {
               sb.AppendLine($"    offline! Loading config from device via REST interface failed!");
            }
         }
         sb.AppendLine("</pre>");
         HtmlContent = sb.ToString();
         await InvokeAsync(StateHasChanged);
      }
   }

   public void OkBtnPress()
   {
      MudDialog.Close();
   }
}
