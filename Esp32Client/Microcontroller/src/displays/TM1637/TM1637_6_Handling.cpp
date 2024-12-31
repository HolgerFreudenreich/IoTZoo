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
#ifdef USE_TM1637_6
#include "./displays/TM1637/TM1637_6_Handling.hpp"
#include "./displays/TM1637/TM1637Helper.hpp"

namespace IotZoo
{
    TM1637_6_Handling::TM1637_6_Handling() : TM1637_Handling(Tm1637DisplayType::Digits6)
    {
    }

    void TM1637_6_Handling::onMqttConnectionEstablished(MqttClient *mqttClient, const String &baseTopic)
    {
        Serial.println("TM1637_6_Handling::onMqttConnectionEstablished");
        if (callbacksAreRegistered)
        {
            Serial.println("Reconnection -> nothing to do.");
            return;
        }

        this->mqttClient = mqttClient;
        if (NULL != mqttClient)
        {
            for (auto &display : displays1637)
            {
                // Can be integer and doube value.
                String topicTm1637 = baseTopic + "/tm1637_6/" + String(display.getDeviceIndex()) + "/number";
                mqttClient->subscribe(topicTm1637, callbackMqttOnReceivedDataTm1637_Number);
            }

            for (auto &display : displays1637)
            {
                String topicTm1637 = baseTopic + "/tm1637_6/" + String(display.getDeviceIndex()) + "/text";
                mqttClient->subscribe(topicTm1637, callMqttbackOnReceivedDataTm1637Text);
            }

            for (auto &display : displays1637)
            {
                String topicTm1637 = baseTopic + "/tm1637_6/" + String(display.getDeviceIndex()) + "/level";
                mqttClient->subscribe(topicTm1637, callbackMqttOnReceivedDataTm1637Level);
            }

            for (auto &display : displays1637)
            {
                String topicTm1637 = baseTopic + "/tm1637_6/" + String(display.getDeviceIndex()) + "/temperature";
                mqttClient->subscribe(topicTm1637, callMqttbackOnReceivedDataTm1637Temperature);
            }
        }
        Serial.println(".");
        callbacksAreRegistered = true;
    }

    /// @brief Data Reveived to display on a TM1637 4 digit display.
    /// @param rawData: data in json format or unformatted.
    void TM1637_6_Handling::callMqttbackOnReceivedDataTm1637Temperature(const String &topic, const String &message)
    {
        try
        {
            String t(message);
            t.trim();

            int indexDot = t.indexOf(".");
            if (indexDot > 0)
            {
                t = t.substring(0, indexDot + 2); // one decimal place
            }

            t += "°C";

            IotZoo::TM1637Helper tm1637Helper(t);

            if (t.length() == 5)
            {
                t = " " + t;
            }
            else if (t.length() == 4)
            {
                t = "  " + t;
            }

            int indexEnd = topic.lastIndexOf("/");
            Serial.println("indexEnd: " + String(indexEnd));
            if (indexEnd >= 0)
            {
                int deviceIndex = topic.c_str()[indexEnd - 1] - '0'; // at least 10 (0 - 9).
                Serial.println(deviceIndex);
                Serial.println(topic.c_str()[indexEnd - 1]);

                auto display = displays1637.begin();
                if (deviceIndex > 0)
                {
                    std::advance(display, deviceIndex);
                }
                display->setBrightness(0x0c, true); // 0x0f = max brightness. Do not delete this, the display may be turned off.
                display->showString(t.c_str(), 6U, 0, tm1637Helper.getDots());
            }
        }
        catch (const std::exception &e)
        {
        }
    }
}
#endif