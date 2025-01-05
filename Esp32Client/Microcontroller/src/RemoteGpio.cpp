// --------------------------------------------------------------------------------------------------------------------
//      ____    ______   _____
//     /  _/___/_  __/  /__  / ____  ____
//     / // __ \/ /       / / / __ \/ __ \  P L A Y G R O U N D
//   _/ // /_/ / /       / /_/ /_/ / /_/ /
//  /___/\____/_/       /____|____/\____/   (c) 2025 Holger Freudenreich under the MIT licence.
//
// --------------------------------------------------------------------------------------------------------------------
// Connect «Things» with microcontrollers in a simple way.
// --------------------------------------------------------------------------------------------------------------------
#include "Defines.hpp"
#ifdef USE_REMOTE_GPIOS
#ifndef __REMOTE_GPIO_HPP__
#include "RemoteGpio.hpp"
#include "./pocos/Topic.hpp"
#endif

namespace IotZoo
{
    RemoteGpio::RemoteGpio(int deviceIndex, MqttClient *const mqttClient, const String &baseTopic,
                           int pin) : DeviceBase(deviceIndex, mqttClient, baseTopic)
    {
        pinGpio = pin;
        pinMode(pinGpio, OUTPUT);
        Serial.println("RemoteGpio on pin " + String(pinGpio));
    }

    RemoteGpio::~RemoteGpio()
    {
        Serial.println("Destructor RemoteGpio on pin " + String(pinGpio));
    }

    /// @brief Let the user know what the device can do.
    /// @param topics
    void RemoteGpio::addMqttTopicsToRegister(std::vector<Topic> *const topics) const
    {
        topics->push_back(*new Topic(getBaseTopic() + "/gpio/" + String(deviceIndex), "0, LOW, OFF or 1, HIGH, ON or TOGGLE for toggling state",
                                     MessageDirection::IotZooClientOutbound));
    }

    /// @brief The MQTT connection is established. Now subscribe to the topics. An existing MQTT connection is a prerequisite for a subscription.
    /// @param mqttClient
    /// @param baseTopic
    void RemoteGpio::onMqttConnectionEstablished()
    {
        Serial.println("RemoteGpio::onMqttConnectionEstablished");
        if (mqttCallbacksAreRegistered)
        {
            Serial.println("Reconnection -> nothing to do.");
            return;
        }

        mqttClient->subscribe(getBaseTopic() + "/gpio/" + String(deviceIndex), [&](const String &json)
                              { handlePayload(json); });
    }

    int RemoteGpio::getGpioPin() const
    {
        return pinGpio;
    }

    int RemoteGpio::readDigitalValue() const
    {
        return digitalRead(pinGpio);
    }

    void RemoteGpio::handlePayload(const String &rawData)
    {
        Serial.println("Remote GPIO RawData: " + rawData);
        String payload(rawData);
        payload.trim();
        payload.toUpperCase();

        if (payload == "0" || payload == "LOW" || payload == "OFF")
        {
            digitalWrite(pinGpio, LOW);
            Serial.println("digitalWrite Pin " + String(pinGpio) + " LOW");
        }
        else if (payload == "1" || payload == "HIGH" || payload == "ON")
        {
            digitalWrite(pinGpio, HIGH);
            Serial.println("digitalWrite Pin " + String(pinGpio) + " HIGH");
        }
        else if (payload.startsWith("TOGGLE"))
        {
            int currentState = readDigitalValue();
            if (LOW == currentState)
            {
                digitalWrite(pinGpio, HIGH);
            }
            else
            {
                digitalWrite(pinGpio, LOW);
            }
        }
    }
}

#endif // USE_REMOTE_GPIOS