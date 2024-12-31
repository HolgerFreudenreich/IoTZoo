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
#ifdef USE_HW040
#include "HW040/HW040Handling.hpp"
#include "HW040/HW040.hpp"
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
    void HW040Handling::addMqttTopicsToRegister(std::vector<Topic> *const topics) const
    {
        for (auto &rotaryEncoder : HW040Helper::rotaryEncoders)
        {
            rotaryEncoder.addMqttTopicsToRegister(topics);
        }
    }

    /// @brief The MQTT connection is established. Now subscribe to the topics. An existing MQTT connection is a prerequisite for a subscription.
    /// @param mqttClient
    /// @param baseTopic
    void HW040Handling::onMqttConnectionEstablished(MqttClient *mqttClient, const String &baseTopic)
    {
        Serial.println("HW040Handling::onMqttConnectionEstablished");
        return;
        if (callbacksAreRegistered)
        {
            Serial.println("Reconnection -> nothing to do.");
            return;
        }
        for (auto &rotaryEncoder : HW040Helper::rotaryEncoders)
        {
            rotaryEncoder.onMqttConnectionEstablished();
        }
        callbacksAreRegistered = true;
    }

    void HW040Handling::addDevice(MqttClient *mqttClient, const String &baseTopic, int deviceIndex,
                                  int boundaryMinValue,
                                  int boundaryMaxValue,
                                  bool circleValues,
                                  int acceleration,
                                  uint8_t encoderSteps,
                                  uint8_t encoderAPin,
                                  uint8_t encoderBPin,
                                  int encoderButtonPin,
                                  int encoderVccPin)
    {
        RotaryEncoder *rotaryEncoder = new IotZoo::RotaryEncoder(mqttClient, deviceIndex, baseTopic,
                                                                 boundaryMinValue,
                                                                 boundaryMaxValue,
                                                                 circleValues,
                                                                 acceleration,
                                                                 encoderSteps,
                                                                 encoderAPin, encoderBPin, encoderButtonPin, encoderVccPin);
        HW040Helper::rotaryEncoders.push_back(*rotaryEncoder);
    }

    void HW040Handling::loop()
    {
        for (auto &rotaryEncoder : HW040Helper::rotaryEncoders)
        {
            rotaryEncoder.loop();
        }
    }
}

#endif // USE_HW040