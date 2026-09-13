// --------------------------------------------------------------------------------------------------------------------
//      ____    ______   _____
//     /  _/___/_  __/  /__  / ____  ____
//     / // __ \/ /       / / / __ \/ __ \
//   _/ // /_/ / /       / /_/ /_/ / /_/ /
//  /___/\____/_/       /____|____/\____/   P L A Y G R O U N D
// --------------------------------------------------------------------------------------------------------------------
// Connect «Things» with microcontrollers in a simple way.
// --------------------------------------------------------------------------------------------------------------------
// (c) 2025 - 2026 Holger Freudenreich under the MIT license
// --------------------------------------------------------------------------------------------------------------------

using Domain.Interfaces.Timer;
using Domain.Pocos;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using System.Reflection;

namespace IotZoo.Dialogs;

public class CloneCronJobDialogBase : EditorBase
{
   [Parameter]
   public CronJob SourceCronJob { get; set; } = null!;

   [Parameter]
   public Project Project { get; set; } = null!;

   [Inject]
   protected ICronCrudService CronCrudService { get; set; } = null!;

   protected CronJob NewCronJob { get; set; } = null!;

   protected override async Task OnInitializedAsync()
   {
      DialogTitle = "Clone Cron Job";
      IsNewRecord = true;

      // Create a copy of the source cron job for modification
      NewCronJob = new CronJob
      {
         SecondExpression = SourceCronJob.SecondExpression,
         MinuteExpression = SourceCronJob.MinuteExpression,
         HourExpression = SourceCronJob.HourExpression,
         DayOfMonthExpression = SourceCronJob.DayOfMonthExpression,
         MonthOfYearExpression = SourceCronJob.MonthOfYearExpression,
         DayOfWeekExpression = SourceCronJob.DayOfWeekExpression,
         Topic = $"{SourceCronJob.Topic}_copy",
         Enabled = SourceCronJob.Enabled,
         ProjectName = SourceCronJob.ProjectName,
         NamespaceName = SourceCronJob.NamespaceName,
         EditAllowed = SourceCronJob.EditAllowed
      };

      HashCode = GetHashCodeBase64(NewCronJob);
      await base.OnInitializedAsync();
   }

   protected override async Task Cancel()
   {
      await Cancel(NewCronJob);
   }

   protected override async Task Save()
   {
      try
      {
         Snackbar.Clear();
         await Task.Delay(10);

         if (ValidateFields())
         {
            // Reset CronId to 0 so it will be inserted as a new record
            NewCronJob.CronId = 0;

            // Insert the cloned cron job to the database
            await CronCrudService.Insert(NewCronJob);
            Snackbar.Add($"Cron job '{NewCronJob.Topic}' cloned successfully", Severity.Success);
            MudDialog.Close(DialogResult.Ok(NewCronJob));
         }
      }
      catch (Exception ex)
      {
         Logger.LogError(ex, $"{MethodBase.GetCurrentMethod()} failed!");
         Snackbar.Add($"Unable to clone cron job: {ex.Message}", Severity.Error);
      }
   }

   private bool ValidateFields()
   {
      if (string.IsNullOrWhiteSpace(NewCronJob.Topic))
      {
         Snackbar.Add("Topic is required", Severity.Warning);
         return false;
      }

      if (NewCronJob.Topic == SourceCronJob.Topic)
      {
         Snackbar.Add("New topic must be different from the original topic", Severity.Warning);
         return false;
      }

      return true;
   }
}
