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

#ifndef __HW040_HANDLING_HPP__
#define __HW040_HANDLING_HPP__

#include "DeviceHandlingBase.hpp"
#include "HW040/HW040.hpp"
#include "HW040/HW040Helper.hpp"

namespace IotZoo
{
  class HW040Handling : public DeviceHandlingBase
  {
  public:
    HW040Handling();

    void setup();

    /// @brief Let the user know what the device can do.
    /// @param topics
    void addMqttTopicsToRegister(std::vector<Topic> *const topics) const;

    /// @brief The MQTT connection is established. Now subscribe to the topics. An existing MQTT connection is a prerequisite for a subscription.
    /// @param mqttClient
    /// @param baseTopic
    void onMqttConnectionEstablished(MqttClient *mqttClient, const String &baseTopic);

    void addDevice(int deviceIndex, Settings* const settings, MqttClient *mqttClient, const String &baseTopic,
                   int boundaryMinValue,
                   int boundaryMaxValue,
                   bool circleValues,
                   int acceleration,
                   uint8_t encoderSteps,
                   uint8_t encoderAPin,
                   uint8_t encoderBPin,
                   int encoderButtonPin,
                   int encoderVccPin);
    void loop();
  };
}

#endif // __HW040_HANDLING_HPP__
#endif // USE_HW040