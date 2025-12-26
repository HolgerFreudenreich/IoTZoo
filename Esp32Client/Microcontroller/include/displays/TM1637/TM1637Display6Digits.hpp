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
#ifndef __TM1637_DISPLAY_6DIGITS_HPP__
#define __TM1637_DISPLAY_6DIGITS_HPP__

#include "Defines.hpp"
#ifdef USE_TM1637_6
#include "TM1637DisplayBase.hpp"
#include "TM1637TinyDisplay6.h"

namespace IotZoo
{
    class TM1637Display6Digits : public TM1637DisplayBase
    {
      public:
        TM1637Display6Digits(int deviceIndex, Settings* const settings, MqttClient* mqttClient, const String& baseTopic,
             uint8_t pinClk, uint8_t pinDio)
            : TM1637DisplayBase(deviceIndex, settings, mqttClient, baseTopic)
        {
            Serial.println("Constructor TM1637Display6Digits");
            tm1637_6_Display = new TM1637TinyDisplay6(pinClk, pinDio); // concrete implementation of the underlying hardware
        }

        int getDefaultDisplayLength() const
        {
            return 6;
        }

        void onIotZooClientUnavailable() override
        {
            tm1637_6_Display->showString(getServerDownText().c_str());
        }

        Tm1637DisplayType getDisplayType() const override
        {
            return Tm1637DisplayType::Digits6;
        }

        void addMqttTopicsToRegister(std::vector<Topic>* const topics) const override
        {
            topics->emplace_back(baseTopic + "/tm1637_6/" + String(deviceIndex) + "/number",
                                 "Send a number to TM1637 6 digits LCD display with index " + String(deviceIndex) + ".",
                                 MessageDirection::IotZooClientOutbound);

            topics->emplace_back(baseTopic + "/tm1637_6/" + String(deviceIndex) + "/text",
                                 "Send a text to TM1637 6 digits LCD display with index " + String(deviceIndex) + ".",
                                 MessageDirection::IotZooClientOutbound);

            topics->emplace_back(baseTopic + "/tm1637_6/" + String(deviceIndex) + "/level",
                                 "Use TM1637 6 digits LCD display with index " + String(deviceIndex) + " to indicate a level between 0 and 100.",
                                 MessageDirection::IotZooClientOutbound);

            topics->emplace_back(baseTopic + "/tm1637_6/" + String(deviceIndex) + "/temperature",
                                 "Use TM1637 6 digits LCD display with index " + String(deviceIndex) + " to indicate a temperature.",
                                 MessageDirection::IotZooClientOutbound);
        }

        void begin()
        {
            tm1637_6_Display->begin(true);
        }

        /// @brief Sets the orientation of the display.
        /// @param flip flip Flip display upside down true/false. Setting this parameter to true will cause the rendering on
        /// digits to be displayed upside down.
        void flipDisplay(bool flip = true)
        {
            tm1637_6_Display->flipDisplay(flip);
        }

        /// @brief Returns the orientation of the display.
        /// @return True = Display has been flipped (upside down)
        bool isDisplayFlipped() const
        {
            return tm1637_6_Display->isflipDisplay();
        }

        //! Sets the brightness of the display.
        //!
        //! The setting takes effect when a command is given to change the data being
        //! displayed.
        //!
        //! @param brightness A number from 0 (lowest brightness) to 7 (highest brightness)
        //! @param on Turn display on or off
        void setBrightness(uint8_t brightness, bool on = true)
        {
            tm1637_6_Display->setBrightness(brightness, on);
        }

        /// @brief  Clears the display
        void clear()
        {
            tm1637_6_Display->clear();
        }

        //! Display a decimal number
        //!
        //! Display the given argument as a decimal number.
        //!
        //! @param num The number to be shown
        //! @param leading_zero When true, leading zeros are displayed. Otherwise unnecessary digits are
        //!        blank. NOTE: leading zero is not supported with negative numbers.
        //! @param length The number of digits to set. The user must ensure that the number to be shown
        //!        fits to the number of digits requested (for example, if two digits are to be displayed,
        //!        the number must be between 0 to 99)
        //! @param pos The position of the most significant digit (0 - leftmost, 3 - rightmost)
        void showNumber(int num, bool leading_zero = false, uint8_t length = MAXDIGITS, uint8_t pos = 0)
        {
            tm1637_6_Display->showNumber(num, leading_zero, length, pos);
        }

        //! Display a decimal number, with dot control
        //!
        //! Display the given argument as a decimal number. The dots between the digits (or colon)
        //! can be individually controlled.
        //!
        //! @param num The number to be shown
        //! @param dots Dot/Colon enable. The argument is a bitmask, with each bit corresponding to a dot
        //!        between the digits (or colon mark, as implemented by each module). i.e.
        //!        For displays with dots between each digit:
        //!        * 0.000 (0b10000000)
        //!        * 00.00 (0b01000000)
        //!        * 000.0 (0b00100000)
        //!        * 0000. (0b00010000)
        //!        * 0.0.0.0 (0b11100000)
        //!        For displays with just a colon:
        //!        * 00:00 (0b01000000)
        //!        For displays with dots and colons colon:
        //!        * 0.0:0.0 (0b11100000)
        //! @param leading_zero When true, leading zeros are displayed. Otherwise unnecessary digits are
        //!        blank. NOTE: leading zero is not supported with negative numbers.
        //! @param length The number of digits to set. The user must ensure that the number to be shown
        //!        fits to the number of digits requested (for example, if two digits are to be displayed,
        //!        the number must be between 0 to 99)
        //! @param pos The position of the most significant digit (0 - leftmost, 3 - rightmost)
        void showNumberDec(int num, uint8_t dots = 0, bool leading_zero = false, uint8_t length = MAXDIGITS, uint8_t pos = 0)
        {
            tm1637_6_Display->showNumberDec(num, dots, leading_zero, length, pos);
        }

        //! Display a string
        //!
        //! Display the given string and if more than 4 characters, will scroll message on display
        //!
        //! @param s The string to be shown
        //! @param scrollDelay  The delay, in microseconds to wait before scrolling to next frame
        //! @param length The number of digits to set.
        //! @param pos The position of the most significant digit (0 - leftmost, 3 - rightmost)
        //! @param dots Dot/Colon enable. The argument is a bitmask, with each bit corresponding to a dot
        //!        between the digits (or colon mark, as implemented by each module). i.e.
        //!        For displays with dots between each digit:
        //!        * 0.000 (0b10000000)
        //!        * 00.00 (0b01000000)
        //!        * 000.0 (0b00100000)
        //!        * 0000. (0b00010000)
        //!        * 0.0.0.0 (0b11100000)
        //!        For displays with just a colon:
        //!        * 00:00 (0b01000000)
        //!        For displays with dots and colons colon:
        //!        * 0.0:0.0 (0b11100000)
        //! See showString_P function for reading PROGMEM read-only flash memory space instead of RAM
        void showString(const char s[], uint8_t length = 4, uint8_t pos = 0, uint8_t dots = 0)
        {
            tm1637_6_Display->showString(s, length, pos, dots);
        }

        //! Display a Level Indicator (both orientations)
        //!
        //! Illuminate LEDs to provide a visual indicator of level (horizontal or vertical orientation)
        //!
        //! @param level A value between 0 and 100 (representing percentage)
        //! @param horizontal Boolean (true/false) where true = horizontal, false = vertical
        void showLevel(unsigned int level = 100, bool horizontal = true)
        {
            tm1637_6_Display->showLevel(level, horizontal);
        }

      protected:
        TM1637TinyDisplay6* tm1637_6_Display = nullptr;
    };
} // namespace IotZoo
#endif // USE_TM1637_6

#endif // __TM1637_DISPLAY_6DIGITS_HPP__