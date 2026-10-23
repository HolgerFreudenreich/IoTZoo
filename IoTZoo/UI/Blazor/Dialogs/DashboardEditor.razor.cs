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

namespace IotZoo.Dialogs;

using Domain.Interfaces.Crud;
using Domain.Pocos;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using System.Reflection;

public class DashboardEditorBase : EditorBase
{
    [Parameter]
    public Dashboard Dashboard { get; set; } = null!;

    [Inject]
    private IDashboardCrudService DashboardService { get; set; } = null!;

    protected async override Task OnInitializedAsync()
    {
        if (Dashboard.DashboardId <= 0)
        {
            DialogTitle = "Add Dashboard";
            IsNewRecord = true;
        }
        else
        {
            DialogTitle = "Edit Dashboard";
            IsNewRecord = false;
        }

        HashCode = GetHashCodeBase64(Dashboard);
        await base.OnInitializedAsync();
    }

    protected override Task Cancel()
    {
        MudDialog.Cancel();
        return Task.CompletedTask;
    }

    protected override async Task Save()
    {
        try
        {
            Snackbar.Clear();
            Dashboard.DashboardName = Dashboard.DashboardName?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(Dashboard.DashboardName))
            {
                Snackbar.Add("Dashboard name is required!", Severity.Error);
                return;
            }

            if (IsNewRecord)
            {
                Dashboard.DashboardId = await DashboardService.Insert(Dashboard);
            }
            else
            {
                await DashboardService.Update(Dashboard);
            }
            MudDialog.Close(DialogResult.Ok(Dashboard));
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, $"{MethodBase.GetCurrentMethod()} failed!");
            Snackbar.Add("Unable to save dashboard!", Severity.Error);
        }
    }
}
