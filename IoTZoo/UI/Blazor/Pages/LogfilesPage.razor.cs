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
// (c) 2025 - 2026 Holger Freudenreich under MIT license
// --------------------------------------------------------------------------------------------------------------------

using Domain.Pocos;
using IotZoo.Logging;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using Serilog.Events;

namespace IotZoo.Pages;

public class LogfilesPageBase : PageBase
{
    [Inject]
    protected ILogEntryService LogEntryService { get; set; } = null!;

    protected static readonly LogEventLevel[] Levels =
    {
        LogEventLevel.Verbose,
        LogEventLevel.Debug,
        LogEventLevel.Information,
        LogEventLevel.Warning,
        LogEventLevel.Error,
        LogEventLevel.Fatal
    };

    protected static readonly int[] RowOptions = { 100, 500, 1000, 5000 };

    protected List<LogEntry> Entries { get; private set; } = new();

    protected LogEventLevel MinimumLevel { get; private set; } = LogEventLevel.Warning;

    protected int MaxRows { get; private set; } = 500;

    protected string? SearchText { get; private set; }

    protected override void OnInitialized()
    {
        DataTransferService.CurrentScreen = ScreenMode.Logfiles;
        base.OnInitialized();
    }

    protected override async Task LoadData()
    {
        IsBusy = true;
        await InvokeAsync(StateHasChanged);
        try
        {
            Entries = await LogEntryService.Load(MinimumLevel, SearchText, MaxRows);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Loading the log entries failed!");
            Snackbar.Add("Loading the log entries failed", Severity.Error);
        }
        finally
        {
            IsBusy = false;
        }
        await InvokeAsync(StateHasChanged);
    }

    protected async Task OnMinimumLevelChanged(LogEventLevel level)
    {
        MinimumLevel = level;
        await LoadData();
    }

    protected async Task OnMaxRowsChanged(int rows)
    {
        MaxRows = rows;
        await LoadData();
    }

    protected async Task OnSearchTextChanged(string? text)
    {
        SearchText = text;
        await LoadData();
    }

    protected async Task DeleteAll()
    {
        bool? result = await DialogService.ShowMessageBoxAsync("Delete",
                                                               "Do you want to delete all log entries?",
                                                               yesText: "Yes", cancelText: "No");
        if (result != true)
        {
            return;
        }
        await LogEntryService.DeleteAll();
        await LoadData();
    }

    protected static Color GetColor(LogEventLevel level)
    {
        return level switch
        {
            LogEventLevel.Fatal or LogEventLevel.Error => Color.Error,
            LogEventLevel.Warning => Color.Warning,
            LogEventLevel.Information => Color.Info,
            _ => Color.Default
        };
    }
}
