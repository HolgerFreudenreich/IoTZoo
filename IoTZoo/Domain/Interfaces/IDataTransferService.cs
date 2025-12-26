// --------------------------------------------------------------------------------------------------------------------
//      ____    ______   _____
//     /  _/___/_  __/  /__  / ____  ____
//     / // __ \/ /       / / / __ \/ __ \
//   _/ // /_/ / /       / /_/ /_/ / /_/ /
//  /___/\____/_/       /____|____/\____/ 
// --------------------------------------------------------------------------------------------------------------------
// (c) 2025 Holger Freudenreich under MIT license
// --------------------------------------------------------------------------------------------------------------------
// Service to transfer data from page to page.
// --------------------------------------------------------------------------------------------------------------------

using Domain.Interfaces.Crud;
using Domain.Pocos;

namespace Domain.Interfaces;

public interface IDataTransferService
{
    bool AlwaysEnqueueTopic { get; set; }
    string DateTimeFormat { get; set; }
    string NamespaceName { get; set; }
    int MqttMessagesQueueSize { get; set; }
    MqttBrokerSettings MqttBrokerSettings { get; set; }
    Project? SelectedProject { get; set; }
    ISettingsCrudService SettingsService { get; set; }
    Queue<TopicEntry> ReceivedTopicsQueue { get; set; }
    long TopicsPerSecond { get; set; }
    PhilipsHueBridgeSettings PhilipsHueBridgeSettings { get; set; }
    bool IsDarkMode { get; set; }
    ScreenMode CurrentScreen { get; set; }
    KnownTopic? SelectedKnownTopic { get; set; }
}