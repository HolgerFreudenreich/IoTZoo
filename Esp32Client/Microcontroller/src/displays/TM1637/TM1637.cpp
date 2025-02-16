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
#include "./displays/TM1637/TM1637.hpp"

#include <Arduino.h>

namespace IotZoo
{
    TM1637::TM1637(Tm1637DisplayType displayType, MqttClient* mqttClient, int deviceIndex, const String& baseTopic, int clkPin, int dioPin,
                   bool flipDisplay, const String& serverDownText)
        : TM1637DisplayBase(deviceIndex, mqttClient, baseTopic)
    {
        Serial.println("Constructor TM1637. DisplayType:" + displayType);

        displayTm1637 = new TM1637Display(mqttClient, deviceIndex, baseTopic, displayType, clkPin, dioPin, flipDisplay, serverDownText);
    }

    TM1637::~TM1637()
    {
        Serial.println("Destructor TM1637 deviceIndex: " + String(deviceIndex));
        delete displayTm1637;
        displayTm1637 = NULL;
    }

    Tm1637DisplayType TM1637::getDisplayType() const
    {
        return displayTm1637->getDisplayType();
    }

    void TM1637::addMqttTopicsToRegister(std::vector<Topic>* const topics) const
    {
        displayTm1637->addMqttTopicsToRegister(topics);
    }
} // namespace IotZoo
#endif