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
// Set Debug Level
// --------------------------------------------------------------------------------------------------------------------
#pragma once
#include "Defines.hpp"

#ifdef USE_DEBUG_MESSAGES
#include <TinyConsole.h>
#include <TinyStreaming.h>

struct DebugHelper
{
    static const int debugLevel = 2; // 0: no debug messages, 1: only important messages, 2: all messages
}; 

#define debug(what)                                                                                                                                  \
    {                                                                                                                                                \
        if (DebugHelper::debugLevel >= 1)                                                                                                                    \
            Console << __FILE__ << " L" << (int)__LINE__ << ' ' << what << _EndLineCode::endl;                                                                           \
    }
#else
#define debug(what)                                                                                                                                  \
    {                                                                                                                                                \
    }
#endif // USE_DEBUG_MESSAGES