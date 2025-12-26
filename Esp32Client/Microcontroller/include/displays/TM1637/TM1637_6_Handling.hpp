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
#ifdef USE_TM1637_6

#ifndef __TM_1637_6_HANDLING_HPP
#define __TM_1637_6_HANDLING_HPP

using namespace std;

#include <Arduino.h>
#include "TM1637_Handling.hpp"

namespace IotZoo
{
    /// @brief Handles a vector of TM1637 6-digits displays.
    class TM1637_6_Handling : public TM1637_Handling
    {
    public:
        TM1637_6_Handling();

        /// @brief Subscribe to topics after the mqtt connection is established.
        /// @param mqttClient 
        /// @param baseTopic 
        void onMqttConnectionEstablished(Settings* const settings, MqttClient *mqttClient, const String &baseTopic);
        
        /// @brief A temperature value should be displayed.
        /// @param topic 
        /// @param message 
        static void callMqttbackOnReceivedDataTm1637Temperature(const String &topic, const String &message);
    };
}
#endif
#endif