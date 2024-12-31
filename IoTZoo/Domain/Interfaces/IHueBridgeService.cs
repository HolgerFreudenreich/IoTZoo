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
using HueApi.Models;
using HueApi.Models.Responses;

namespace DataAccess.Interfaces;

public interface IHueBridgeService
{
   public delegate void LightChanged(EventStreamData eventStreamData);
   public event LightChanged OnLightChanged;

   public void ApplySettings();

   /// <summary>
   /// Turns on a light or a plug.
   /// </summary>
   /// <param name="lightId"></param>
   Task TurnOnLight(int lightId);

   Task TurnOnLight(Light light);

   Task TurnOn(Guid lightGuid);

   /// <summary>
   /// Turns off a light or a plug.
   /// </summary>
   /// <param name="lightId"></param>
   Task TurnOffLight(int lightId);

   Task TurnOffLight(Light light);

   Task TurnOff(Guid lightGuid);

   /// <summary>
   /// TurnOn or TurnOf depending on the current state.
   /// </summary>
   /// <param name="lightId"></param>
   Task ToggleState(int deviceId);

   Task MakeLightBrighter(int lightId);

   Task MakeLightDarker(int lightId);

   Task SetLightBrightness(int lightId, double brightness);

   Task SetLightRedComponent(int lightId, int redPortion);

   Task SetLightGreenComponent(int lightId, int redPortion);

   Task SetLightBlueComponent(int lightId, int redPortion);

   Task<bool> SetColor(int lightId, int r, int g, int b);

   Task<bool> SetColor(int lightId, double x, double y);

   Task<HueResponse<Light>> GetLights();

   Task<string?> RegisterAppAtHueBridgeAsync(string hueBridgeIp,
                                             string appName,
                                             string deviceName);
}
