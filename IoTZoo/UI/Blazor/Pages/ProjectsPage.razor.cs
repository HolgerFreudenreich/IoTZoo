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

using DataAccess.Services;
using Domain.Interfaces.Crud;
using Domain.Interfaces.MQTT;
using Domain.Interfaces.Timer;
using Domain.Pocos;
using IotZoo.Dialogs;
using Microsoft.AspNetCore.Components;
using MQTTnet.Protocol;
using MudBlazor;

namespace IotZoo.Pages;

public class ProjectsPageBase : PageBase
{
   [Inject]
   public IProjectCrudService ProjectDatabaseService
   {
      get;
      set;
   } = null!;

   [Inject]
   public IKnownMicrocontrollerCrudService MicrocontrollerDatabaseService 
   {
      get;
      set;
   } = null!;

   [Inject]
   public IKnownTopicsCrudService KnownTopicsService
   {
      get;
      set;
   } = null!;

   [Inject]
   IIoTZooMqttClient IoTZooMqttClient { get; set; } = null!;

   [Inject]
   ICronService CronService { get; set; } = null!;

   protected List<Project>? Projects
   {
      get;
      set;
   } = new();

   protected override void OnInitialized()
   {
      DataTransferService.CurrentScreen = ScreenMode.Projects;
      base.OnInitialized();
   }

   protected override async Task OnAfterRenderAsync(bool firstRender)
   {
      base.OnAfterRender(firstRender);
      if (firstRender)
      {
         await LoadData();
      }
   }

   protected override async Task LoadData()
   {
      Projects = await this.ProjectDatabaseService.LoadProjects();
      await InvokeAsync(StateHasChanged);
   }

   public async Task OpenProjectEditor(Project project)
   {
      try
      {
         var options = GetDialogOptions();

         var parameters = new DialogParameters { ["Project"] = project };
         IsEditorOpen = true;
         var dialog = await this.DialogService.ShowAsync<IotZoo.Dialogs.ProjectEditor>("Edit Project",
                                                                                       parameters,
                                                                                       options);
         var result = await dialog.Result;
         if (null != result)
         {
            var createdProject = result.Data as Project;
            if (createdProject != null)
            {
               if (createdProject.IsNew)
               {
                  await this.CronService.StartProjectCronJobs(createdProject);
               }
            }
         }
      }
      finally
      {
         IsEditorOpen = false;
         await LoadData();
      }
   }

   public async Task OpenProjectCronJobsEditor(Project project)
   {
      try
      {
         var options = GetDialogOptions();

         var parameters = new DialogParameters { ["Project"] = project };
         IsEditorOpen = true;
         var dialog = await this.DialogService.ShowAsync<IotZoo.Dialogs.ProjectCronJobsEditor>("Edit Project Cron Jobs",
                                                                                               parameters,
                                                                                               options);
         var result = await dialog.Result;
      }
      finally
      {
         IsEditorOpen = false;
         await LoadData();
      }
   }

   public async Task OpenProjectEditorAsync()
   {
      await OpenProjectEditor(new Project());
   }

   protected async Task EditProject(Project project)
   {
      await OpenProjectEditor(project);
   }

   protected async Task RegisterProjectTopics(Project project)
   {
      await this.KnownTopicsService.RegisterProjectDefaultKnownTopics(project);
      Snackbar.Add("Done");
   }

   protected async Task EditProjectCronJobs(Project project)
   {
      await OpenProjectCronJobsEditor(project);
   }

   protected async Task DeleteProject(Project project)
   {
      bool? result = await DialogService.ShowMessageBox("Delete",
                                                        message: $"Do you want to delete the Project '{project.ProjectName}'?",
                                                        yesText: "Yes", cancelText: "No");
      if (!result.HasValue)
      {
         return;
      }

      // Delete all register_microcontroller topics from MQTT Broker, otherwhise the project well be recreated after next subcribing.
      var microcontrollers = await MicrocontrollerDatabaseService.GetMicrocontrollers(project);
      bool allRetainedMessagesRemovedFromBroker = true;
      foreach (KnownMicrocontroller microcontroller in microcontrollers)
      {
         bool ok = await IoTZooMqttClient.RemoveRetainedMessageFromBroker($"{microcontroller.ProjectName}/esp32/{microcontroller.MacAddress}/{TopicConstants.REGISTER_MICROCONTROLLER}"); 
         if (!ok)
         {
            allRetainedMessagesRemovedFromBroker = false;
         }
      }

      if (allRetainedMessagesRemovedFromBroker)
      {
         await CronService.StopProjectCronJobs(project);
         await ProjectDatabaseService.Delete(project); // assigned microcontrollers will be automatically deleted by foreign key contraint!
         Snackbar.Add("The project is deleted!", Severity.Success);
         Snackbar.Add("Be aware of if you start a microcontroller which has this project name configured, than the project will be recreated automatically!", Severity.Warning);
      }
      else
      {
         Snackbar.Add("Project not deleted because of problems to remove retained topic from MQTT Broker. Ensure the MQTT broker is online!");
      }

      await LoadData();
   }

   protected async Task RestartProject(Project project)
   {
      bool? result = await DialogService.ShowMessageBox("Restart",
                                                        message: $"Do you want to restart the Project '{project.ProjectName}'? The topic '{project.ProjectName}/{TopicConstants.INIT}' will be published. The assigned microcontrollers will not be bootet.",
                                                        yesText: "Yes", cancelText: "No");
      if (!result.HasValue)
      {
         return;
      }

      string topic = $"{project.ProjectName}/{TopicConstants.INIT}";
  
      await IoTZooMqttClient.PublishTopic(topic, null, MqttQualityOfServiceLevel.ExactlyOnce);

      await LoadData();
   }

   protected async Task OpenProjectOverviewDialog(Project project)
   {
      try
      {
         var options = GetDialogOptions();

         var parameters = new DialogParameters { ["Project"] = project };
         IsEditorOpen = true;
         var dialog = await this.DialogService.ShowAsync<ProjectOverviewDialog>($"Overview of Project '{project.ProjectName}'",
                                                                                parameters,
                                                                                options);
         var result = await dialog.Result;
      }
      finally
      {
         IsEditorOpen = false;
         // await LoadData();
      }
   }
}
