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

using Domain.Interfaces;
using Domain.Pocos;
using IotZoo.Dialogs;
using IotZoo.Pages;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace IotZoo.Shared
{
   public partial class MainLayout : LayoutComponentBase
   {
      [Inject]
      IDataTransferService DataTransferService
      {
         get;
         set;
      } = null!;

      [Inject]
      protected IDialogService DialogService
      {
         get;
         set;
      } = null!;

      bool drawerOpen = true;

      string Title { get; set; } = "IoT Zoo";

      protected MudThemeProvider mudThemeProvider = null!;

      void DrawerToggle()
      {
         drawerOpen = !drawerOpen;
      }

      string GetTitle()
      {
         Title = "IoT Zoo";
         switch (DataTransferService.CurrentScreen)
         {
            case ScreenMode.MqttExplorer:
               Title = "MQTT Message Explorer";
               break;
            case ScreenMode.Settings:
               Title = "Settings";
               break;
            case ScreenMode.KnownMicrocontrollers:
               Title = "Known Microcontrollers";
               break;
            case ScreenMode.KnownTopics:
               Title = "Known Topics";
               break;
            case ScreenMode.TopicsHistory:
               Title = "Topic History";
               break;
            case ScreenMode.Rules:
               Title = "Rules";
               break;
            case ScreenMode.PublishTopic:
               Title = "Publish Topic";
               break;
            case ScreenMode.SetLocation:
               Title = "Set Location";
               break;
            case ScreenMode.HueLights:
               Title = "Philips Hue Lights";
               break;
            case ScreenMode.Projects:
               Title = "Projects";
               break;
            case ScreenMode.Components:
               Title = "Components";
               break;
            case ScreenMode.Boxes:
               Title = "Boxes";
               break;
            case ScreenMode.StorageAllocation:
               Title = "Storage Allocation (What is in which Box?)";
               break;
            case ScreenMode.CoreConcept:
               Title = "Core Concept";
               break;
            case ScreenMode.HowDoRulesWork:
               Title = "How do Rules work";
               break;
            case ScreenMode.HowToFlashFirmware:
               Title = "How to flash the IoTZoo Firmware to the Microcontroller?";
               break;
            case ScreenMode.InstructionsRunOnDocker:
               Title = "Run IoT Zoo on Docker";
               break;
            case ScreenMode.ExampleProject01:
               Title = "Example Project 1: Time on a TM1637 4 digits display";
               break;
            case ScreenMode.ExampleProject02:
               Title = "Example Project 2: Temperature on a TM1637 6 digits display";
               break;
            case ScreenMode.ExampleProject03:
               Title = "Example Project 1: Heart rate on a TM1637 4 digits display";
               break;
            case ScreenMode.ExampleProject04:
               Title = "Example Project 4: Control a Philips HUE lamp with a rotary encoder";
               break;
            case ScreenMode.InstructionsKnownTopics:
               Title = "What is a Topic?";
               break;
            case ScreenMode.Scripts:
               Title = "Scripts";
               break;
            case ScreenMode.ThingsToConnect:
               Title = "Things to connect";
               break;
            case ScreenMode.TestPage:
               Title = "Test Page";
               break;
         }

         return Title;
      }

      async void Refresh()
      {
         Title = GetTitle();
         await InvokeAsync(StateHasChanged);
      }

      protected override Task OnInitializedAsync()
      {
         AppStatus.OnChange -= Refresh;
         AppStatus.OnChange += Refresh;
         return base.OnInitializedAsync();
      }

      protected async Task AboutBtnPress()
      {
         var dialog = await this.DialogService.ShowAsync<AboutDialog>("About");

         DialogResult? result = await dialog.Result;
      }

      public void Dispose()
      {
         AppStatus.OnChange -= Refresh;
      }
   }
}
