// --------------------------------------------------------------------------------------------------------------------
//      ____    ______   _____
//     /  _/___/_  __/  /__  / ____  ____
//     / // __ \/ /       / / / __ \/ __ \
//   _/ // /_/ / /       / /_/ /_/ / /_/ /
//  /___/\____/_/       /____|____/\____/   P L A Y G R O U N D
// --------------------------------------------------------------------------------------------------------------------
// Connect «Things» with microcontrollers in a simple way.
// --------------------------------------------------------------------------------------------------------------------
// (c) 2025 Holger Freudenreich under the MIT license
// --------------------------------------------------------------------------------------------------------------------

using DataAccess.Interfaces;
using Domain.Pocos;
using HueApi.Models.Responses;
using Microsoft.AspNetCore.Components;
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
