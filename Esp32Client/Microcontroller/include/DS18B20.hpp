// --------------------------------------------------------------------------------------------------------------------
//      ____    ______   _____
//     /  _/___/_  __/  /__  / ____  ____
//     / // __ \/ /       / / / __ \/ __ \
//   _/ // /_/ / /       / /_/ /_/ / /_/ /
//  /___/\____/_/       /____|____/\____/ (c) 2025 - 2026 Holger Freudenreich under the MIT licence.
// --------------------------------------------------------------------------------------------------------------------
// Connect «Things» with microcontrollers in a simple way.
// --------------------------------------------------------------------------------------------------------------------
// Connecting 1..64 temperature Sensors DS18B20
// --------------------------------------------------------------------------------------------------------------------
#include "Defines.hpp"
#ifdef USE_DS18B20
#ifndef __DS18B20_HPP__
#define __DS18B20_HPP__

#include <DallasTemperature.h>
#include <OneWire.h>
#include <list>

namespace IotZoo
{
    class DS18B20
    {
      protected:
        OneWire*           oneWire                  = nullptr;
        DallasTemperature* dallasTemperatureSensors = nullptr;

        int numberOfDevices = 0;
        int interval        = 0; // 0 = ASAP

        // function to print a device address
        void printDeviceAddress(DeviceAddress deviceAddress);

        long lastPublishedTemperatureMillis = millis();

      public:
        DS18B20();

        virtual ~DS18B20();

        /// @brief
        /// @param gpioNumber GPIO where the DS18B20 is connected to.
        /// @param resolution resolution of a device to 9, 10, 11, or 12 bits.
        /// @param transmissionIntervalMs Interval at which the temperatures are sent via MQTT.
        void setup(int gpioNumber, u_int8_t resolution, int transmissionIntervalMs);

        std::vector<float> requestTemperatures();

        /// @brief Sets the interval at which the temperature is sent
        /// @param interval in milliseconds
        void setInterval(int intervalMs)
        {
            this->interval = intervalMs;
        }

        int getInterval() const
        {
            return interval;
        }

        long getLastPublishedTemperatureMillis() const
        {
            return lastPublishedTemperatureMillis;
        }

        void setLastPublishedTemperatureMillis(long lastPublishedMillis)
        {
            lastPublishedTemperatureMillis = lastPublishedMillis;
        }
    };
} // namespace IotZoo
#endif // __DS18B20_HPP__
#endif // USE_DS18B20