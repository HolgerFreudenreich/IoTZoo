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

using DataAccess.Interfaces;
using Domain.Pocos;
using HueApi.Models.Responses;
using Microsoft.AspNetCore.Components;
using MudBlazor.Utilities;
using System.Reflection;

namespace IotZoo.Pages;

public class HueComponentsPageBase : PageBase, IDisposable
{
   [Inject]
   public IHueBridgeService HueBridgeService
   {
      get;
      set;
   } = null!;

   public List<HueComponent> HueComponents
   {
      get;
      set;
   } = new();

   private readonly Dictionary<int, MudColor> colors = new();
   private readonly Dictionary<int, CancellationTokenSource> colorUpdates = new();
   private readonly HashSet<int> pendingColorLights = new();
   private readonly Dictionary<int, DateTime> lastLocalColorChange = new();

   public MudColor GetColor(HueComponent component)
   {
      if (colors.TryGetValue(component.IdNumeric, out var color))
      {
         return color;
      }
      var xy = component.Light.Color?.Xy;
      return null == xy ? new MudColor("#FFFFFF") : XyToMudColor(xy.X, xy.Y);
   }

   private static MudColor XyToMudColor(double x, double y)
   {
      if (y <= 0)
      {
         return new MudColor("#FFFFFF");
      }
      double z = 1.0 - x - y;
      double bigX = x / y;
      double bigZ = z / y;

      double r = bigX * 1.656492 - 0.354851 - bigZ * 0.255038;
      double g = -bigX * 0.707196 + 1.655397 + bigZ * 0.036152;
      double b = bigX * 0.051713 - 0.121364 + bigZ * 1.011530;

      double max = Math.Max(r, Math.Max(g, b));
      if (max > 0)
      {
         r /= max;
         g /= max;
         b /= max;
      }

      static int ToByte(double v)
      {
         v = Math.Clamp(v, 0.0, 1.0);
         v = v <= 0.0031308 ? 12.92 * v : 1.055 * Math.Pow(v, 1.0 / 2.4) - 0.055;
         return (int)Math.Round(Math.Clamp(v, 0.0, 1.0) * 255);
      }

      return new MudColor(ToByte(r), ToByte(g), ToByte(b), 255);
   }

   public async Task OnColorChanged(HueComponent component, MudColor color)
   {
      int lightId = component.IdNumeric;
      Logger.LogInformation($"HueColor local change light {lightId}: R={color.R} G={color.G} B={color.B} at {DateTime.UtcNow:HH:mm:ss.fff}");
      colors[lightId] = color;
      lastLocalColorChange[lightId] = DateTime.UtcNow;

      if (colorUpdates.TryGetValue(lightId, out var previous))
      {
         previous.Cancel();
         previous.Dispose();
      }
      var colorUpdate = new CancellationTokenSource();
      colorUpdates[lightId] = colorUpdate;
      pendingColorLights.Add(lightId);

      try
      {
         await Task.Delay(300, colorUpdate.Token);
         await HueBridgeService.SetColor(lightId, color.R, color.G, color.B);
      }
      catch (OperationCanceledException)
      {
         // a newer color change for this light superseded this one
      }
      catch (Exception ex)
      {
         Logger.LogError(ex, $"{MethodBase.GetCurrentMethod()} failed!");
      }
      finally
      {
         if (!colorUpdate.IsCancellationRequested)
         {
            pendingColorLights.Remove(lightId);
         }
      }
   }

   private bool IsRecentLocalColorChange(int lightId)
   {
      return lastLocalColorChange.TryGetValue(lightId, out var changed) && DateTime.UtcNow - changed < TimeSpan.FromSeconds(3);
   }

   protected override Task OnAfterRenderAsync(bool firstRender)
   {
      if (firstRender)
      {
         DataTransferService.CurrentScreen = ScreenMode.HueLights;
         HueBridgeService.OnLightChanged -= HueBridgeService_OnLightChanged;
         HueBridgeService.OnLightChanged += HueBridgeService_OnLightChanged;
      }
      return base.OnAfterRenderAsync(firstRender);
   }

   private async void HueBridgeService_OnLightChanged(EventStreamData eventStreamData)
   {
      var existingLight = (from data in HueComponents where data.Light.Id == eventStreamData.Id select data).FirstOrDefault();
      if (existingLight != null)
      {
         if (null != eventStreamData.ExtensionData)
         {
            foreach (var extensionData in eventStreamData.ExtensionData)
            {
               Snackbar.Add($"{eventStreamData.IdV1} Key: {extensionData.Key}, Value: {extensionData.Value}");

               if (extensionData.Key == "on")
               {
                  var property = extensionData.Value.GetProperty("on");
                  if (property.GetBoolean() == true)
                  {
                     existingLight.IsLightOn = true;
                  }
                  else
                  {
                     existingLight.IsLightOn = false;
                  }
               }
               else if (extensionData.Key == "color")
               {
                  var xy = extensionData.Value.GetProperty("xy");
                  var ignored = pendingColorLights.Contains(existingLight.IdNumeric) || IsRecentLocalColorChange(existingLight.IdNumeric);
                  Logger.LogInformation($"HueColor bridge event light {existingLight.IdNumeric}: x={xy.GetProperty("x").GetDouble()} y={xy.GetProperty("y").GetDouble()} ignored={ignored} at {DateTime.UtcNow:HH:mm:ss.fff}");
                  if (!ignored)
                  {
                     colors[existingLight.IdNumeric] = XyToMudColor(xy.GetProperty("x").GetDouble(), xy.GetProperty("y").GetDouble());
                  }
               }
                               else if (extensionData.Key == "dimming")
               {
                  var property = extensionData.Value.GetProperty("brightness");
                  var brightness = property.GetDouble();
                  existingLight.Brightness = brightness;
               }
            }
         }
         await InvokeAsync(StateHasChanged);
      }
   }

   protected override async Task LoadData()
   {
      try
      {
         var lightsTmp = await HueBridgeService.GetLights();
         if (null != lightsTmp)
         {
            foreach (var light in lightsTmp.Data)
            {
               var hueComponent = new HueComponent(HueBridgeService)
               {
                  Light = light
               };
               if (null != light.Dimming)
               {
                  hueComponent.Brightness = light.Dimming.Brightness;
               }
               else
               {
                  // not a light, maybe a plug.
                  //continue;
               }

               var existingLight = (from data in HueComponents where data.Light.Id == light.Id select data).FirstOrDefault();
               if (existingLight != null)
               {
                  existingLight.Light = hueComponent.Light;
                                     colors.Remove(existingLight.IdNumeric);
                  if (null != light.Dimming)
                  {
                     existingLight.Brightness = light.Dimming.Brightness;
                     existingLight.IsLightOn = light.On.IsOn;
                  }
                  else
                  {
                     // not light, maybe a plug.
                  }
               }
               else
               {
                  HueComponents.Add(hueComponent);
               }
            }
         }     
      }
      catch (Exception ex)
      {
         Logger.LogError(ex, $"{MethodBase.GetCurrentMethod()} failed!");
      }
      finally
      {
         await InvokeAsync(StateHasChanged);
      }
   }

   public void Dispose()
   {
      HueBridgeService.OnLightChanged -= HueBridgeService_OnLightChanged;
   }
}
