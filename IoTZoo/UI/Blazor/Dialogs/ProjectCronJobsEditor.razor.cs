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

public class ProjectCronJobsEditorBase : EditorBase
{
    [Parameter]
    public Project Project { get; set; } = null!;

    protected List<CronJob> CronJobs { get; set; } = new List<CronJob>();

    [Inject]
    ICronCrudService CronCrudService { get; set; } = null!;

    public ProjectCronJobsEditorBase()
    {
    }

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        DialogTitle = "Edit Project Cron Jobs";
        CronJobs = await CronCrudService.LoadByProject(Project, onlyEnabledJobs: false);
        HashCode = GetHashCodeBase64(CronJobs);
    }

    protected override async Task Cancel()
    {
        MudDialog.Cancel();
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

    public async Task AddCronJob()
    {
        try
        {
            var options = GetDialogOptions();

            var parameters = new DialogParameters
            {
                ["CronJob"] = new CronJob(),
                ["Project"] = Project
            };

            var dialog = await this.DialogService.ShowAsync<IotZoo.Dialogs.AddCronJobDialog>("Add Cron Job",
                                                                                              parameters,
                                                                                              options);
            var result = await dialog.Result;

            if (result != null && !result.Canceled)
            {
                // Refresh the cron jobs list after successful insertion
                CronJobs = await CronCrudService.LoadByProject(Project, onlyEnabledJobs: false);
                HashCode = GetHashCodeBase64(CronJobs);
                Snackbar.Add("Cron job added successfully", Severity.Success);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, $"{MethodBase.GetCurrentMethod()} failed!");
            Snackbar.Add($"Error adding cron job: {ex.Message}", Severity.Error);
        }
    }

    public async Task EditCronJob(CronJob cronJob)
    {
        try
        {
            var options = GetDialogOptions();

            var parameters = new DialogParameters
            {
                ["CronJob"] = cronJob,
                ["Project"] = Project
            };

            var dialog = await this.DialogService.ShowAsync<IotZoo.Dialogs.EditCronJobDialog>("Edit Cron Job",
                                                                                               parameters,
                                                                                               options);
            var result = await dialog.Result;

            if (result != null && !result.Canceled)
            {
                // Refresh the cron jobs list after successful update
                CronJobs = await CronCrudService.LoadByProject(Project, onlyEnabledJobs: false);
                HashCode = GetHashCodeBase64(CronJobs);
                Snackbar.Add("Cron job updated successfully", Severity.Success);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, $"{MethodBase.GetCurrentMethod()} failed!");
            Snackbar.Add($"Error updating cron job: {ex.Message}", Severity.Error);
        }
    }

    public async Task DeleteCronJob(CronJob cronJob)
    {
        try
        {
            bool? result = await DialogService.ShowMessageBoxAsync(
               "Delete Cron Job",
               $"Do you want to delete the cron job '{cronJob.Topic}'?",
               yesText: "Delete",
               cancelText: "Cancel");

            if (result == true)
            {
                await CronCrudService.Delete(cronJob);
                // Refresh the cron jobs list after successful deletion
                CronJobs = await CronCrudService.LoadByProject(Project, onlyEnabledJobs: false);
                HashCode = GetHashCodeBase64(CronJobs);
                Snackbar.Add("Cron job deleted successfully. The changes will take effect after a restart.", Severity.Success);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, $"{MethodBase.GetCurrentMethod()} failed!");
            Snackbar.Add($"Error deleting cron job: {ex.Message}", Severity.Error);
        }
    }

    public async Task CloneCronJob(CronJob cronJob)
    {
        try
        {
            var options = GetDialogOptions();

            var parameters = new DialogParameters
            {
                ["SourceCronJob"] = cronJob,
                ["Project"] = Project
            };

            var dialog = await this.DialogService.ShowAsync<IotZoo.Dialogs.CloneCronJobDialog>("Clone Cron Job",
                                                                                               parameters,
                                                                                               options);
            var result = await dialog.Result;

            if (result != null && !result.Canceled)
            {
                // Refresh the cron jobs list after successful clone
                CronJobs = await CronCrudService.LoadByProject(Project, onlyEnabledJobs: false);
                HashCode = GetHashCodeBase64(CronJobs);
                Snackbar.Add("Cron job cloned successfully", Severity.Success);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, $"{MethodBase.GetCurrentMethod()} failed!");
            Snackbar.Add($"Error cloning cron job: {ex.Message}", Severity.Error);
        }
    }
}

