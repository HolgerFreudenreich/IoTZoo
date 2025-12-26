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

#ifdef USE_BUZZER
#ifndef __BUZZER_HPP__
#define __BUZZER_HPP__

#include "Arduino.h"
#include "Buzzer.h"
#include "DeviceBase.hpp"

namespace IotZoo
{
    class Buzzer : public DeviceBase
    {
      protected:
        ::Buzzer* buzzer;
        String    topicBeep;

      public:
        Buzzer(int deviceIndex, Settings* const settings, MqttClient* const mqttClient, const String& baseTopic, 
          uint8_t pinBuzzer, uint8_t pinLed);

        ~Buzzer() override;

        /// @brief Let the user know what the device can do.
        /// @param topics
        void addMqttTopicsToRegister(std::vector<Topic>* const topics) const override;

        /// @brief The MQTT connection is established. Now subscribe to the topics. An existing MQTT connection is a
        /// prerequisite for a subscription.
        /// @param mqttClient
        /// @param baseTopic
        void onMqttConnectionEstablished() override;

        String toString();

        void beep(u_int16_t frequencyHz, u_int16_t durationMs);
    };
} // namespace IotZoo
#endif // USE_BUZZER
#endif // __BUZZER_HPP__