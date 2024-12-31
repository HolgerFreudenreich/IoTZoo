// --------------------------------------------------------------------------------------------------------------------
//      ____    ______   _____
//     /  _/___/_  __/  /__  / ____  ____
//     / // __ \/ /       / / / __ \/ __ \
//   _/ // /_/ / /       / /_/ /_/ / /_/ /
//  /___/\____/_/       /____|____/\____/ (c) 2025 Holger Freudenreich under the MIT licence.
// --------------------------------------------------------------------------------------------------------------------
// Connect «Things» with microcontrollers in a simple way.
// --------------------------------------------------------------------------------------------------------------------
#include "Defines.hpp"
#ifdef USE_HW040
#ifndef __HW040_HPP__
#define __HW040_HPP__

#include "AiEsp32RotaryEncoder.h"
#include "Arduino.h"
#include <iostream>
#include <vector>
#include "DeviceBase.hpp"
#include "MqttClient.hpp"

using namespace std;

namespace IotZoo
{
  class RotaryEncoder : public AiEsp32RotaryEncoder, public DeviceBase
  {
  public:
    RotaryEncoder(MqttClient *mqttClient, int deviceIndex, const String &baseTopic,
                  int boundaryMinValue,
                  int boundaryMaxValue,
                  bool circleValues,
                  int acceleration,
                  uint8_t encoderSteps,
                  uint8_t encoderAPin,
                  uint8_t encoderBPin,
                  int encoderButtonPin,
                  int encoderVccPin);

    virtual ~RotaryEncoder();

    /// @brief Let the user know what the device can do.
    /// @param topics
    virtual void addMqttTopicsToRegister(std::vector<Topic> *const topics) const override;

    void onReceivedRotaryEncoderValue(const String &strValue);

    /// @brief The MQTT connection is established. Now subscribe to the topics. An existing MQTT connection is a prerequisite for a subscription.
    /// @param mqttClient
    /// @param baseTopic
    virtual void onMqttConnectionEstablished() override;

    virtual void loop() override;

    void setLastTimeButtonDown(unsigned long lastTimeButtonDown);

    unsigned long getLastTimeButtonDown() const;

    void setWasButtonDown(bool wasButtonDown);

    bool getWasButtonDown() const;

  protected:
    unsigned long lastTimeButtonDown;
    bool wasButtonDown;
    String topicEncoderValue;
  };
}

#endif // __HW040_HPP__
#endif // USE_HW040