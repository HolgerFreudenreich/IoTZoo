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

namespace IotZoo.Pages;

using Dialogs;
using Domain.Interfaces.Crud;
using Domain.Interfaces.MQTT;
using Domain.Pocos;
using Microsoft.AspNetCore.Components;
using MQTTnet;
using MQTTnet.Protocol;
using MudBlazor;
using System.Reflection;
using System.Text.Json;

public class RulesPageBase : PageBase
{
    [Inject]
    public IIoTZooMqttClient MqttClient
    {
        get;
        set;
    } = null!;

    [Inject]
    public IRulesCrudService RulesService
    {
        get;
        set;
    } = null!;

    [Inject]
    public IProjectCrudService ProjectService { get; set; } = null!;

    protected List<Project> ProjectsCatalog { get; private set; } = new();

    protected Project? SelectedProject
    {
        get => DataTransferService.SelectedProject;
        set
        {
            DataTransferService.SelectedProject = value;
            _ = LoadData();
        }
    }

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        DataTransferService.CurrentScreen = ScreenMode.Rules;
    }

    protected override async Task RefreshData(bool firstRender = false)
    {
        if (firstRender)
        {
            DataTransferService.CurrentScreen = ScreenMode.Rules;
        }
        ProjectsCatalog = await this.ProjectService.LoadProjects();
        await base.RefreshData(firstRender); // calls LoadData and StateHasChanged.
    }

    protected List<Rule>? Rules
    {
        get;
        private set;
    } = new();

    protected async Task EditRule(Rule rule)
    {
        await OpenRuleEditor(rule);
    }

    protected async Task DeleteRule(Rule rule)
    {
        bool? result = await DialogService.ShowMessageBox("Delete",
                                                          $"Do you want to delete the rule with Id {rule.RuleId}?",
                                                          yesText: "Yes", cancelText: "No");
        if (!result.HasValue)
        {
            return;
        }
        await RulesService.Delete(rule);
        await LoadData();
    }

    protected async Task CloneRule(Rule rule)
    {
        var clonedRule = Infrastructure.Tools.DeepCopyReflection(rule);
        clonedRule.RuleId = 0; // force new
        await OpenRuleEditor(clonedRule);
    }

    protected async Task ExportRule(Rule rule)
    {
        var serializerOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };
        string json = System.Text.Json.JsonSerializer.Serialize(rule, serializerOptions);
        await CopyToClipboard(json);
    }

    protected async Task ExecuteRule(Rule rule)
    {
        var applicationMessage = new MqttApplicationMessageBuilder()
                                 .WithTopic(rule.SourceTopicFullQualified)
                                 .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                                 .Build();

        await MqttClient.Client.PublishAsync(applicationMessage);
    }

    public void ApplyRules()
    {
        MqttClient.ApplyRulesAsync();
    }

    public async Task OpenRuleEditorAsync()
    {
        await OpenRuleEditor(new Rule());
    }

    protected async Task ExportRulesToClipboard()
    {
        try
        {
            await RulesService.GetRulesByProject(SelectedProject);

            var serializerOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            };

            string json = System.Text.Json.JsonSerializer.Serialize(Rules, serializerOptions);
            await CopyToClipboard(json);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, $"{MethodBase.GetCurrentMethod()} failed!");
            Snackbar.Add($"Exporting rules to Clipboard failed: {ex.Message}", Severity.Error);
        }
        finally
        {
            await LoadData();
        }
    }

    protected async Task OpenTextEditorAndImportRules()
    {
        try
        {
            var options = GetDialogOptions();

            var parameters = new DialogParameters { };
            IsEditorOpen = true;
            var dialog = await DialogService.ShowAsync<Dialogs.TextEditor>("Import Rule/s",
                                                                                         parameters,
                                                                                         options);
            var result = await dialog.Result;
            string json = result?.Data as string ?? string.Empty;
            json = json.Trim();
            if (json.Length > 0)
            {
                if (json.StartsWith("{") && json.EndsWith("}"))
                {
                    var singleRule = System.Text.Json.JsonSerializer.Deserialize<Rule>(json);
                    if (singleRule != null)
                    {
                        singleRule.RuleId = 0; // force new
                        await RulesService.Save(singleRule);
                    }
                }
                else if (json.StartsWith("[") && json.EndsWith("]"))
                {
                    var importedRules = System.Text.Json.JsonSerializer.Deserialize<List<Rule>>(json);
                    if (importedRules != null)
                    {
                        foreach (var rule in importedRules)
                        {
                            rule.RuleId = 0; // force new
                            await RulesService.Save(rule);
                        }
                    }
                }
                else
                {
                    Snackbar.Add("Importing rules failed: JSON is not valid!", Severity.Error);
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, $"{MethodBase.GetCurrentMethod()} failed!");
            Snackbar.Add($"Importing rules failed: {ex.Message}", Severity.Error);
        }
        finally
        {
            IsEditorOpen = false;
            await LoadData();
        }
    }



    private async Task OpenRuleEditor(Rule rule)
    {
        try
        {
            var options = GetDialogOptions();

            var parameters = new DialogParameters { ["Rule"] = rule };
            IsEditorOpen = true;
            var dialog = await this.DialogService.ShowAsync<RuleEditor>("Edit Rule",
                                                                        parameters,
                                                                        options);
            var result = await dialog.Result;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, $"{MethodBase.GetCurrentMethod()} failed!");
        }
        finally
        {
            IsEditorOpen = false;
            await LoadData();
            ApplyRules();
        }
    }

    protected override async Task LoadData()
    {
        Rules = await this.RulesService.GetRulesByProject(SelectedProject);
        await InvokeAsync(StateHasChanged);
    }
}