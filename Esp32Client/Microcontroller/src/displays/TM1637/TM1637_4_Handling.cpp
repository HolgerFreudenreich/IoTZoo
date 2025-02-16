// --------------------------------------------------------------------------------------------------------------------
//      ____    ______   _____
//     /  _/___/_  __/  /__  / ____  ____
//     / // __ \/ /       / / / __ \/ __ \  P L A Y G R O U N D
//   _/ // /_/ / /       / /_/ /_/ / /_/ /
//  /___/\____/_/       /____|____/\____/   (c) 2025 Holger Freudenreich under the MIT licence.
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
        if (NULL != mqttClient)
        {
            for (auto& display : displays1637)
            {
                String topicTm1637 = baseTopic + "/tm1637_4/" + String(display.getDeviceIndex()) + "/number";
                if (mqttClient->subscribe(topicTm1637, callbackMqttOnReceivedDataTm1637_Number))
                {
                    Serial.println("Subscribed: " + topicTm1637);
                }
            }

            for (auto& display : displays1637)
            {
                String topicTm1637 = baseTopic + "/tm1637_4/" + String(display.getDeviceIndex()) + "/time";
                if (mqttClient->subscribe(topicTm1637, callbackMqttOnReceivedDataTm1637_Number))
                {
                    Serial.println("Subscribed: " + topicTm1637);
                }
            }

            for (auto& display : displays1637)
            {
                String topicTm1637 = baseTopic + "/tm1637_4/" + String(display.getDeviceIndex()) + "/text";
                if (mqttClient->subscribe(topicTm1637, callMqttbackOnReceivedDataTm1637Text))
                {
                    Serial.println("Subscribed: " + topicTm1637);
                }
            }

            for (auto& display : displays1637)
            {
                String topicTm1637 = baseTopic + "/tm1637_4/" + String(display.getDeviceIndex()) + "/level";
                if (mqttClient->subscribe(topicTm1637, callbackMqttOnReceivedDataTm1637Level))
                {
                    Serial.println("Subscribed: " + topicTm1637);
                }
            }
        }
        Serial.println(".");
        callbacksAreRegistered = true;
    }
} // namespace IotZoo
#endif // USE_TM1637_4