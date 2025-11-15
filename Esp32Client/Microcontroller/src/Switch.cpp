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
#ifdef USE_SWITCH
#include "Switch.hpp"

namespace IotZoo
{
    Switch::Switch(int deviceIndex, Settings* const settings, MqttClient* const mqttClient, const String& baseTopic, uint8_t pin)
        : DeviceBase(deviceIndex, settings, mqttClient, baseTopic)
    {
        this->pin = pin;
        Serial.println("Constructor Switch. Pin: " + String(pin));
        pinMode(pin, INPUT_PULLUP);
    }

    Switch::~Switch()
    {
        Serial.println("Destructor Switch. Pin: " + String(pin));
    }

    uint8_t Switch::getPin() const
    {
        return pin;
    }

    bool Switch::isPressed() const
    {
        return isButtonPressed;
    }

    /// @brief Let the user know what the device can do.
    /// @param topics
    void Switch::addMqttTopicsToRegister(std::vector<Topic>* const topics) const
    {
        topics->emplace_back(getBaseTopic() + "/switch/" + String(getDeviceIndex()) + "/on",
                             "Switch " + String(getDeviceIndex()) + " is on. Payload: millis();", MessageDirection::IotZooClientInbound);
        topics->emplace_back(getBaseTopic() + "/switch/" + String(getDeviceIndex()) + "/off",
                             "Switch " + String(getDeviceIndex()) + " is off. Payload: millis();", MessageDirection::IotZooClientInbound);
    }

    bool Switch::hasStateChanged()
    {
        oldIsButtonPressed    = isButtonPressed;
        isButtonPressed       = digitalRead(pin) == LOW;
        buttonStateHasChanged = oldIsButtonPressed != isButtonPressed;
        if (buttonStateHasChanged)
        {
            Serial.println("State has changed. ButtonState at Pin " + String(pin) + " is now " + String(isButtonPressed));
        }
        return buttonStateHasChanged;
    }

    void Switch::loop()
    {
        if (hasStateChanged())
        {
            String topicButton = getBaseTopic() + "/switch/" + String(getDeviceIndex());
            if (isPressed())
            {
                Serial.println("Switch at Pin + " + String(getPin()) + " changed state to on. Payload: millis on ESP32.");
                topicButton += "/on";
                mqttClient->publish(topicButton, String(millis()));
            }
            else
            {
                Serial.println("Switch at Pin + " + String(getPin()) + " changed state to off. Payload: millis on ESP32.");
                topicButton += "/off";
                mqttClient->publish(topicButton, String(millis()));
            }
        }
    }
} // namespace IotZoo

#endif // USE_SWITCH