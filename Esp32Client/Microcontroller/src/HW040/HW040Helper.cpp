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
#ifdef USE_HW040
#include "HW040/HW040Helper.hpp"

namespace IotZoo
{
    void HW040Helper::onInterruptTriggered()
    {
        // not nice, because we do not know which of the devices is the trigger. Loop over all devices as workaround.
        for (auto& encoder : rotaryEncoders)
        {
            encoder.readEncoder_ISR();
        }
    }

    std::vector<IotZoo::RotaryEncoder> HW040Helper::rotaryEncoders{};
} // namespace IotZoo

#endif // USE_HW040