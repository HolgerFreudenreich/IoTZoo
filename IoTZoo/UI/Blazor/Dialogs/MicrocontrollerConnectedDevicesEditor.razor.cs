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

using Domain.Interfaces;
using Domain.Pocos;
using Microsoft.AspNetCore.Components;
using MQTTnet;
using MudBlazor;
using System.Reflection;

namespace IotZoo.Dialogs;

public class MicrocontrollerConnectedDevicesEditorBase : EditorBase, IDisposable
{
   protected string BaseTopic { get; set; } = null!;

   [Inject]
   public IMicrocontrollerService MicrocontrollerService { get; set; } = null!;

   [Parameter]
   public KnownMicrocontroller Microcontroller { get; set; } = null!;

   protected override async Task OnInitializedAsync()
   {
      DialogTitle = "Connected Devices";
      BaseTopic = MicrocontrollerService.GetBaseTopic(Microcontroller);
      HashCode = GetHashCodeBase64(Microcontroller);
      MicrocontrollerService.OnMqttConnected -= MicrocontrollerService_ConnectedAsync;
      MicrocontrollerService.OnMqttConnected += MicrocontrollerService_ConnectedAsync;

      await base.OnInitializedAsync();
   }

   private void MicrocontrollerService_ConnectedAsync(MqttClientConnectedEventArgs arg)
   {
      MicrocontrollerService.OnReceivedDeviceConfig -= MicrocontrollerService_OnReceivedDeviceConfig;
      MicrocontrollerService.OnReceivedDeviceConfig += MicrocontrollerService_OnReceivedDeviceConfig;

      MicrocontrollerService.RequestDeviceConfiguration(Microcontroller);
   }

   private void MicrocontrollerService_OnReceivedDeviceConfig(List<ConnectedDevice> connectedDevices)
   {
      InvokeAsync(StateHasChanged);
   }

   protected async Task AddButton()
   {
      try
      {
         Snackbar.Add("Adding Button configuration to the microcontroller...", Severity.Info);
         MicrocontrollerService.AddConnectedDevice(this.Microcontroller, ConnectedDevices.FromButton());
         Snackbar.Add("Ensure that the button is correctly wired from GND to the pin!", Severity.Info);
         await InvokeAsync(StateHasChanged);
      }
      catch (Exception ex)
      {
         Logger.LogError(ex, $"{MethodBase.GetCurrentMethod()} failed!");
         Snackbar.Add(ex.GetBaseException().Message, Severity.Error);
      }
   }

   protected async Task AddBuzzer()
   {
      try
      {
         Snackbar.Add("Adding Buzzer configuration to the microcontroller...", Severity.Info);
         MicrocontrollerService.AddConnectedDevice(this.Microcontroller, ConnectedDevices.FromBuzzer());
         Snackbar.Add("Ensure that the buzzer is correctly wired! Optionally you can connect an LED.", Severity.Info);
         await InvokeAsync(StateHasChanged);
      }
      catch (Exception ex)
      {
         Logger.LogError(ex, $"{MethodBase.GetCurrentMethod()} failed!");
         Snackbar.Add(ex.GetBaseException().Message, Severity.Error);
      }
   }

   protected async Task AddSwitch()
   {
      try
      {
         Snackbar.Add("Adding Switch configuration to the microcontroller...", Severity.Info);
         MicrocontrollerService.AddConnectedDevice(this.Microcontroller, ConnectedDevices.FromSwitch());
         Snackbar.Add("Ensure that the switch is correctly wired from GND to the pin!", Severity.Info);
         await InvokeAsync(StateHasChanged);
      }
      catch (Exception ex)
      {
         Logger.LogError(ex, $"{MethodBase.GetCurrentMethod()} failed!");
         Snackbar.Add(ex.GetBaseException().Message, Severity.Error);
      }
   }

   protected async Task AddDS18B20TemperatureSensor()
   {
      try
      {
         Snackbar.Add("Adding DS18B20 temperature sensor configuration to the microcontroller...", Severity.Info);
         MicrocontrollerService.AddConnectedDevice(this.Microcontroller, ConnectedDevices.FromDS18B20());
         Snackbar.Add("Ensure that the device is correctly wired!", Severity.Info);
         await InvokeAsync(StateHasChanged);
      }
      catch (Exception ex)
      {
         Logger.LogError(ex, $"{MethodBase.GetCurrentMethod()} failed!");
         Snackbar.Add(ex.GetBaseException().Message, Severity.Error);
      }
   }

   protected async Task Add28BYJ48StepperMotor()
   {
      try
      {
         Snackbar.Add("Adding 28BYJ-48 Stepper Motor configuration to the microcontroller...", Severity.Info);
         MicrocontrollerService.AddConnectedDevice(this.Microcontroller, ConnectedDevices.From28BYJ48StepperMotor());
         Snackbar.Add("Ensure that the device is correctly wired!", Severity.Info);
         await InvokeAsync(StateHasChanged);
      }
      catch (Exception ex)
      {
         Logger.LogError(ex, $"{MethodBase.GetCurrentMethod()} failed!");
         Snackbar.Add(ex.GetBaseException().Message, Severity.Error);
      }
   }

   protected async Task AddBleHeartRateMonitor()
   {
      try
      {
         Snackbar.Add("Adding BLE Heart Rate Monitor configuration to the microcontroller...", Severity.Info);
         MicrocontrollerService.AddConnectedDevice(this.Microcontroller, ConnectedDevices.BleHeartRateMonitor());
         Snackbar.Add("Ensure that the device is correctly wired!", Severity.Info);
         await InvokeAsync(StateHasChanged);
      }
      catch (Exception ex)
      {
         Logger.LogError(ex, $"{MethodBase.GetCurrentMethod()} failed!");
         Snackbar.Add(ex.GetBaseException().Message, Severity.Error);
      }
   }

   protected async Task AddHCSR501MotionDetector()
   {
      try
      {
         Snackbar.Add("Adding HCSR501 Motion detector configuration to the microcontroller...", Severity.Info);
         MicrocontrollerService.AddConnectedDevice(this.Microcontroller, ConnectedDevices.FromHcSr501PirSensor());
         Snackbar.Add("Ensure that the device is correctly wired!", Severity.Info);
         await InvokeAsync(StateHasChanged);
      }
      catch (Exception ex)
      {
         Logger.LogError(ex, $"{MethodBase.GetCurrentMethod()} failed!");
         Snackbar.Add(ex.GetBaseException().Message, Severity.Error);
      }
   }

   protected async Task AddRd03DPresenceDetector()
   {
      try
      {
         Snackbar.Add("Adding RD-03D presence detector configuration to the microcontroller...", Severity.Info);
         MicrocontrollerService.AddConnectedDevice(this.Microcontroller, ConnectedDevices.FromRd03DPresenseSensor());
         Snackbar.Add("Ensure that the device is correctly wired!", Severity.Info);
         await InvokeAsync(StateHasChanged);
      }
      catch (Exception ex)
      {
         Logger.LogError(ex, $"{MethodBase.GetCurrentMethod()} failed!");
         Snackbar.Add(ex.GetBaseException().Message, Severity.Error);
      }
   }

   protected async Task Add4x4ButtonMatrix()
   {
      try
      {
         Snackbar.Add("Adding 4x4 Button Matrix configuration to the microcontroller...", Severity.Info);
         MicrocontrollerService.AddConnectedDevice(this.Microcontroller, ConnectedDevices.FromKeypad4x4());
         Snackbar.Add("Ensure that the device is correctly wired!", Severity.Info);
         await InvokeAsync(StateHasChanged);
      }
      catch (Exception ex)
      {
         Logger.LogError(ex, $"{MethodBase.GetCurrentMethod()} failed!");
         Snackbar.Add(ex.GetBaseException().Message, Severity.Error);
      }
   }

   protected async Task AddLedsTrafficLight()
   {
      try
      {
         Snackbar.Add("Adding Led Traffic Light Matrix configuration to the microcontroller...", Severity.Info);
         MicrocontrollerService.AddConnectedDevice(this.Microcontroller, ConnectedDevices.FromLedTrafficLights());
         Snackbar.Add("Ensure that the device is correctly wired!", Severity.Info);
         await InvokeAsync(StateHasChanged);
      }
      catch (Exception ex)
      {
         Logger.LogError(ex, $"{MethodBase.GetCurrentMethod()} failed!");
         Snackbar.Add(ex.GetBaseException().Message, Severity.Error);
      }
   }

   protected async Task AddRemoteGpio()
   {
      try
      {
         Snackbar.Add("Adding Remote GPIO configuration to the microcontroller...", Severity.Info);
         MicrocontrollerService.AddConnectedDevice(this.Microcontroller, ConnectedDevices.FromRemoteGpio());
         Snackbar.Add("Ensure that the device is correctly wired!", Severity.Info);
         await InvokeAsync(StateHasChanged);
      }
      catch (Exception ex)
      {
         Logger.LogError(ex, $"{MethodBase.GetCurrentMethod()} failed!");
         Snackbar.Add(ex.GetBaseException().Message, Severity.Error);
      }
   }

   protected async Task AddNeoPixel()
   {
      try
      {
         Snackbar.Add("Adding NEO-pixel configuration to the microcontroller...", Severity.Info);
         MicrocontrollerService.AddConnectedDevice(this.Microcontroller, ConnectedDevices.FromNeoPixel());
         Snackbar.Add("Ensure that the device is correctly wired!", Severity.Info);
         await InvokeAsync(StateHasChanged);
      }
      catch (Exception ex)
      {
         Logger.LogError(ex, $"{MethodBase.GetCurrentMethod()} failed!");
         Snackbar.Add(ex.GetBaseException().Message, Severity.Error);
      }
   }

   protected async Task AddOledDisplaySSD1306()
   {
      try
      {
         Snackbar.Add("SDA must be connected to pin 21 and SCL to pin 22. I²C Adddress: 0x3C", Severity.Info);
         Snackbar.Add("Ensure that the device is correctly wired! You can connect serveral devices on the same pins but the Device Address must be unique.", Severity.Info);
         MicrocontrollerService.AddConnectedDevice(this.Microcontroller, ConnectedDevices.FromOledSSD1306());
         await InvokeAsync(StateHasChanged);
      }
      catch (Exception ex)
      {
         Logger.LogError(ex, $"{MethodBase.GetCurrentMethod()} failed!");
      }
   }

   protected async Task AddLiquidCristalDisplay()
   {
      try
      {
         Snackbar.Add("SDA must be connected to pin 21 and SCL to pin 22. Device Address is 0x27.", Severity.Info);
         MicrocontrollerService.AddConnectedDevice(this.Microcontroller, ConnectedDevices.FromLcd160x());

         Snackbar.Add("Ensure that the device is correctly wired! You can connect serveral devices on the same pins but the Device Address must be unique.", Severity.Info);
         await InvokeAsync(StateHasChanged);
      }
      catch (Exception ex)
      {
         Logger.LogError(ex, $"{MethodBase.GetCurrentMethod()} failed!");
      }
   }

   protected async Task AddTM1637_4_DigitDisplay()
   {
      try
      {
         Snackbar.Add("Adding TM1637 4 digit display configuration to the microcontroller...", Severity.Info);
         MicrocontrollerService.AddConnectedDevice(this.Microcontroller, ConnectedDevices.FromTM1637_4());
         Snackbar.Add("Ensure that the device is correctly wired!", Severity.Info);
         await InvokeAsync(StateHasChanged);
      }
      catch (Exception ex)
      {
         Logger.LogError(ex, $"{MethodBase.GetCurrentMethod()} failed!");
         Snackbar.Add(ex.GetBaseException().Message, Severity.Error);
      }
   }

   protected async Task AddTM1637_6_DigitDisplay()
   {
      try
      {
         Snackbar.Add("Adding TM1637 6 digit display configuration to the microcontroller...", Severity.Info);
         var device = ConnectedDevices.FromTM1637_6();
         MicrocontrollerService.AddConnectedDevice(this.Microcontroller, device);
         Snackbar.Add("Ensure that the device is correctly wired!", Severity.Info);
         await InvokeAsync(StateHasChanged);
      }
      catch (Exception ex)
      {
         Logger.LogError(ex, $"{MethodBase.GetCurrentMethod()} failed!");
         Snackbar.Add(ex.GetBaseException().Message, Severity.Error);
      }
   }

   protected async Task AddTM1638Display()
   {
      try
      {
         Snackbar.Add("Adding TM1637 8 digit display", Severity.Info);
         var device = ConnectedDevices.FromTM1638();
         MicrocontrollerService.AddConnectedDevice(this.Microcontroller, device);

         Snackbar.Add("Ensure that the device is correctly wired!", Severity.Info);
         await InvokeAsync(StateHasChanged);
      }
      catch (Exception ex)
      {
         Logger.LogError(ex, $"{MethodBase.GetCurrentMethod()} failed!");
         Snackbar.Add(ex.GetBaseException().Message, Severity.Error);
      }
   }

   protected async Task AddHT1621Display()
   {
      try
      {
         Snackbar.Add("Adding HT1621 6 digit LED display", Severity.Info);
         var device = ConnectedDevices.FromHT1621();
         MicrocontrollerService.AddConnectedDevice(this.Microcontroller, device);

         Snackbar.Add("Ensure that the device is correctly wired!", Severity.Info);
         await InvokeAsync(StateHasChanged);
      }
      catch (Exception ex)
      {
         Logger.LogError(ex, $"{MethodBase.GetCurrentMethod()} failed!");
         Snackbar.Add(ex.GetBaseException().Message, Severity.Error);
      }
   }

   protected async Task AddHW040RotaryEncoder()
   {
      try
      {
         Snackbar.Add("Adding HW040 rotary encoder configuration to the microcontroller...", Severity.Info);
         MicrocontrollerService.AddConnectedDevice(this.Microcontroller, ConnectedDevices.FromHW040());
         Snackbar.Add("Ensure that the device is correctly wired!", Severity.Info);

         await InvokeAsync(StateHasChanged);
      }
      catch (Exception ex)
      {
         Logger.LogError(ex, $"{MethodBase.GetCurrentMethod()} failed!");
         Snackbar.Add(ex.GetBaseException().Message, Severity.Error);
      }
   }

   public async Task OpenManagePinsEditor(ConnectedDevice deviceConfiguration)
   {
      try
      {
         var options = GetDialogOptions();

         var parameters = new DialogParameters { ["ConnectedDevice"] = deviceConfiguration };

         var dialog = await this.DialogService.ShowAsync<PinEditor>("Edit Pins",
                                                                    parameters,
                                                                    options);
         DialogResult? result = await dialog.Result;
         if (result != null)
         {
            if (!result.Canceled)
            {
               //await MicrocontrollerService.PostDeviceConfigToMicrocontroller();
               if (await MicrocontrollerService.PushDeviceConfigToMicrocontroller(this.Microcontroller))
               {
                  Snackbar.Add("Configuration has been sent via MQTT.");
               }
            }
         }
      }
      finally
      {
         await InvokeAsync(StateHasChanged);
      }
   }

   protected async Task ManagePins(ConnectedDevice device)
   {
      await OpenManagePinsEditor(device);
   }

   protected async Task RemoveDevice(ConnectedDevice device)
   {
      try
      {
         Snackbar.Add("Deleting configuration on the microcontroller...", Severity.Info);

         MicrocontrollerService.ConnectedDevicesList.Remove(device);
         //await this.MicrocontrollerService.PostDeviceConfigToMicrocontroller(this.Microcontroller);
         await InvokeAsync(StateHasChanged);
      }
      catch (Exception ex)
      {
         Logger.LogError(ex, $"{MethodBase.GetCurrentMethod()} failed!");
      }
   }

   protected override async Task Cancel()
   {
      await Cancel(Microcontroller);
   }

   protected override async Task Save()
   {
      await MicrocontrollerService.Save(Microcontroller, pushToMicrocontroller: true);
      MudDialog.Close(DialogResult.Ok(Microcontroller));
   }

   public void Dispose()
   {
      MicrocontrollerService.OnMqttConnected -= MicrocontrollerService_ConnectedAsync;
      MicrocontrollerService.OnReceivedDeviceConfig -= MicrocontrollerService_OnReceivedDeviceConfig;
   }
}
