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
using Domain.Pocos;
using IotZoo.Dialogs;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;

namespace IotZoo.Pages;

public class KnownTopicsPageBase : PageBase
{
    [Inject]
    public IKnownTopicsCrudService KnownTopicsService
    {
        get;
        set;
    } = null!;

    protected List<KnownTopic>? KnownTopics
    {
        get;
        set;
    } = new();

    [Inject]
    public IProjectCrudService ProjectService { get; set; } = null!;

    protected List<Project> ProjectsCatalog { get; private set; } = new();

    private Project? selectedProject = null!;

    protected Project? SelectedProject
    {
        get => DataTransferService.SelectedProject;
        set
        {
            selectedProject = value;
            DataTransferService.SelectedProject = selectedProject;
            _ = LoadData();
        }
    }

    protected override void OnInitialized()
    {
        DataTransferService.CurrentScreen = ScreenMode.KnownTopics;
        base.OnInitialized();
    }

    protected override async Task RefreshData(bool firstRender = false)
    {
        ProjectsCatalog = await ProjectService.LoadProjects();
        await base.RefreshData(firstRender);
    }

    protected async Task CloneTopic(KnownTopic topic)
    {
        var clonedTopic = Infrastructure.Tools.DeepCopyReflection(topic);
        clonedTopic.KnownTopicId = 0; // force new
        await OpenKnownTopicEditor(clonedTopic);
    }

    protected async Task PublishTopic(KnownTopic topic)
    {
        DataTransferService.SelectedKnownTopic = topic;
        await JsRuntime.InvokeVoidAsync("openNewTab", "PublishTopic");
    }

    protected override async Task LoadData()
    {
        if (null == SelectedProject)
        {
            return;
        }
        IsBusy = true;
        await InvokeAsync(StateHasChanged);
        try
        {
            KnownTopics = await KnownTopicsService.GetKnownTopicsByProjectName(SelectedProject.ProjectName, null, 100);
            IsBusy = false;
            await InvokeAsync(StateHasChanged);
            KnownTopics = await KnownTopicsService.GetKnownTopicsByProjectName(SelectedProject.ProjectName, null);
        }
        finally
        {
            IsBusy = false;
        }
        await InvokeAsync(StateHasChanged);
    }

    public async Task OpenKnowTopicEditor()
    {
        if (DataTransferService.SelectedProject == null)
        {
            return;
        }
        await OpenKnownTopicEditor(new KnownTopic { NamespaceName = DataTransferService.NamespaceName, ProjectName = DataTransferService.SelectedProject.ProjectName });
    }

    public async Task OpenKnownTopicEditor(KnownTopic knownTopic)
    {
        try
        {
            var options = GetDialogOptions();

            var parameters = new DialogParameters { ["KnownTopic"] = knownTopic };
            IsEditorOpen = true;
            var dialog = await this.DialogService.ShowAsync<KnownTopicEditor>("Edit Known Topic",
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

    protected async Task EditKnownTopic(KnownTopic knownTopic)
    {
        await OpenKnownTopicEditor(knownTopic);
    }

    protected async Task DeleteKnownTopic(KnownTopic knownTopic)
    {
        bool? result = await DialogService.ShowMessageBoxAsync("Delete",
                                                          $"Do you want to delete the known topic with Id {knownTopic.KnownTopicId}?",
                                                          yesText: "Yes", cancelText: "No");
        if (!result.HasValue)
        {
            return;
        }
        await KnownTopicsService.Delete(knownTopic);
        await LoadData();
    }

    protected void ShowTopicHistory(KnownTopic knownTopic)
    {
        NavigationManager.NavigateTo("TopicsHistory");
    }

}
