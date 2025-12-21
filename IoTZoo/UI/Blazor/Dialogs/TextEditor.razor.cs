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

using Domain.Pocos;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using System.Reflection;

namespace IotZoo.Dialogs;

public class TextEditorBase : EditorBase
{
    [Parameter]
    public string LabelText { get; set; } = "Text";

    [Parameter]
    public string LabelButtonSave { get; set; } = "Save";

    public string Text { get; set; } = string.Empty;


    protected override Task Cancel()
    {
        MudDialog.Cancel();
        return Task.CompletedTask;
    }

    protected override Task Save()
    {
        try
        {
            Snackbar.Clear();
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, $"{MethodBase.GetCurrentMethod()} failed!");
            Snackbar.Add("Unable to save!", Severity.Error);
        }
        MudDialog.Close(DialogResult.Ok(Text));
        return Task.CompletedTask;
    }

}

