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

using Domain.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using System.Reflection;

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

    [Inject]
    protected ILogger<NavMenuBase> Logger
    {
        get; set;
    } = null!;


    private string selectedMenuItem = string.Empty;
   public string SelectedMenuItem
   {
      get => selectedMenuItem;
      set
      {
            try
            {
                if (string.IsNullOrEmpty(value))
                {
                    return;
                }
                selectedMenuItem = value;

                switch (value.ToUpper())
                {
                    case "PHILIPS HUE":
                        NavigationManager.NavigateTo("Hue");
                        break;
                    case "INSTRUCTIONS":
                    case "COMPONENT MANAGEMENT":
                    case "EXAMPLE PROJECTS":
                        // do nothing
                        break;

                    case "TIME ON A TM1637 4 DIGITS DISPLAY":
                        NavigationManager.NavigateTo("EXAMPLEPROJECTS01");
                        break;

                    case "TEMPERATURE ON A TM1637 6 DIGITS DISPLAY":
                        NavigationManager.NavigateTo("EXAMPLEPROJECTS02");
                        break;

                    case "HEART RATE ON A TM1637 4 DIGITS DISPLAY":
                        NavigationManager.NavigateTo("EXAMPLEPROJECTS03");
                        break;

                    case "CONTROL A PHILIPS HUE LAMP WITH A ROTARY ENCODER":
                        NavigationManager.NavigateTo("EXAMPLEPROJECTS04");
                        break;

                    case "HOW DO RULES WORK":
                        NavigationManager.NavigateTo("HOWDORULESWORK");
                        break;

                    case "WHAT IS A TOPIC?":
                        NavigationManager.NavigateTo("INSTRUCTIONSKNOWNTOPICS");
                        break;
                    case "PUBLISH TOPIC":
                        JsRuntime.InvokeVoidAsync("openNewTab", "PublishTopic");
                        break;

                    default:
                        NavigationManager.NavigateTo(value.Replace(" ", string.Empty));
                        break;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"{MethodBase.GetCurrentMethod()} failed!");
            }
        }
   }
}
