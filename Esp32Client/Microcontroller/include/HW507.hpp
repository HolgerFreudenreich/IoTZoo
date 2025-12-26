// --------------------------------------------------------------------------------------------------------------------
//      ____    ______   _____
//     /  _/___/_  __/  /__  / ____  ____
//     / // __ \/ /       / / / __ \/ __ \
//   _/ // /_/ / /       / /_/ /_/ / /_/ /
//  /___/\____/_/       /____|____/\____/ (c) 2025 - 2026 Holger Freudenreich under the MIT licence.
// --------------------------------------------------------------------------------------------------------------------
// Connect a HW-507 humidity Sensor with a microcontrollers in a simple way.
// D – Digital
// H – Humidity (Luftfeuchtigkeit)
// T – Temperature (Temperatur)
// --------------------------------------------------------------------------------------------------------------------
#include "Defines.hpp"
#ifdef USE_HW507
#include "DHT.h"
#include "DeviceBase.hpp"
#include <Arduino.h>

namespace IotZoo
{
    class HW507 : public DeviceBase
    {
      public:
        HW507(int deviceIndex, Settings* const settings, MqttClient* const mqttClient, const String& baseTopic, uint8_t deviceType, uint8_t pinData);

        void addMqttTopicsToRegister(std::vector<Topic>* const topics) const;

        void onMqttConnectionEstablished() override;

        void loop();

        String getHumiditySensorType() const
        {
            String strDeviceType = "dht11";
            if (deviceType == DHT11)
            {
                // Humidity range 0% to 100%
                // Humidity accuracy ±5%
                strDeviceType = "dht11";
            }
            else if (deviceType == DHT22)
            {
                // Humidity range 0% to 100%
                // Humidity accuracy ±5%
                strDeviceType = "dht22";
            }
            return strDeviceType;
        }

      private:
        DHT*    dht        = nullptr;
        uint8_t deviceType = DHT11;
        ulong lastMillis = millis();
    };
} // namespace IotZoo

// namespace IotZoo

#endif // HW507