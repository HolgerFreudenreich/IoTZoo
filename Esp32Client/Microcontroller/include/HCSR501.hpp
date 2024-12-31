// --------------------------------------------------------------------------------------------------------------------
//      ____    ______   _____
//     /  _/___/_  __/  /__  / ____  ____
//     / // __ \/ /       / / / __ \/ __ \
//   _/ // /_/ / /       / /_/ /_/ / /_/ /
//  /___/\____/_/       /____|____/\____/ (c) 2025 Holger Freudenreich under the MIT licence.
// --------------------------------------------------------------------------------------------------------------------
// Connect up to three HC SR501 Motion detectors with microcontrollers in a simple way.
// --------------------------------------------------------------------------------------------------------------------
#include "Defines.hpp"
#ifdef USE_HC_SR501
#ifndef __HCSR501_HPP__
#define __HCSR501_HPP__

// --------------------------------------------------------------------------------------------------------------------
// Includes
// --------------------------------------------------------------------------------------------------------------------
#include <Arduino.h>
#include "DeviceBase.hpp"

namespace IotZoo
{
  class HCSC501 : public DeviceBase
  {
  protected:
    int index;

  public:
    HCSC501(int deviceIndex, MqttClient *const mqttClient, const String &baseTopic, uint8_t pinMotionDetector);

    virtual ~HCSC501();

    void setup(void (*ISR_callback)(void));

    void IRAM_ATTR isrMotionDetectorRising();

    /// @brief Let the user know what the device can do.
    /// @param topics
    virtual void addMqttTopicsToRegister(std::vector<Topic> *const topics) const override;

    int getIndex() const;

    void loop();

    bool isTriggered();

    unsigned long getCounterRising() const;

  protected:
    uint8_t pinMotionDetector;
    unsigned long lastMillisMotionDetectorRising = millis();
    unsigned long motionDetectorCounterRising = 0;
    unsigned long oldMotionDetectorCounterRising = lastMillisMotionDetectorRising;
  };
}

#endif // __HCSR501_HPP__
#endif // USE_HC_SR501