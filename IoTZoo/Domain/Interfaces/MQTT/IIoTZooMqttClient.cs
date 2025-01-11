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

using Domain.Interfaces.Crud;
using Domain.Pocos;
using Domain.Services.RuleEngine;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Protocol;

namespace Domain.Interfaces.MQTT;

public interface IIoTZooMqttClient
{
   IMqttClient Client { get; }
   IExpressionEvaluationService ExpressionEvaluationService { get; set; }
   ILogger<IIoTZooMqttClient> Logger { get; set; }
   ISettingsCrudService SettingsService { get; set; }
   ITopicHistoryCrudService TopicHistoryService { get; set; }
   Task ApplyRulesAsync();
   Task<MessageHandlingResult> HandleMqttMessage(TopicEntry topicEntry);
   void ClearData();
   Task Connect();
   Task Connect(string brokerIp, int port, string topicToSubscribe);
   Task<bool> PublishTopic(string topic,
                           string? payload,
                           MqttQualityOfServiceLevel exactlyOnce = MqttQualityOfServiceLevel.AtLeastOnce,
                           bool retain = false);
   /// <summary>
   /// Removes a retained topic from the broker.
   /// </summary>
   /// <param name="topic"></param>
   /// <returns></returns>
   Task<bool> RemoveRetainedMessageFromBroker(string topic);
}