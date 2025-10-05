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
#ifdef USE_BUTTON
#ifndef __BUTTON_HPP__
#define __BUTTON_HPP__

#include "Arduino.h"
#include "DeviceBase.hpp"

namespace IotZoo
{
    class Button : public DeviceBase
    {
      protected:
        uint8_t       pin;
        uint16_t      counter;
        uint16_t      counterOld;
        unsigned long lastPressedMillis;
        String        topicButtonPushedCounter;
        String        topicButtonSetCounter;

      public:
        Button(int deviceIndex, MqttClient* const mqttClient, const String& baseTopic, uint8_t pin);

        ~Button() override;

        /// @brief interrupt is triggered.
        void IRAM_ATTR onInterruptTriggered()
        {
            // We do not know which of the buttons is pushed (the interrupt method is static and has no
            // parameters). so we have to read the button state and increment the counter of all pushed
            // buttons.
            if (millis() - lastPressedMillis > 50) // debounce
            {
                if (digitalRead(pin) == LOW)
                {
                    counter++;
                }
            }
            lastPressedMillis = millis();
        }

        void setup(void (*ISR_callback)(void))
        {
            Serial.println("attach interrupt for Button with deviceIndex " + String(deviceIndex) + ", pin: " + String(pin));
            attachInterrupt(pin, ISR_callback, RISING);
        }

        /// @brief Let the user know what the device can do.
        /// @param topics
        void addMqttTopicsToRegister(std::vector<Topic>* const topics) const override;

        /// @brief The MQTT connection is established. Now subscribe to the topics. An existing MQTT connection is
        /// a prerequisite for a subscription.
        /// @param mqttClient
        /// @param baseTopic
        void onMqttConnectionEstablished() override;

        uint8_t getPin() const;

        bool hasStateChanged();

        void loop() override;
    };
} // namespace IotZoo

#endif // __BUTTON_HPP__
#endif // USE_BUTTON