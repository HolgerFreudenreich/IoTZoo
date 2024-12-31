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

using Domain.Interfaces.Timer;
using Domain.Pocos;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using Quartz.Spi;
using System.Reflection;

namespace IotZoo.Dialogs;

public class ProjectCronJobsEditorBase : EditorBase
{
   [Parameter]
   public Project Project { get; set; } = null!;

   protected List<CronJob> CronJobs { get; set; } = new List<CronJob>();

   [Inject]
   ICronCrudService CronCrudService { get; set; } = null!;

   [Inject]
   IJobFactory JobFactory { get; set; } = null!;

   public ProjectCronJobsEditorBase()
   {
   }

   protected override async Task OnInitializedAsync()
   {
      await base.OnInitializedAsync();

      DialogTitle = "Edit Project Cron Jobs";
      CronJobs = await CronCrudService.LoadByProject(Project);
      HashCode = GetHashCodeBase64(CronJobs);
   }

   protected override async Task Cancel()
   {
      await base.Cancel(Project);
   }

   protected override async Task Save()
   {
      try
      {
         Snackbar.Clear();
         await Task.Delay(10);
         TrimTextFields(Project);
         if (ValidateFields())
         {
            MudDialog.Close(DialogResult.Ok(Project));
         }
      }
      catch (Exception ex)
      {
         Logger.LogError(ex, $"{MethodBase.GetCurrentMethod()} failed!");
         Snackbar.Add("Unable to save script", Severity.Error);
      }
   }

   private bool ValidateFields()
   {
      return true;
   }

   public async Task AddCronJobAsync()
   {
      Snackbar.Add("Not yet implemented. You can modify table cron in database iotzoo.db", Severity.Info);
      //return;
      //var options = GetDialogOptions();

      //var parameters = new DialogParameters { ["CronJob"] = new CronJob { ProjectId = Project.ProjectId } };

      //var dialog = await this.DialogService.ShowAsync<IotZoo.Dialogs.CronJobEditor>("Add Cron Job",
      //                                                                              parameters,
      //                                                                              options);
      //var result = await dialog.Result;
   }


}

