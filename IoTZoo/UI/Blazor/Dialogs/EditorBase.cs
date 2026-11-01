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

using Domain.Interfaces;
using Domain.Pocos;
using Infrastructure;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using System.Reflection;
using System.Text.Json;

namespace IotZoo.Dialogs;

public class DialogBase : ComponentBase
{
    [Inject]
    protected ILogger<EditorBase> Logger { get; set; } = null!;

    [Inject]
    protected ISnackbar Snackbar
    {
        get;
        set;
    } = null!;

    [Inject]
    protected IDataTransferService DataTransferService { get; set; } = null!;
}

public class EditorBase : DialogBase
{
    [Inject]
    protected IDialogService DialogService { get; set; } = null!;

    [Inject]
    protected IHashingService HashingService
    {
        get;
        set;
    } = null!;

    /// <summary>
    /// HashCode of the data.
    /// </summary>
    protected string HashCode
    {
        get;
        set;
    } = null!;

    [CascadingParameter]
    protected IMudDialogInstance MudDialog { get; set; } = null!;

    protected bool IsNewRecord
    {
        get;
        set;
    }

    protected string DialogTitle
    {
        get;
        set;
    } = string.Empty;


#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    protected virtual async Task Save()
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    {
        Snackbar.Add("Please override Save!", Severity.Warning);
    }

    protected virtual Task Cancel()
    {
        Snackbar.Add("Please override Cancel!", Severity.Warning);
        return Task.CompletedTask;
    }

    protected async Task Cancel(BasePoco basePoco)
    {
        // Are there any unsaved changes?
        var hashCodeTmp = GetHashCodeBase64(basePoco);
        if (hashCodeTmp != HashCode)
        {
            bool? result = await DialogService.ShowMessageBoxAsync("Warning!",
                                                                   $"There are unsaved changes! Do you want to cancel without saving?",
                                                                   yesText: "Yes",
                                                                   cancelText: "No");
            if (!result.HasValue)
            {
                return;
            }
        }
        MudDialog.Cancel();
    }

    protected void TrimTextFields(BasePoco poco)
    {
        try
        {
            foreach (var property in poco.GetType().GetProperties())
            {
                try
                {
                    if (property.PropertyType != typeof(string) || !property.CanWrite)
                    {
                        continue;
                    }

                    if (property.GetValue(poco) is string value && !string.IsNullOrEmpty(value))
                    {
                        property.SetValue(poco, value.Trim());
                    }
                }
                catch
                {
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex,
                            $"{MethodBase.GetCurrentMethod()} failed!");
        }
    }

    protected virtual DialogOptions GetDialogOptions()
    {
        return new DialogOptions
        {
            BackdropClick = false,
            CloseButton = false,
            MaxWidth = MaxWidth.Large,
            CloseOnEscapeKey = false,
        };
    }

    protected string GetHashCodeBase64(object? poco)
    {
        return poco == null ? string.Empty : HashingService.AsBase64(JsonSerializer.Serialize(poco));
    }
}