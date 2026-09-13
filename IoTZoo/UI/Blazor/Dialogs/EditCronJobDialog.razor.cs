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
using Quartz;
using System.Reflection;

namespace IotZoo.Dialogs;

public class EditCronJobDialogBase : EditorBase
{
   [Parameter]
   public CronJob CronJob { get; set; } = null!;

   [Parameter]
   public Project Project { get; set; } = null!;

   [Inject]
   protected ICronCrudService CronCrudService { get; set; } = null!;

   protected List<DateTime> Occurences { get; set; } = new();

   protected override async Task OnInitializedAsync()
   {
      DialogTitle = "Edit Cron Job";
      IsNewRecord = false;

      // Ensure all fields are populated for display and edit
      if (string.IsNullOrWhiteSpace(CronJob.SecondExpression))
         CronJob.SecondExpression = "*";
      if (string.IsNullOrWhiteSpace(CronJob.MinuteExpression))
         CronJob.MinuteExpression = "*";
      if (string.IsNullOrWhiteSpace(CronJob.HourExpression))
         CronJob.HourExpression = "*";
      if (string.IsNullOrWhiteSpace(CronJob.DayOfMonthExpression))
         CronJob.DayOfMonthExpression = "*";
      if (string.IsNullOrWhiteSpace(CronJob.MonthOfYearExpression))
         CronJob.MonthOfYearExpression = "*";
      if (string.IsNullOrWhiteSpace(CronJob.DayOfWeekExpression))
         CronJob.DayOfWeekExpression = "*";

      HashCode = GetHashCodeBase64(CronJob);
      await base.OnInitializedAsync();
   }

   protected void Parse()
   {
      try
      {
         // Quartz.NET Format: Seconds Minutes Hours DayOfMonth Month DayOfWeek (6 fields)
         string quartzCronExpression = $"{CronJob.SecondExpression} {CronJob.MinuteExpression} {CronJob.HourExpression} {CronJob.DayOfMonthExpression} {CronJob.MonthOfYearExpression} {CronJob.DayOfWeekExpression}";

         var now = DateTimeOffset.UtcNow;
         var endTime = now.AddDays(100);

         // Use Quartz.CronExpression directly from Quartz.NET
         var cronExpression = new Quartz.CronExpression(quartzCronExpression);

         Occurences = new List<DateTime>();
         DateTimeOffset? nextFire = cronExpression.GetNextValidTimeAfter(now);

         while (nextFire.HasValue && nextFire.Value.UtcDateTime <= endTime && Occurences.Count < 10)
         {
            Occurences.Add(nextFire.Value.UtcDateTime);

            // Get next occurrence after current
            nextFire = cronExpression.GetNextValidTimeAfter(nextFire.Value);
         }
      }
      catch (Exception ex)
      {
         Snackbar.Add(ex.GetBaseException().Message, Severity.Error);
      }
   }

   protected override async Task Cancel()
   {
      await Cancel(CronJob);
   }

   protected override async Task Save()
   {
      try
      {
         Snackbar.Clear();
         await Task.Delay(10);

         if (ValidateFields())
         {
            // Update the cron job in the database
            await CronCrudService.Update(CronJob);
            Snackbar.Add($"Cron job '{CronJob.Topic}' updated successfully. The changes will take effect after a restart.", Severity.Success);
            MudDialog.Close(DialogResult.Ok(CronJob));
         }
      }
      catch (Exception ex)
      {
         Logger.LogError(ex, $"{MethodBase.GetCurrentMethod()} failed!");
         Snackbar.Add($"Unable to save cron job: {ex.Message}", Severity.Error);
      }
   }

   private bool ValidateFields()
   {
      if (string.IsNullOrWhiteSpace(CronJob.Topic))
      {
         Snackbar.Add("Topic is required", Severity.Warning);
         return false;
      }

      // Validate Cron Expression using Quartz.NET
      string cronExpression = $"{CronJob.SecondExpression} {CronJob.MinuteExpression} {CronJob.HourExpression} {CronJob.DayOfMonthExpression} {CronJob.MonthOfYearExpression} {CronJob.DayOfWeekExpression}";

      try
      {
         // Create a CronExpression to validate the syntax
         new global::Quartz.CronExpression(cronExpression);
      }
      catch
      {
         Snackbar.Add("Invalid cron expression - please check the syntax and click 'Parse' to validate", Severity.Warning);
         return false;
      }

      return true;
   }
}
