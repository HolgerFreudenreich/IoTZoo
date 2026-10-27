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
        TM1637_Handling::onMqttConnectionEstablished(mqttClient, baseTopic);
    }
} // namespace IotZoo
#endif // USE_TM1637_4