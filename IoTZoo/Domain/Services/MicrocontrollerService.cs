// --------------------------------------------------------------------------------------------------------------------
//      ____    ______   _____
//     /  _/___/_  __/  /__  / ____  ____
//     / // __ \/ /       / / / __ \/ __ \
//   _/ // /_/ / /       / /_/ /_/ / /_/ /
//  /___/\____/_/       /____|____/\____/ 
// --------------------------------------------------------------------------------------------------------------------
// (c) 2025 Holger Freudenreich under MIT license
// --------------------------------------------------------------------------------------------------------------------
// Provides an interface to configure things/devices on the microcontroller (mapping to the correct gpio pins, etc). 
// --------------------------------------------------------------------------------------------------------------------

using Dapper;
using DataAccess.Services;
using Domain.Interfaces;
using Domain.Interfaces.Crud;
using Domain.Pocos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Protocol;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace Domain.Services;

public class MicrocontrollerService : DataServiceBase,
                                      IMicrocontrollerService, IDisposable
{
   protected IMqttClient MqttClient
   {
      get;
      set;
   } = null!;

   private readonly HttpClient httpClient;

   public event Action<MqttClientConnectedEventArgs> OnMqttConnected = null!;
   public event Action<AliveMessage> OnReceivedAliveMessage = null!;
   public event Action<List<ConnectedDevice>> OnReceivedDeviceConfig = null!;

   public List<ConnectedDevice> ConnectedDevicesList
   {
      get;
      set;
   }

   public IDataTransferService DataTransferService { get; set; }
   public IProjectCrudService ProjectCrudService { get; }

   public MicrocontrollerService(IOptions<AppSettings> options,
                                 ILogger<DataServiceBase> logger,
                                 IDataTransferService dataTransferService,
                                 IProjectCrudService projectCrudService) : base(options, logger)
   {
      Initialize(typeof(KnownMicrocontroller), "cfg", "microcontroller");
      httpClient = new();
      httpClient.Timeout = TimeSpan.FromMilliseconds(1000);
      ConnectedDevicesList = new();
      DataTransferService = dataTransferService;
      ProjectCrudService = projectCrudService;
      _ = InitMqttClient();
   }

   public async Task Save(KnownMicrocontroller microcontroller, bool pushDeviceConfigToMicrocontroller)
   {
      if (pushDeviceConfigToMicrocontroller)
      {
         await PushDeviceConfigToMicrocontroller(microcontroller);
      }

      // First check if there is already a microcontroller with this MAC address.
      KnownMicrocontroller? existingMicrocontroller = null;
      if (!string.IsNullOrEmpty(microcontroller.MacAddress))
      {
         existingMicrocontroller = await GetMicrocontroller(microcontroller.MacAddress);
      }
      if (existingMicrocontroller != null)
      {
         microcontroller.MicroControllerId = existingMicrocontroller.MicroControllerId;
         await Update(microcontroller);
      }
      else
      {
         await Insert(microcontroller);
      }
   }

   public async Task Insert(KnownMicrocontroller microcontroller)
   {
      try
      {
         // The project muss exist!
         await EnsureProjectExists(microcontroller.ProjectName);

         int createdMicrocontrollerId = await Db.QuerySingleAsync<int>(InsertSql, microcontroller);
         microcontroller.MicroControllerId = createdMicrocontrollerId;
      }
      catch (Exception ex)
      {
         Logger.LogError(ex, $"{MethodBase.GetCurrentMethod()} failed!");
         throw;
      }
   }

   private async Task EnsureProjectExists(string? projectName)
   {
      if (!string.IsNullOrEmpty(projectName))
      {
         Project? project = await this.ProjectCrudService.LoadProjectByName(projectName);
         if (project == null)
         {
            // Microcontroller with unkown ProjectName detected!
            this.Logger.LogWarning($"Microcontroller with unkown ProjectName {projectName} detected! -> create");
            // We create this project with all default topics.
            await ProjectCrudService.Insert(new Project { ProjectName = projectName });
         }
      }
   }

   public async Task Update(KnownMicrocontroller microcontroller)
   {
      try
      {
         // The project must exist!
         await EnsureProjectExists(microcontroller.ProjectName);
         int rowsProcessed = await Db.ExecuteAsync(UpdateSql,
                                                   microcontroller);
      }
      catch (Exception exception)
      {
         Logger.LogError(exception, $"{MethodBase.GetCurrentMethod()} failed!");
      }
   }

   public async Task<bool> Delete(KnownMicrocontroller microcontroller)
   {
      try
      {
         int rowsProcessed = await Db.ExecuteAsync(DeleteSql,
                                                   microcontroller);
         // Delete Retained Messages like RegisterMicrocontroller and the assigned topics.

         string topic = $"{microcontroller.ProjectName}/esp32/{microcontroller.MacAddress}/{TopicConstants.REGISTER_MICROCONTROLLER}";
         var applicationMessage = new MqttApplicationMessageBuilder()
                                  .WithTopic(topic)
                                  .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                                  .WithRetainFlag(true)
                                  .Build();

         var result = await MqttClient.PublishAsync(applicationMessage);

         return 1 == rowsProcessed;
      }
      catch (Exception exception)
      {
         Logger.LogError(exception, $"{MethodBase.GetCurrentMethod()} failed!");
      }
      return false;
   }

   public async Task<List<KnownMicrocontroller>> GetMicrocontrollers()
   {
      IEnumerable<KnownMicrocontroller>? microcontrollers = null!;
      try
      {
         microcontrollers = await Db.QueryAsync<KnownMicrocontroller>($"select {FieldListSelect} from {FullQualifiedTableName} order by boot_datetime desc;");
      }
      catch (Exception exception)
      {
         Logger.LogError(exception, $"{MethodBase.GetCurrentMethod()} failed!");
      }
      return microcontrollers.AsList();
   }

   public async Task<List<KnownMicrocontroller>> GetMicrocontrollers(Project project)
   {
      IEnumerable<KnownMicrocontroller>? microcontrollers = null!;
      try
      {
         microcontrollers = await Db.QueryAsync<KnownMicrocontroller>(@$"select {FieldListSelect} from {FullQualifiedTableName} where 
                                                                         project_name = @ProjectName order by boot_datetime desc;",
            new { ProjectName = project.ProjectName });
      }
      catch (Exception exception)
      {
         Logger.LogError(exception, $"{MethodBase.GetCurrentMethod()} failed!");
      }
      return microcontrollers.AsList();
   }

   public async Task<KnownMicrocontroller?> GetMicrocontroller(string macAddress)
   {
      KnownMicrocontroller? microcontroller = null;
      try
      {
         microcontroller = await Db.QueryFirstOrDefaultAsync<KnownMicrocontroller>($"select {FieldListSelect} from {FullQualifiedTableName} where mac_address = @macAddress;",
                                                                             new { macAddress });
      }
      catch (Exception exception)
      {
         Logger.LogError(exception, $"{MethodBase.GetCurrentMethod()} failed!");
      }
      return microcontroller;
   }

   // -------------------------------------------------------------------------------------------------------------------
   // REST
   // -------------------------------------------------------------------------------------------------------------------
   #region REST
   /// <summary>
   /// Reads the configuration of the microcontroller. Drawback: This is only possible if the device is reachable over the network.
   /// So this method will not work if we use Wokwi as a simulator. A workaround could be to do this communication over mqtt.
   /// We will register to this topic "IOTZOO/DEVICE_CONFIG" and then send a mqtt message to the microcontroller to push the
   /// configuration data.
   /// </summary>
   /// <returns>List of the connected devices with the used gpio-pins.</returns>
   public async Task<List<ConnectedDevice>> GetDeviceConfig(KnownMicrocontroller microcontroller)
   {
      HttpResponseMessage? httpResponseMessage = null;
      List<ConnectedDevice> list = new();
      try
      {
         httpResponseMessage = await httpClient.GetAsync($"http://{microcontroller.IpAddress}/deviceConfig");

         string responseMessageContent = await httpResponseMessage.Content.ReadAsStringAsync();
         if (!string.IsNullOrEmpty(responseMessageContent))
         {
            list.AddRange(System.Text.Json.JsonSerializer.Deserialize<List<ConnectedDevice>>(responseMessageContent)!);
         }
      }
      catch (Exception ex)
      {
         Logger.LogError(ex, $"{MethodBase.GetCurrentMethod()} failed!");
         throw;
      }
      return list;
   }

   public async Task<bool> IsMicrocontrollerOnline(KnownMicrocontroller microcontroller)
   {
      HttpResponseMessage? httpResponseMessage = null;
      try
      {
         httpResponseMessage = await httpClient.GetAsync($"http://{microcontroller?.IpAddress}/alive");

         return httpResponseMessage.IsSuccessStatusCode;

         //string responseMessageContent = await httpResponseMessage.Content.ReadAsStringAsync();
      }
      catch (Exception ex)
      {
         Logger.LogError(ex, $"{MethodBase.GetCurrentMethod()} failed!");
         throw;
      }
   }

   public void AddConnectedDevice(KnownMicrocontroller microcontroller, ConnectedDevice device)
   {
      var max = (from data in ConnectedDevicesList where data.DeviceType == device.DeviceType select data.DeviceIndex).Max();
      if (null == max)
      {
         max = 0;
      }
      else
      {
         max++;
      }
      device.DeviceIndex = max;
      ConnectedDevicesList.Add(device);
      //await PushDeviceConfigToMicrocontroller(microcontroller);


      //if (await IsMicrocontrollerOnline())
      //{
      //   ConnectedDevicesList.Add(connectedDevice);
      //   await PostDeviceConfigToMicrocontroller();
      //}
      //else
      //{
      //   throw new Exception("Microcontroller is offline!");
      //}
   }

   /// <summary>
   /// Sends ConnectedDevicesList to the microcontroller via REST interface.
   /// </summary>
   /// <returns></returns>
   public async Task<HttpResponseMessage?> PostDeviceConfigToMicrocontroller(KnownMicrocontroller microcontroller)
   {
      HttpResponseMessage? httpResponseMessage = null;
      if (null == microcontroller)
      {
         return httpResponseMessage;
      }
      try
      {
         string json = System.Text.Json.JsonSerializer.Serialize(ConnectedDevicesList);
         StringContent content = new StringContent(json, Encoding.UTF8, "application/json");
         httpResponseMessage = await httpClient.PostAsync($"http://{microcontroller?.IpAddress}/deviceConfig",
                                                          content);
         string responseMessageContent = await httpResponseMessage.Content.ReadAsStringAsync();
      }
      catch (Exception ex)
      {
         Logger.LogError(ex, $"{MethodBase.GetCurrentMethod()} failed!");
         throw;
      }
      return httpResponseMessage;
   }

   public async Task<bool> PostMicrocontrollerConfigToMicrocontroller(KnownMicrocontroller microcontroller)
   {
      try
      {
         HttpResponseMessage? httpResponseMessage = null;
         if (string.IsNullOrEmpty(microcontroller.IpMqttBroker))
         {
            microcontroller.IpMqttBroker = DataTransferService.MqttBrokerSettings.Ip;
         }
         microcontroller.IpMqttBroker = DataTransferService.MqttBrokerSettings.Ip;
         string json = System.Text.Json.JsonSerializer.Serialize(microcontroller);
         Logger.LogInformation("PostMicrocontrollerConfigToMicrocontroller json: " + json);
         StringContent content = new StringContent(json, Encoding.UTF8, "application/json");
         httpResponseMessage = await httpClient.PostAsync($"http://{microcontroller.IpAddress}/microcontrollerConfig",
                                                            content);

         string responseMessageContent = await httpResponseMessage.Content.ReadAsStringAsync();
         Logger.LogInformation(responseMessageContent);
         return httpResponseMessage.IsSuccessStatusCode;
      }
      catch (Exception ex)
      {
         Logger.LogError(ex, $"{MethodBase.GetCurrentMethod()} failed!");
         throw;
      }
   }

   #endregion REST

   // -------------------------------------------------------------------------------------------------------------------
   // MQTT
   // -------------------------------------------------------------------------------------------------------------------
   #region MQTT
   private async Task InitMqttClient()
   {
      try
      {
         if (null != DataTransferService.MqttBrokerSettings)
         {
            var factory = new MqttClientFactory();
            MqttClient = factory.CreateMqttClient();

            var mqttClientOptions = new MqttClientOptionsBuilder().WithTcpServer(DataTransferService.MqttBrokerSettings.Ip,
                                                                                 DataTransferService.MqttBrokerSettings.Port).Build();
            MqttClient.ConnectedAsync -= MqttClient_ConnectedAsync;
            MqttClient.ConnectedAsync += MqttClient_ConnectedAsync;
            MqttClient.ApplicationMessageReceivedAsync += MqttClient_ApplicationMessageReceivedAsync;

            MqttClientConnectResult mqttClientConnectionResult = await MqttClient.ConnectAsync(mqttClientOptions);
         }
      }
      catch (Exception ex)
      {
         Logger.LogError(ex, $"{MethodBase.GetCurrentMethod()} failed!");
      }
   }

   private async Task MqttClient_ApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs mqttApplicationMessageReceivedEventArgs)
   {
      if (null == mqttApplicationMessageReceivedEventArgs)
      {
         return;
      }
      if (null == mqttApplicationMessageReceivedEventArgs.ApplicationMessage)
      {
         return;
      }


      TopicEntry topicEntry = new TopicEntry();

      topicEntry.Topic = mqttApplicationMessageReceivedEventArgs.ApplicationMessage.Topic;
      if (topicEntry.Topic == null)
      {
         return;
      }
      if (topicEntry.Topic.EndsWith(TopicConstants.ALIVE, StringComparison.OrdinalIgnoreCase))
      {
         string payload = mqttApplicationMessageReceivedEventArgs.ApplicationMessage.ConvertPayloadToString();

         AliveMessage? aliveMessage = null;
         try
         {
            aliveMessage = JsonSerializer.Deserialize<AliveMessage>(payload);
         }
         catch (Exception ex)
         {
            Logger.LogError(ex, ex.GetBaseException().Message);
         }
         if (aliveMessage != null)
         {
            OnReceivedAliveMessage?.Invoke(aliveMessage);

            await AcknowledgeAliveMessageFromMicrocontroller(aliveMessage.Microcontroller);
         }
      }
      else if (topicEntry.Topic.EndsWith(TopicConstants.DEVICE_CONFIG))
      {
         string payload = GetPayload(mqttApplicationMessageReceivedEventArgs);
         if (null != payload)
         {
            List<ConnectedDevice>? connectedDevices = null;
            try
            {
               connectedDevices = JsonSerializer.Deserialize<List<ConnectedDevice>>(payload);
            }
            catch (Exception ex)
            {
               Logger.LogError(ex, ex.GetBaseException().Message);
            }

            if (connectedDevices != null)
            {
               this.ConnectedDevicesList.Clear();
               this.ConnectedDevicesList.AddRange(connectedDevices);
               OnReceivedDeviceConfig?.Invoke(connectedDevices);
            }
         }
      }
   }

   private string GetPayload(MqttApplicationMessageReceivedEventArgs mqttApplicationMessageReceivedEventArgs)
   {
      string? payload = mqttApplicationMessageReceivedEventArgs.ApplicationMessage.ConvertPayloadToString();
      if (null == payload)
      {
         return string.Empty;
      }
      return payload.Trim();
   }

   /// <summary>
   /// The MQTT Client has established a connected to the broker.
   /// </summary>
   /// <param name="arg"></param>
   /// <returns></returns>
   private Task MqttClient_ConnectedAsync(MqttClientConnectedEventArgs args)
   {
      OnMqttConnected?.Invoke(args);
      return Task.CompletedTask;
   }

   public string GetBaseTopic(KnownMicrocontroller microcontroller)
   {
      return $"{DataTransferService.NamespaceName}/{microcontroller.ProjectName}/{microcontroller.BoardType}/{microcontroller.MacAddress}";
   }

   /// <summary>
   /// Publishes the connected devices.
   /// </summary>
   /// <param name="microcontroller"></param>
   /// <returns></returns>
   public async Task<bool> PushDeviceConfigToMicrocontroller(KnownMicrocontroller microcontroller)
   {
      if (!ConnectedDevicesList.Any())
      {
         return false;
      }
      string json = System.Text.Json.JsonSerializer.Serialize(ConnectedDevicesList);

      string topic = $"{GetBaseTopic(microcontroller)}/{TopicConstants.SAVE_DEVICE_CONFIG}";
      var applicationMessage = new MqttApplicationMessageBuilder()
                               .WithTopic(topic)
                               .WithPayload(json)
                               .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                               .WithRetainFlag(false)
                               .Build();

      MqttClientPublishResult result = await MqttClient.PublishAsync(applicationMessage);
      return result.IsSuccess;
   }

   /// <summary>
   /// Publishes the connected devices.
   /// </summary>
   /// <param name="microcontroller"></param>
   /// <returns></returns>
   public async Task<bool> AcknowledgeAliveMessageFromMicrocontroller(KnownMicrocontroller microcontroller)
   {
      string topic = $"{GetBaseTopic(microcontroller)}/{TopicConstants.ALIVE_ACK}";
      string json = JsonSerializer.Serialize(microcontroller);
      var applicationMessage = new MqttApplicationMessageBuilder()
                               .WithTopic(topic)
                               .WithPayload(json)
                               .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                               .WithRetainFlag(false)
                               .Build();

      MqttClientPublishResult result = await MqttClient.PublishAsync(applicationMessage);
      return result.IsSuccess;
   }

   /// <summary>
   /// Publishes the microcontroller base configuration. Says the microcontroller in which project it is used and what's it's MQTTBroker.
   /// </summary>
   /// <param name="microcontroller"></param>
   /// <returns></returns>
   public async Task<bool> PushMicrocontrollerConfigToMicrocontroller(KnownMicrocontroller microcontroller)
   {
      string json = System.Text.Json.JsonSerializer.Serialize(microcontroller);

      string topic = $"{microcontroller.MacAddress}/{TopicConstants.SAVE_MICROCONTROLLER_CONFIG}";
      var applicationMessage = new MqttApplicationMessageBuilder()
                               .WithTopic(topic)
                               .WithPayload(json)
                               .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                               .WithRetainFlag(true)
                               .Build();

      MqttClientPublishResult result = await MqttClient.PublishAsync(applicationMessage);
      // The ESP removes the retained message from the broker after saving this important information.
      return result.IsSuccess;
   }

   public void Dispose()
   {
      MqttClient?.Dispose();
   }

   public async Task RequestAliveMessageAsync(KnownMicrocontroller microcontroller)
   {
      // Subscribe to alive message of the microcontroller. This is the answer on topic /status with payload alive.
      await MqttClient.SubscribeAsync($"{GetBaseTopic(microcontroller)}/alive");

      string topic = $"{GetBaseTopic(microcontroller)}/status";
      var applicationMessage = new MqttApplicationMessageBuilder()
                               .WithTopic(topic)
                               .WithPayload("alive")
                               .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                               .Build();

      // Request the microcontroller to send an alive message.
      await MqttClient.PublishAsync(applicationMessage);
   }

   /// <summary>
   /// Sends topic status with payload device_config. The microcontroller then sends topic device_config with json payload of the connected devices.
   /// </summary>
   /// <param name="microcontroller"></param>
   /// <returns></returns>
   public async Task RequestDeviceConfiguration(KnownMicrocontroller microcontroller)
   {
      // Subscribe to alive message of the microcontroller. This is the answer on send_alive.
      await MqttClient.SubscribeAsync($"{GetBaseTopic(microcontroller)}/device_config");

      string topic = $"{GetBaseTopic(microcontroller)}/status";
      var applicationMessage = new MqttApplicationMessageBuilder()
                               .WithTopic(topic)
                               .WithPayload("device_config")
                               .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                               .Build();

      // Request the microcontroller to send an alive message.
      await MqttClient.PublishAsync(applicationMessage);
   }

   public async Task Reboot(KnownMicrocontroller microcontroller)
   {
      // Send to microcontroller
      string payload = "reboot";
      string topic = $"{GetBaseTopic(microcontroller)}/system";
      var applicationMessage = new MqttApplicationMessageBuilder()
                               .WithTopic(topic)
                               .WithPayload(payload)
                               .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                               .Build();

      await MqttClient.PublishAsync(applicationMessage);
   }
   #endregion MQTT
}