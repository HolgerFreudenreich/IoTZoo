// --------------------------------------------------------------------------------------------------------------------
//      ____    ______   _____
//     /  _/___/_  __/  /__  / ____  ____
//     / // __ \/ /       / / / __ \/ __ \
//   _/ // /_/ / /       / /_/ /_/ / /_/ /
//  /___/\____/_/       /____|____/\____/ 
// --------------------------------------------------------------------------------------------------------------------
// (c) 2025 Holger Freudenreich under MIT license
// --------------------------------------------------------------------------------------------------------------------
// The MQTT Client which processes the incoming topics and fires new topics resulting on executing rules.
// --------------------------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Logging;
using MQTTnet;

namespace Domain.Interfaces
{
    public interface IMqttPublisher
    {
        ILogger Logger { get; }
        IMqttClient MqttClient { get; }

        void Dispose();
    }
}