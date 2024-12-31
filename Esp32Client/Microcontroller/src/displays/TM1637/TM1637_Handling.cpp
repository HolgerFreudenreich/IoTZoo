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
#if defined(USE_TM1637_4) || defined(USE_TM1637_6)
#include "./displays/TM1637/TM1637_Handling.hpp"
#include "./displays/TM1637/TM1637Helper.hpp"

namespace IotZoo
{
    TM1637_Handling::TM1637_Handling(Tm1637DisplayType tm1637DisplayType) : DeviceHandlingBase()
    {
        this->tm1637DisplayType = tm1637DisplayType;
    }

    void TM1637_Handling::setup()
    {
        int index = 0;
        for (auto &display : displays1637)
        {
            display.clear();
            display.setBrightness(0x0A, true); // 0x0f = max brightness
            String text = String(index);
            display.showString(text.c_str());
            index++;
        }
    }

    void TM1637_Handling::onIotZooClientUnavailable()
    {
        for (auto &display : displays1637)
        {
            display.onIotZooClientUnavailable();
        }
    }

    void TM1637_Handling::addMqttTopicsToRegister(std::vector<Topic> *const topics) const
    {
        for (auto &display : displays1637)
        {
            display.addMqttTopicsToRegister(topics);
        }
    }

    /// @brief Data received to display on a TM1637 display.
    /// @param rawData: data in json format or unformatted.
    void TM1637_Handling::onReceivedDataTm1637_Number(const String &rawData, int deviceIndex)
    {
        Serial.println("onReceivedDataTm1637_Number " + rawData);

        TM1637 *display = getDisplayByDeviceIndex(deviceIndex);

        if (NULL != display)
        {
            int index = 0;

            String data(rawData);
            data.trim();
            data.toLowerCase();

            bool containsColon = false;
            if (data[0] != '{' && data.indexOf(":") > 0)
            {
                containsColon = true;
                data.replace(":", ""); // needed to display Time like 10:23
            }

            int number = 0;
            bool showLeadingZeros = false;
            int displayLength = display->getDefaultDisplayLength();
            int position = 0;
            int dots = 0;

            if (containsColon)
            {
                dots = 64; // 01000000
            }
            else
            {
                IotZoo::TM1637Helper tm1637Helper(data);
                dots = tm1637Helper.getDots();
            }

            display->setBrightness(0x0A, true); // 0x0f = max brightness. Do not delete this, the display may be turned off.

            try
            {
                number = std::stoi(data.c_str());
            }
            catch (const std::exception &e)
            {
                Serial.println("Unable to convert to a number!");
            }

            Serial.println("device index: " + String(deviceIndex) + "; number: " + String(number) + "; LeadingZeros: " + String(showLeadingZeros) + "; displayLength: " +
                           String(displayLength) + "; position: " + String(position) + "; dots: " + String(dots));
            display->showNumberDec(number, dots, showLeadingZeros, displayLength, position);
        }
    }

    void TM1637_Handling::callbackMqttOnReceivedDataTm1637_Number(const String &topic, const String &message)
    {
        Serial.println("callbackMqttOnReceivedDataTm1637_Number topic: " + topic + " message: " + message);

        int indexEnd = topic.lastIndexOf("/");
        Serial.println("indexEnd: " + String(indexEnd));
        if (indexEnd >= 0)
        {
            int deviceIndex = topic.c_str()[indexEnd - 1] - '0'; // at least 10 (0 - 9).
            Serial.println(deviceIndex);
            Serial.println(topic.c_str()[indexEnd - 1]);
            onReceivedDataTm1637_Number(message, deviceIndex);
        }
    }

    void TM1637_Handling::callMqttbackOnReceivedDataTm1637Text(const String &topic, const String &message)
    {
        Serial.println("callMqttbackOnReceivedDataTm1637Text topic: " + topic + " message: " + message);

        int indexEnd = topic.lastIndexOf("/");
        Serial.println("indexEnd: " + String(indexEnd));
        if (indexEnd >= 0)
        {
            int deviceIndex = topic.c_str()[indexEnd - 1] - '0'; // at least 10 (0 - 9).
            Serial.println(deviceIndex);
            Serial.println(topic.c_str()[indexEnd - 1]);

            TM1637 *display = getDisplayByDeviceIndex(deviceIndex);

            if (NULL != display)
            {
                display->setBrightness(0x0A, true); // 0x0f = max brightness. Do not delete this, the display may be turned off.
                Serial.println(message);
                display->showString(message.c_str(), display->getDefaultDisplayLength());
            }
        }
    }

    TM1637 *TM1637_Handling::getDisplayByDeviceIndex(int index)
    {
        for (auto &display : displays1637)
        {
            if (display.getDeviceIndex() == index)
            {
                return &display;
            }
        }
        Serial.println("TM1637 display with index " + String(index) + " not found!");
        return NULL;
    }

    /// @brief Incoming MqttMessage to indicate a level between 0 and 100.
    /// @param topic
    /// @param message
    void TM1637_Handling::callbackMqttOnReceivedDataTm1637Level(const String &topic, const String &message)
    {
        Serial.println("callbackMqttOnReceivedDataTm1637Level topic: " + topic + " message: " + message);

        int indexEnd = topic.lastIndexOf("/");
        Serial.println("indexEnd: " + String(indexEnd));
        if (indexEnd >= 0)
        {
            int deviceIndex = topic.c_str()[indexEnd - 1] - '0'; // at least 10 (0 - 9).
            Serial.println(deviceIndex);
            Serial.println(topic.c_str()[indexEnd - 1]);

            TM1637 *display = getDisplayByDeviceIndex(deviceIndex);
            if (NULL != display)
            {
                display->setBrightness(0x0A, true); // 0x0f = max brightness. Do not delete this, the display may be turned off before.
                int level = 0;
                try
                {
                    level = std::stoi(message.c_str());
                }
                catch (const std::exception &e)
                {
                    Serial.println("Unable to convert to a number!");
                }

                display->showLevel(level, false);
            }
        }
    }

    void TM1637_Handling::addDevice(const String &baseTopic, int deviceIndex, int clkPin, int dioPin, bool flipDisplay, const String &serverDownText)
    {
        IotZoo::TM1637 *displayTm1637 = new IotZoo::TM1637(tm1637DisplayType,
                                                           mqttClient,
                                                           deviceIndex,
                                                           baseTopic,
                                                           clkPin,
                                                           dioPin,
                                                           flipDisplay,
                                                           serverDownText);
        displays1637.push_back(*displayTm1637);
    }

    // Initialize static members
    std::vector<IotZoo::TM1637> TM1637_Handling::displays1637{};
}
#endif