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
using Domain.Services.MQTT;
using HueApi;
using HueApi.ColorConverters.Original.Extensions;
using HueApi.Models;
using HueApi.Models.Requests;
using HueApi.Models.Responses;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Protocol;
using System.Reflection;
using System.Text.Json;

namespace DataAccess.Services;

public class HueBridgeService : MqttPublisher, IHueBridgeService, IDisposable
{
    public event Action<EventStreamData>? OnLightChanged;


    protected LightColor LightColorPoc
    {
        get;
        set;
    }

    protected IKnownTopicsCrudService KnownTopicsDatabaseService
    {
        get; set;
    }

    protected LocalHueApi HueApi { get; set; } = null!;

    public HueBridgeService(ILogger<HueBridgeService> logger,
                            IOptions<AppSettings> options,
                            IDataTransferService dataTransferService,
                            IKnownTopicsCrudService knownTopicsCrudService) : base(logger, dataTransferService)
    {
        KnownTopicsDatabaseService = knownTopicsCrudService;

        LightColorPoc = new LightColor()
        {
            Red = 255,
            Blue = 255,
            Green = 255
        };

        _ = ApplySettingsAsync();
    }


    public async Task ApplySettingsAsync()
    {
        try
        {
            if (!string.IsNullOrEmpty(DataTransferService.PhilipsHueBridgeSettings.Ip) &&
                !string.IsNullOrEmpty(DataTransferService.PhilipsHueBridgeSettings.Key))
            {
                HueApi = new LocalHueApi(DataTransferService.PhilipsHueBridgeSettings.Ip, DataTransferService.PhilipsHueBridgeSettings.Key);

                HueApi.OnEventStreamMessage -= HueApi_OnEventStreamMessage;
                HueApi.OnEventStreamMessage += HueApi_OnEventStreamMessage;
                await HueApi.StartEventStream();
            }
            await InitMqttClientAsync();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, $"{MethodBase.GetCurrentMethod()} failed!");
        }
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

    protected bool IsLightDevice(Light? light)
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
            await TurnOnLight(light);
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
            var updateLight = new UpdateLight().TurnOn().SetBrightness(100);
            await HueApi.Light.UpdateAsync(light.Id, updateLight);
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
            var updateLight = new UpdateLight().TurnOff();
            await HueApi.Light.UpdateAsync(light.Id, updateLight);
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, $"{MethodBase.GetCurrentMethod()} failed!");
        }
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
            await TurnOffLight(light);
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

    public async Task SetLightBrightness(int lightId,
                                         double brightnessPercent)
    {
        try
        {
            Logger.LogInformation($"Adjust brightness of lightId {lightId} to {brightnessPercent} %.");
            var light = await GetLight(lightId);
            if (!IsLightDevice(light))
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
            var updateLight = new UpdateLight().TurnOn().SetBrightness(newBrightness);
            await HueApi.Light.UpdateAsync(light.Id, updateLight);
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

    /// <summary>
    /// 10 % brighter
    /// </summary>
    /// <param name="lightId"></param>
    /// <returns></returns>
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
            await SetLightBrightness(lightId, newBrightness);
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, $"{MethodBase.GetCurrentMethod()} failed!");
        }
    }

    /// <summary>
    /// -10 % brightness.
    /// </summary>
    /// <param name="lightId"></param>
    /// <returns></returns>
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
            await SetLightBrightness(lightId, newBrightness);
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

            var updateLight = new UpdateLight().TurnOn().SetColor(rgbColor);
            var response = await HueApi.Light.UpdateAsync(light.Id, updateLight);
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
            if (!IsLightDevice(light))
            {
                return false;
            }
            var response = await HueApi.Light.UpdateAsync(light.Id, new UpdateLight().TurnOn().SetColor(x, y));
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
            return await HueApi.Light.GetAllAsync();
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

    public new void Dispose()
    {
        base.Dispose();
        HueApi.OnEventStreamMessage -= HueApi_OnEventStreamMessage;
    }
}