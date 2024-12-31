// --------------------------------------------------------------------------------------------------------------------
//      ____    ______   _____
//     /  _/___/_  __/  /__  / ____  ____
//     / // __ \/ /       / / / __ \/ __ \  P L A Y G R O U N D
//   _/ // /_/ / /       / /_/ /_/ / /_/ /
//  /___/\____/_/       /____|____/\____/   (c) 2025 Holger Freudenreich under the MIT licence.
//
// --------------------------------------------------------------------------------------------------------------------
// Firmware for ESP8266 and ESP32 Microcontrollers
// Support for LCD1602, LCD2004
// --------------------------------------------------------------------------------------------------------------------
#include "Defines.hpp"
#ifdef USE_LCD_160X

#ifndef __LCD_DISPLAY_HPP__
#define __LCD_DISPLAY_HPP__

#include "DeviceBase.hpp"
#include <Wire.h>
#include <LiquidCrystal_I2C.h>

namespace IotZoo
{
    class LcdDisplay : public DeviceBase, public Print
    {
    public:
        LcdDisplay(u_int8_t address, u_int8_t cols, u_int8_t rows,
                   int deviceIndex, MqttClient *mqttClient, const String &baseTopic);

        virtual ~LcdDisplay();

        void turnBacklightOn();

        void turnBacklightOff();

        void clear();

        void setCursor(uint8_t col, uint8_t row);

        /// @brief Let the user know what the device can do.
        /// @param topics
        virtual void addMqttTopicsToRegister(std::vector<Topic> *const topics) const override;

        /// @brief The MQTT connection is established. Now subscribe to the topics. An existing MQTT connection is a prerequisite for a subscription.
        /// @param mqttClient
        /// @param baseTopic
        virtual void onMqttConnectionEstablished() override;

        virtual void onIotZooClientUnavailable() override;

        virtual size_t write(uint8_t data) override;

        void setLcd160xBacklight(const String &rawData);

        // {"text": "IoT Zoo", "clear": true, "x":1, "y": 0}
        void setLcd160xData(const String &json);

        void subscribeSetLcd160xData();

        void subscribeSetBacklight();

    protected:
        LiquidCrystal_I2C *lcd = NULL;
    };
}

#endif // __LCD_DISPLAY_HPP__

#endif // USE_LCD_160X