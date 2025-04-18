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

#ifndef __BUTTONMATRIX_HANDLING_HPP__
#define __BUTTONMATRIX_HANDLING_HPP__

#include "ButtonMatrix.hpp"
#include "DeviceHandlingBase.hpp"

namespace IotZoo
{
    class ButtonMatrixHandling : DeviceHandlingBase
    {
      public:
        /// @brief Let the user know what the device can do.
        /// @param topics
        void addMqttTopicsToRegister(std::vector<Topic>* const topics) const override;

        void AddDevice(ButtonMatrix* const buttonMatrix);

        void loop();

      protected:
        vector<ButtonMatrix> buttonMatrixVector;
    };
} // namespace IotZoo

#endif // __BUTTONMATRIX_HANDLING_HPP__
#endif // USE_KEYPAD