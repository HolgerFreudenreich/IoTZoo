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

namespace IotZoo.Pages;

using Domain.Interfaces.Crud;
using Domain.Pocos;
using IotZoo.Dialogs;
using Microsoft.AspNetCore.Components;
using MQTTnet;
using MudBlazor;

public class DashboardOverviewPageBase : MqttPageBase
{
   [Inject]
   IDashboardCrudService DashboardService { get; set; } = null!;

   [Inject]
   IDashboardItemsCrudService DashboardItemsService { get; set; } = null!;

   [Inject]
   IKnownTopicsCrudService KnownTopicsService { get; set; } = null!;

   [Inject]
   public IProjectCrudService ProjectService { get; set; } = null!;

   protected List<Project> ProjectsCatalog { get; private set; } = new();

   protected Project? SelectedProject
   {
      get => DataTransferService.SelectedProject;
      set
      {
         DataTransferService.SelectedProject = value;
         _ = LoadDashboardsAsync();
      }
   }

   public List<Dashboard> Dashboards { get; private set; } = new();

   public Dictionary<int, IReadOnlyList<string>> DashboardTopics { get; } = new();

   public IReadOnlyList<string> Topics { get; private set; } = [];

   public Dictionary<string, string> Messages { get; } = new();

   public Dictionary<string, DateTime> MessageReceipts { get; } = new();

   protected override async Task OnInitializedAsync()
   {
      DataTransferService.CurrentScreen = ScreenMode.Dashboards;
      ProjectsCatalog = (await ProjectService.LoadProjects()).ToList();
      await LoadDashboardsAsync();
      await base.OnInitializedAsync();
   }

   public async Task LoadDashboardsAsync()
   {
      DashboardTopics.Clear();
      Dashboards = SelectedProject == null
         ? new List<Dashboard>()
         : (await DashboardService.GetDashboards()).Where(dashboard => dashboard.ProjectId == SelectedProject.ProjectId).ToList();

      foreach (var dashboard in Dashboards)
      {
         var dashboardItems = await DashboardItemsService.GetDashboardItemsByDashboardId(dashboard.DashboardId);
         var topics = new List<string>();
         foreach (var dashboardItem in dashboardItems.OrderBy(item => item.IndexInDashboard))
         {
            var knownTopic = await KnownTopicsService.GetKnownTopicById(dashboardItem.KnownTopicId);
            if (knownTopic != null)
            {
               topics.Add(knownTopic.FullQualifiedTopic);
            }
         }
         DashboardTopics[dashboard.DashboardId] = topics;
      }

         Topics = DashboardTopics.Values.SelectMany(topics => topics).Distinct().ToList();
         foreach (var topic in Topics)
         {
            Messages.TryAdd(topic, string.Empty);
         }
         if (MqttClient?.IsConnected == true)
         {
            foreach (var topic in Topics)
            {
               await MqttClient.SubscribeAsync(topic);
            }
         }
         StateHasChanged();
      }

   public async Task ReorderTopicInDashboard(Dashboard dashboard, int sourceIndex, int targetIndex)
   {
      var dashboardItems = (await DashboardItemsService.GetDashboardItemsByDashboardId(dashboard.DashboardId))
         .OrderBy(item => item.IndexInDashboard)
         .ToList();

      if (sourceIndex == targetIndex || sourceIndex < 0 || targetIndex < 0 || sourceIndex >= dashboardItems.Count || targetIndex >= dashboardItems.Count)
      {
         return;
      }

      var moved = dashboardItems[sourceIndex];
      dashboardItems.RemoveAt(sourceIndex);
      dashboardItems.Insert(targetIndex, moved);

      for (var index = 0; index < dashboardItems.Count; index++)
      {
         if (dashboardItems[index].IndexInDashboard == index)
         {
            continue;
         }
         dashboardItems[index].IndexInDashboard = index;
         await DashboardItemsService.Update(dashboardItems[index]);
      }

         await LoadDashboardsAsync();
         StateHasChanged();
      }

      public async Task DeleteDashboard(Dashboard dashboard)
   {
      bool? confirmed = await DialogService.ShowMessageBoxAsync("Warning!",
                                                                $"Do you really want to delete the dashboard '{dashboard.DashboardName}'?",
                                                                yesText: "Yes",
                                                                cancelText: "No");
      if (confirmed != true)
      {
         return;
      }

      var dashboardItems = await DashboardItemsService.GetDashboardItemsByDashboardId(dashboard.DashboardId);
      foreach (var dashboardItem in dashboardItems)
      {
         await DashboardItemsService.Delete(dashboardItem);
      }
      await DashboardService.Delete(dashboard);

      Dashboards = await DashboardService.GetDashboards();
      DashboardTopics.Remove(dashboard.DashboardId);
      StateHasChanged();
   }

   public async Task RemoveTopicFromDashboard(Dashboard dashboard, string topic)
   {
      var knownTopics = await KnownTopicsService.GetKnownTopics();
      var knownTopic = knownTopics.FirstOrDefault(knownTopic => knownTopic.FullQualifiedTopic == topic);
      if (knownTopic == null)
      {
         return;
      }

      var dashboardItems = await DashboardItemsService.GetDashboardItemsByDashboardId(dashboard.DashboardId);
      var dashboardItem = dashboardItems.FirstOrDefault(item => item.KnownTopicId == knownTopic.KnownTopicId);
      if (dashboardItem != null)
      {
         await DashboardItemsService.Delete(dashboardItem);
      }

      await LoadDashboardsAsync();
      StateHasChanged();
   }

   public async Task AddTopicToDashboard(Dashboard dashboard)
   {
      var knownTopics = await KnownTopicsService.GetKnownTopics();
      var assignedKnownTopicIds = (await DashboardItemsService.GetDashboardItemsByDashboardId(dashboard.DashboardId))
         .Select(item => item.KnownTopicId)
         .ToHashSet();
      var availableTopics = knownTopics.Where(knownTopic => !assignedKnownTopicIds.Contains(knownTopic.KnownTopicId)).ToList();
      if (availableTopics.Count == 0)
      {
         Snackbar.Add("No known topics available!", Severity.Info);
         return;
      }

      var parameters = new DialogParameters { ["KnownTopics"] = availableTopics };
      var dialog = await DialogService.ShowAsync<KnownTopicSelectDialog>("Add Topic to Dashboard", parameters);
      var result = await dialog.Result;
      if (result == null || result.Canceled || result.Data is not KnownTopic selectedTopic)
      {
         return;
      }

      await DashboardItemsService.Insert(new DashboardItem
      {
         DashboardId = dashboard.DashboardId,
         KnownTopicId = selectedTopic.KnownTopicId,
         IndexInDashboard = (await DashboardItemsService.GetDashboardItemsByDashboardId(dashboard.DashboardId)).Count
      });

      await LoadDashboardsAsync();
      if (MqttClient.IsConnected)
      {
         foreach (var topic in DashboardTopics.GetValueOrDefault(dashboard.DashboardId, []))
         {
            await MqttClient.SubscribeAsync(topic);
         }
      }
      StateHasChanged();
   }

   public async Task CloneDashboard(Dashboard dashboard)
   {
      var parameters = new DialogParameters { [nameof(DashboardCloneDialog.DashboardName)] = $"{dashboard.DashboardName} (Copy)" };
      var dialog = await DialogService.ShowAsync<DashboardCloneDialog>("Clone Dashboard", parameters);
      var result = await dialog.Result;
      if (result == null || result.Canceled || result.Data is not string newName || string.IsNullOrWhiteSpace(newName))
      {
         return;
      }

      var newDashboardId = await DashboardService.Insert(new Dashboard
      {
         DashboardName = newName,
         Enabled = dashboard.Enabled,
         ProjectId = dashboard.ProjectId,
         IndexInProject = dashboard.IndexInProject
      });

      var dashboardItems = await DashboardItemsService.GetDashboardItemsByDashboardId(dashboard.DashboardId);
      foreach (var dashboardItem in dashboardItems)
      {
         await DashboardItemsService.Insert(new DashboardItem
         {
            DashboardId = newDashboardId,
            KnownTopicId = dashboardItem.KnownTopicId,
            IndexInDashboard = dashboardItem.IndexInDashboard
         });
      }

      await LoadDashboardsAsync();
      if (MqttClient.IsConnected)
      {
         foreach (var topic in DashboardTopics.GetValueOrDefault(newDashboardId, []))
         {
            await MqttClient.SubscribeAsync(topic);
         }
      }
      StateHasChanged();
   }

   public async Task MoveDashboard(Dashboard dashboard, int direction)
   {
      var ordered = Dashboards.ToList();
      int index = ordered.FindIndex(item => item.DashboardId == dashboard.DashboardId);
      int targetIndex = index + direction;
      if (index < 0 || targetIndex < 0 || targetIndex >= ordered.Count)
      {
         return;
      }

      (ordered[index], ordered[targetIndex]) = (ordered[targetIndex], ordered[index]);

      for (int i = 0; i < ordered.Count; i++)
      {
         if (ordered[i].IndexInProject == i)
         {
            continue;
         }
         ordered[i].IndexInProject = i;
         await DashboardService.Update(ordered[i]);
      }

      await LoadDashboardsAsync();
      StateHasChanged();
   }

   public async Task ReorderDashboard(int sourceIndex, int targetIndex)
   {
      if (sourceIndex == targetIndex || sourceIndex < 0 || targetIndex < 0 || sourceIndex >= Dashboards.Count || targetIndex >= Dashboards.Count)
      {
         return;
      }

      var ordered = Dashboards.ToList();
      var moved = ordered[sourceIndex];
      ordered.RemoveAt(sourceIndex);
      ordered.Insert(targetIndex, moved);

      for (int i = 0; i < ordered.Count; i++)
      {
         if (ordered[i].IndexInProject == i)
         {
            continue;
         }
         ordered[i].IndexInProject = i;
         await DashboardService.Update(ordered[i]);
      }

      await LoadDashboardsAsync();
      StateHasChanged();
   }

   public async Task RenameDashboard(Dashboard dashboard)
   {
      var options = new DialogOptions { MaxWidth = MaxWidth.Small, FullWidth = true, BackdropClick = false, CloseOnEscapeKey = false };
      var parameters = new DialogParameters { ["Dashboard"] = new Dashboard { DashboardId = dashboard.DashboardId, ProjectId = dashboard.ProjectId, DashboardName = dashboard.DashboardName } };
      var dialog = await DialogService.ShowAsync<DashboardEditor>("Edit Dashboard", parameters, options);
      var result = await dialog.Result;
      if (result != null && !result.Canceled)
      {
         await LoadDashboardsAsync();
         StateHasChanged();
      }
   }

   public async Task OpenDashboardEditor()
   {
      var options = new DialogOptions { MaxWidth = MaxWidth.Small, FullWidth = true, BackdropClick = false, CloseOnEscapeKey = false };
      var parameters = new DialogParameters { ["Dashboard"] = new Dashboard { ProjectId = DataTransferService.SelectedProject?.ProjectId ?? 0 } };
      var dialog = await DialogService.ShowAsync<DashboardEditor>("Add Dashboard", parameters, options);
      var result = await dialog.Result;
      if (result != null && !result.Canceled)
      {
         await LoadDashboardsAsync();
         StateHasChanged();
      }
   }

   protected override async Task Client_Connected(MqttClientConnectedEventArgs arg)
   {
      await base.Client_Connected(arg);

      foreach (var topic in Topics)
      {
         await MqttClient.SubscribeAsync(topic);
      }
   }

   protected override async Task Client_ApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs arg)
   {
      await base.Client_ApplicationMessageReceivedAsync(arg);

      string topic = arg.ApplicationMessage.Topic;
      if (!Messages.ContainsKey(topic))
      {
         return;
      }

      string message = arg.ApplicationMessage.ConvertPayloadToString();
      await InvokeAsync(() =>
      {
         Messages[topic] = message;
         MessageReceipts[topic] = DateTime.UtcNow;
         StateHasChanged();
      });
   }
}
