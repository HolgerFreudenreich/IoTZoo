// --------------------------------------------------------------------------------------------------------------------
//      ____    ______   _____
//     /  _/___/_  __/  /__  / ____  ____
//     / // __ \/ /       / / / __ \/ __ \
//   _/ // /_/ / /       / /_/ /_/ / /_/ /
//  /___/\____/_/       /____|____/\____/ 
// --------------------------------------------------------------------------------------------------------------------
// Connect «Things» with microcontrollers without programming knowledge.
// --------------------------------------------------------------------------------------------------------------------
// (c) 2025 Holger Freudenreich under MIT license
// --------------------------------------------------------------------------------------------------------------------

using Domain.Interfaces.Crud;
using Domain.Pocos;
using Microsoft.AspNetCore.Components;

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
      base.OnAfterRender(firstRender);
      if (firstRender)
      {
         ProjectsCatalog = await ProjectService.LoadProjects();
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


   protected override void OnInitialized()
   {
      DataTransferService.CurrentScreen = ScreenMode.TopicsHistory;
      base.OnInitialized();
   }

   protected override async Task LoadData()
   {
      TopicHistoryList = await TopicHistoryService.LoadTopicHistory(SelectedProject);
      await InvokeAsync(StateHasChanged);
   }
   
   protected async void Delete()
   {
      bool? result = await DialogService.ShowMessageBox("Delete",
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
}