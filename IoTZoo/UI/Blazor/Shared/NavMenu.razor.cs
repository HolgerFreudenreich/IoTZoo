// --------------------------------------------------------------------------------------------------------------------
//      ____    ______   _____
//     /  _/___/_  __/  /__  / ____  ____
//     / // __ \/ /       / / / __ \/ __ \
//   _/ // /_/ / /       / /_/ /_/ / /_/ /
//  /___/\____/_/       /____|____/\____/ 
// --------------------------------------------------------------------------------------------------------------------
// Connect «Things» with microcontrollers without programming knowledge.
// MIT License
// --------------------------------------------------------------------------------------------------------------------
// (c) 2025 Holger Freudenreich under MIT license
// --------------------------------------------------------------------------------------------------------------------

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace IotZoo.Shared;

public class NavMenuBase : ComponentBase
{
   [Inject]
   protected NavigationManager NavigationManager
   {
      get;
      set;
   } = null!;

   [Inject]
   protected IJSRuntime JsRuntime { get; set; } = null!;

   private string selectedMenuItem = string.Empty;
   public string SelectedMenuItem
   {
      get => selectedMenuItem;
      set
      {
         if (string.IsNullOrEmpty(value))
         {
            return;
         }
         selectedMenuItem = value;

         switch (value)
         {
            case "Philips Hue":
               NavigationManager.NavigateTo("Hue");
               break;
            case "Instructions":
            case "Component management":
            case "Example projects":
               // do nothing
               break;

            case "Time on a TM1637 4 digits display":
               NavigationManager.NavigateTo("ExampleProjects01");
               break;

            case "Temperature on a TM1637 6 digits display":
               NavigationManager.NavigateTo("ExampleProjects02");
               break;

            case "Heart rate on a TM1637 4 digits display":
               NavigationManager.NavigateTo("ExampleProjects03");
               break;

            case "Control a Philips HUE lamp with a rotary encoder":
               NavigationManager.NavigateTo("ExampleProjects04");
               break;

            case "How do Rules work":
               NavigationManager.NavigateTo("HowDoRulesWork");
               break;

            case "What is a Topic?":
               NavigationManager.NavigateTo("InstructionsKnownTopics");
               break;
            case "Publish Topic":
               JsRuntime.InvokeVoidAsync("openNewTab", "PublishTopic");
               break;

            default:
               NavigationManager.NavigateTo(value.Replace(" ", string.Empty));
               break;
         }
      }
   }
}
