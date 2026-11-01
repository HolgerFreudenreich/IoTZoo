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

using Domain.Interfaces.Crud;
using Domain.Interfaces.RuleEngine;
using Domain.Pocos;
using Domain.Services.RuleEngine;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace IotZoo.Pages;

public class ScriptsPageBase : PageBase
{
    [Inject]
    public IScriptCrudService ScriptsService
    {
        get;
        set;
    } = null!;

    [Inject]
    public IScriptService ScriptService
    {
        get;
        set;
    } = null!;

    protected List<Script>? Scripts
    {
        get;
        set;
    } = new();

    protected override void OnInitialized()
    {
        DataTransferService.CurrentScreen = ScreenMode.Scripts;
        base.OnInitialized();
    }

    protected override async Task LoadData()
    {
        Scripts = await this.ScriptsService.LoadScripts();
        await InvokeAsync(StateHasChanged);
    }

    private async Task OpenScriptEditor(Script script)
    {
        try
        {
            var options = GetDialogOptions();

            var parameters = new DialogParameters { ["Script"] = script };
            IsEditorOpen = true;
            var dialog = await DialogService.ShowAsync<Dialogs.ScriptEditor>("Edit Script",
                                                                                         parameters,
                                                                                         options);
            var result = await dialog.Result;
        }
        finally
        {
            IsEditorOpen = false;
            await LoadData();
        }
    }

    public async Task OpenScriptEditorAsync()
    {
        await OpenScriptEditor(new Script());
    }

    protected async Task EditScript(Script script)
    {
        await OpenScriptEditor(script);
    }

    protected async Task DeleteScript(Script script)
    {
        bool? result = await DialogService.ShowMessageBoxAsync("Delete",
                                                          message: $"Do you want to delete the Script '{script.ScriptName}'?",
                                                          yesText: "Yes", cancelText: "No");
        if (!result.HasValue)
        {
            return;
        }
        await ScriptsService.Delete(script);
        await LoadData();
    }

    protected async Task CloneScript(Script script)
    {
        var clonedScript = Infrastructure.Tools.DeepCopyReflection(script);
        clonedScript.ScriptId = 0; // force new
        await OpenScriptEditor(clonedScript);
    }

    protected async Task ExportScriptToClipboard(Script script)
    {
        try
        {
            string json = JsonSerializer.Serialize(script);
            await JsRuntime.InvokeVoidAsync("copyToClipboard", json);
            Snackbar.Add($"{script.ScriptName} exported to clipboard.", Severity.Success);
        }
        catch (Exception exception)
        {
            Snackbar.Add($"Export failed: {exception.GetBaseException().Message}", Severity.Error);
        }
    }

    protected async Task ImportScriptFromClipboard()
    {
        try
        {
            string? json = await JsRuntime.InvokeAsync<string?>("readClipboardText");
            var script = string.IsNullOrWhiteSpace(json) ? null : JsonSerializer.Deserialize<Script>(json);
            if (script == null || string.IsNullOrWhiteSpace(script.SourceCode))
            {
                Snackbar.Add("Clipboard does not contain a valid script.", Severity.Warning);
                return;
            }
            script.ScriptId = 0;
            await OpenScriptEditor(script);
        }
        catch (Exception exception)
        {
            Snackbar.Add($"Import failed: {exception.GetBaseException().Message}", Severity.Error);
        }
    }

    protected async Task ExecuteScript(Script script)
    {
        try
        {
            var inputs = await CollectParameterValues(GetParameterLabels(script));
            if (inputs == null)
            {
                return;
            }
            string input = string.Join(", ", inputs);

            string output = await ScriptService.ProcessScript(script, input);
            Snackbar.Add($"{script.ScriptName}: {output}", Severity.Info, options => options.VisibleStateDuration = 20000);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"{script.ScriptName}: {ex.GetBaseException().Message}", Severity.Error, options => options.VisibleStateDuration = 20000);
        }
    }

    private async Task<List<string>?> CollectParameterValues(List<string> parameterLabels)
    {
        var inputs = new List<string>();
        for (int index = 0; index < parameterLabels.Count; index++)
        {
            var parameters = new DialogParameters
            {
                ["Title"] = $"Parameter {index + 1}",
                ["LabelText"] = parameterLabels[index],
                ["LabelButtonSave"] = "OK"
            };
            var dialog = await DialogService.ShowAsync<Dialogs.TextEditor>($"Parameter {index + 1}", parameters, GetDialogOptions());
            var result = await dialog.Result;
            if (result == null || result.Canceled)
            {
                return null;
            }
            string value = result.Data as string ?? string.Empty;
            if (parameterLabels[index].EndsWith("(string)") && !IsQuoted(value))
            {
                value = QuoteString(value);
            }
            inputs.Add(value);
        }
        return inputs;
    }

    private static bool IsQuoted(string value)
    {
        return value.Length >= 2 && value.StartsWith('"') && value.EndsWith('"');
    }

    private static string QuoteString(string value)
    {
        return $"\"{value.Replace("\\", "\\\\").Replace("\"", "\\\"")}\"";
    }

    private static List<string> GetParameterLabels(Script script)
    {
        var match = Regex.Match(script.SourceCode ?? string.Empty,
                                $@"\b{Regex.Escape(script.ScriptName)}\s*\(([^)]*)\)\s*\{{");
        if (!match.Success)
        {
            return new List<string>();
        }
        return match.Groups[1].Value
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(p => p.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                    .Where(parts => parts.Length >= 2)
                    .Select(parts => $"{parts[^1]} ({string.Join(' ', parts[..^1])})")
                    .ToList();
    }
}
