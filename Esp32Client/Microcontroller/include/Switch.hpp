// --------------------------------------------------------------------------------------------------------------------
//      ____    ______   _____
//     /  _/___/_  __/  /__  / ____  ____
//     / // __ \/ /       / / / __ \/ __ \  P L A Y G R O U N D
//   _/ // /_/ / /       / /_/ /_/ / /_/ /
//  /___/\____/_/       /____|____/\____/   (c) 2025 Holger Freudenreich under the MIT licence.
//
// --------------------------------------------------------------------------------------------------------------------
// Simple switch (on/off).
// --------------------------------------------------------------------------------------------------------------------
#include "Defines.hpp"
#ifdef USE_SWITCH
#ifndef __SWITCH_HPP__
#define __SWITCH_HPP__

#include "Arduino.h"
#include "DeviceBase.hpp"

namespace IotZoo
{
    class Switch : public DeviceBase
    {
      private:
        uint8_t pin;
        bool    isButtonPressed       = false;
        bool    oldIsButtonPressed    = false;
        bool    buttonStateHasChanged = false;

      public:
        Switch(int deviceIndex, Settings* const settings, MqttClient* const mqttClient, const String& baseTopic, uint8_t pin);

        ~Switch() override;

        /// @brief Let the user know what the device can do.
        /// @param topics
        void addMqttTopicsToRegister(std::vector<Topic>* const topics) const override;

        uint8_t getPin() const;

        bool isPressed() const;

        bool hasStateChanged();

        void loop() override;
    };
} // namespace IotZoo

#endif // __SWITCH_HPP__
#endif // USE_SWITCH