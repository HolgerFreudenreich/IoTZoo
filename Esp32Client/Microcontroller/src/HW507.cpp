// --------------------------------------------------------------------------------------------------------------------
//      ____    ______   _____
//     /  _/___/_  __/  /__  / ____  ____
//     / // __ \/ /       / / / __ \/ __ \
//   _/ // /_/ / /       / /_/ /_/ / /_/ /
//  /___/\____/_/       /____|____/\____/ (c) 2025 - 2026 Holger Freudenreich under the MIT licence.
// --------------------------------------------------------------------------------------------------------------------
// Connect a HW-507 humidity Sensor with a microcontrollers in a simple way.
// D – Digital
// H – Humidity
// T – Temperature
// --------------------------------------------------------------------------------------------------------------------
#include "Defines.hpp"
#ifdef USE_HW507

#include "HW507.hpp"

namespace IotZoo
{
    HW507::HW507(int deviceIndex, Settings* const settings, MqttClient* const mqttClient, const String& baseTopic, uint8_t deviceType,
                 uint8_t pinData, uint16_t intervalMs)
        : DeviceBase(deviceIndex, settings, mqttClient, baseTopic)
    {
        this->intervalMs = intervalMs;
        Serial.print("Constructor HW507, deviceType: " + String(deviceType) + ", dataPin: " + String(pinData) +
                     ", intervalMs: " + String(intervalMs));
        dht = new DHT(pinData, deviceType);
    }

    void HW507::addMqttTopicsToRegister(std::vector<Topic>* const topics) const
    {
        String topic = getBaseTopic() + "/dht/" + this->getHumiditySensorType() + "/humidity";
        topics->emplace_back(topic, String(44), MessageDirection::IotZooClientInbound);
    }

    void HW507::onMqttConnectionEstablished()
    {
        Serial.println("HW507::onMqttConnectionEstablished");
        DeviceBase::onMqttConnectionEstablished();
    }

    void HW507::loop()
    {
        String topic    = getBaseTopic() + "/dht/" + this->getHumiditySensorType() + "/humidity";
        float  humidity = dht->readHumidity();
        if (millis() - lastMillis > intervalMs)
        {
            if (humidity > 0 && humidity < 100)
            {
                mqttClient->publish(topic, String(humidity, 1));
            }
            lastMillis = millis();
        }
    }
} // namespace IotZoo

#endif // USE_HW507