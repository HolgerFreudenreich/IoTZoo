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
using NCrontab;
using System.Reflection;

namespace IotZoo.Dialogs;

public class AddCronJobDialogBase : EditorBase
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
        DialogTitle = "Add New Cron Job";
        IsNewRecord = true;

        // Initialize with project information
        CronJob.ProjectName = Project.ProjectName;
        CronJob.NamespaceName = DataTransferService.NamespaceName;
        CronJob.Enabled = true;

        // Initialize with default cron values
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
            string cronExpression = $"{CronJob.SecondExpression} {CronJob.MinuteExpression} {CronJob.HourExpression} {CronJob.DayOfMonthExpression} {CronJob.MonthOfYearExpression} {CronJob.DayOfWeekExpression}";
            CrontabSchedule schedule = CrontabSchedule.Parse(cronExpression, new CrontabSchedule.ParseOptions { IncludingSeconds = true });

            Occurences = schedule.GetNextOccurrences(DateTime.UtcNow, DateTime.UtcNow.AddDays(100)).Take(10).ToList();
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

            if (ValidateFields())
            {
                // Insert the new cron job to the database
                await CronCrudService.Insert(CronJob);
                Snackbar.Add($"Cron job '{CronJob.Topic}' created successfully. The changes will take effect after a restart.", Severity.Success);
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
        // Validate Topic
        if (string.IsNullOrWhiteSpace(CronJob.Topic))
        {
            Snackbar.Add("Topic is required", Severity.Warning);
            return false;
        }

        // Validate Cron Expression - try to parse it
        string cronExpression = $"{CronJob.SecondExpression} {CronJob.MinuteExpression} {CronJob.HourExpression} {CronJob.DayOfMonthExpression} {CronJob.MonthOfYearExpression} {CronJob.DayOfWeekExpression}";
        CrontabSchedule? schedule = CrontabSchedule.TryParse(cronExpression, new CrontabSchedule.ParseOptions { IncludingSeconds = true });

        if (null == schedule)
        {
            Snackbar.Add("Invalid cron expression - please check the syntax and click 'Parse' to validate", Severity.Warning);
            return false;
        }

        return true;
    }
}
