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
#ifdef USE_TRAFFIC_LIGHT_LEDS

#ifndef __TRAFFIC_LIGHT_HPP__
#define __TRAFFIC_LIGHT_HPP__

#include "DeviceBase.hpp"

namespace IotZoo
{
  class TrafficLight : DeviceBase
  {
  private:
    u_int8_t pinRedLed = 0;
    u_int8_t pinYellowLed = 0;
    u_int8_t pinGreenLed = 0;

  public:
    TrafficLight(int deviceIndex, MqttClient *const mqttClient, const String &baseTopic,
                 u_int8_t pinRedLed, u_int8_t pinYellowLed, u_int8_t pinGreenLed);

    virtual ~TrafficLight();

    void handleTrafficLightPayload(const String &payload);

    /// @brief Let the user know what the device can do.
    /// @param topics
    virtual void addMqttTopicsToRegister(std::vector<Topic> *const topics) const override;

    /// @brief The MQTT connection is established. Now subscribe to the topics. An existing MQTT connection is a prerequisite for a subscription.
    /// @param mqttClient
    /// @param baseTopic
    virtual void onMqttConnectionEstablished() override;
  };
}

#endif // __TRAFFIC_LIGHT_HPP__
#endif // USE_TRAFFIC_LIGHT_LEDS
