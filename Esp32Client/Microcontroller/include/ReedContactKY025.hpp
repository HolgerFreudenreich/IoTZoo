// --------------------------------------------------------------------------------------------------------------------
//      ____    ______   _____
//     /  _/___/_  __/  /__  / ____  ____
//     / // __ \/ /       / / / __ \/ __ \  P L A Y G R O U N D
//   _/ // /_/ / /       / /_/ /_/ / /_/ /
//  /___/\____/_/       /____|____/\____/   (c) 2025 - 2026 Holger Freudenreich under the MIT licence.
//
// --------------------------------------------------------------------------------------------------------------------
// ReedContact
// --------------------------------------------------------------------------------------------------------------------

#include "Defines.hpp"
#ifdef USE_KY025

#ifndef __READCONTACT_KY025_HPP__
#define __READCONTACT_KY025_HPP__

#include "DeviceBase.hpp"

namespace IotZoo
{
    class KY025 : DeviceBase
    {
      public:
        KY025(int deviceIndex, Settings* const settings, MqttClient* const mqttClient, const String& baseTopic, u16_t intervalMs, u8_t pinData);

        void loop() override;

        void addMqttTopicsToRegister(std::vector<Topic>* const topics) const override;

        void onMqttConnectionEstablished() override;

      private:      
        u16_t intervalMs;
        unsigned long lastMillis = millis();
    };
} // namespace IotZoo

#endif // __READCONTACT_KY025_HPP__

#endif // USE_KY025
