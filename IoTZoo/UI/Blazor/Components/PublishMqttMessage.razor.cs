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
using Domain.Interfaces.MQTT;
using Domain.Pocos;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using System.Reflection;
using System.Text.Json;

namespace IotZoo.Components;

public partial class PublishMqttMessage : ComponentBase
{
    [Parameter]
    public TopicEntry TopicEntry { get; set; } = new();

    [Inject]
    protected ILogger<PublishMqttMessage> Logger { get; set; } = null!;

    [Inject]
    protected IIoTZooMqttClient MqttClient { get; set; } = null!;

    [Inject]
    protected ISnackbar Snackbar { get; set; } = null!;

    [Inject]
    protected IJSRuntime JsRuntime { get; set; } = null!;

    [Inject]
    IDataTransferService DataTransferService { get; set; } = null!;

    protected async Task ExportToClipboardAsync()
    {
        try
        {
            string json = JsonSerializer.Serialize(TopicEntry);
            await JsRuntime.InvokeVoidAsync("copyToClipboard", json);
            Snackbar.Add("Topic entry exported to clipboard.", Severity.Success);
        }
        catch (Exception exception)
        {
            Snackbar.Add($"Export failed: {exception.GetBaseException().Message}", Severity.Error);
        }
    }

    protected async Task ImportFromClipboardAsync()
    {
        try
        {
            string? json = await JsRuntime.InvokeAsync<string?>("readClipboardText");
            var topicEntry = string.IsNullOrWhiteSpace(json) ? null : JsonSerializer.Deserialize<TopicEntry>(json);
            if (topicEntry == null || string.IsNullOrWhiteSpace(topicEntry.Topic))
            {
                Snackbar.Add("Clipboard does not contain a valid topic entry.", Severity.Warning);
                return;
            }

            TopicEntry.Topic = topicEntry.Topic;
            TopicEntry.Payload = topicEntry.Payload;
            TopicEntry.Retain = topicEntry.Retain;
            Snackbar.Add("Topic entry imported from clipboard.", Severity.Success);
        }
        catch (Exception exception)
        {
            Snackbar.Add($"Import failed: {exception.GetBaseException().Message}", Severity.Error);
        }
    }

    public async Task PublishAsync()
    {
        try
        {
            bool success = await MqttClient.PublishTopic(TopicEntry.Topic,
                                                         TopicEntry.Payload,
                                                         MQTTnet.Protocol.MqttQualityOfServiceLevel.ExactlyOnce,
                                                         TopicEntry.Retain);

            if (!success)
            {
                Snackbar.Add("Not successful!", Severity.Error);
            }
            else
            {
                Snackbar.Add("Successfully sent.", Severity.Success);
            }
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, $"{MethodBase.GetCurrentMethod()} failed!");
        }
    }
}