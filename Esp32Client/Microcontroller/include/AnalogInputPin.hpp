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

#ifdef USE_ANALOG_INPUT_PIN

#pragma once

#include "DeviceBase.hpp"

namespace IotZoo
{
    // Use 3.3 Volt as reference voltage! 5.0 Volt will damage the ESP32 ADC pin!
    class AnalogInputPin : public DeviceBase
    {
      public:
        AnalogInputPin(int deviceIndex, IotZoo::Settings* const settings, IotZoo::MqttClient* const mqttClient, const String& baseTopic,
                       uint8_t pinAdc, uint32_t intervalMs)
            : DeviceBase(deviceIndex, settings, mqttClient, baseTopic)
        {
            this->pinAdc = pinAdc;
            this->intervalMs = intervalMs;
            if (pinAdc == 0)
            {
                this->pinAdc = 35; // Fallback ADC Pin
                Serial.println("Warning: pinAdc was 0, set to default pin 35.");
            }
            
            analogSetPinAttenuation(this->pinAdc, ADC_11db);
            analogReadResolution(12); // 0..4095

            Serial.println("Constructor AnalogInputPin pinAdc: " + String(pinAdc));
        }

        ~AnalogInputPin() override
        {
            Serial.println("Destructor UV Sensor on Pin " + String(pinAdc));
        }

        /// @brief Let the user know what the device can do.
        /// @param topics
        void addMqttTopicsToRegister(std::vector<IotZoo::Topic>* const topics) const override
        {
            topics->emplace_back(getBaseTopic() + "/volt/" + String(deviceIndex), "3.3", MessageDirection::IotZooClientInbound);
        }

        void loop() override
        {
            if (millis() - lastLoopMillis > intervalMs)
            {
                float analogSignal = analogRead(pinAdc);
                float voltage      = analogSignal * 3.3 / 4095.0;

                Serial.print("Signal: ");
                Serial.println(analogSignal);
                Serial.print("Volt: ");
                Serial.println(voltage, 3);
                mqttClient->publish(getBaseTopic() + "/volt/" + String(deviceIndex), String(voltage, 3U));

                lastLoopMillis = millis();
            }            
        }

      protected:
        uint8_t       pinAdc;
        uint32_t      intervalMs;
        float         uvIndex;
        unsigned long lastLoopMillis = 0;
    };

} // namespace IotZoo
#endif // USE_ANALOG_INPUT_PIN