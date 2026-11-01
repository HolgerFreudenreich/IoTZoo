// --------------------------------------------------------------------------------------------------------------------
//      ____    ______   _____
//     /  _/___/_  __/  /__  / ____  ____
//     / // __ \/ /       / / / __ \/ __ \
//   _/ // /_/ / /       / /_/ /_/ / /_/ /
//  /___/\____/_/       /____|____/\____/ 
// --------------------------------------------------------------------------------------------------------------------
// Connect «Things» with microcontrollers in the simplest possible way.
// --------------------------------------------------------------------------------------------------------------------
// (c) 2025 - 2026 Holger Freudenreich under MIT license
// --------------------------------------------------------------------------------------------------------------------

namespace IotZoo.Pages;

using Domain.Interfaces;
using Domain.Pocos;
using IotZoo.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;
using MudBlazor;
using System.Diagnostics;
using System.Reflection;

public class PageBase : ComponentBase
{
    protected bool IsBusy
    {
        get;
        set;
    }
    protected bool IsEditorOpen
    {
        get;
        set;
    }

    [Inject]
    protected IJSRuntime JsRuntime { get; set; } = null!;

    [Inject]
    protected IDialogService DialogService { get; set; } = null!;

    [Inject]
    protected ISearchHelper SearchHelper { get; set; } = null!;

    [Inject]
    protected ISnackbar Snackbar
    {
        get;
        set;
    } = null!;

    [Inject]
    protected ILogger<PageBase> Logger
    {
        get;
        set;
    } = null!;

    [Inject]
    protected IOptions<AppSettings> Options
    {
        get;
        set;
    } = null!;

    [Inject]
    protected IDataTransferService DataTransferService
    {
        get;
        set;
    } = null!;

    [Inject]
    protected NavigationManager NavigationManager
    {
        get;
        set;
    } = null!;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    protected virtual async Task LoadData()
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    {
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);
        if (firstRender)
        {
            AppStatus.NotifyStateChanged();
            await RefreshData(firstRender);
        }
    }

    protected async Task RefreshData()
    {
        await RefreshData(false);
    }

    protected async Task CopyToClipboard(string text)
    {
        try
        {
            bool ok = await JsRuntime.InvokeAsync<bool>("copyToClipboard", text);
            Snackbar.Add(ok ? "Copied to clipboard" : "Copy to clipboard failed", ok ? Severity.Info : Severity.Error);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, $"{MethodBase.GetCurrentMethod()} failed!");
            Snackbar.Add("Copy to clipboard failed", Severity.Error);
        }
    }

    protected virtual async Task RefreshData(bool firstRender = false)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();

        await LoadData();

        if (!firstRender)
        {
            ShowReloadedMessage(Convert.ToInt32(stopwatch.Elapsed.TotalMilliseconds));
            await InvokeAsync(StateHasChanged);
        }
    }

    private void ShowReloadedMessage(int ms)
    {
        if (ms > 100)
        {
            Snackbar.Add($"Reloaded in {ms} ms", Severity.Info);
        }
        if (ms > 1000)
        {
            Snackbar.Add($"Reloaded in {ms / 1000} s", Severity.Info);
        }
        else
        {
            Snackbar.Add("Reloaded", Severity.Info);
        }
    }

    protected virtual DialogOptions GetDialogOptions()
    {
        DialogOptions options = new DialogOptions
        {
            BackdropClick = false,
            CloseButton = false,
            MaxWidth = MaxWidth.Large,
            CloseOnEscapeKey = false,
        };

        return options;
    }

    protected async Task OpenBrowserTab(string url)
    {
        await JsRuntime.InvokeVoidAsync("openNewTab", url);
    }
}