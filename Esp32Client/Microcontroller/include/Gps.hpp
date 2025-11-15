// --------------------------------------------------------------------------------------------------------------------
//      ____    ______   _____
//     /  _/___/_  __/  /__  / ____  ____
//     / // __ \/ /       / / / __ \/ __ \
//   _/ // /_/ / /       / /_/ /_/ / /_/ /
//  /___/\____/_/       /____|____/\____/ (c) 2025 Holger Freudenreich under the MIT licence.
// --------------------------------------------------------------------------------------------------------------------
// Connect «Things» with microcontrollers in a simple way.
// --------------------------------------------------------------------------------------------------------------------
// GPS support
// --------------------------------------------------------------------------------------------------------------------
#include "Defines.hpp"

#ifdef USE_GPS
#ifndef __GPS_HPP__
#define __GPS_HPP__

#include "DeviceBase.hpp"
#include "TinyGPSPlus.h"

#include <SoftwareSerial.h>

namespace IotZoo
{
    class Gps : public DeviceBase
    {
      protected:
        uint8_t pinRx;
        uint8_t pinTx;

        TinyGPSPlus     gps;
        SoftwareSerial* softwareSerial;

      public:
        Gps(int deviceIndex, Settings* const settings, MqttClient* const mqttClient, const String& baseTopic,
          uint8_t pinRx, uint8_t pinTx, uint32_t baud = 9600);
        ~Gps() override;

        /// @brief Let the user know what the device can do.
        /// @param topics
        void addMqttTopicsToRegister(std::vector<Topic>* const topics) const;

        void loop();

        void smartDelay(unsigned long ms);
    };
} // namespace IotZoo

#endif // __GPS_HPP__
#endif // USE_GPS