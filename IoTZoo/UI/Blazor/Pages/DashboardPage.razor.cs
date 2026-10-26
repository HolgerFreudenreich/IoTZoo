// --------------------------------------------------------------------------------------------------------------------
//      ____    ______   _____
//     /  _/___/_  __/  /__  / ____  ____
//     / // __ \\/ /       / / / __ \\/ __ \\
//   _/ // /_/ / /       / /_/ /_/ / /_/ /
//  /___/\\____/_/       /____|____/\\____/   P L A Y G R O U N D
// --------------------------------------------------------------------------------------------------------------------
// Connect «Things» with microcontrollers in a simple way.
// --------------------------------------------------------------------------------------------------------------------
// (c) 2025 - 2026 Holger Freudenreich under the MIT license
// --------------------------------------------------------------------------------------------------------------------

using Domain.Pocos;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace IotZoo.Pages;

public class DashboardPageBase : ComponentBase
{
    [Parameter]
    public Project? Project { get; set; }

   [Parameter, EditorRequired]
   public IReadOnlyList<string> Topics { get; set; } = [];

   [Parameter, EditorRequired]
   public Dictionary<string, string> Messages { get; set; } = new();

   [Parameter]
   public Dictionary<string, DateTime> MessageReceipts { get; set; } = new();

   [Parameter]
   public Dictionary<string, TopicDisplayType> DisplayTypes { get; set; } = new();

   [Parameter]
   public Dictionary<string, GaugeSettings> GaugeSettings { get; set; } = new();

   [Parameter]
   public Dictionary<string, string?> FriendlyNames { get; set; } = new();

   [Parameter]
   public string DateTimeFormat { get; set; } = string.Empty;

   [Parameter]
   public EventCallback<string> OnDelete { get; set; }

   [Parameter]
   public EventCallback OnEdit { get; set; }

   [Parameter]
   public EventCallback<(string Topic, int SourceIndex, int TargetIndex)> OnTopicReordered { get; set; }

   protected List<TopicItem> TopicItems = [];

   protected record TopicItem(string Topic, string? FriendlyName, string Message, DateTime? DateOfReceipt, TopicDisplayType DisplayType, GaugeSettings GaugeSettings);

   protected override void OnParametersSet()
   {
         TopicItems = Topics.Select(topic => new TopicItem(topic, FriendlyNames.GetValueOrDefault(topic), Messages.GetValueOrDefault(topic, string.Empty), MessageReceipts.TryGetValue(topic, out var receipt) ? receipt : null, DisplayTypes.GetValueOrDefault(topic, TopicDisplayType.Text), GaugeSettings.GetValueOrDefault(topic, new()))).ToList();
      }

   protected async Task MoveTopic(int sourceIndex, int targetIndex)
   {
      if (targetIndex < 0 || targetIndex >= TopicItems.Count)
      {
         return;
      }

      await OnTopicReordered.InvokeAsync((TopicItems[sourceIndex].Topic, sourceIndex, targetIndex));
   }
}
