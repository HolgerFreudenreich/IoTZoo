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
#include "ButtonHelper.hpp"

namespace IotZoo
{
    Button::Button(int deviceIndex, Settings* const settings, MqttClient* const mqttClient, const String& baseTopic, uint8_t pin)
        : DeviceBase(deviceIndex, settings, mqttClient, baseTopic)
    {
        this->pin = pin;
        Serial.println("Constructor Button. Pin: " + String(pin));
        topicButtonPushedCounter = getBaseTopic() + "/button/" + String(deviceIndex) + "/pushed_counter";
        topicButtonSetCounter    = getBaseTopic() + "/button/" + String(deviceIndex) + "/set_counter";
        pinMode(pin, INPUT_PULLUP);
        counter = counterOld = 0;
        setup(ButtonHelper::onInterruptTriggered);
    }

    Button::~Button()
    {
        Serial.println("Destructor Button on Pin " + String(pin));
    }

    uint8_t Button::getPin() const
    {
        return pin;
    }

    /// @brief Let the user know what the device can do.
    /// @param topics
    void Button::addMqttTopicsToRegister(std::vector<Topic>* const topics) const
    {
        topics->emplace_back(topicButtonPushedCounter, "Button " + String(getDeviceIndex()) + " was pushed x times.",
                                     MessageDirection::IotZooClientInbound);
        topics->emplace_back(topicButtonSetCounter, "Reset push counter for Button " + String(getDeviceIndex()), MessageDirection::IotZooClientOutbound);
    }

    /// @brief The MQTT connection is established. Now subscribe to the topics. An existing MQTT connection is a prerequisite
    /// for a subscription.
    /// @param mqttClient
    /// @param baseTopic
    void Button::onMqttConnectionEstablished()
    {
        Serial.println("Button::onMqttConnectionEstablished");
        if (mqttCallbacksAreRegistered)
        {
            Serial.println("Reconnection -> nothing to do.");
            return;
        }

        mqttClient->subscribe(topicButtonSetCounter,
                              [&](const String& json)
                              {
                                  Serial.println("set counter");
                                  counter = counterOld = atoi(json.c_str());
                              });
    }

    bool Button::hasStateChanged()
    {
        return counterOld != counter;
    }

    void Button::loop()
    {
        if (hasStateChanged())
        {
            counterOld = counter;
            Serial.println("Button at Pin + " + String(getPin()) + " has been pushed " + String(counter) + " times.");
            mqttClient->publish(topicButtonPushedCounter, String(counter));
        }
    }
} // namespace IotZoo

#endif // USE_BUTTON