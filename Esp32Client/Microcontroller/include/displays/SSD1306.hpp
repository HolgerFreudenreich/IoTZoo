// --------------------------------------------------------------------------------------------------------------------
//      ____    ______   _____
//     /  _/___/_  __/  /__  / ____  ____
//     / // __ \/ /       / / / __ \/ __ \  P L A Y G R O U N D
//   _/ // /_/ / /       / /_/ /_/ / /_/ /
//  /___/\____/_/       /____|____/\____/   (c) 2025 Holger Freudenreich under the MIT licence.
//
// --------------------------------------------------------------------------------------------------------------------
// Firmware for ESP8266 and ESP32 Microcontrollers
// Support for OLED SSD1306 Display
// --------------------------------------------------------------------------------------------------------------------
#include "Defines.hpp"
#ifdef USE_OLED_SSD1306

#ifndef __OLED_SSD1306_DISPLAY_HPP__
#define __OLED_SSD1306_DISPLAY_HPP__

#include "DeviceBase.hpp"
#include "SSD1306Ascii.h"
#include "SSD1306AsciiWire.h"

#include <Wire.h>

namespace IotZoo
{
    class OledSsd1306Display : public DeviceBase
    {
      public:
        OledSsd1306Display(u_int8_t i2cAddress, int deviceIndex, MqttClient* mqttClient, const String& baseTopic);

        virtual ~OledSsd1306Display();

        /// @brief Let the user know what the device can do.
        /// @param topics
        virtual void addMqttTopicsToRegister(std::vector<Topic>* const topics) const override;

        /// @brief Subscribe to Topics
        virtual void onMqttConnectionEstablished() override;

        virtual void onIotZooClientUnavailable() override;

        /// @brief Prints the text <@see text> in lineNumber <@lineNumber>.
        /// @param lineNumber
        /// @param text
        void setTextLine(u_int8_t lineNumber, const String& text);

      protected:
        void setupDisplay(uint8_t i2cAddress);

      protected:
        SSD1306AsciiWire* oled = nullptr;
    };
} // namespace IotZoo

#endif // __OLED_SSD1306_DISPLAY_HPP__
#endif // USE_OLED_SSD1306
