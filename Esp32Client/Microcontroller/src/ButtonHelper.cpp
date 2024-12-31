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
#ifdef USE_BUTTON

#include <vector>
#include "ButtonHelper.hpp"
#include "Button.hpp"

namespace IotZoo
{
    void ButtonHelper::onInterruptTriggered()
    {
        for (auto &button : ButtonHelper::buttons)
        {
            button.onInterruptTriggered();
        }
    }

    // Initialize static members.
    std::vector<IotZoo::Button> ButtonHelper::buttons{};
}
#endif // USE_BUTTON