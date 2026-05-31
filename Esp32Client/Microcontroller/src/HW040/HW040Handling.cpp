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
#ifdef USE_HW040
#include "HW040/HW040.hpp"
#include "HW040/HW040Handling.hpp"
#include "HW040/HW040Helper.hpp"

namespace IotZoo
{
    HW040Handling::HW040Handling() : DeviceHandlingBase()
    {
        Serial.println("Constructor HW040Handling");
    }

    void HW040Handling::setup()
    {
    }

    /// @brief Let the user know what the device can do.
    /// @param topics
    void HW040Handling::addMqttTopicsToRegister(std::vector<Topic>* const topics) const
    {
        for (auto& rotaryEncoder : HW040Helper::rotaryEncoders)
        {
            rotaryEncoder.addMqttTopicsToRegister(topics);
        }
    }

    /// @brief The MQTT connection is established. Now subscribe to the topics. An existing MQTT connection is a prerequisite for a subscription.
    /// @param mqttClient
    /// @param baseTopic
    void HW040Handling::onMqttConnectionEstablished(MqttClient* mqttClient, const String& baseTopic)
    {
        Serial.println("HW040Handling::onMqttConnectionEstablished");
        if (callbacksAreRegistered)
        {
            Serial.println("Reconnection -> nothing to do.");
            return;
        }
        for (auto& rotaryEncoder : HW040Helper::rotaryEncoders)
        {
            rotaryEncoder.onMqttConnectionEstablished();
        }
        callbacksAreRegistered = true;
    }

    DeviceBase& HW040Handling::addDevice(int deviceIndex, Settings* const settings, MqttClient* mqttClient, const String& baseTopic,
                                         int boundaryMinValue, int boundaryMaxValue, bool circleValues, int acceleration, uint8_t encoderSteps,
                                         uint8_t encoderAPin, uint8_t encoderBPin, int encoderButtonPin, int encoderVccPin)
    {

        DeviceBase& device =
            HW040Helper::rotaryEncoders.emplace_back(deviceIndex, settings, mqttClient, baseTopic, boundaryMinValue, boundaryMaxValue, circleValues,
                                                     acceleration, encoderSteps, encoderAPin, encoderBPin, encoderButtonPin, encoderVccPin);
        return device;
    }

    void HW040Handling::loop()
    {
        for (auto& rotaryEncoder : HW040Helper::rotaryEncoders)
        {
            rotaryEncoder.loop();
        }

    }
} // namespace IotZoo

#ifdef USE_INTERNAL_MQTT

static void onInternalReceivedData(const InternalMqttClient* /* srce */, const InternalTopic& topic, const char* payload, size_t /* length */)
{
    String strTopic = String(topic.c_str());
    /*
    if (strTopic.endsWith("/number"))
    {
        TM1637_Handling::callbackMqttOnReceivedDataTm1637Number(topic.c_str(), String(payload));
    }
    else if (strTopic.endsWith("/text"))
    {
        debug("Received internal MQTT message for text: " + String(payload));
        TM1637_Handling::callMqttbackOnReceivedDataTm1637Text(topic.c_str(), String(payload));
    }
    else if (strTopic.endsWith("/level"))
    {
        TM1637_Handling::callbackMqttOnReceivedDataTm1637Level(topic.c_str(), String(payload));
    }
    else if (strTopic.endsWith("/temperature"))
    {
        TM1637_Handling::callbackMqttOnReceivedDataTm1637Temperature(topic.c_str(), String(payload));
    }
    */
}

void IotZoo::HW040Handling::setInternalCallback(InternalMqttClient* const internalMqttClient)
{
    debug("Setting internal MQTT callback for HW040Handling... You need a callback and a subscription to receive internal MQTT messages.");

    if (internalMqttClient == nullptr)
    {
        Serial.println("Internal MQTT client is not available. Cannot set internal callbacks for TM1637_Handling.");
        return;
    }

    internalMqttClient->setCallback(onInternalReceivedData);

    for (auto& encoder : HW040Helper::rotaryEncoders)
    {
        encoder.setInternalMqttClient(internalMqttClient);
    }
}

// Will we have internal MQTT topics for the HW-040 rotary encoder? Maybe for a future feature, but currently we do not have any internal MQTT topics
// for the HW-040 rotary encoder, so we do not subscribe to any internal MQTT topics.
void IotZoo::HW040Handling::subscribeToInternalMqttTopics(InternalMqttClient* internalMqttClient, const String& baseTopic)
{
}
#endif // USE_INTERNAL_MQTT

#endif // USE_HW040