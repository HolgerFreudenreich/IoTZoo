// --------------------------------------------------------------------------------------------------------------------
//      ____    ______   _____
//     /  _/___/_  __/  /__  / ____  ____
//     / // __ \/ /       / / / __ \/ __ \
//   _/ // /_/ / /       / /_/ /_/ / /_/ /
//  /___/\____/_/       /____|____/\____/ (c) 2025 - 2026 Holger Freudenreich under the MIT licence.
// --------------------------------------------------------------------------------------------------------------------
// Connect «Things» with microcontrollers in a simple way.
// --------------------------------------------------------------------------------------------------------------------
#include "Defines.hpp"
#ifdef USE_KEYPAD

#ifndef __BUTTON_MATRIX_HPP__
#define __BUTTON_MATRIX_HPP__

#include "DeviceBase.hpp"

#include <Keypad.h>

namespace IotZoo
{
    // 4 x 4 Button Matrix
    class ButtonMatrix : public DeviceBase
    {
      public:
        ButtonMatrix(int deviceIndex, Settings* const settings, MqttClient* mqttClient, const String& baseTopic);

        ~ButtonMatrix() override;

        /// @brief Let the user know what the device can do.
        /// @param topics
        void addMqttTopicsToRegister(std::vector<Topic>* const topics) const override;

        void loop() override;

        char* getKeyMap() const
        {
            return keyMap;
        }

        uint8_t getCountOfCols() const
        {
            return COLS;
        }

        uint8_t getCountOfRows() const
        {
            return ROWS;
        }

        Keypad* getCustomKeypad() const
        {
            return customKeypad;
        }

        void setColPins(uint8_t pin0, uint8_t pin1, uint8_t pin2, uint8_t pin3)
        {
            colPins[0] = pin0;
            colPins[1] = pin1;
            colPins[2] = pin2;
            colPins[3] = pin3;
        }

        void setRowPins(uint8_t pin0, uint8_t pin1, uint8_t pin2, uint8_t pin3)
        {
            rowPins[0] = pin0;
            rowPins[1] = pin1;
            rowPins[2] = pin2;
            rowPins[3] = pin3;
        }

      protected:
        // Constants for row and column sizes
        const uint8_t ROWS = 4;
        const uint8_t COLS = 4;

        // Array to represent keys on keypad. Do not use MQTT Wildcards like + or # here!
        char hexaKeys[4][4] = {{'7', '8', '9', 'A'}, {'4', '5', '6', 'S'}, {'1', '2', '3', 'M'}, {'0', '.', '=', 'D'}};

        // Connections to Arduino for 38 PIN Layout
        // uint8_t rowPins[ROWS] = {16, 4, 0, 2};
        // uint8_t colPins[COLS] = {19, 18, 5, 17};

        // Connections to Arduino for 30 PIN Layout

        uint8_t rowPins[4] = {26, 25, 33, 32}; // R1, R2, R3, R4
        uint8_t colPins[4] = {27, 14, 12, 13}; // C1, C2, C3, C4

        // Create keypad object
        char* keyMap = nullptr;

        Keypad* customKeypad;
    };
} // namespace IotZoo

#endif // __BUTTON_MATRIX_HPP__
#endif // USE_KEYPAD