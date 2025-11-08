// --------------------------------------------------------------------------------------------------------------------
//      ____    ______   _____
//     /  _/___/_  __/  /__  / ____  ____
//     / // __ \/ /       / / / __ \/ __ \  P L A Y G R O U N D
//   _/ // /_/ / /       / /_/ /_/ / /_/ /
//  /___/\____/_/       /____|____/\____/   (c) 2025 Holger Freudenreich under the MIT licence.
//
// --------------------------------------------------------------------------------------------------------------------
// Connect WS2812, WS2818 Leds with microcontrollers in a simple way.
// --------------------------------------------------------------------------------------------------------------------
// WS2818 Adafruit_NeoPixel
// --------------------------------------------------------------------------------------------------------------------
#include "Defines.hpp"
#ifdef USE_WS2818
#ifndef __WS2818_HPP__
#define __WS2818_HPP__

#include "DeviceBase.hpp"
#include <Adafruit_NeoPixel.h>
#include <ArduinoJson.h>

namespace IotZoo
{
    struct PixelProperty
    {
        int           PixelId            = 0;
        unsigned long MillisUntilTurnOff = 0;
    };

    class WS2818 : DeviceBase
    {
      protected:
        Adafruit_NeoPixel*    pixels = nullptr;
        int                   dioPin;
        int                   numberOfLeds;
        vector<PixelProperty> pixelProperties;

      public:
        WS2818(int deviceIndex, MqttClient* const mqttClient, const String& baseTopic, int pin, int numberOfLeds);

        virtual ~WS2818();

        void setup();

        void loop();

        /// @brief Example: iotzoo/esp32/08:D1:F9:E0:31:78/neo/0/setPixelColor
        /// @param json
        void setPixelColor(const String& json);

        /// @brief Let the user know what the device can do.
        /// @param topics
        void addMqttTopicsToRegister(std::vector<Topic>* const topics) const override;

        /// @brief The MQTT connection is established. Now subscribe to the topics. An existing MQTT connection is a prerequisite for a subscription.
        /// @param mqttClient
        /// @param baseTopic
        void onMqttConnectionEstablished() override;

        void setPixelColorRgb(uint8_t r, uint8_t g, uint8_t b, uint16_t index, uint8_t brightness = 20, uint64_t millisUntilTurnOff = 0);

        void setPixelColor(uint32_t color, uint16_t index, uint8_t brightness = 20, uint64_t millisUntilTurnOff = 0);
    };
} // namespace IotZoo

#endif // __WS2818_HPP__
#endif // USE_WS2818