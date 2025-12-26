// --------------------------------------------------------------------------------------------------------------------
//      ____    ______   _____
//     /  _/___/_  __/  /__  / ____  ____
//     / // __ \/ /       / / / __ \/ __ \  P L A Y G R O U N D
//   _/ // /_/ / /       / /_/ /_/ / /_/ /
//  /___/\____/_/       /____|____/\____/   (c) 2025 - 2026 Holger Freudenreich under the MIT licence.
//
// --------------------------------------------------------------------------------------------------------------------
// Firmware for ESP8266 and ESP32 Microcontrollers
// --------------------------------------------------------------------------------------------------------------------
#include "Defines.hpp"
#ifdef USE_BUTTON
#include "Button.hpp"
#include "ButtonHandling.hpp"

#include <vector>

namespace IotZoo
{
    /// @brief Let the user know what the device can do.
    /// @param topics
    void ButtonHandling::addMqttTopicsToRegister(std::vector<Topic>* const topics) const
    {
        for (auto& button : ButtonHelper::buttons)
        {
            button.addMqttTopicsToRegister(topics);
        }
    }

    /// @brief The MQTT connection is established. Now subscribe to the topics. An existing MQTT connection is a prerequisite
    /// for a subscription.
    /// @param mqttClient
    /// @param baseTopic
    void ButtonHandling::onMqttConnectionEstablished()
    {
        for (auto& button : ButtonHelper::buttons)
        {
            button.onMqttConnectionEstablished();
        }
    }

    void ButtonHandling::addDevice(int deviceIndex, Settings* const settings, MqttClient* mqttClient, const String& baseTopic,
         u_int8_t buttonPin)
    {
        ButtonHelper::buttons.emplace_back(deviceIndex, settings, mqttClient, baseTopic, buttonPin);
    }

    void ButtonHandling::loop()
    {
        for (auto& button : ButtonHelper::buttons)
        {
            button.loop();
        }
    }
} // namespace IotZoo

#endif // USE_BUTTON