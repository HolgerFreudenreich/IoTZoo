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
using MudBlazor;

namespace IotZoo.Pages;

public class TopicsHistoryPageBase : PageBase
{
    [Inject]
    public ITopicHistoryCrudService TopicHistoryService
    {
        get;
        set;
    } = null!;

    protected List<TopicHistory> TopicHistoryList
    {
        get;
        set;
    } = new();

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);
        if (firstRender)
        {
            ProjectsCatalog = await ProjectService.LoadProjects();
            if (!string.IsNullOrEmpty(ProjectName))
            {
                DataTransferService.SelectedProject = ProjectsCatalog.FirstOrDefault(p => p.ProjectName == ProjectName);
            }
            await LoadData();
        }
    }

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

    [SupplyParameterFromQuery(Name = "search")]
    public string? Search { get; set; }

    [SupplyParameterFromQuery(Name = "project")]
    public string? ProjectName { get; set; }

    protected override void OnInitialized()
    {
        DataTransferService.CurrentScreen = ScreenMode.TopicsHistory;
        if (!string.IsNullOrEmpty(Search))
        {
            SearchHelper.SearchString = Search;
        }
        base.OnInitialized();
    }

    protected override async Task LoadData()
    {
        IsBusy = true;
        await InvokeAsync(StateHasChanged);
        try
        {
            TopicHistoryList = await TopicHistoryService.LoadTopicHistory(SelectedProject);
        }
        finally
        {
            IsBusy = false;
        }
        await InvokeAsync(StateHasChanged);
    }

    protected async void Delete()
    {
        bool? result = await DialogService.ShowMessageBoxAsync("Delete",
                                                               $"Do you want to delete the complete topic history?",
                                                               yesText: "Yes", cancelText: "No");
        if (!result.HasValue)
        {
            return;
        }
        await TopicHistoryService.DeleteAll();
        await LoadData();
    }

    protected void DeleteTopicHistoryEntry(TopicHistory topicHistory)
    {
        TopicHistoryList.Remove(topicHistory);
        TopicHistoryService.DeleteTopicHistoryEntry(topicHistory);
    }

    protected async Task OpenDeleteDialog()
    {
        var parameters = new DialogParameters
        {
            { nameof(DeleteFromTopicHistoryDialogBase.Topics), TopicHistoryList.Select(x => x.Topic).Distinct().OrderBy(x => x).ToList() }
        };
        var options = new DialogOptions { MaxWidth = MaxWidth.Small, FullWidth = true };
        var dialog = await DialogService.ShowAsync<DeleteFromTopicHistoryDialog>("Delete from topic history", parameters, options);
        var result = await dialog.Result;
        if (result == null || result.Canceled || result.Data is not DeleteFromTopicHistoryDialogBase.DeleteRange range)
        {
            return;
        }
        await TopicHistoryService.DeleteTopicHistoryEntries(range.Topic, range.From, range.To);
        await LoadData();
    }

    protected async Task ShowTopicChart(TopicHistory topicHistory)
    {
        var entries = TopicHistoryList.Where(x => x.Topic == topicHistory.Topic).ToList();
        var parameters = new DialogParameters
        {
            { nameof(TopicChartDialogBase.Topic), topicHistory.Topic },
            { nameof(TopicChartDialogBase.Entries), entries }
        };
        var options = new DialogOptions { MaxWidth = MaxWidth.False, FullWidth = true };
        await DialogService.ShowAsync<TopicChartDialog>(topicHistory.Topic, parameters, options);
    }
}