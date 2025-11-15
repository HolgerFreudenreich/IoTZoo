// --------------------------------------------------------------------------------------------------------------------
//      ____    ______   _____
//     /  _/___/_  __/  /__  / ____  ____
//     / // __ \/ /       / / / __ \/ __ \  P L A Y G R O U N D
//   _/ // /_/ / /       / /_/ /_/ / /_/ /
//  /___/\____/_/       /____|____/\____/   (c) 2025 Holger Freudenreich under the MIT licence.
//
// --------------------------------------------------------------------------------------------------------------------
// Firmware for ESP8266 and ESP32 Microcontrollers
// --------------------------------------------------------------------------------------------------------------------
#include "Defines.hpp"
#ifdef USE_BUTTON
#ifndef __BUTTON_HANDLING_HPP__
#define __BUTTON_HANDLING_HPP__

#include <vector>
#include "Arduino.h"
#include "DeviceHandlingBase.hpp"
#include "Button.hpp"
#include "ButtonHelper.hpp"

namespace IotZoo
{
    class ButtonHandling : public DeviceHandlingBase
    {
    public:
        /// @brief Let the user know what the device can do.
        /// @param topics
        void addMqttTopicsToRegister(std::vector<Topic> *const topics) const override;

        /// @brief The MQTT connection is established. Now subscribe to the topics. An existing MQTT connection is a prerequisite for a subscription.
        /// @param mqttClient
        /// @param baseTopic
        void onMqttConnectionEstablished();

        void addDevice(int deviceIndex, Settings* const settings, MqttClient* mqttClient, const String &baseTopic, u_int8_t buttonPin);

        void loop();
    };
}

#endif // __BUTTON_HANDLING_HPP__
#endif // USE_BUTTON