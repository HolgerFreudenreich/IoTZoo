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

#include "ReedContactKY025.hpp"
#include <Arduino.h>

namespace IotZoo
{
    volatile unsigned long reedContactCounter = 0;
    volatile unsigned long lastRotationMillis = 0;
    volatile unsigned long diff               = 0;
    volatile unsigned long oldReedContactCounter = 0;

    double rpm = 0.0;

    void IRAM_ATTR isrKY025()
    {
        diff = millis() - lastRotationMillis;
        if (diff > 300.0)
        {
            digitalWrite(2, !digitalRead(2));
            reedContactCounter++;
        }
        lastRotationMillis = millis();
    }

    KY025::KY025(int deviceIndex, Settings* const settings, MqttClient* const mqttClient, const String& baseTopic, u16_t intervalMs, u8_t pinData)
        : DeviceBase(deviceIndex, settings, mqttClient, baseTopic)
    {
        Serial.println("Constructor KY025, intervalMs: " + String(intervalMs) + ", pinData: " + String(pinData));
        this->intervalMs = intervalMs;
        pinMode(pinData, INPUT_PULLUP);
        attachInterrupt(pinData, isrKY025, FALLING);
    }

    void KY025::addMqttTopicsToRegister(std::vector<Topic>* const topics) const
    {
        String topic = getBaseTopic() + "/reed_contact/" + String(getDeviceIndex()) + "/ppm";
        topics->emplace_back(topic, String(44.6), MessageDirection::IotZooClientInbound);
        topic = getBaseTopic() + "/reed_contact/" + String(getDeviceIndex()) + "/counter";
        topics->emplace_back(topic, String(44151), MessageDirection::IotZooClientInbound);
    }

    void KY025::onMqttConnectionEstablished()
    {
        Serial.println("KY025::onMqttConnectionEstablished");
        if (mqttCallbacksAreRegistered)
        {
            Serial.println("Reconnection -> nothing to do.");
            return;
        }
    }

    void KY025::loop()
    {
        if (millis() - lastMillis > intervalMs)
        {           
            if (oldReedContactCounter != reedContactCounter)
            {
                oldReedContactCounter = reedContactCounter;
                rpm                   = 1.0 / (diff / 60000.0);
                mqttClient->publish(getBaseTopic() + "/reed_contact/" + String(getDeviceIndex()) + "/ppm", String(rpm, 0));
                mqttClient->publish(getBaseTopic() + "/reed_contact/" + String(getDeviceIndex()) + "/counter", String(reedContactCounter));
                lastMillis = millis();
            }
            else
            {
                mqttClient->publish(getBaseTopic() + "/reed_contact/" + String(getDeviceIndex()) + "/ppm", "0");
                lastMillis = millis();
            }           
        }
    }

} // namespace IotZoo

#endif // USE_KY025