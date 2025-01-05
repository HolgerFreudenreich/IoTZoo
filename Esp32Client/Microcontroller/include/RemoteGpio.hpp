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
#ifdef USE_REMOTE_GPIOS
#ifndef __REMOTE_GPIO_HPP__
#define __REMOTE_GPIO_HPP__

#include <Arduino.h>
#include "DeviceBase.hpp"

namespace IotZoo
{
  class RemoteGpio : DeviceBase
  {
  protected:
    int pinGpio = -1;

  public:
    RemoteGpio(int deviceIndex, MqttClient *const mqttClient, const String &baseTopic,
               int pin);

    virtual ~RemoteGpio();

    /// @brief Let the user know what the device can do.
    /// @param topics
    virtual void addMqttTopicsToRegister(std::vector<Topic> *const topics) const override;

    /// @brief The MQTT connection is established. Now subscribe to the topics. An existing MQTT connection is a prerequisite for a subscription.
    /// @param mqttClient
    /// @param baseTopic
    virtual void onMqttConnectionEstablished() override;

    int getGpioPin() const;

    int readDigitalValue() const;

    void handlePayload(const String &rawData);
  };
}

#endif // __REMOTE_GPIO_HPP__
#endif // USE_REMOTE_GPIOS