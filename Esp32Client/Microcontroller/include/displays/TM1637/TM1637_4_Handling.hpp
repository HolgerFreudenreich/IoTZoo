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

#ifndef __TM_1637_4_HANDLING_HPP
#define __TM_1637_4_HANDLING_HPP

using namespace std;

#include <Arduino.h>
#include "TM1637_Handling.hpp"

namespace IotZoo
{
    /// @brief Holds a vector of TM1637 displays, all of the same type 6 digits display.
    class TM1637_4_Handling : public TM1637_Handling
    {
    public:
        TM1637_4_Handling();

        void onMqttConnectionEstablished(MqttClient *mqttClient, const String &baseTopic);
    };
}
#endif // __TM_1637_4_HANDLING_HPP
#endif // USE_TM1637_4
