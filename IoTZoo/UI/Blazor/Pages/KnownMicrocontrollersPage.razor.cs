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
using Domain.Interfaces.Crud;
using Domain.Pocos;
using Domain.Services.Timer;
using IotZoo.Dialogs;
using Microsoft.AspNetCore.Components;
using MQTTnet.Client;
using MudBlazor;
using System.Reflection;

namespace IotZoo.Pages;

public class KnownMicrocontrollersPageBase : PageBase, IDisposable
{
   TimerService timerService = null!;

   [Inject]
   protected IMicrocontrollerService MicrocontrollerService
   {
      get;
      set;
   } = null!;

   [Inject]
   public IProjectCrudService ProjectDatabaseService { get; set; } = null!;

   protected List<Project> ProjectsCatalog { get; private set; } = new();

   protected Project? SelectedProject
   {
      get => DataTransferService.SelectedProject;
      set
      {
         DataTransferService.SelectedProject = value;
         _ = LoadData();
      }
   }

   protected List<KnownMicrocontroller> Microcontrollers
   {
      get;
      set;
   } = new();

   protected override async Task RefreshData(bool firstRender = false)
   {
      if (firstRender)
      {
         // Is called as soon as the client has established the connection to the broker.
         MicrocontrollerService.ConnectedAsync += MqttClient_ConnectedAsync;
         MicrocontrollerService.AliveMessageAsync += MicrocontrollerService_Alive;
         InitTimerService();
      }
      ProjectsCatalog = await ProjectDatabaseService.LoadProjects();
      await base.RefreshData(firstRender); // Calls LoadData and StateHasChanged.
   }

   protected override void OnInitialized()
   {
      DataTransferService.CurrentScreen = ScreenMode.KnownMicrocontrollers;
      base.OnInitialized();
   }

   /// <summary>
   /// The MQTT Client has established a connected to the broker.
   /// </summary>
   /// <param name="arg"></param>
   /// <returns></returns>
   private async Task MqttClient_ConnectedAsync(MqttClientConnectedEventArgs arg)
   {
      foreach (var microcontroller in Microcontrollers)
      {
         await MicrocontrollerService.RequestAliveMessageAsync(microcontroller);
      }
   }

   private Task MicrocontrollerService_Alive(AliveMessage aliveMessage)
   {
      var microcontroller = (from data in Microcontrollers where data.MacAddress == aliveMessage.Microcontroller.MacAddress select data).FirstOrDefault();

      if (microcontroller != null)
      {

         if (aliveMessage.Microcontroller.ProjectName != microcontroller.ProjectName)
         {
            Snackbar.Add($"Microcontroller {microcontroller.MacAddress} has project name {aliveMessage.Microcontroller.ProjectName}", Severity.Warning);
            // This must be corrected!
            microcontroller.NamespaceName = DataTransferService.NamespaceName;
            this.MicrocontrollerService.PushMicrocontrollerConfigToMicrocontroller(microcontroller);
         }
         microcontroller.Online = true;
      }

      return Task.CompletedTask;
   }

   /// <summary>
   /// Observe if a microcontroller is offline.
   /// </summary>
   private void InitTimerService()
   {
      try
      {
         timerService = new TimerService(5000);
         timerService.OnElapsed += TimerService_OnElapsed;
      }
      catch (Exception exception)
      {
         Logger.LogError(exception, $"{MethodBase.GetCurrentMethod()} failed!");
      }
   }

   private async void TimerService_OnElapsed(System.Timers.Timer timer, TimerServiceEventArgs elapsedEventArgs)
   {
      if (null == Microcontrollers)
      {
         return;
      }
      if (!Microcontrollers.Any())
      {
         return;
      }
      foreach (var microcontroller in Microcontrollers)
      {
         await MicrocontrollerService.RequestAliveMessageAsync(microcontroller);

         var result = (from data in DataTransferService.ReceivedTopicsQueue
                       where data.Topic.EndsWith($"{microcontroller.MacAddress}/alive", StringComparison.OrdinalIgnoreCase) &&
                       (DateTime.UtcNow - data.DateOfReceipt).TotalMilliseconds < 20_000
                       select data).FirstOrDefault();
         microcontroller.Online = result != null;
      }
      await InvokeAsync(StateHasChanged);
   }

   protected override async Task LoadData()
   {
      try
      {
         if (null == SelectedProject)
         {
            Microcontrollers = await MicrocontrollerService.GetMicrocontrollers();
         }
         else
         {
            Microcontrollers = await MicrocontrollerService.GetMicrocontrollers(SelectedProject);
         }
         foreach (var microcontroller in Microcontrollers)
         {
            await MicrocontrollerService.RequestAliveMessageAsync(microcontroller);
         }
      }
      catch (Exception ex)
      {
         Snackbar.Add(ex.GetBaseException().Message);
      }
      await InvokeAsync(StateHasChanged);
   }

   public async Task OpenKnownMicrocontrollerEditor()
   {
      bool? result = await DialogService.ShowMessageBox("Warning!",
                                                        $"Normally the microcontroller is registered automatically after booting. That would be the recommended and safe way. Do you want to proceed?",
                                                        yesText: "Yes",
                                                        cancelText: "No");
      if (!result.HasValue)
      {
         return;
      }

      await OpenKnownMicrocontrollerEditor(new KnownMicrocontroller
      {
         NamespaceName = DataTransferService.NamespaceName,
         ProjectName = DataTransferService.SelectedProject!.ProjectName
      });
   }

   public async Task OpenKnownMicrocontrollerEditor(KnownMicrocontroller knownMicrocontroller)
   {
      try
      {
         var options = GetDialogOptions();

         var parameters = new DialogParameters { ["Microcontroller"] = knownMicrocontroller };
         IsEditorOpen = true;
         string? currentProjectName = knownMicrocontroller.ProjectName;
         var dialog = await this.DialogService.ShowAsync<KnownMicrocontrollerEditor>("Edit Known Microcontroller",
                                                                                     parameters,
                                                                                     options);
         var result = await dialog.Result;
         if (result != null)
         {
            var microcontroller = result.Data as KnownMicrocontroller;
            if (microcontroller != null)
            {
               if (microcontroller.ProjectName != currentProjectName)
               {
                  if (await this.MicrocontrollerService.PushDeviceConfigToMicrocontroller(microcontroller))
                  {
                     Snackbar.Add("Configuration has been sent via MQTT.");
                  }
               }
            }
         }
      }
      catch (Exception ex)
      {
         Logger.LogError(ex, $"{MethodBase.GetCurrentMethod()} failed!");
         Snackbar.Add(ex.GetBaseException().Message, Severity.Error);
      }
      finally
      {
         IsEditorOpen = false;
         await LoadData();
      }
   }

   public async Task EditMicrocontrollerAsync(KnownMicrocontroller microcontroller)
   {
      await OpenKnownMicrocontrollerEditor(microcontroller);
      await LoadData();
   }

   public async Task ConfigureConnectedDevicesAsync(KnownMicrocontroller microcontroller)
   {
      await LoadData(); // Projectname could have changed in the meantime.
      await OpenKnownMicrocontrollerConnectedDevicesEditor(microcontroller);
      await LoadData();
   }

   private async Task OpenKnownMicrocontrollerConnectedDevicesEditor(KnownMicrocontroller microcontroller)
   {
      try
      {
         var options = GetDialogOptions();

         var parameters = new DialogParameters { ["Microcontroller"] = microcontroller };
         IsEditorOpen = true;
         var dialog = await this.DialogService.ShowAsync<MicrocontrollerConnectedDevicesEditor>("Edit Connected Devices",
                                                                                                parameters,
                                                                                                options);
         var result = await dialog.Result;
      }
      catch (Exception ex)
      {
         Logger.LogError(ex, $"{MethodBase.GetCurrentMethod()} failed!");
         Snackbar.Add(ex.GetBaseException().Message, Severity.Error);
      }
      finally
      {
         IsEditorOpen = false;
         await LoadData();
      }
   }

   public async Task DeleteMicrocontrollerAsync(KnownMicrocontroller microcontroller)
   {
      bool? result = await DialogService.ShowMessageBox("Delete",
                                                        $"Do you want to delete the microcontroller with Mac Address {microcontroller.MacAddress}?",
                                                        yesText: "Yes", cancelText: "No");
      if (!result.HasValue)
      {
         return;
      }

      await MicrocontrollerService.Delete(microcontroller);
      await LoadData();
   }

   /// <summary>
   /// Reboot the microcontroller.
   /// </summary>
   /// <param name="microcontroller"></param>
   /// <returns></returns>
   public async Task Reboot(KnownMicrocontroller microcontroller)
   {
      try
      {
         await MicrocontrollerService.Reboot(microcontroller);
         Snackbar.Add($"Reboot of microcontroller {microcontroller.MacAddress}", Severity.Info);
      }
      catch (Exception ex)
      {
         Logger.LogError(ex, $"{MethodBase.GetCurrentMethod()} failed!");
         Snackbar.Add(ex.GetBaseException().Message, Severity.Error);
      }
   }

   /// <summary>
   /// Post the settings to the microcontroller via REST interface.
   /// </summary>
   /// <param name="microcontroller"></param>
   /// <returns></returns>
   protected async Task PostMicrocontrollerConfigToMicrocontroller(KnownMicrocontroller microcontroller)
   {
      await MicrocontrollerService.PostMicrocontrollerConfigToMicrocontroller(microcontroller);
   }

   public void Dispose()
   {
      timerService?.Dispose();
   }
}