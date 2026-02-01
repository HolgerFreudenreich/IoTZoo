// --------------------------------------------------------------------------------------------------------------------
//      ____    ______   _____
//     /  _/___/_  __/  /__  / ____  ____
//     / // __ \/ /       / / / __ \/ __ \
//   _/ // /_/ / /       / /_/ /_/ / /_/ /
//  /___/\____/_/       /____|____/\____/ (c) 2025 - 2026 Holger Freudenreich under the MIT licence.
// --------------------------------------------------------------------------------------------------------------------
// Connect «Things» with microcontrollers in a simple way.
// --------------------------------------------------------------------------------------------------------------------
// Ultra violette sensor GUVAS12SD
// --------------------------------------------------------------------------------------------------------------------
#include "Defines.hpp"

#ifdef USE_UV

#pragma once

#include "DeviceBase.hpp"

namespace IotZoo
{
    class UvSensorGUVAS12SD : public DeviceBase
    {
      public:
        UvSensorGUVAS12SD(int deviceIndex, IotZoo::Settings* const settings, IotZoo::MqttClient* const mqttClient, const String& baseTopic,
                          uint8_t pinAdc)
            : DeviceBase(deviceIndex, settings, mqttClient, baseTopic)
        {
            this->pinAdc = pinAdc;
            if (pinAdc == 0)
            {
                this->pinAdc = 35; // Fallback ADC Pin
                Serial.println("Warning: pinAdc was 0, set to default pin 35.");
            }

            analogSetPinAttenuation(this->pinAdc, ADC_11db);
            analogReadResolution(12); // 0..4095

            Serial.println("Constructor UV Sensor pinAdc: " + String(pinAdc));
        }

        ~UvSensorGUVAS12SD() override
        {
            Serial.println("Destructor UV Sensor on Pin " + String(pinAdc));
        }

        /// @brief Let the user know what the device can do.
        /// @param topics
        void addMqttTopicsToRegister(std::vector<IotZoo::Topic>* const topics) const override
        {
            topics->emplace_back(getBaseTopic() + "/uv/" + String(deviceIndex), "3.4", MessageDirection::IotZooClientInbound);
        }

        void loop() override
        {
            if (millis() - lastLoopMillis > 5000)
            {
                float analogSignal = analogRead(pinAdc);
                float voltage      = analogSignal * 3.3 / 4095.0;
                uvIndex            = voltage / 0.1;

                Serial.print("Signal: ");
                Serial.println(analogSignal);
                Serial.print("Volt: ");
                Serial.println(voltage, 3);
                Serial.print("UV-Index: ");
                Serial.println(uvIndex);
                mqttClient->publish(getBaseTopic() + "/uv/" + String(deviceIndex), String(uvIndex, 3U));
                lastLoopMillis = millis();
            }
        }

      protected:
        uint8_t       pinAdc;
        float         uvIndex;
        unsigned long lastLoopMillis = 0;
    };

} // namespace IotZoo
#endif // USE_UV