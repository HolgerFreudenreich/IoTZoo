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

    private const string DeviceWiredMessage = "Ensure that the device is correctly wired!";

    private async Task AddDeviceAsync(string startMessage, Func<ConnectedDevice> createDevice, string? wiringMessage = null)
    {
        try
        {
            Snackbar.Add(startMessage, Severity.Info);
            MicrocontrollerService.AddConnectedDevice(this.Microcontroller, createDevice());
            if (wiringMessage != null)
            {
                Snackbar.Add(wiringMessage, Severity.Info);
            }
            await InvokeAsync(StateHasChanged);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, $"{MethodBase.GetCurrentMethod()} failed!");
            Snackbar.Add(ex.GetBaseException().Message, Severity.Error);
        }
    }

    protected Task AddButton() => AddDeviceAsync("Adding Button configuration to the microcontroller...", ConnectedDevices.FromButton, "Ensure that the button is correctly wired from GND to the pin!");

    protected Task AddAnalogInputPin() => AddDeviceAsync("Adding Analog Pin configuration to the microcontroller...", ConnectedDevices.FromAnalogPin, "Ensure that the pin is correctly wired from 3.3V to the pin (Do not use 5V!!!). Use an extra 10 K-Ohm resistor as voltage devider to ground if you want to connect a photo resistor!");

    protected Task AddReedContact() => AddDeviceAsync("Adding reed contact configuration to the microcontroller...", ConnectedDevices.FromReedContact);

    /// <summary>
    /// Microphone INMP441
    /// </summary>
    /// <returns></returns>
    protected Task AddINMP441() => AddDeviceAsync("Adding Microphone configuration to the microcontroller...", ConnectedDevices.FromMicrophone);

    protected Task AddBuzzer() => AddDeviceAsync("Adding Buzzer configuration to the microcontroller...", ConnectedDevices.FromBuzzer, "Ensure that the buzzer is correctly wired!");

    protected Task AddGps() => AddDeviceAsync("Adding Gps configuration to the microcontroller...", ConnectedDevices.FromGps, "Ensure that the Gps Device is correctly wired!");

    protected Task AddSwitch() => AddDeviceAsync("Adding Switch configuration to the microcontroller...", ConnectedDevices.FromSwitch, "Ensure that the switch is correctly wired from GND to the pin!");

    protected Task AddDS18B20TemperatureSensor() => AddDeviceAsync("Adding DS18B20 temperature sensor configuration to the microcontroller...", ConnectedDevices.FromDS18B20, DeviceWiredMessage);

    protected Task AddHW507HumiditySensor() => AddDeviceAsync("Adding HW507 humidity sensor configuration to the microcontroller...", ConnectedDevices.FromHW507, DeviceWiredMessage);

    protected Task Add28BYJ48StepperMotor() => AddDeviceAsync("Adding 28BYJ-48 Stepper Motor configuration to the microcontroller...", ConnectedDevices.From28BYJ48StepperMotor, DeviceWiredMessage);

    protected Task AddBleHeartRateMonitor() => AddDeviceAsync("Adding BLE Heart Rate Monitor configuration to the microcontroller...", ConnectedDevices.BleHeartRateMonitor, DeviceWiredMessage);

    protected Task AddHCSR501MotionDetector() => AddDeviceAsync("Adding HCSR501 Motion detector configuration to the microcontroller...", ConnectedDevices.FromHcSr501PirSensor, DeviceWiredMessage);

    protected Task AddRd03DPresenceDetector() => AddDeviceAsync("Adding RD-03D presence detector configuration to the microcontroller...", ConnectedDevices.FromRd03DPresenseSensor, DeviceWiredMessage);

    protected Task Add4x4ButtonMatrix() => AddDeviceAsync("Adding 4x4 Button Matrix configuration to the microcontroller...", ConnectedDevices.FromKeypad4x4, DeviceWiredMessage);

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

    protected Task AddNeoPixel() => AddDeviceAsync("Adding NEO-pixel configuration to the microcontroller...", ConnectedDevices.FromNeoPixel, DeviceWiredMessage);

    protected Task AddNeoPixelMatrix() => AddDeviceAsync("Adding NEO-pixel matrix configuration to the microcontroller...", ConnectedDevices.FromNeoPixelMatrix, DeviceWiredMessage);

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

    protected Task AddTM1637_4_DigitDisplay() => AddDeviceAsync("Adding TM1637 4 digit display configuration to the microcontroller...", ConnectedDevices.FromTM1637_4, DeviceWiredMessage);

    protected Task AddTM1637_6_DigitDisplay() => AddDeviceAsync("Adding TM1637 6 digit display configuration to the microcontroller...", ConnectedDevices.FromTM1637_6, DeviceWiredMessage);

    protected Task AddTM1638Display() => AddDeviceAsync("Adding TM1637 8 digit display", ConnectedDevices.FromTM1638, DeviceWiredMessage);

    protected Task AddHT1621Display() => AddDeviceAsync("Adding HT1621 6 digit LED display", ConnectedDevices.FromHT1621, DeviceWiredMessage);

    protected Task AddMax7219Display() => AddDeviceAsync("Adding Max7219 6 digit LED display", ConnectedDevices.FromMax7219, DeviceWiredMessage);

    protected Task AddHW040RotaryEncoder() => AddDeviceAsync("Adding HW040 rotary encoder configuration to the microcontroller...", ConnectedDevices.FromHW040, DeviceWiredMessage);

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
