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

using DataAccess.Interfaces;
using DataAccess.Services;
using Domain.Interfaces;
using Domain.Interfaces.Crud;
using Domain.Interfaces.MQTT;
using Domain.Interfaces.RuleEngine;
using Domain.Interfaces.Timer;
using Domain.Pocos;
using Domain.Services.RuleEngine;
using Domain.Services.Timer;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Protocol;
using MudBlazor;
using Quartz.Spi;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Timers;

namespace Domain.Services.MQTT;

public class IotZooMqttClient : IIoTZooMqttClient, IDisposable
{
   public ILogger<IIoTZooMqttClient> Logger { get; set; }

   public IMqttClient Client { get; private set; } = null!;

   protected IKnownMicrocontrollerCrudService MicrocontrollerService { get; set; }

   protected IKnownTopicsCrudService KnownTopicsDatabaseService { get; set; }

   protected IHueBridgeService HueBridgeService { get; set; }

   protected IProjectCrudService ProjectService { get; set; }

   public IExpressionEvaluationService ExpressionEvaluationService { get; set; }

   public ITopicHistoryCrudService TopicHistoryService { get; set; }

   public ISettingsCrudService SettingsService { get; set; }

   /// <summary>
   /// Topics of ApplicationMessages to be send delayed.
   /// </summary>
   private readonly List<TimerService> delayedMessages = new();

   private bool firstConnected;

   protected IRulesCrudService RulesService { get; set; }

   protected IDataTransferService DataTransferService { get; set; }

   protected IPrepareTargetPayload PrepareTargetPayload { get; set; }

   public ICronCrudService CronCrudService { get; }

   public IotZooMqttClient(IOptions<AppSettings> options,
                           IKnownMicrocontrollerCrudService microcontrollerDatabaseService,
                           IDataTransferService dataTransferService,
                           IKnownTopicsCrudService knownTopicsService,
                           IHueBridgeService hueBridgeService,
                           IProjectCrudService projectService,
                           ILogger<IotZooMqttClient> logger,
                           IRulesCrudService rulesService,
                           ISettingsCrudService settingsService,
                           ITopicHistoryCrudService topicHistoryService,
                           IExpressionEvaluationService expressionEvaluationService,
                           IPrepareTargetPayload prepareTargetPayload,
                           IJobFactory jobFactory,
                           ICronCrudService cronCrudService)
   {
      Logger = logger;
      MicrocontrollerService = microcontrollerDatabaseService;
      RulesService = rulesService;
      KnownTopicsDatabaseService = knownTopicsService;
      HueBridgeService = hueBridgeService;
      ProjectService = projectService;
      DataTransferService = dataTransferService;
      SettingsService = settingsService;
      TopicHistoryService = topicHistoryService;

      ExpressionEvaluationService = expressionEvaluationService;
      PrepareTargetPayload = prepareTargetPayload;
      CronCrudService = cronCrudService;
      _ = ApplyRulesAsync();

      // Read settings from database
      /*
      var mqttBrokerSettings = SettingsService.GetObject(SettingCategory.MqttBrokerSettings,
                                                         SettingKey.MqttBrokerSettings).Result;
      if (null != mqttBrokerSettings)
      {
         dataTransferService.MqttBrokerSettings = (MqttBrokerSettings)mqttBrokerSettings;
         if (DataTransferService.MqttBrokerSettings.UseInternalMqttBroker)
         {
            DataTransferService.MqttBrokerSettings.Ip = Dns.GetHostName(); // "localhost" does not work!
         }
      }
      */
   }

   public async Task ApplyRulesAsync()
   {
      await RulesService.GetRules();
   }

   public async Task Connect(string brokerIp, int port, string topicToSubscribe)
   {
      if (string.IsNullOrEmpty(brokerIp))
      {
         return;
      }

      var factory = new MqttClientFactory();
      Client = factory.CreateMqttClient();
      var mqttClientOptions = new MqttClientOptionsBuilder().WithTcpServer(brokerIp, port).Build();

      Client.ApplicationMessageReceivedAsync += Client_ApplicationMessageReceivedAsync;

      Client.ConnectedAsync += async e =>
      {
         await Client.SubscribeAsync(topicToSubscribe, MqttQualityOfServiceLevel.ExactlyOnce);
         await OnConnectedAsync();
      };

      Client.DisconnectedAsync += async e =>
      {
         await Task.Delay(TimeSpan.FromMilliseconds(1000));
         try
         {
            await Client.ConnectAsync(mqttClientOptions);
         }
         catch
         {
            Logger.LogError("Reconnectfailed!");
         }
      };
      try
      {
         await Client.ConnectAsync(mqttClientOptions);
      }
      catch (Exception exception)
      {
         Logger.LogError(exception, "Connecting failed!");
      }
   }

   private async Task OnConnectedAsync()
   {
      if (!firstConnected)
      {
         firstConnected = true;
         var projects = await ProjectService.LoadProjects();

         // publish <ProjectName>/INIT topic.
         foreach (var project in projects)
         {
            string topic = $"{DataTransferService.NamespaceName}/{project.ProjectName}/{TopicConstants.INIT}";
            //await KnownTopicsDatabaseService.Save(new KnownTopic
            //{
            //   ProjectName = project.ProjectName,
            //   MessageDirection = MessageDirection.Internal,
            //   Topic = topic,
            //   Description = $"topic to setup your rules for the project '{project.ProjectName}'. It is triggert when the IotZoo-Client is starting."
            //});

            await PublishTopic(topic, null, MqttQualityOfServiceLevel.ExactlyOnce);
         }

         await Client.SubscribeAsync(TopicConstants.I_AM_LOST);
      }
   }

   /// <summary>
   /// Apply rules to an incoming message.
   /// </summary>
   /// <param name="topicEntry"></param>
   /// <returns></returns>
   public async Task<MessageHandlingResult> HandleMqttMessage(TopicEntry topicEntry)
   {
      MessageHandlingResult messageHandlingResult = new MessageHandlingResult();

      if (DataTransferService.NamespaceName != topicEntry.NamespaceName)
      {
         return messageHandlingResult;
      }

      if (topicEntry.Topic.EndsWith(TopicConstants.ALIVE, StringComparison.OrdinalIgnoreCase))
      {
         AliveMessage? aliveMessage = null;
         try
         {
            aliveMessage = JsonSerializer.Deserialize<AliveMessage>(topicEntry.Payload!);
         }
         catch (Exception ex)
         {
            Logger.LogError(ex, ex.GetBaseException().Message);
         }
         if (aliveMessage != null)
         {
            string topic = $"{DataTransferService.NamespaceName}/{aliveMessage.Microcontroller.ProjectName}/{aliveMessage.Microcontroller.BoardType}/{aliveMessage.Microcontroller.MacAddress}/{TopicConstants.ALIVE_ACK}";
            //string json = JsonSerializer.Serialize(aliveMessage.Microcontroller);
            await Client.PublishStringAsync(topic, "ok");
         }
      }

      // Is it a known Topic?
      KnownTopic? knownTopic = await KnownTopicsDatabaseService.GetKnownTopicByTopicName(topicEntry.ProjectName, topicEntry.Topic);

      if (knownTopic != null)
      {
         if (knownTopic.KeepHistory)
         {
            await TopicHistoryService.Insert(new TopicHistory
            {
               Topic = topicEntry.Topic,
               Payload = topicEntry.Payload,
               ProjectName = knownTopic.ProjectName
            });
         }

         // To enable the payload to be read out.
         knownTopic.LastPayload = topicEntry.Payload;
         knownTopic.PayloadUpdatedAt = DateTime.UtcNow;
         knownTopic.Retained = topicEntry.Retain;
         knownTopic.Sender = topicEntry.Sender;
         topicEntry.IsKnown = true;
         topicEntry.Description = knownTopic.Description;
         topicEntry.MessageDirection = knownTopic.MessageDirection;

         await KnownTopicsDatabaseService.Save(knownTopic);
      }

      // Handle Default Topics
      await HandleDefaultTopicsAsync(topicEntry);

      // Logger.LogDebug(topicEntry.Topic);

      // Are there rules for the topic? Note that there can also be rules for default topics.
      topicEntry.Rules = (from data in await RulesService.GetRulesBySourceTopic(topicEntry, onlyEnabledRules: true)
                          select data).OrderBy(x => x.Priority).ToList();
      foreach (Rule rule in topicEntry.Rules)
      {
         var expressionEvaluationResult =
             await EvaluateExpressionAsync(topicEntry, messageHandlingResult.EnqueuedTopic, rule);

         rule.ExpressionEvaluationResult = expressionEvaluationResult.Matches;
         rule.ExpressionEvaluationProtocol = expressionEvaluationResult.Protocol;

         await RulesService.Save(rule);
         if (rule.ExpressionEvaluationResult)
         {
            var targetPayload = await PublishTargetPayload(rule, topicEntry);
            if (targetPayload != null)
            {
               messageHandlingResult.TargetPayloads.Add(targetPayload);
            }
         }
      }
      return messageHandlingResult;
   }

   private async Task<ExpressionEvaluationResult> EvaluateExpressionAsync(TopicEntry topicEntry,
                                                                          TopicEntry? enqueuedTopic,
                                                                          Rule rule)
   {
      ExpressionEvaluationResult expressionEvaluationResult = new();
      try
      {
         if (rule.TriggerCondition == TriggerCondition.FireOnSourcePayloadChanged)
         {
            if (null == enqueuedTopic)
            {
               return new ExpressionEvaluationResult { Protocol = "null == enqueuedTopic" };
            }

            // Only fire when there is a change.
            if (enqueuedTopic.PreviousPayload == topicEntry.Payload)
            {
               return new ExpressionEvaluationResult { Protocol = "no data change" };
            }
         }

         expressionEvaluationResult =
             await ExpressionEvaluationService.EvaluateExpressionAsync(rule.Expression, topicEntry.Payload);
      }
      catch (Exception ex)
      {
         Logger.LogError(ex, $"{MethodBase.GetCurrentMethod()} failed!");
      }

      return expressionEvaluationResult;
   }

   /// <summary>
   /// We have received a message from the broker. We have subscribed #.
   /// </summary>
   /// <param name="mqttApplicationMessageReceivedEventArgs"></param>
   /// <returns></returns>
   public async Task Client_ApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs mqttApplicationMessageReceivedEventArgs)
   {
      try
      {
         if (null == mqttApplicationMessageReceivedEventArgs.ApplicationMessage)
         {
            return;
         }

         TopicEntry topicEntry = new TopicEntry();

         var splitted = mqttApplicationMessageReceivedEventArgs.ApplicationMessage.Topic.Split('/');
         topicEntry.NamespaceName = splitted[0];

         topicEntry.Payload = mqttApplicationMessageReceivedEventArgs.ApplicationMessage.ConvertPayloadToString();
         topicEntry.QualityOfServiceLevel = (int)mqttApplicationMessageReceivedEventArgs.ApplicationMessage.QualityOfServiceLevel;
         topicEntry.Retain = mqttApplicationMessageReceivedEventArgs.ApplicationMessage.Retain;
         topicEntry.DateOfReceipt = DateTime.UtcNow;

         if (splitted.Length < 3)
         {
            if (mqttApplicationMessageReceivedEventArgs.ApplicationMessage.Topic.StartsWith(TopicConstants.I_AM_LOST))
            {
               if (!string.IsNullOrEmpty(topicEntry.Payload))
               {
                  var microcontroller = JsonSerializer.Deserialize<KnownMicrocontroller>(topicEntry.Payload);
                  if (null != microcontroller && !string.IsNullOrEmpty(microcontroller.MacAddress))
                  {
                     // probably the namespace has been changed.
                     var microcontrollerFromDatabase = await MicrocontrollerService.GetMicrocontroller(topicEntry.Payload);
                     if (null == microcontrollerFromDatabase)
                     {
                        // this microcontroller was never registered!
                        microcontrollerFromDatabase = new KnownMicrocontroller
                        {
                           NamespaceName = DataTransferService.NamespaceName,
                           IpAddress = microcontroller.IpAddress,
                           MacAddress = microcontroller.MacAddress,
                           ProjectName = microcontroller.ProjectName,
                           FirmwareVersion = microcontroller.FirmwareVersion,
                           IpMqttBroker = DataTransferService.MqttBrokerSettings.Ip
                        };
                        if (string.IsNullOrEmpty(microcontrollerFromDatabase.ProjectName))
                        {
                           microcontrollerFromDatabase.ProjectName = "default_project";
                        }
                     }

                     if (microcontrollerFromDatabase != null && microcontroller != null &&
                         (microcontroller.ProjectName != microcontrollerFromDatabase.ProjectName ||
                          microcontroller.NamespaceName != DataTransferService.NamespaceName))
                     {
                        microcontrollerFromDatabase.NamespaceName = DataTransferService.NamespaceName;
                        await MicrocontrollerService.Save(microcontrollerFromDatabase, false);
                        // tell the microcontroller the namespace and the project.
                        string topic = $"{microcontrollerFromDatabase.MacAddress}/{TopicConstants.SAVE_MICROCONTROLLER_CONFIG}";
                        string json = System.Text.Json.JsonSerializer.Serialize(microcontrollerFromDatabase);
                        await PublishTopic(topic, json);
                     }
                  }
               }
            }
            return;
         }

         topicEntry.ProjectName = splitted[1];
         topicEntry.Topic = mqttApplicationMessageReceivedEventArgs.ApplicationMessage.Topic.Substring(topicEntry.NamespaceName.Length + 1 + topicEntry.ProjectName.Length + 1);

         MessageHandlingResult messageHandlingResult = await HandleMqttMessage(topicEntry);
         if (messageHandlingResult != null)
         {
            messageHandlingResult.EnqueuedTopic =
                (from data in DataTransferService.ReceivedTopicsQueue where data.Topic == topicEntry.Topic select data)
                .FirstOrDefault();
            if (messageHandlingResult.EnqueuedTopic != null)
            {
               messageHandlingResult.EnqueuedTopic.LastPayload = messageHandlingResult.EnqueuedTopic.Payload;
               messageHandlingResult.EnqueuedTopic.Payload = topicEntry.Payload;
               messageHandlingResult.EnqueuedTopic.TimeDiff = topicEntry.DateOfReceipt - messageHandlingResult.EnqueuedTopic.DateOfReceipt;
               messageHandlingResult.EnqueuedTopic.DateOfReceipt = topicEntry.DateOfReceipt;
               messageHandlingResult.EnqueuedTopic.MessageDirection = topicEntry.MessageDirection;
               messageHandlingResult.EnqueuedTopic.Retain = topicEntry.Retain;
            }
            else
            {
               DataTransferService.ReceivedTopicsQueue.Enqueue(topicEntry);
            }
         }

         if (DataTransferService.ReceivedTopicsQueue.Count > DataTransferService.MqttMessagesQueueSize)
         {
            DataTransferService.ReceivedTopicsQueue.Dequeue();
         }
      }

      catch (Exception exception)
      {
         Logger.LogError(exception, $"{MethodBase.GetCurrentMethod()} failed!");
         throw;
      }
   }

   private MqttApplicationMessage CreateApplicationMessage(string topic,
                                                           string? payload,
                                                           MqttQualityOfServiceLevel mqttQualityOfServiceLevel = MqttQualityOfServiceLevel.AtMostOnce,
                                                           bool retain = false)
   {
      if (retain == false && string.IsNullOrEmpty(payload))
      {
         payload = "NULL";
      }
      MqttApplicationMessage applicationMessage = new MqttApplicationMessageBuilder()
             .WithTopic(topic)
             .WithPayload(payload)
             .WithQualityOfServiceLevel(mqttQualityOfServiceLevel)
             .WithRetainFlag(retain)
             .Build();
      return applicationMessage;
   }

   private async Task<TopicEntry?> PublishTargetPayload(Rule rule, TopicEntry topicEntry)
   {
      TopicEntry publishedTopic = new();
      string? targetPayload = await PrepareTargetPayload.Execute(expression: rule.TargetPayload, payload: topicEntry.Payload);
      if (null == targetPayload)
      {
         return publishedTopic;
      }

      if (string.IsNullOrEmpty(rule.TargetTopicFullQualified))
      {
         return publishedTopic;
      }

      MqttApplicationMessage applicationMessage = CreateApplicationMessage(rule.TargetTopicFullQualified, targetPayload);

      if (rule.DelayMs > 0)
      {
         TimerService? existingTimerService = null;

         var payload = targetPayload;
         existingTimerService = (from data in delayedMessages
                                 where data.ApplicationMessage.Topic == rule.TargetTopic &&
                                       data.ApplicationMessage.ConvertPayloadToString() == payload
                                 select data).FirstOrDefault();

         if (null != existingTimerService)
         {
            // Cancel this timer.
            existingTimerService.Stop();
            delayedMessages.Remove(existingTimerService);
            rule.LastTriggerDateTime = DateTime.UtcNow;
            return publishedTopic;
         }
         else
         {
            // Send with a delay
            TimerService timerService = new TimerService(rule.DelayMs,
                true)
            {
               ApplicationMessage = applicationMessage
            };
            timerService.OnElapsed += TimerService_OnElapsed;
            delayedMessages.Add(timerService);
            return publishedTopic;
         }
      }

      rule.LastTriggerDateTime = DateTime.UtcNow;

      await Client.PublishAsync(applicationMessage);

      publishedTopic.Topic = rule.TargetTopic ?? string.Empty;
      publishedTopic.Payload = targetPayload;

      return publishedTopic;
   }

   /// <summary>
   /// 
   /// </summary>
   /// <returns>true, if the topic is handled; else false.</returns>
   private async Task<bool> HandleDefaultTopicsAsync(TopicEntry topicEntry)
   {
      if (string.IsNullOrEmpty(topicEntry.Topic))
      {
         return true;
      }

      if (!string.IsNullOrEmpty(topicEntry.Payload))
      {
         if (topicEntry.Topic.EndsWith("alive",
                                       StringComparison.OrdinalIgnoreCase))
         {
            await PublishTopic(topicEntry.Topic + "_ack", Convert.ToString(DateTime.Now));
         }
         else if (topicEntry.Topic.EndsWith(TopicConstants.REGISTER_MICROCONTROLLER,
                                            StringComparison.OrdinalIgnoreCase))
         {
            await RegisterMicrocontroller(topicEntry.Payload);
         }
         else if (topicEntry.Topic.EndsWith(TopicConstants.REGISTER_KNOWN_TOPIC,
                                          StringComparison.OrdinalIgnoreCase))
         {
            await RegisterKnownTopic(topicEntry.Payload);
         }
      }

      // green: {"LightId": 9, "Color": {"X": 0.2465, "Y": 0.6425}}
      // yellow: {"LightId": 9, "Color": {"X": 0.469,"Y":0.4754}}
      // red: {"LightId": 9, "Color": {"X": 0.6915,"Y":0.3083}}
      if (topicEntry.Topic.Contains($"{TopicConstants.HUE}/{TopicConstants.LIGHT}/{TopicConstants.COLOR}", StringComparison.OrdinalIgnoreCase))
      {
         string jsonCmd = string.Empty;
         if (topicEntry.Topic.EndsWith(TopicConstants.GREEN, StringComparison.OrdinalIgnoreCase))
         {
            jsonCmd = "{\"LightId\": " + topicEntry.Payload + " , \"Color\": {\"X\": 0.2465, \"Y\": 0.6425}}";
         }
         else if (topicEntry.Topic.EndsWith(TopicConstants.YELLOW, StringComparison.OrdinalIgnoreCase))
         {
            jsonCmd = "{\"LightId\": " + topicEntry.Payload + " , \"Color\": {\"X\": 0.469, \"Y\": 0.4754}}";
         }
         else if (topicEntry.Topic.EndsWith(TopicConstants.ORANGE, StringComparison.OrdinalIgnoreCase))
         {
            jsonCmd = "{\"LightId\": " + topicEntry.Payload + " , \"Color\": {\"X\": 0.5469, \"Y\": 0.417}}";
         }
         else if (topicEntry.Topic.EndsWith(TopicConstants.CYAN, StringComparison.OrdinalIgnoreCase))
         {
            jsonCmd = "{\"LightId\": " + topicEntry.Payload + " , \"Color\": {\"X\":  0.1644, \"Y\":0.4825 }}";
         }
         else if (topicEntry.Topic.EndsWith(TopicConstants.RED, StringComparison.OrdinalIgnoreCase))
         {
            jsonCmd = "{\"LightId\": " + topicEntry.Payload + " , \"Color\": {\"X\": 0.6915, \"Y\":  0.3083}}";
         }
         else if (topicEntry.Topic.EndsWith(TopicConstants.BLUE, StringComparison.OrdinalIgnoreCase))
         {
            jsonCmd = "{\"LightId\": " + topicEntry.Payload + " , \"Color\": {\"X\": 0.1559, \"Y\": 0.1511}}";
         }
         else if (topicEntry.Topic.EndsWith(TopicConstants.PURPLE, StringComparison.OrdinalIgnoreCase))
         {
            jsonCmd = "{\"LightId\": " + topicEntry.Payload + " , \"Color\": {\"X\": 0.3768, \"Y\": 0.2758}}";
         }
         else if (topicEntry.Topic.EndsWith(TopicConstants.GOLD, StringComparison.OrdinalIgnoreCase))
         {
            jsonCmd = "{\"LightId\": " + topicEntry.Payload + " , \"Color\": {\"X\": 0.5061, \"Y\": 0.4476}}";
         }
         else if (topicEntry.Topic.EndsWith(TopicConstants.WHITE_COLD, StringComparison.OrdinalIgnoreCase))
         {
            jsonCmd = "{\"LightId\": " + topicEntry.Payload + " , \"Color\": {\"X\": 0.4046, \"Y\":0.3915}}";
         }
         else if (topicEntry.Topic.EndsWith(TopicConstants.WHITE_WARM, StringComparison.OrdinalIgnoreCase))
         {
            jsonCmd = "{\"LightId\": " + topicEntry.Payload + " , \"Color\": {\"X\": 0.4672, \"Y\": 0.412}}";
         }
         else
         {
            jsonCmd = topicEntry.Payload!;
         }

         HueColorCommand? colorCommand = null;

         try
         {
            if (!string.IsNullOrEmpty(jsonCmd))
            {
               colorCommand = JsonSerializer.Deserialize<HueColorCommand>(jsonCmd);
            }
         }
         catch (Exception exception)
         {
            Logger.LogError(exception, $"{MethodBase.GetCurrentMethod()} failed!");
            return false;
         }
         if (null != colorCommand)
         {
            if (null != colorCommand.Color)
            {
               return await HueBridgeService.SetColor(colorCommand.LightId, colorCommand.Color.X, colorCommand.Color.Y);
            }
         }
      }
      else if (topicEntry.Topic.EndsWith($"{TopicConstants.HUE}/{TopicConstants.LIGHT}/{TopicConstants.ON}"))
      {
         await HueBridgeService.TurnOnLight(Convert.ToInt32(topicEntry.Payload));
         return true;
      }
      else if (topicEntry.Topic.EndsWith($"{TopicConstants.HUE}/{TopicConstants.LIGHT}/{TopicConstants.OFF}",
                   StringComparison.OrdinalIgnoreCase))
      {
         await HueBridgeService.TurnOffLight(Convert.ToInt32(topicEntry.Payload));
         return true;
      }
      else if (topicEntry.Topic.EndsWith($"{TopicConstants.HUE}/{TopicConstants.LIGHT}/{TopicConstants.TOGGLE}",
                   StringComparison.OrdinalIgnoreCase))
      {
         await HueBridgeService.ToggleState(Convert.ToInt32(topicEntry.Payload));

         return true;
      }
      else if (topicEntry.Topic.EndsWith($"{TopicConstants.HUE}/{TopicConstants.LIGHT}/{TopicConstants.DARKER}",
                   StringComparison.OrdinalIgnoreCase))
      {
         await HueBridgeService.MakeLightDarker(Convert.ToInt32(topicEntry.Payload));
         return true;
      }
      else if (topicEntry.Topic.EndsWith($"{TopicConstants.HUE}/{TopicConstants.LIGHT}/{TopicConstants.BRIGHTER}",
                   StringComparison.OrdinalIgnoreCase))
      {
         await HueBridgeService.MakeLightBrighter(Convert.ToInt32(topicEntry.Payload));
         return true;
      }
      else if (topicEntry.Topic.EndsWith($"{TopicConstants.HUE}/{TopicConstants.LIGHT}/{TopicConstants.BRIGHTNESS}",
                 StringComparison.OrdinalIgnoreCase))
      {
         try
         {
            if (!string.IsNullOrEmpty(topicEntry.Payload))
            {
               HueBrightnessCommand? command = null;
               try
               {
                  command = JsonSerializer.Deserialize<HueBrightnessCommand>(topicEntry.Payload);
               }
               catch (Exception exception)
               {
                  Logger.LogError(exception, $"Deserialize<HueBrightnessCommand> of payload {topicEntry.Payload} failed!");
               }
               if (command != null)
               {
                  await HueBridgeService.SetLightBrightness(command.LightId, command.Brightness);
               }
            }
         }
         catch (Exception exception)
         {
            Logger.LogError(exception, $"{MethodBase.GetCurrentMethod()} failed!");
         }
         return true;
      }
      return false;
   }

   private async Task RegisterKnownTopic(string payload)
   {
      KnownTopic? knownTopic = null;
      try
      {
         knownTopic = JsonSerializer.Deserialize<KnownTopic>(payload);
         if (null == knownTopic)
         {
            return;
         }
         if (knownTopic.Topic.StartsWith(DataTransferService.NamespaceName))
         {
            knownTopic.Topic = knownTopic.Topic.Substring(DataTransferService.NamespaceName.Length + 1 + knownTopic.ProjectName.Length + 1);
         }

      }
      catch (Exception exception)
      {
         Logger.LogError(exception, $"{MethodBase.GetCurrentMethod()} failed!");
      }
      await KnownTopicsDatabaseService.Save(knownTopic!);
   }

   /// <summary>
   /// Registers the microcontroller an it's KnownTopics.
   /// </summary>
   /// <param name="payload"></param>
   private async Task RegisterMicrocontroller(string payload)
   {
      KnownMicrocontroller? microcontrollerToRegister = null;
      try
      {
         microcontrollerToRegister = JsonSerializer.Deserialize<KnownMicrocontroller>(payload);
      }
      catch (Exception exception)
      {
         Logger.LogError(exception, $"{MethodBase.GetCurrentMethod()} failed!");
      }

      if (microcontrollerToRegister != null)
      {
         microcontrollerToRegister.BootDateTime = DateTime.Now;
         await MicrocontrollerService.Save(microcontrollerToRegister, pushToMicrocontroller: false);

         if (microcontrollerToRegister.KnownTopics != null)
         {
            int? parentKnownTopicId = null;
            var parentKnownTopic = await KnownTopicsDatabaseService.GetKnownTopicByTopicName(microcontrollerToRegister.ProjectName, TopicConstants.INIT);
            if (parentKnownTopic != null)
            {
               parentKnownTopicId = parentKnownTopic.KnownTopicId;
            }

            foreach (KnownTopic knownTopicToRegister in microcontrollerToRegister.KnownTopics)
            {
               knownTopicToRegister.NamespaceName = microcontrollerToRegister.NamespaceName;
               knownTopicToRegister.ProjectName = microcontrollerToRegister.ProjectName;
               knownTopicToRegister.AllowEdit = false;
               knownTopicToRegister.AllowDelete = false;
               knownTopicToRegister.ParentKnownTopicId = parentKnownTopicId;
               knownTopicToRegister.Sender = microcontrollerToRegister.MacAddress;

               if (knownTopicToRegister.Topic.StartsWith(DataTransferService.NamespaceName))
               {
                  knownTopicToRegister.Topic = knownTopicToRegister.Topic.Substring(DataTransferService.NamespaceName.Length + 1 + knownTopicToRegister.ProjectName.Length + 1);
               }

               await KnownTopicsDatabaseService.Save(knownTopicToRegister, allowUpdate: false);
               if (knownTopicToRegister.Topic.EndsWith(TopicConstants.REGISTER_MICROCONTROLLER))
               {
                  parentKnownTopicId = knownTopicToRegister.KnownTopicId;
               }
            }
         }
      }
   }

   /// <summary>
   /// The delay time has expired, the message is now being sent.
   /// </summary>
   /// <param name="timer"></param>
   /// <param name="elapsedEventArgs"></param>
   private async void TimerService_OnElapsed(System.Timers.Timer timer, TimerServiceEventArgs elapsedEventArgs)
   {
      try
      {
         timer.Dispose();
         if (null == elapsedEventArgs.ApplicationMessage)
         {
            return;
         }

         string payload = elapsedEventArgs.ApplicationMessage.ConvertPayloadToString();

         if (string.IsNullOrEmpty(payload))
         {
            return;
         }

         await Client.PublishAsync(elapsedEventArgs.ApplicationMessage);

         if (delayedMessages.Any())
         {
            var existingTimerService = (from data in delayedMessages
                                        where data.ApplicationMessage.Topic == elapsedEventArgs.ApplicationMessage.Topic
                                              && data.ApplicationMessage.ConvertPayloadToString() == payload
                                        select data).FirstOrDefault();

            if (null != existingTimerService)
            {
               bool removed = delayedMessages.Remove(existingTimerService);
            }
         }
      }
      catch (Exception exception)
      {
         Logger.LogError(exception, $"{MethodBase.GetCurrentMethod()} failed!");
      }
   }

   public void ClearData()
   {
      DataTransferService.ReceivedTopicsQueue.Clear();
   }

   public void Dispose()
   {
      Client?.Dispose();
   }

   public async Task<bool> PublishTopic(string topic,
                                        string? payload,
                                        MqttQualityOfServiceLevel mqttQualityOfServiceLevel = MqttQualityOfServiceLevel.AtMostOnce,
                                        bool retain = false)
   {
      try
      {
         MqttApplicationMessage applicationMessage = CreateApplicationMessage(topic, payload, mqttQualityOfServiceLevel, retain);
         MqttClientPublishResult publishResult = await Client.PublishAsync(applicationMessage);
         return publishResult.IsSuccess;
      }
      catch (Exception exception)
      {
         Logger.LogError(exception, $"{MethodBase.GetCurrentMethod()} failed!");
         return false;
      }
   }

   public Task<bool> RemoveRetainedMessageFromBroker(string topic)
   {
      return PublishTopic(topic, null, MqttQualityOfServiceLevel.AtMostOnce, true);
   }

   public async Task Connect()
   {
      await Connect(DataTransferService.MqttBrokerSettings.Ip,
                    DataTransferService.MqttBrokerSettings.Port,
                    $"{DataTransferService.NamespaceName}/#");
   }
}