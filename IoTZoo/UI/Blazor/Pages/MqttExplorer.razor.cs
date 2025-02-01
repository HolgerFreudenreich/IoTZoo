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

using Domain.Interfaces.Crud;
using Domain.Pocos;
using Domain.Services.Timer;
using IotZoo.Dialogs;
using Microsoft.AspNetCore.Components;
using MQTTnet;
using MudBlazor;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

public class MqttExplorerBase : MqttPageBase, IDisposable
{
   private readonly TimerService timerServiceRefreshUserInterface;

   private bool groupTopics = true;
   public bool GroupTopics
   {
      get => groupTopics;

      set
      {
         groupTopics = value;
      }
   }

   [Inject]
   protected IRulesCrudService RulesService { get; set; } = null!;

   [Inject]
   IKnownTopicsCrudService KnownTopicsDatabaseService { get; set; } = null!;

   [Inject]
   public IProjectCrudService ProjectService { get; set; } = null!;

   protected List<Project> ProjectsCatalog { get; private set; } = new();

   protected Project? SelectedProject
   {
      get => DataTransferService.SelectedProject;
      set
      {
         DataTransferService.SelectedProject = value;
         _ = OnSelectedProjectChanged(value);
      }
   }

   protected Queue<TopicEntry> ReceivedTopicsQueue { get; set; }

   private string selectedTopic = string.Empty;

   protected JsonSerializerOptions JsonSerializerOptions { get; } = new JsonSerializerOptions
   {
      WriteIndented = true,
      Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
   };

   protected override async Task Client_Connected(MqttClientConnectedEventArgs args)
   {
      Snackbar.Add("MQTT connected!", Severity.Info);
      await SubscribeToProjectTopics();
   }

   private async Task OnSelectedProjectChanged(Project? value)
   {
      ReceivedTopicsQueue.Clear();

      await MqttClient.UnsubscribeAsync(selectedTopic);

      await SubscribeToProjectTopics();

      ReceivedTopicsQueue.Clear();

      await InvokeAsync(StateHasChanged);
   }

   private async Task SubscribeToProjectTopics()
   {
      if (null == DataTransferService.SelectedProject)
      {
         selectedTopic = $"{DataTransferService.NamespaceName}/#";
      }
      else
      {
         selectedTopic = $"{DataTransferService.NamespaceName}/{DataTransferService.SelectedProject.ProjectName}/#";
      }
      await MqttClient.SubscribeAsync(selectedTopic);
   }

   public MqttExplorerBase()
   {
      timerServiceRefreshUserInterface = new TimerService(900);
      timerServiceRefreshUserInterface.OnElapsed -= TimerServiceRefreshUserInterfaceOnElapsed;
      timerServiceRefreshUserInterface.OnElapsed += TimerServiceRefreshUserInterfaceOnElapsed;
      ReceivedTopicsQueue = new Queue<TopicEntry>(75);
   }

   protected override void OnInitialized()
   {
      DataTransferService.CurrentScreen = ScreenMode.MqttExplorer;
      base.OnInitialized();
   }

   protected override async Task OnAfterRenderAsync(bool firstRender)
   {
      if (firstRender)
      {
         ProjectsCatalog = await ProjectService.LoadProjects();
      }
      await base.OnAfterRenderAsync(firstRender);
   }

   /// <summary>
   /// Received a message from the MQTT Broker.
   /// </summary>
   /// <param name="mqttApplicationMessageReceivedEventArgs"></param>
   /// <returns></returns>
   protected override async Task Client_ApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs mqttApplicationMessageReceivedEventArgs)
   {
      try
      {
         await base.Client_ApplicationMessageReceivedAsync(mqttApplicationMessageReceivedEventArgs);

         if (null == mqttApplicationMessageReceivedEventArgs.ApplicationMessage)
         {
            return;
         }

         TopicEntry topicEntry = new();
         var splitted = mqttApplicationMessageReceivedEventArgs.ApplicationMessage.Topic.Split('/');
         if (splitted.Length > 2)
         {
            topicEntry.NamespaceName = splitted[0];
            topicEntry.ProjectName = splitted[1];
            topicEntry.Topic = mqttApplicationMessageReceivedEventArgs.ApplicationMessage.Topic.Substring(topicEntry.NamespaceName.Length + 1 + topicEntry.ProjectName.Length + 1);
         }
         else
         {
            return;
         }

         topicEntry.Payload = JsonPrettify(mqttApplicationMessageReceivedEventArgs.ApplicationMessage.ConvertPayloadToString());
         topicEntry.Retain = mqttApplicationMessageReceivedEventArgs.ApplicationMessage.Retain;

         // Is it a known Topic?
         var knownTopic = await KnownTopicsDatabaseService.GetKnownTopicByTopicName(topicEntry.ProjectName, topicEntry.Topic);

         if (knownTopic != null)
         {
            topicEntry.MessageDirection = knownTopic.MessageDirection; // It is expensive to look in table known_topics, but only there is this information.
         }

         if (GroupTopics)
         {
            var enqueuedTopic = (from data in ReceivedTopicsQueue where data.Topic == topicEntry.Topic select data).FirstOrDefault();

            if (enqueuedTopic != null)
            {
               enqueuedTopic.LastPayload = enqueuedTopic.Payload;
               enqueuedTopic.Payload = topicEntry.Payload;
               enqueuedTopic.TimeDiff = topicEntry.DateOfReceipt - enqueuedTopic.DateOfReceipt;
               enqueuedTopic.DateOfReceipt = topicEntry.DateOfReceipt;
               enqueuedTopic.Retain = topicEntry.Retain;
            }
            else
            {
               this.ReceivedTopicsQueue.Enqueue(topicEntry);
            }
         }
         else
         {
            this.ReceivedTopicsQueue.Enqueue(topicEntry);
         }

         if (ReceivedTopicsQueue.Count > 75)
         {
            ReceivedTopicsQueue.Dequeue();
         }
      }
      catch (Exception ex)
      {
         Snackbar.Add(ex.GetBaseException().Message, Severity.Error);
      }
   }

   private string JsonPrettify(string data)
   {
      try
      {
         if (data.StartsWith("{"))
         {
            using var jsonDocument = JsonDocument.Parse(data);
            return JsonSerializer.Serialize(jsonDocument, JsonSerializerOptions);
         }
      }
      catch
      {
      }
      return data;
   }

   public override void Dispose()
   {
      timerServiceRefreshUserInterface.Dispose();
      if (!string.IsNullOrEmpty(selectedTopic))
      {
         MqttClient?.UnsubscribeAsync(selectedTopic);
      }
      base.Dispose();
   }

   protected async Task ShowRulesBtnPress(TopicEntry topicEntry)
   {
      topicEntry.ShowRules = !topicEntry.ShowRules;

      if (topicEntry.ShowRules)
      {
         topicEntry.Rules = await RulesService.GetRulesBySourceTopic(topicEntry);
      }
   }
   protected override async Task RefreshData(bool firstRender = false)
   {
      await base.RefreshData(firstRender);
      ProjectsCatalog = await ProjectService.LoadProjects();
      await InvokeAsync(StateHasChanged);
   }

   protected async Task RefreshDataGridAsync()
   {
      try
      {
         DataTransferService.TopicsPerSecond = MessageCounter;
         await InvokeAsync(StateHasChanged);
      }
      finally
      {
         MessageCounter = 0;
      }
   }

   private async void TimerServiceRefreshUserInterfaceOnElapsed(System.Timers.Timer timer, TimerServiceEventArgs elapsedEventArgs)
   {
      await RefreshDataGridAsync();
   }

   protected async Task ClearDataAsync()
   {
      ReceivedTopicsQueue.Clear();
      await InvokeAsync(StateHasChanged);
   }

   public async Task AddRule(TopicEntry topicEntry)
   {
      if (string.IsNullOrEmpty(topicEntry.ProjectName))
      {
         if (null != DataTransferService.SelectedProject)
         {
            topicEntry.ProjectName = DataTransferService.SelectedProject.ProjectName;
         }
         else
         {
            string? projectName = await KnownTopicsDatabaseService.LoadProjectNameByTopicName(topicEntry.Topic);
            if (projectName != null)
            {
               topicEntry.ProjectName = projectName;
            }
         }
      }

      var createdRule = await OpenRuleEditor(new Rule { SourceTopic = topicEntry.Topic, ProjectName = topicEntry.ProjectName });
      if (null != topicEntry.Rules && null != createdRule)
      {
         topicEntry.Rules.Add(createdRule);
      }
   }

   public async Task<Rule?> OpenRuleEditor(Rule rule)
   {
      try
      {
         var options = GetDialogOptions();

         var parameters = new DialogParameters { ["Rule"] = rule };
         IsEditorOpen = true;
         var dialog = await this.DialogService.ShowAsync<RuleEditor>("Edit Rule",
                                                                     parameters,
                                                                     options);
         DialogResult? result = await dialog.Result;
         if (result != null)
         {
            return result.Data as Rule;
         }
      }
      finally
      {
         IsEditorOpen = false;
         await LoadData();
      }
      return null;
   }

   protected async Task EditRule(Rule rule)
   {
      await OpenRuleEditor(rule);
   }
}