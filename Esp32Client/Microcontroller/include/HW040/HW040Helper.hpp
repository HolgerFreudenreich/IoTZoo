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
#ifdef USE_HW040
#ifndef __HW040_HELPER_HPP__
#define __HW040_HELPER_HPP__

#include <Arduino.h>
#include <list>
#include "HW040.hpp"

namespace IotZoo
{
    class HW040Helper
    {
    public:
        static void onInterruptTriggered();

        static std::vector<IotZoo::RotaryEncoder> rotaryEncoders;
    };
}

#endif // __HW040_HELPER_HPP__
#endif // USE_HW040