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

using DataAccess.Services;
using Domain.Interfaces.Crud;
using Domain.Interfaces.MQTT;
using Domain.Interfaces.Timer;
using Domain.Pocos;
using IotZoo.Dialogs;
using Microsoft.AspNetCore.Components;
using MQTTnet.Protocol;
using MudBlazor;
using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace IotZoo.Pages;

public class ProjectsPageBase : PageBase
{
    [Inject]
    public IProjectCrudService ProjectDatabaseService
    {
        get;
        set;
    } = null!;

    [Inject]
    public IKnownMicrocontrollerCrudService MicrocontrollerDatabaseService
    {
        get;
        set;
    } = null!;

    [Inject]
    public IKnownTopicsCrudService KnownTopicsService
    {
        get;
        set;
    } = null!;

    [Inject]
    public IRulesCrudService RulesService { get; set; } = null!;

    [Inject]
    IIoTZooMqttClient IoTZooMqttClient { get; set; } = null!;

    [Inject]
    ICronService CronService { get; set; } = null!;

    [Inject]
    public IDashboardCrudService DashboardService { get; set; } = null!;

    [Inject]
    public IDashboardItemsCrudService DashboardItemsService { get; set; } = null!;

    protected List<Project>? Projects
    {
        get;
        set;
    } = new();

    public sealed record ProjectExport(Project Project,
                                       List<KnownTopic>? KnownTopics,
                                       List<Rule>? Rules,
                                       List<KnownMicrocontroller>? KnownMicrocontrollers,
                                       List<DashboardExport>? Dashboards = null);

    public sealed record DashboardExport(Dashboard Dashboard,
                                         List<DashboardItemExport> Items);

    public sealed record DashboardItemExport(DashboardItem Item,
                                             string TopicName);

    protected override void OnInitialized()
    {
        DataTransferService.CurrentScreen = ScreenMode.Projects;
        base.OnInitialized();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);
    }

    protected override async Task LoadData()
    {
        Projects = await this.ProjectDatabaseService.LoadProjects();
        await InvokeAsync(StateHasChanged);
    }

    public async Task OpenProjectEditor(Project project)
    {
        try
        {
            var options = GetDialogOptions();

            var parameters = new DialogParameters { ["Project"] = project };
            IsEditorOpen = true;
            var dialog = await this.DialogService.ShowAsync<IotZoo.Dialogs.ProjectEditor>("Edit Project",
                                                                                          parameters,
                                                                                          options);
            var result = await dialog.Result;
            if (null != result)
            {
                var createdProject = result.Data as Project;
                if (createdProject != null)
                {
                    if (createdProject.IsNew)
                    {
                        await this.CronService.StartProjectCronJobs(createdProject);
                    }
                }
            }
        }
        finally
        {
            IsEditorOpen = false;
            await LoadData();
        }
    }

    public async Task OpenProjectCronJobsEditor(Project project)
    {
        try
        {
            var options = GetDialogOptions();

            var parameters = new DialogParameters { ["Project"] = project };
            IsEditorOpen = true;
            var dialog = await this.DialogService.ShowAsync<IotZoo.Dialogs.ProjectCronJobsEditor>("Edit Project Cron Jobs",
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

    public async Task OpenProjectEditorAsync()
    {
        await OpenProjectEditor(new Project());
    }

    protected async Task EditProject(Project project)
    {
        await OpenProjectEditor(project);
    }

    protected async Task RegisterProjectTopics(Project project)
    {
        try
        {
            await this.KnownTopicsService.RegisterProjectDefaultKnownTopics(project);
            Snackbar.Add("Done");
        }
        catch (Exception exception)
        {
            Snackbar.Add(exception.GetBaseException().Message, Severity.Error);
        }
    }

    protected async Task EditProjectCronJobs(Project project)
    {
        await OpenProjectCronJobsEditor(project);
    }

    protected async Task ExportProject(Project project)
    {
        try
        {
            var export = new ProjectExport(
                project,
                await KnownTopicsService.GetKnownTopicsByProjectName(project.ProjectName, null),
                await RulesService.GetRulesByProject(project) ?? new List<Rule>(),
                await MicrocontrollerDatabaseService.GetMicrocontrollers(project),
                await GetDashboardExports(project));

            var serializerOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            };

            string json = JsonSerializer.Serialize(export, serializerOptions);
            await CopyToClipboard(json);
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, $"Export of project '{project.ProjectName}' failed!");
            Snackbar.Add($"Exporting project to clipboard failed: {exception.GetBaseException().Message}", Severity.Error);
        }
    }

    private async Task<List<DashboardExport>> GetDashboardExports(Project project)
    {
        var knownTopics = await KnownTopicsService.GetKnownTopicsByProjectName(project.ProjectName, null);
        var topicNames = knownTopics.ToDictionary(t => t.KnownTopicId, t => t.Topic);
        var dashboards = (await DashboardService.GetDashboards())
            .Where(d => d.ProjectId == project.ProjectId)
            .OrderBy(d => d.IndexInProject)
            .ToList();

        var result = new List<DashboardExport>();
        foreach (var dashboard in dashboards)
        {
            var items = (await DashboardItemsService.GetDashboardItemsByDashboardId(dashboard.DashboardId))
                .OrderBy(item => item.IndexInDashboard)
                .Where(item => topicNames.ContainsKey(item.KnownTopicId))
                .Select(item => new DashboardItemExport(item, topicNames[item.KnownTopicId]))
                .ToList();
            result.Add(new DashboardExport(dashboard, items));
        }

        return result;
    }

    protected async Task OpenProjectImportDialog()
    {
        try
        {
            var options = GetDialogOptions();

            var parameters = new DialogParameters { ["LabelText"] = "Project JSON", ["LabelButtonSave"] = "Import" };
            IsEditorOpen = true;
            var dialog = await DialogService.ShowAsync<IotZoo.Dialogs.TextEditor>("Import Project", parameters, options);
            var result = await dialog.Result;
            string json = (result?.Data as string ?? string.Empty).Trim();
            if (json.Length > 0)
            {
                await ImportProject(json);
            }
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, $"{MethodBase.GetCurrentMethod()} failed!");
            Snackbar.Add($"Importing project failed: {exception.GetBaseException().Message}", Severity.Error);
        }
        finally
        {
            IsEditorOpen = false;
            await LoadData();
        }
    }

    private async Task ImportProject(string json)
    {
        ProjectExport? export;
        try
        {
            export = JsonSerializer.Deserialize<ProjectExport>(json);
        }
        catch (JsonException)
        {
            Snackbar.Add("Importing project failed: JSON is not valid!", Severity.Error);
            return;
        }

        if (export?.Project == null || string.IsNullOrWhiteSpace(export.Project.ProjectName))
        {
            Snackbar.Add("Importing project failed: JSON does not contain a project name!", Severity.Error);
            return;
        }

        Project importedProject = export.Project;
        string projectName = importedProject.ProjectName;
        Project? existingProject = await ProjectDatabaseService.LoadProjectByName(projectName);
        if (existingProject != null)
        {
            bool? overrideConfirmed = await DialogService.ShowMessageBoxAsync("Override",
                                                                  message: $"The project '{projectName}' already exists. Do you want to override it? Existing topics, rules and microcontrollers with the same identity will be updated.",
                                                                  yesText: "Yes", cancelText: "No");
            if (overrideConfirmed != true)
            {
                return;
            }

            importedProject.ProjectId = existingProject.ProjectId;
            await ProjectDatabaseService.Update(importedProject);
        }
        else
        {
            importedProject.ProjectId = 0;
            await ProjectDatabaseService.Insert(importedProject);
        }

        Dictionary<int, string> exportedTopicNames = (export.KnownTopics ?? new List<KnownTopic>())
            .Where(t => t.KnownTopicId > 0)
            .ToDictionary(t => t.KnownTopicId, t => t.Topic);
        List<KnownTopic> exportedTopics = export.KnownTopics ?? new List<KnownTopic>();
        Dictionary<string, string> parentTopicNames = new();
        foreach (KnownTopic knownTopic in exportedTopics)
        {
            if (knownTopic.ParentKnownTopicId.HasValue && exportedTopicNames.TryGetValue(knownTopic.ParentKnownTopicId.Value, out string? parentName))
            {
                parentTopicNames[knownTopic.Topic] = parentName;
            }
            knownTopic.ProjectName = projectName;
            knownTopic.ParentKnownTopicId = null;
            KnownTopic? existingTopic = await KnownTopicsService.GetKnownTopicByTopicName(projectName, knownTopic.Topic);
            knownTopic.KnownTopicId = existingTopic?.KnownTopicId ?? 0;
            await KnownTopicsService.Save(knownTopic, true);
        }
        foreach (KeyValuePair<string, string> parentTopicName in parentTopicNames)
        {
            KnownTopic? child = await KnownTopicsService.GetKnownTopicByTopicName(projectName, parentTopicName.Key);
            KnownTopic? parent = await KnownTopicsService.GetKnownTopicByTopicName(projectName, parentTopicName.Value);
            if (child != null && parent != null)
            {
                child.ParentKnownTopicId = parent.KnownTopicId;
                await KnownTopicsService.Save(child, true);
            }
        }

        foreach (Rule rule in export.Rules ?? new List<Rule>())
        {
            rule.ProjectName = projectName;
            List<Rule> existingRules = await RulesService.GetRules();
            Rule? existingRule = existingRules.FirstOrDefault(r => r.TargetTopic == rule.TargetTopic
                                                                && r.Expression == rule.Expression
                                                                && r.TargetPayload == rule.TargetPayload);
            rule.RuleId = existingRule?.RuleId ?? 0;
            await RulesService.Save(rule);
        }

        foreach (KnownMicrocontroller microcontroller in export.KnownMicrocontrollers ?? new List<KnownMicrocontroller>())
        {
            microcontroller.ProjectName = projectName;
            await MicrocontrollerDatabaseService.Save(microcontroller, false);
        }

        List<string> skippedTopics = new();
        if (export.Dashboards != null)
        {
            int importedProjectId = importedProject.ProjectId;
            List<Dashboard> existingDashboards = (await DashboardService.GetDashboards())
                .Where(d => d.ProjectId == importedProjectId)
                .ToList();

            foreach (DashboardExport dashboardExport in export.Dashboards)
            {
                Dashboard dashboard = dashboardExport.Dashboard;
                dashboard.ProjectId = importedProjectId;

                int dashboardId;
                Dashboard? existingDashboard = existingDashboards.FirstOrDefault(d => d.DashboardName == dashboard.DashboardName);
                if (existingDashboard != null)
                {
                    dashboard.DashboardId = existingDashboard.DashboardId;
                    await DashboardService.Update(dashboard);
                    dashboardId = dashboard.DashboardId;

                    foreach (DashboardItem existingItem in await DashboardItemsService.GetDashboardItemsByDashboardId(dashboardId))
                    {
                        await DashboardItemsService.Delete(existingItem);
                    }
                }
                else
                {
                    dashboard.DashboardId = 0;
                    dashboardId = await DashboardService.Insert(dashboard);
                }

                foreach (DashboardItemExport itemExport in dashboardExport.Items)
                {
                    KnownTopic? knownTopic = await KnownTopicsService.GetKnownTopicByTopicName(projectName, itemExport.TopicName);
                    if (knownTopic == null)
                    {
                        skippedTopics.Add(itemExport.TopicName);
                        continue;
                    }

                    await DashboardItemsService.Insert(new DashboardItem
                    {
                        DashboardId = dashboardId,
                        KnownTopicId = knownTopic.KnownTopicId,
                        IndexInDashboard = itemExport.Item.IndexInDashboard,
                        DisplayType = itemExport.Item.DisplayType,
                        SettingsJson = itemExport.Item.SettingsJson
                    });
                }
            }
        }

        if (skippedTopics.Count > 0)
        {
            Snackbar.Add($"Dashboard items skipped, unknown topics: {string.Join(", ", skippedTopics.Distinct())}", Severity.Warning);
        }

        Snackbar.Add($"The project '{projectName}' is imported!", Severity.Success);
    }

    protected async Task DeleteProject(Project project)
    {
        bool? result = await DialogService.ShowMessageBoxAsync("Delete",
                                                          message: $"Do you want to delete the Project '{project.ProjectName}'?",
                                                          yesText: "Yes", cancelText: "No");
        if (!result.HasValue)
        {
            return;
        }

        // Delete all register_microcontroller topics from MQTT Broker, otherwhise the project well be recreated after next subcribing.
        var microcontrollers = await MicrocontrollerDatabaseService.GetMicrocontrollers(project);
        bool allRetainedMessagesRemovedFromBroker = true;
        foreach (KnownMicrocontroller microcontroller in microcontrollers)
        {
            bool ok = await IoTZooMqttClient.RemoveRetainedMessageFromBroker($"{microcontroller.ProjectName}/esp32/{microcontroller.MacAddress}/{TopicConstants.REGISTER_MICROCONTROLLER}");
            if (!ok)
            {
                allRetainedMessagesRemovedFromBroker = false;
            }
        }

        if (allRetainedMessagesRemovedFromBroker)
        {
            await CronService.StopProjectCronJobs(project);
            foreach (KnownMicrocontroller microcontroller in microcontrollers)
            {
                await MicrocontrollerDatabaseService.Delete(microcontroller);
            }
            List<Rule> rules = await RulesService.GetRules();
            foreach (Rule rule in rules.Where(r => r.ProjectName == project.ProjectName))
            {
                await RulesService.Delete(rule);
            }
            List<KnownTopic> knownTopics = await KnownTopicsService.GetKnownTopicsByProjectName(project.ProjectName, null);
            foreach (KnownTopic knownTopic in knownTopics)
            {
                await KnownTopicsService.Delete(knownTopic);
            }
            await ProjectDatabaseService.Delete(project);
            Snackbar.Add("The project is deleted!", Severity.Success);
            Snackbar.Add("Be aware of if you start a microcontroller which has this project name configured, than the project will be recreated automatically!", Severity.Warning);
        }
        else
        {
            Snackbar.Add("Project not deleted because of problems to remove retained topic from MQTT Broker. Ensure the MQTT broker is online!");
        }

        await LoadData();
    }

    protected async Task RestartProject(Project project)
    {
        bool? result = await DialogService.ShowMessageBoxAsync("Restart",
                                                          message: $"Do you want to restart the Project '{project.ProjectName}'? The topic '{project.ProjectName}/{TopicConstants.INIT}' will be published. The assigned microcontrollers will not be bootet.",
                                                          yesText: "Yes", cancelText: "No");
        if (!result.HasValue)
        {
            return;
        }

        string topic = $"{project.ProjectName}/{TopicConstants.INIT}";

        await IoTZooMqttClient.PublishTopic(topic, null, MqttQualityOfServiceLevel.ExactlyOnce);

        await LoadData();
    }

    protected async Task OpenProjectOverviewDialog(Project project)
    {
        try
        {
            var options = GetDialogOptions();

            var parameters = new DialogParameters { ["Project"] = project };
            IsEditorOpen = true;
            var dialog = await this.DialogService.ShowAsync<ProjectOverviewDialog>($"Overview of Project '{project.ProjectName}'",
                                                                                   parameters,
                                                                                   options);
            var result = await dialog.Result;
        }
        finally
        {
            IsEditorOpen = false;
            // await LoadData();
        }
    }
}
