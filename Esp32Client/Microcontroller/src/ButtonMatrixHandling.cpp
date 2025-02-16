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
#ifdef USE_KEYPAD
#include "ButtonMatrixHandling.hpp"

namespace IotZoo
{
    /// @brief Let the user know what the device can do.
    /// @param topics
    void ButtonMatrixHandling::addMqttTopicsToRegister(std::vector<Topic>* const topics) const
    {
        for (auto& buttonMatrix : buttonMatrixVector)
        {
            buttonMatrix.addMqttTopicsToRegister(topics);
        }
    }

    void ButtonMatrixHandling::AddDevice(ButtonMatrix* const buttonMatrix)
    {
        buttonMatrixVector.push_back(*buttonMatrix);
    }

    void ButtonMatrixHandling::loop()
    {
        for (auto& buttonMatrix : buttonMatrixVector)
        {
            buttonMatrix.loop();
        }
    }
} // namespace IotZoo
#endif // USE_KEYPAD