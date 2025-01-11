// --------------------------------------------------------------------------------------------------------------------
//      ____    ______   _____
//     /  _/___/_  __/  /__  / ____  ____
//     / // __ \/ /       / / / __ \/ __ \
//   _/ // /_/ / /       / /_/ /_/ / /_/ /
//  /___/\____/_/       /____|____/\____/
// --------------------------------------------------------------------------------------------------------------------
// (c) 2025 Holger Freudenreich under MIT license
// --------------------------------------------------------------------------------------------------------------------
// Communicate with the Philips HUE bridge to for example turn on a HUE-light.
// --------------------------------------------------------------------------------------------------------------------
using DataAccess.Interfaces;
using Domain.Interfaces;
using Domain.Interfaces.Crud;
using Domain.Pocos;
using HueApi;
using HueApi.ColorConverters.Original.Extensions;
using HueApi.Models;
using HueApi.Models.Requests;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Protocol;
using System.Reflection;
using System.Text.Json;

namespace DataAccess.Services;

public class HueBridgeService : IHueBridgeService
{
   public event IHueBridgeService.LightChanged OnLightChanged = null!;

   private ILogger<HueBridgeService> Logger { get; set; }

   /// <summary>
   /// To publish the HUE Events via MQTT.
   /// </summary>
   protected IMqttClient MqttClient
   {
      get;
      set;
   } = null!;

   protected LightColor LightColorPoc
   {
      get;
      set;
   }

   protected IKnownTopicsCrudService KnownTopicsDatabaseService
   {
      get; set;
   }

   IDataTransferService DataTransferService { get; } = null!;

   protected LocalHueApi HueApi { get; set; } = null!;

   public HueBridgeService(ILogger<HueBridgeService> logger,
                           IOptions<AppSettings> options,
                           IDataTransferService dataTransferService,
                           IKnownTopicsCrudService knownTopicsCrudService)
   {
      Logger = logger;
      DataTransferService = dataTransferService;
      KnownTopicsDatabaseService = knownTopicsCrudService;

      LightColorPoc = new LightColor()
      {
         Red = 255,
         Blue = 255,
         Green = 255
      };

      ApplySettings();
      _ = RegisterKnownTopicsAsync();
   }

   private async Task RegisterKnownTopicsAsync()
   {
      //var response = await GetLights();
      //foreach (var light in response.Data)
      //{
      //   // Inbound messages from the Philips Hue Bridge
      //   // fixme Project is unknown. await KnownTopicsDatabaseService.Save(new KnownTopic { Topic = $"{this.DataTransferService.MqttNamespace}/{TopicConstants.HUE_BRIDGE}/0{light.IdV1}", MessageDirection = MessageDirection.Inbound });
      //}
   }

   private async Task MqttClient_DisconnectedAsync(MqttClientDisconnectedEventArgs arg)
   {
      Logger.LogWarning("MQTT disconnected! Try to reconnect...");
      await Task.Delay(2000);
      await MqttClient.ReconnectAsync();
   }

   private void InitMqttClient()
   {
      var factory = new MqttClientFactory();
      MqttClient = factory.CreateMqttClient();

      var mqttClientOptions = new MqttClientOptionsBuilder().WithTcpServer(DataTransferService.MqttBrokerSettings.Ip,
                                                                           DataTransferService.MqttBrokerSettings.Port).Build();
      MqttClient.DisconnectedAsync -= MqttClient_DisconnectedAsync;
      MqttClient.DisconnectedAsync += MqttClient_DisconnectedAsync;
      MqttClientConnectResult connectionResult = MqttClient.ConnectAsync(mqttClientOptions).Result;
      if (connectionResult.ResultCode == MqttClientConnectResultCode.Success)
      {
         Logger.LogInformation("MQTT connected!");
      }
   }

   public void ApplySettings()
   {
      if (!string.IsNullOrEmpty(DataTransferService.PhilipsHueBridgeSettings.Ip) &&
          !string.IsNullOrEmpty(DataTransferService.PhilipsHueBridgeSettings.Key))
      {
         HueApi = new LocalHueApi(DataTransferService.PhilipsHueBridgeSettings.Ip, DataTransferService.PhilipsHueBridgeSettings.Key);

         HueApi.OnEventStreamMessage -= HueApi_OnEventStreamMessage;
         HueApi.OnEventStreamMessage += HueApi_OnEventStreamMessage;
         HueApi.StartEventStream();
      }
      InitMqttClient();
   }

   /// <summary>
   /// Event from Philips-Hue-Bridge. Used to refresh the UI if an Hue Event is triggert from outsite of IotZoo like Amazon Alexa.
   /// </summary>
   /// <param name="bridgeIp"></param>
   /// <param name="events"></param>
   private async void HueApi_OnEventStreamMessage(string bridgeIp,
                                                  List<HueApi.Models.Responses.EventStreamResponse> events)
   {
      try
      {
         foreach (HueApi.Models.Responses.EventStreamResponse hueEvent in events)
         {
            foreach (var data in hueEvent.Data)
            {
               string json = string.Empty;

               try
               {
                  json = JsonSerializer.Serialize(hueEvent);
               }
               catch (Exception)
               {
                  continue;
               }
               if (!string.IsNullOrEmpty(json))
               {
                  //Console.WriteLine($"Data: {data.Metadata?.Name} / {data.IdV1}");
                  var applicationMessage = new MqttApplicationMessageBuilder()
                                      .WithTopic($"{this.DataTransferService.NamespaceName}/{TopicConstants.HUE_BRIDGE}/0{data.IdV1}")
                                      .WithPayload(json)
                                      .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtMostOnce)
                                      .Build();

                  await MqttClient.PublishAsync(applicationMessage);
                  if (OnLightChanged != null)
                  {
                     OnLightChanged.Invoke(data);
                  }
               }
            }
         }
      }
      catch (Exception exception)
      {
         Logger.LogError(exception, $"{MethodBase.GetCurrentMethod()} failed!");
      }
   }

   protected bool IsLight(Light? light)
   {
      if (null == light)
      {
         return false;
      }
      if (null == light.Dimming)
      {
         return false;
      }
      return true;
   }

   public async Task TurnOnLight(int lightId)
   {
      try
      {
         Logger.LogInformation($"Turn on light with lightId {lightId}.");
         var light = await GetLight(lightId);

         if (null == light)
         {
            return;
         }
         await HueApi.UpdateLightAsync(light.Id,
                                       new UpdateLight().TurnOn());
      }
      catch (Exception exception)
      {
         Logger.LogError(exception, $"{MethodBase.GetCurrentMethod()} failed!");
      }
   }

   public async Task TurnOnLight(Light light)
   {
      try
      {
         await HueApi.UpdateLightAsync(light.Id,
                                       new UpdateLight().TurnOn());
      }
      catch (Exception exception)
      {
         Logger.LogError(exception, $"{MethodBase.GetCurrentMethod()} failed!");
      }
   }

   public async Task TurnOn(Guid lightGuid)
   {
      await HueApi.UpdateLightAsync(lightGuid,
                                    new UpdateLight().TurnOn());
   }

   public async Task TurnOffLight(int lightId)
   {
      try
      {
         Logger.LogInformation($"Turn off light with lightId {lightId}.");
         Light? light = await GetLight(lightId);
         if (null == light)
         {
            return;
         }
         await HueApi.UpdateLightAsync(light!.Id,
                                       new UpdateLight().TurnOff());
      }
      catch (Exception exception)
      {
         Logger.LogError(exception, $"{MethodBase.GetCurrentMethod()} failed!");
      }
   }

   public async Task TurnOffLight(Light light)
   {
      try
      {
         await HueApi.UpdateLightAsync(light!.Id,
                                       new UpdateLight().TurnOff());
      }
      catch (Exception exception)
      {
         Logger.LogError(exception, $"{MethodBase.GetCurrentMethod()} failed!");
      }
   }

   public async Task ToggleState(int deviceId)
   {
      var light = await GetLight(deviceId);
      if (light == null)
      {
         return;
      }
      if (light.On.IsOn)
      {
         await TurnOffLight(light);
      }
      else
      {
         await TurnOnLight(light);
      }
   }

   public async Task TurnOff(Guid lightGuid)
   {
      await HueApi.UpdateLightAsync(lightGuid,
                                    new UpdateLight().TurnOff());
   }

   public async Task SetLightBrightness(int lightId,
                                        double brightnessPercent)
   {
      try
      {
         Logger.LogInformation($"Adjust brightness of lightId {lightId} to {brightnessPercent} %.");
         var light = await GetLight(lightId);
         if (!IsLight(light))
         {
            return;
         }

         double currentBrightness = light!.Dimming!.Brightness;
         double newBrightness = brightnessPercent;
         if (currentBrightness == newBrightness)
         {
            return;
         }
         if (newBrightness < 0.0)
         {
            newBrightness = 0.0;
         }
         else if (newBrightness > 100.0)
         {
            newBrightness = 100.0;
         }
         await HueApi.UpdateLightAsync(light.Id,
                                       new UpdateLight().TurnOn().SetBrightness(newBrightness));
      }
      catch (Exception exception)
      {
         Logger.LogError(exception, $"{MethodBase.GetCurrentMethod()} failed!");
      }
   }

   public async Task SetLightRedComponent(int lightId,
                                        int redPortion)
   {
      LightColorPoc.Red = redPortion;

      await SetColor(lightId, redPortion, LightColorPoc.Green, LightColorPoc.Blue);
   }

   public async Task SetLightGreenComponent(int lightId,
                                        int greenPortion)
   {
      LightColorPoc.Green = greenPortion;
      await SetColor(lightId, LightColorPoc.Red, greenPortion, LightColorPoc.Blue);
   }

   public async Task SetLightBlueComponent(int lightId,
                                          int bluePortion)
   {
      LightColorPoc.Blue = bluePortion;
      await SetColor(lightId, LightColorPoc.Red, LightColorPoc.Green, bluePortion);
   }

   public async Task MakeLightBrighter(int lightId)
   {
      try
      {
         Logger.LogInformation($"Make light with lightId {lightId} brighter.");
         var light = await GetLight(lightId);
         if (null == light)
         {
            return;
         }
         if (null == light.Dimming)
         {
            return;
         }

         double currentBrightness = light.Dimming.Brightness;
         double newBrightness = currentBrightness + 10.0;
         if (newBrightness > 100)
         {
            newBrightness = 100.0;
         }
         await HueApi.UpdateLightAsync(light.Id,
                                       new UpdateLight().SetBrightness(newBrightness));
      }
      catch (Exception exception)
      {
         Logger.LogError(exception, $"{MethodBase.GetCurrentMethod()} failed!");
      }
   }

   public async Task MakeLightDarker(int lightId)
   {
      try
      {
         Logger.LogInformation($"Make light with lightId {lightId} darker.");
         var light = await GetLight(lightId);
         if (null == light)
         {
            return;
         }
         if (null == light.Dimming)
         {
            return;
         }

         double currentBrightness = light.Dimming.Brightness;
         double newBrightness = currentBrightness - 10.0;
         if (newBrightness < 0)
         {
            newBrightness = 0.0;
         }
         await HueApi.UpdateLightAsync(light.Id,
                                       new UpdateLight().TurnOn().SetBrightness(newBrightness));
      }
      catch (Exception exception)
      {
         Logger.LogError(exception, $"{MethodBase.GetCurrentMethod()} failed!");
      }
   }

   public async Task<bool> SetColor(int lightId,
                              int r,
                              int g,
                              int b)
   {
      try
      {
         var light = await GetLight(lightId);
         if (null == light)
         {
            return false;
         }
         if (null == light.Dimming)
         {
            return false;
         }

         var rgbColor = new HueApi.ColorConverters.RGBColor(r, g, b);
         var response = await HueApi.UpdateLightAsync(light.Id,
                                       new UpdateLight().SetColor(rgbColor));
         return !response.HasErrors;
      }
      catch (Exception exception)
      {
         Logger.LogError(exception, $"{MethodBase.GetCurrentMethod()} failed!");
         throw;
      }
   }

   public async Task<bool> SetColor(int lightId,
                              double x,
                              double y)
   {
      try
      {
         var light = await GetLight(lightId);
         if (null == light)
         {
            return false;
         }
         if (!IsLight(light))
         {
            return false;
         }

         var response = await HueApi.UpdateLightAsync(light.Id,
                                       new UpdateLight().SetColor(x, y));
         return !response.HasErrors;
      }
      catch (Exception ex)
      {
         Logger.LogError(ex, $"{MethodBase.GetCurrentMethod()} failed!");
         throw;
      }
   }

   public async Task<HueResponse<Light>> GetLights()
   {
      try
      {
         if (null == this.HueApi)
         {
            throw new Exception("Hue bridge is not configured!");
         }
         return await HueApi.GetLightsAsync();
      }
      catch (Exception ex)
      {
         Logger.LogError(ex, $"{MethodBase.GetCurrentMethod()} failed!");
         throw;
      }
   }

   /// <summary>
   /// Only lights
   /// </summary>
   /// <param name="lightId"></param>
   /// <returns></returns>
   private async Task<Light?> GetLight(int lightId)
   {
      try
      {
         HueResponse<Light> lights = await GetLights();
         if (null == lights)
         {
            Logger.LogInformation($"No light von with lightId {lightId}.");
            return null;
         }

         IEnumerable<Light> light = from data in lights.Data where data.IdV1 == $"/lights/{lightId}" select data;
         return light.FirstOrDefault();
      }
      catch (Exception ex)
      {
         Logger.LogError(ex, $"{MethodBase.GetCurrentMethod()} failed!");
         throw;
      }
   }

   public async Task<string?> RegisterAppAtHueBridgeAsync(string hueBridgeIp, string appName, string deviceName)
   {
      // Make sure the user has pressed the button on the bridge before calling RegisterAsync
      // It will throw an LinkButtonNotPressedException if the user did not press the button
      HueApi.Models.Clip.RegisterEntertainmentResult? regResult = await LocalHueApi.RegisterAsync(hueBridgeIp,
                                                                                                  appName,
                                                                                                  deviceName,
                                                                                                  false,
                                                                                                  null);
      if (null == regResult)
      {
         return null;
      }

      return regResult.Username;
   }
}