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

using Domain.Interfaces;
using Domain.Interfaces.Crud;
using Domain.Pocos;
using IotZoo.Dialogs;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using System.Globalization;

namespace IotZoo.Components;

public partial class TopicComponent : ComponentBase
{
    [Inject]
    public IDialogService DialogService { get; set; } = null!;

    [Inject]
    public IDataTransferService DataTransferService { get; set; } = null!;

    [Inject]
    public ITopicHistoryCrudService TopicHistoryService { get; set; } = null!;

    [Inject]
    public IKnownTopicsCrudService KnownTopicsService { get; set; } = null!;

    [Parameter]
    public string Topic { get; set; } = string.Empty;

    [Parameter]
    public string? FriendlyName { get; set; }

    [Parameter]
    public string Message { get; set; } = string.Empty;

    [Parameter]
    public TopicDisplayType DisplayType { get; set; } = TopicDisplayType.Text;

    [Parameter]
    public int FontSize { get; set; } = 14;

    [Parameter]
    public GaugeSettings GaugeSettings { get; set; } = new();

    [Parameter]
    public DateTime? DateOfReceipt { get; set; }

    [Parameter]
    public string DateTimeFormat { get; set; } = string.Empty;

    [Parameter]
    public Project? Project { get; set; }

    [Parameter]
    public EventCallback<string> OnDelete { get; set; }

    [Parameter]
    public EventCallback OnEdit { get; set; }

    private string GaugeStyle => $"width:calc(100% - 24px); height:{GaugeSettings.Height}px; font-size:{FontSize}px;";

    private double GaugeValue => double.TryParse(Message, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ? value : 0;

    private double GaugeStep => (GaugeSettings.Max - GaugeSettings.Min) / 4;

    private string LabelStyle => GaugeSettings.GaugeType == GaugeType.Arc ? "margin-top:-80px;" : string.Empty;

    private string TextStyle => double.TryParse(Message, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) && GaugeSettings.Ranges.FirstOrDefault(r => value >= r.From && value <= r.To) is { } range ? $"color:{range.Color};" : string.Empty;

    private Radzen.Orientation RadzenOrientation => GaugeSettings.Orientation == GaugeOrientation.Vertical ? Radzen.Orientation.Vertical : Radzen.Orientation.Horizontal;

    private string RelativeTopic
    {
        get
        {
            var prefix = $"{DataTransferService.NamespaceName}/{Project?.ProjectName}/";
            return Topic.StartsWith(prefix, StringComparison.Ordinal) ? Topic[prefix.Length..] : Topic;
        }
    }

    private string DisplayName => string.IsNullOrWhiteSpace(FriendlyName) ? RelativeTopic : FriendlyName;

    private async Task EditTopic()
    {
        var knownTopic = await KnownTopicsService.GetKnownTopicByTopicName(Project?.ProjectName ?? string.Empty, RelativeTopic);
        if (knownTopic == null)
        {
            return;
        }
        var parameters = new DialogParameters { [nameof(KnownTopicEditorBase.KnownTopic)] = knownTopic };
        var options = new DialogOptions { MaxWidth = MaxWidth.Medium, FullWidth = true };
        var dialog = await DialogService.ShowAsync<KnownTopicEditor>("Edit Known Topic", parameters, options);
        var result = await dialog.Result;
        if (result?.Canceled == false)
        {
            await OnEdit.InvokeAsync();
        }
    }

    private async Task ShowChart()
    {
        var history = await TopicHistoryService.LoadTopicHistory(Project);
        var entries = history.Where(x => x.Topic == RelativeTopic).ToList();
        var parameters = new DialogParameters
        {
            { nameof(TopicChartDialogBase.Topic), Topic },
            { nameof(TopicChartDialogBase.Entries), entries }
        };
        var options = new DialogOptions { MaxWidth = MaxWidth.False, FullWidth = true };
        await DialogService.ShowAsync<TopicChartDialog>(Topic, parameters, options);
    }
}
