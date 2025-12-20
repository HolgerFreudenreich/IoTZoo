// --------------------------------------------------------------------------------------------------------------------
//      ____    ______   _____
//     /  _/___/_  __/  /__  / ____  ____
//     / // __ \/ /       / / / __ \/ __ \ 
//   _/ // /_/ / /       / /_/ /_/ / /_/ /
//  /___/\____/_/       /____|____/\____/ (c) 2025 Holger Freudenreich under the MIT licence.
// --------------------------------------------------------------------------------------------------------------------
// Connect «Things» with microcontrollers in a simple way.
// --------------------------------------------------------------------------------------------------------------------

#ifndef __DEFINES_HPP__
#define __DEFINES_HPP__

namespace IotZoo
{
#define USE_MQTT
#define USE_REST_SERVER // Do not uncomment, otherwise you cannot configure the microcontroller out of the IOTZOO UI over REST. To use MQTT for
                        // configuration is the better way because then the IOTZOO Client und the ESP32 can be in different networks. But, if the
                        // MQTTBroker settings in the ESP32 are wrong, then there is a chance to correct this over REST.

// --------------------------------------------------------------------------------------------------------------------
// Comment feature(s) out if you run out of memory.
// --------------------------------------------------------------------------------------------------------------------
#define USE_DS18B20 // Temperatur Sensor | Default Pins: DAT: Pin 23

#define USE_BUTTON // briefly on, then off again after releasing
#define USE_GPS
#define USE_SWITCH // permanently off or on

#define USE_KEYPAD      // 4 x 4 Button Matrix | Default Pins: R1: 26, R2: 25, R3: 33, R4: 32, C1: 27, C2: 14, C3: 12, C4: 13
#define USE_LED_AND_KEY // 1 x 8 Button Row combined with 2 TM1638 4 digits displays | Default Pins: STB: 14, CLK: 27, DIO: 26
#define USE_WS2818      // NeoPixel | Default Pins: DIN: 22
#ifdef USE_WS2818
// #define USE_WS2818_PIXEL_MATRIX
#endif

#define USE_AUDIO_STREAMER // INMP441 microphone Default Pins: I2S_WS: 22, I2S_SCK: 15, I2S_SD: 35

#define USE_TRAFFIC_LIGHT_LEDS // Default Pins: R: 5, Y: 18, G: 19
// #define USE_REMOTE_GPIOS       // Default Pins: 36
// #define USE_HC_SR501           // Motion detector, Default Pin: 21 (this is the pin in the middle of the motion detector).
// #define USE_RD_03D             // Multi-Target Human Motion Detector
// #define USE_STEPPER_MOTOR

// #define USE_KY025             // Reed Contact, Default Pin: 19
// #define USE_HB0014            // Hitchi IR ttl
#define USE_HW040 // Rotary Encoder, Default Pins: CLK: 32, DT: 21, MS: 33

// DISPLAYS
#define USE_OLED_SSD1306 // OLED, Default Pins: SDA: 21, SCL: 22
#define USE_LCD_160X     // 16 x 2, 16 x 4 LCD DISPLAY with I2C HW-061, Default Pins: SDA: 21, SCL: 22
#define USE_TM1637_4     // 4 digits display. You can connect up to 10 items. Default Pins (Item 0): CLK: 27, DIO: 26
// #define USE_TM1637_6             // 6 digits display. You can connect up to 10 items. Default Pins: CLK: 14, DIO: 27
#define USE_MAX7219 // 8x8 Led Dot Matrix, Default Poins: Data: 27, CLK: 25, CS: 26
#define USE_BUZZER

// #define USE_BLE_HEART_RATE_SENSOR // Bluetooth hearte rate sensor. Needs alot of the available RAM so do not use to many other defines!

// A special project that must function without an Internet connection. Exercise bike with display Display of heart rate, pedaling speed and
// rotations. #define USE_EXERCISE_BIKE_STANDALONE
#ifdef USE_EXERCISE_BIKE_STANDALONE
#define USE_BLE_HEART_RATE_SENSOR
#define USE_KY025
#define USE_TM1637_4
#undef USE_MQTT   // Conflicts with USE_BLE_HEART_RATE_SENSOR, because of to much memory consumption.
#define USE_MQTT2 // PubSubLibrary instead of EspMQTTClient to save memory.
#endif
} // namespace IotZoo

// #define ERASE_FLASH

#endif // __DEFINES_HPP__

// --------------------------------------------------------------------------------------------------------------------
// PIN LAYOUT ESP32 view from top and USB C port is on left site
//
// ESP32 30 PIN LAYOUT
//
// 15 ADC channels            | 15 channels of 12-bit SAR ADC with selectable ranges of 0-1V, 0-1.4V, 0-2V, or 0-4V
//  2 UART interfaces         | 2 UART interfaces with flow control and IrDA support
// 25 PWM outputs             | 25 PWM pins to control things like motor speed or LED brightness
//  2 DAC channels	          | 2 8-bit DACs to generate true analog voltages
// SPI, I2C and I2S interface |	Three SPI and one I2C interfaces for connecting various sensors and peripherals, as well
//                              as two I2S interfaces for adding sound to your project
// 9 Touch Pads	9 GPIOs with capacitive touch sensing

// --------------------------------------------------------------------------------------------------------------------
// top pin row:
// 01 | VIN 5V
// 02 | GND
// 03 | IO 13
// 04 | IO 12 | if peripherals are connected to these pins, you may encounter issues when attempting to upload new code or flash the ESP32 with new firmware, as these peripherals prevent the ESP32 from entering the correct mode.
// 05 | IO 14
// 06 | IO 27
// 07 | IO 26
// 08 | IO 25
// 09 | IO 33
// 10 | IO 32
// 11 | IO 35 Input only
// 12 | IO 34 Input only
// 13 | VN IO 36 Input only
// 14 | VP IO 39 Input only
// 15 | EN

// bottom pin row:
// 16 | 3.3V 600 mA max
// 17 | GND
// 18 | IO 15 | if peripherals are connected to these pins, you may encounter issues when attempting to upload new code or flash the ESP32 with new firmware, as these peripherals prevent the ESP32 from entering the correct mode.
// 19 | IO  2 | if peripherals are connected to these pins, you may encounter issues when attempting to upload new code or flash the ESP32 with new firmware, as these peripherals prevent the ESP32 from entering the correct mode.
// 20 | IO  4
// 21 | IO 16 | RX2
// 22 | IO 17 | TX2
// 23 | IO  5 | if peripherals are connected to these pins, you may encounter issues when attempting to upload new code or flash the ESP32 with new firmware, as these peripherals prevent the ESP32 from entering the correct mode.
// 24 | IO 18 | SCK
// 25 | IO 19 | MISO
// 26 | IO 21 | SDA
// 27 | IO  1 | RX
// 28 | IO  3 | TX
// 29 | IO 22 | SCL
// 30 | IO 23 | MOSI
