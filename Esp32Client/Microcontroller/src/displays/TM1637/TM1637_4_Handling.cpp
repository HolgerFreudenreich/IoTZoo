// --------------------------------------------------------------------------------------------------------------------
//      ____    ______   _____
//     /  _/___/_  __/  /__  / ____  ____
//     / // __ \/ /       / / / __ \/ __ \  P L A Y G R O U N D
//   _/ // /_/ / /       / /_/ /_/ / /_/ /
//  /___/\____/_/       /____|____/\____/   (c) 2025 - 2026 Holger Freudenreich under the MIT licence.
//
// --------------------------------------------------------------------------------------------------------------------
// Firmware for ESP8266 and ESP32 Microcontrollers
// --------------------------------------------------------------------------------------------------------------------
#include "Defines.hpp"
#ifdef USE_TM1637_4
#include "./displays/TM1637/TM1637_4_Handling.hpp"

namespace IotZoo
{
    TM1637_4_Handling::TM1637_4_Handling() : TM1637_Handling(Tm1637DisplayType::Digits4)
    {
    }

    void TM1637_4_Handling::onMqttConnectionEstablished(MqttClient* mqttClient, const String& baseTopic)
    {
        Serial.println("TM1637_4_Handling::onMqttConnectionEstablished");
        if (callbacksAreRegistered)
        {
            Serial.println("Reconnection -> nothing to do.");
            return;
        }

        this->mqttClient = mqttClient;
        if (nullptr != mqttClient)
        {
            Serial.println("MQTT client is available. Registering callbacks for TM1637_4_Handling...");

            for (auto& display : displays1637)
            {
                Serial.println("Display " + String(display.getDeviceIndex()) + " has " + String(display.getDataSources().size()) + " data sources." + " Base topic: " + baseTopic);
                for (auto& dataSource : display.getDataSources())
                {
                    Serial.println("!!!Subscribing to topic: " + dataSource.Topic + ", method: " + dataSource.Method);
                    if (dataSource.Method == "number")
                    {
                        mqttClient->subscribe(dataSource.Topic, callbackMqttOnReceivedDataTm1637Number);
                    }
                    else if (dataSource.Method == "text")
                    {
                        mqttClient->subscribe(dataSource.Topic, callMqttbackOnReceivedDataTm1637Text);
                    }
                    else if (dataSource.Method == "level")
                    {
                        mqttClient->subscribe(dataSource.Topic, callbackMqttOnReceivedDataTm1637Level);
                    }
                    else
                    {
                        Serial.println("Unknown method: " + dataSource.Method);
                    }
                }
            }

            for (auto& display : displays1637)
            {
                String topicTm1637 = baseTopic + "/tm1637_4/" + String(display.getDeviceIndex()) + "/number";
                mqttClient->subscribe(topicTm1637, callbackMqttOnReceivedDataTm1637Number);
            }

            for (auto& display : displays1637)
            {
                String topicTm1637 = baseTopic + "/tm1637_4/" + String(display.getDeviceIndex()) + "/time";
                mqttClient->subscribe(topicTm1637, callbackMqttOnReceivedDataTm1637Number);
            }

            for (auto& display : displays1637)
            {
                String topicTm1637 = baseTopic + "/tm1637_4/" + String(display.getDeviceIndex()) + "/text";
                mqttClient->subscribe(topicTm1637, callMqttbackOnReceivedDataTm1637Text);
            }

            for (auto& display : displays1637)
            {
                String topicTm1637 = baseTopic + "/tm1637_4/" + String(display.getDeviceIndex()) + "/level";
                mqttClient->subscribe(topicTm1637, callbackMqttOnReceivedDataTm1637Level);
            }
        }
        Serial.println(".");
        callbacksAreRegistered = true;
    }
} // namespace IotZoo
#endif // USE_TM1637_4