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
#include <vector>
#include "Button.hpp"
#include "ButtonHandling.hpp"

namespace IotZoo
{
    /// @brief Let the user know what the device can do.
    /// @param topics
    void ButtonHandling::addMqttTopicsToRegister(std::vector<Topic> *const topics) const
    {
        for (auto &button : ButtonHelper::buttons)
        {
            button.addMqttTopicsToRegister(topics);
        }
    }

    /// @brief The MQTT connection is established. Now subscribe to the topics. An existing MQTT connection is a prerequisite for a subscription.
    /// @param mqttClient
    /// @param baseTopic
    void ButtonHandling::onMqttConnectionEstablished()
    {
        for (auto &button : ButtonHelper::buttons)
        {
            button.onMqttConnectionEstablished();
        }
    }

    void ButtonHandling::addDevice(MqttClient *mqttClient, const String &baseTopic, int deviceIndex, u_int8_t buttonPin)
    {
        Button *button = new Button(deviceIndex, mqttClient, baseTopic, buttonPin);

        ButtonHelper::buttons.push_back(*button);
    }

    void ButtonHandling::loop()
    {
        for (auto &button : ButtonHelper::buttons)
        {
            button.loop();
        }
    }
}

#endif // USE_BUTTON