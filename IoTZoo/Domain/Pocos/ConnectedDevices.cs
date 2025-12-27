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


namespace Domain.Pocos;

public static class ConnectedDevices
{
    public static ConnectedDevice FromDS18B20()
    {
        return new ConnectedDevice
        {
            IsEnabled = true,
            DeviceType = "DS18B20",
            Pins = new List<DevicePin>
                              {
                                 new DevicePin
                                 {
                                    MicrocontrollerGpoPin = "23",
                                    PinName               = "DAT"
                                 }
                              },
            PropertyValues = new List<PropertyValue> { new PropertyValue("Interval", "10000"),
                                                    new PropertyValue("Resolution", "11") }
        };
    }

    public static ConnectedDevice FromTM1637_4()
    {
        return new ConnectedDevice
        {
            IsEnabled = true,
            DeviceType = "TM1637_4",
            Pins = new List<DevicePin>
                              {
                                 new DevicePin
                                 {
                                    MicrocontrollerGpoPin = "27",
                                    PinName               = "CLK"
                                 },
                                 new DevicePin
                                 {
                                    MicrocontrollerGpoPin = "26",
                                    PinName               = "DIO"
                                 }
         },
            PropertyValues = new List<PropertyValue> { new PropertyValue { Name = "flipDisplay", Value = "false" },
                                                    new PropertyValue { Name = "serverDownText", Value = "----" } }
        };
    }

    public static ConnectedDevice FromTM1637_6()
    {
        return new ConnectedDevice
        {
            IsEnabled = true,
            DeviceType = "TM1637_6",
            Pins = new List<DevicePin>
                    {
                      new DevicePin
                      {
                        MicrocontrollerGpoPin = "14",
                        PinName               = "CLK"
                      },
                      new DevicePin
                      {
                        MicrocontrollerGpoPin = "27",
                        PinName               = "DIO"
                      }
                    },
            PropertyValues = new List<PropertyValue> { new PropertyValue { Name = "flipDisplay", Value = "false" },
                                                    new PropertyValue { Name = "serverDownText", Value = "-------" } }
        };
    }

    public static ConnectedDevice FromTM1638()
    {
        return new ConnectedDevice
        {
            IsEnabled = true,
            DeviceType = "TM1638",
            Pins = new List<DevicePin>
                              {
                                 new DevicePin
                                 {
                                    MicrocontrollerGpoPin = "14",
                                    PinName               = "STB" // GPIO connected to strobe line of module
                                 },
                                 new DevicePin
                                 {
                                    MicrocontrollerGpoPin = "27",
                                    PinName               = "CLK" // GPIO connected to clock line of module
                                 },
                                                        new DevicePin
                                 {
                                    MicrocontrollerGpoPin = "26",
                                    PinName               = "DIO" // GPIO connected to data line of module
                                 }
         },
            PropertyValues = new List<PropertyValue> {
                                                    new PropertyValue { Name = "serverDownText", Value = "---------" }
         }
        };
    }

    public static ConnectedDevice FromHT1621()
    {
        return new ConnectedDevice
        {
            IsEnabled = true,
            DeviceType = "HT1621",
            Pins = new List<DevicePin>
                              {
                                 new DevicePin
                                 {
                                    MicrocontrollerGpoPin = "27",
                                    PinName               = "CS_PIN"
                                 },
                                 new DevicePin
                                 {
                                    MicrocontrollerGpoPin = "26",
                                    PinName               = "WS_PIN"
                                 },
                                 new DevicePin
                                 {
                                    MicrocontrollerGpoPin = "25",
                                    PinName               = "DATA_PIN"
                                 },
                                 new DevicePin
                                 {
                                    MicrocontrollerGpoPin = "33",
                                    PinName               = "BACKLIGHT_PIN"
                                 }
                              }
        };
    }

    public static ConnectedDevice FromMax7219()
    {
        return new ConnectedDevice
        {
            IsEnabled = true,
            DeviceType = "MAX7219",
            Pins = new List<DevicePin>
                              {
                                 new DevicePin
                                 {
                                    MicrocontrollerGpoPin = "27",
                                    PinName               = "DATA_PIN"
                                 },
                                 new DevicePin
                                 {
                                    MicrocontrollerGpoPin = "25",
                                    PinName               = "CLK_PIN"
                                 },
                                 new DevicePin
                                 {
                                    MicrocontrollerGpoPin = "26",
                                    PinName               = "CS_PIN"
                                 }
                              },
            PropertyValues = new List<PropertyValue> { new PropertyValue { Name = "numberOfDevices", Value = "1" } }
        };
    }

    public static ConnectedDevice FromLedTrafficLights()
    {
        return new ConnectedDevice
        {
            IsEnabled = true,
            DeviceType = "LEDS Traffic Light",
            Pins = new List<DevicePin>
                              {
                                 new DevicePin
                                 {
                                    MicrocontrollerGpoPin = "13",
                                    PinName               = "R"
                                 },
                                 new DevicePin
                                 {
                                    MicrocontrollerGpoPin = "12",
                                    PinName               = "Y"
                                 },
                                 new DevicePin
                                 {
                                    MicrocontrollerGpoPin = "14",
                                    PinName               = "G"
                                 }
                              }
        };
    }

    public static ConnectedDevice FromRemoteGpio()
    {
        return new ConnectedDevice
        {
            IsEnabled = true,
            DeviceType = "Remote GPIO", // do not change, @see main.cpp
            Pins = new List<DevicePin>
                              {
                                 new DevicePin
                                 {
                                    MicrocontrollerGpoPin = "36",
                                    PinName               = "GPIO"
                                 },
                              }
        };
    }

    public static ConnectedDevice FromNeoPixel()
    {
        return new ConnectedDevice
        {
            IsEnabled = true,
            DeviceType = "NEO", // do not change, @see main.cpp
            Pins = new List<DevicePin>
                              {
                                 new DevicePin
                                 {
                                    MicrocontrollerGpoPin = "22",
                                    PinName               = "DIN"
                                 },
                              },
            PropertyValues = new List<PropertyValue> { new PropertyValue { Name = "numberOfLeds", Value = "256" } }
        };
    }

    public static ConnectedDevice FromNeoPixelMatrix()
    {
        return new ConnectedDevice
        {
            IsEnabled = true,
            DeviceType = "PixelMatrix", // do not change, @see main.cpp
            Pins = new List<DevicePin>
                              {
                                 new DevicePin
                                 {
                                    MicrocontrollerGpoPin = "22",
                                    PinName               = "DIN"
                                 },
                              },
            PropertyValues = new List<PropertyValue> { new PropertyValue { Name = "numberOfLedsPerColumn", Value = "8" },
                                                       new PropertyValue { Name = "numberOfLedsPerRow", Value = "8" },
                                                       new PropertyValue { Name = "extensions", Value = "1" } }
        };
    }

    public static ConnectedDevice FromHW040()
    {
        return new ConnectedDevice
        {
            IsEnabled = true,
            DeviceType = "HW-040",
            Pins = new List<DevicePin>
                              {
                                 // Do not change the order, the c++ code simply maps to the index!
                                 new DevicePin
                                 {
                                    MicrocontrollerGpoPin = "32",
                                    PinName               = "CLK"
                                 },
                                 new DevicePin
                                 {
                                    MicrocontrollerGpoPin = "21",
                                    PinName               = "DT"
                                 },
                                 new DevicePin
                                 {
                                 MicrocontrollerGpoPin = "33",
                                 PinName               = "SW"
                              }
                              },
            PropertyValues = new List<PropertyValue>()
                          {
                             new PropertyValue {Name = "Acceleration", Value     = "250"},
                             new PropertyValue {Name  = "BoundaryMinValue", Value = "0"},
                             new PropertyValue {Name  = "BoundaryMaxValue", Value = "255"},
                             new PropertyValue {Name  = "CircleValue", Value      = "false"},
                             new PropertyValue {Name  = "EncoderSteps", Value = "2"}
                          }
        };
    }

    public static ConnectedDevice FromKeypad4x4()
    {
        return new ConnectedDevice
        {
            IsEnabled = true,
            DeviceType = "Keypad 4x4",
            Pins = new List<DevicePin>
                              {
                                 new DevicePin
                                 {
                                    MicrocontrollerGpoPin = "13",
                                    PinName               = "C4"
                                 },
                                 new DevicePin
                                 {
                                    MicrocontrollerGpoPin = "12",
                                    PinName               = "C3"
                                 },
                                 new DevicePin
                                 {
                                    MicrocontrollerGpoPin = "14",
                                    PinName               = "C2"
                                 },
                                 new DevicePin
                                 {
                                    MicrocontrollerGpoPin = "27",
                                    PinName               = "C1"
                                 },
                                 new DevicePin
                                 {
                                    MicrocontrollerGpoPin = "26",
                                    PinName               = "R1"
                                 },
                                 new DevicePin
                                 {
                                    MicrocontrollerGpoPin = "25",
                                    PinName               = "R2"
                                 },
                                 new DevicePin
                                 {
                                    MicrocontrollerGpoPin = "33",
                                    PinName               = "R3"
                                 },
                                 new DevicePin
                                 {
                                    MicrocontrollerGpoPin = "32",
                                    PinName               = "R4"
                                 }
                              }
        };
    }

    public static ConnectedDevice From28BYJ48StepperMotor()
    {
        return new ConnectedDevice
        {
            IsEnabled = true,
            DeviceType = "28BY48Stepper",
            Pins = new List<DevicePin>
                              {
                                 new DevicePin
                                 {
                                    MicrocontrollerGpoPin = "13",
                                    PinName               = "IN1"
                                 },
                                 new DevicePin
                                 {
                                    MicrocontrollerGpoPin = "12",
                                    PinName               = "IN2"
                                 },
                                 new DevicePin
                                 {
                                    MicrocontrollerGpoPin = "14",
                                    PinName               = "IN3"
                                 },
                                 new DevicePin
                                 {
                                    MicrocontrollerGpoPin = "27",
                                    PinName               = "IN4"
                                 }
                              }
        };
    }

    /// <summary>
    /// Motion detector
    /// </summary>
    /// <returns></returns>
    public static ConnectedDevice FromHcSr501PirSensor()
    {
        return new ConnectedDevice
        {
            IsEnabled = true,
            DeviceType = "HC-SR501",
            Pins = new List<DevicePin>
                              {
                                 new DevicePin
                                 {
                                    MicrocontrollerGpoPin = "21",
                                    PinName               = "IN"
                                 }
                              }
        };
    }

    /// <summary>
    /// Motion detector
    /// </summary>
    /// <returns></returns>
    public static ConnectedDevice FromRd03DPresenseSensor()
    {
        return new ConnectedDevice
        {
            IsEnabled = true,
            DeviceType = "Rd-03D",
            Pins = new List<DevicePin>
                              {
                                 new DevicePin
                                 {
                                    MicrocontrollerGpoPin = "16",
                                    PinName               = "RX"
                                 },
                                  new DevicePin
                                 {
                                    MicrocontrollerGpoPin = "17",
                                    PinName               = "TX"
                                 }
                              }
           ,
            PropertyValues = new List<PropertyValue>()
                          {
                             new PropertyValue {Name = "MultiTargetMode", Value = "true"},
                             new PropertyValue {Name = "TimeoutMillis", Value = "25000"},
                             new PropertyValue {Name = "MaxDistanceMillimeters", Value = "5500"},
                          }
        };
    }

    public static ConnectedDevice FromButton()
    {
        return new ConnectedDevice
        {
            IsEnabled = true,
            DeviceType = "Button",
            Pins = new List<DevicePin>
                              {
                                 new DevicePin
                                 {
                                    MicrocontrollerGpoPin = "23",
                                    PinName               = "BTN_PIN" // connect this PIN to GROUND.
                                 }
                              }
        };
    }

    public static ConnectedDevice FromHW507()
    {
        return new ConnectedDevice
        {
            IsEnabled = true,
            DeviceType = "HW507",
            Pins = new List<DevicePin>
                              {
                                 new DevicePin
                                 {
                                    MicrocontrollerGpoPin = "23",
                                    PinName               = "DATA_PIN"
                                 }
                              },
            PropertyValues = new List<PropertyValue>()
                          {
                             new PropertyValue {Name = "DeviceType", Value = "DHT11"},
                               new PropertyValue {Name = "IntervalMs", Value = "10000"},
                          }
        };
    }

    public static ConnectedDevice FromMicrophone()
    {
        return new ConnectedDevice
        {
            IsEnabled = true,
            DeviceType = "INMP441",
            Pins = new List<DevicePin>
                              {
                                 new DevicePin
                                 {
                                    MicrocontrollerGpoPin = "22",
                                    PinName               = "SD"
                                 },
                                  new DevicePin
                                 {
                                    MicrocontrollerGpoPin = "35",
                                    PinName               = "WS" // connect this PIN to GROUND.
                                 },
                                   new DevicePin
                                 {
                                    MicrocontrollerGpoPin = "15",
                                    PinName               = "SCK" // connect this PIN to GROUND.
                                 }
                              },
            PropertyValues = new List<PropertyValue>()
                          {
                             new PropertyValue {Name = "AllowStreaming", Value = "false"}, // Streaming uses a lot of bandwidth and the quality is not that good because only 16 bit sample rate.
                             new PropertyValue {Name = "AllowSoundLevel", Value = "true"},
                             new PropertyValue {Name = "MinRms", Value = "400"},
                          }
        };
    }

    public static ConnectedDevice FromBuzzer()
    {
        return new ConnectedDevice
        {
            IsEnabled = true,
            DeviceType = "Buzzer",
            Pins = new List<DevicePin>
                              {
                                 new DevicePin
                                 {
                                    MicrocontrollerGpoPin = "16",
                                    PinName               = "BUZZER_PIN"
                                 },
                                  new DevicePin
                                 {
                                    MicrocontrollerGpoPin = "-1",
                                    PinName               = "LED_PIN"
                                 }
                              }
        };
    }

    public static ConnectedDevice FromGps()
    {
        return new ConnectedDevice
        {
            IsEnabled = true,
            DeviceType = "GPS",
            Pins = new List<DevicePin>
                              {
                                 new DevicePin
                                 {
                                    MicrocontrollerGpoPin = "22",
                                    PinName               = "RX"
                                 },
                                  new DevicePin
                                 {
                                    MicrocontrollerGpoPin = "23",
                                    PinName               = "TX"
                                 }
                              }
        };
    }

    public static ConnectedDevice FromSwitch()
    {
        return new ConnectedDevice
        {
            IsEnabled = true,
            DeviceType = "Switch",
            Pins = new List<DevicePin>
                              {
                                 new DevicePin
                                 {
                                    MicrocontrollerGpoPin = "24",
                                    PinName               = "SWITCH_PIN" // connect this PIN to GROUND.
                                 }
                              }
        };
    }

    public static ConnectedDevice FromOledSSD1306()
    {
        return new ConnectedDevice
        {
            IsEnabled = true,
            DeviceType = "OLED_SSD1306", // do not change this name, the same name must be used in main.cpp of the client.
            Pins = new List<DevicePin>
                     {
                       new DevicePin
                       {
                         MicrocontrollerGpoPin = "22",
                         PinName               = "SCL",
                         IsReadOnly = true

                       },
                       new DevicePin
                       {
                         MicrocontrollerGpoPin = "21",
                         PinName               = "SCA",
                         IsReadOnly = true
                       }
                     }
           ,
            PropertyValues = new List<PropertyValue>
         {
            new PropertyValue("I2CAddress", "0x3C")
         }
        };
    }

    public static ConnectedDevice FromLcd160x()
    {
        return new ConnectedDevice
        {
            IsEnabled = true,
            DeviceType = "LCD160x", // do not change this name, the same name must be used in main.cpp of the client.
            Pins = new List<DevicePin>
                     {
                       new DevicePin
                       {
                         MicrocontrollerGpoPin = "22",
                         PinName               = "SCL",
                         IsReadOnly = true
                       },
                       new DevicePin
                       {
                         MicrocontrollerGpoPin = "21",
                         PinName               = "SCA",
                         IsReadOnly = true
                       }
                     },
            PropertyValues = new List<PropertyValue>
         {
            new PropertyValue("Columns", "20"),
            new PropertyValue("Rows", "4"),
            new PropertyValue("I2CAddress", "0x27")
         }
        };
    }

    public static ConnectedDevice BleHeartRateMonitor()
    {
        return new ConnectedDevice
        {
            IsEnabled = true,
            DeviceType = "BleHeartRateSensor", // do not change this name, the same name must be used in main.cpp of the client.
            PropertyValues = new List<PropertyValue>
         {
            new PropertyValue("AdvertisingTimeoutSeconds", "60"),

         }
        };
    }
}