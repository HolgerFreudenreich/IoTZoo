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
using Microsoft.AspNetCore.Components;
using MudBlazor;
using System.Text.Json;

public class PublishTopicBase : PageBase, IDisposable
{
   [Inject]
   public ISettingsCrudService SettingsService { get; set; } = null!;

   protected class TabView
   {
      public Guid TabGuid { get; init; }
      public string? LabelText { get; init; }
      public TopicEntry? TopicEntry { get; init; }
   }
   protected List<TabView> Tabs { get; private set; } = [];
   
   private bool stateHasChanged;
   protected int UserIndex { get; set; }
   
   async Task RestoreUserTabs()
   {
      var json = await SettingsService.GetSettingString(SettingCategory.MqttPublishTestMessage, SettingKey.MqttData);
      if (!string.IsNullOrEmpty(json))
      {
         Tabs = JsonSerializer.Deserialize<List<TabView>>(json)!;
      }
      if (!Tabs.Any())
      {
         Tabs = [];
         AddTab();
      }

      UserIndex = 0;
      stateHasChanged = true;
   }

   protected override async Task OnInitializedAsync()
   {
      DataTransferService.CurrentScreen = ScreenMode.PublishTopic;
      await RestoreUserTabs();

      await base.OnInitializedAsync();
   }


   protected override void OnAfterRender(bool firstRender)
   {
      base.OnAfterRender(firstRender);
      if (stateHasChanged)
      {
         stateHasChanged = false;
         StateHasChanged();
      }
   }

   public void AddTab()
   {
      Tabs.Add(new TabView { TabGuid = Guid.NewGuid(), LabelText = $"Topic", TopicEntry = new TopicEntry() });
      UserIndex = Tabs.Count - 1; // Automatically switch to the new tab.
      stateHasChanged = true;
   }

   public void RemoveTab(Guid tabIdentificator)
   {
      var tabView = Tabs?.FirstOrDefault((t) => Equals(t.TabGuid, tabIdentificator));
      if (tabView is not null)
      {
         Tabs?.Remove(tabView);
         stateHasChanged = true;
      }
   }

   protected void AddTabCallback()
   {
      AddTab();
   }

   protected async Task Save()
   {
      int result = await SettingsService.Update(SettingCategory.MqttPublishTestMessage, SettingKey.MqttData, JsonSerializer.Serialize<List<TabView>>(Tabs));
      if (1 == result)
      {
         Snackbar.Add("Saved", Severity.Success);
      }
      else
      {
         Snackbar.Add("Could not save!", Severity.Error);
      }
   }

   protected void CloseTabCallback(MudTabPanel panel)
   {
      if (null != panel.ID)
      {
         RemoveTab((Guid)panel.ID);
      }
   }

   public async void Dispose()
   {
      await Save();
   }
}