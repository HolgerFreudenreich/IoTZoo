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
#ifdef USE_BUTTON
#ifndef __BUTTON_HELPER_HPP__
#define __BUTTON_HELPER_HPP__

#include "Arduino.h"
#include "Button.hpp"

#include <vector>

namespace IotZoo
{
    class ButtonHelper
    {
      public:
        static void onInterruptTriggered();

        static std::vector<Button> buttons;
    };
} // namespace IotZoo

#endif // __BUTTON_HELPER_HPP__
#endif // USE_BUTTON