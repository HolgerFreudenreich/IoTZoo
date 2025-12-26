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
#if defined(USE_TM1637_4) || defined(USE_TM1637_6)
#ifndef __TM1637_4_HPP__
#define __TM1637_4_HPP__

#include "DeviceBase.hpp"
#include "TM1637Display.hpp"

#include <ArduinoJson.h>

using namespace IotZoo;
using namespace std;

namespace IotZoo
{
    /// @brief For each configured TM1637 display this class will be instantiated.
    class TM1637 : public TM1637DisplayBase
    {
      protected:
        TM1637Display* displayTm1637 = nullptr;

      public:
        TM1637(int deviceIndex, Settings* const settings, MqttClient* mqttClient, const String& baseTopic, 
            Tm1637DisplayType displayType, int clkPin, int dioPin, bool flipDisplay, const String& serverDownText);

        ~TM1637() override;

        void addMqttTopicsToRegister(std::vector<Topic>* const topics) const override;

        void begin()
        {
            displayTm1637->begin();
        }

        Tm1637DisplayType getDisplayType() const;

        int getDefaultDisplayLength() const
        {
            return displayTm1637->getDefaultDisplayLength();
        }

        virtual void onIotZooClientUnavailable() override
        {
            displayTm1637->onIotZooClientUnavailable();
        }

        /// @brief Sets the orientation of the display.
        /// @param flip flip Flip display upside down true/false. Setting this parameter to true will cause the rendering on digits to be displayed
        /// upside down.
        void flipDisplay(bool flip = true)
        {
            displayTm1637->flipDisplay(flip);
        }

        /// @brief Returns the orientation of the display.
        /// @return True = Display has been flipped (upside down)
        bool isDisplayFlipped() const
        {
            return displayTm1637->isDisplayFlipped();
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
            displayTm1637->setBrightness(brightness, on);
        }

        /// @brief  Clears the display
        void clear()
        {
            displayTm1637->clear();
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
        void showNumber(int num, bool leading_zero = false, uint8_t length = 4, uint8_t pos = 0)
        {
            displayTm1637->showNumber(num, leading_zero, length, pos);
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
            displayTm1637->showNumberDec(num, dots, leading_zero, length, pos);
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
            displayTm1637->showString(s, length, pos, dots);
        }

        //! Display a Level Indicator (both orientations)
        //!
        //! Illuminate LEDs to provide a visual indicator of level (horizontal or vertical orientation)
        //!
        //! @param level A value between 0 and 100 (representing percentage)
        //! @param horizontal Boolean (true/false) where true = horizontal, false = vertical
        void showLevel(unsigned int level = 100, bool horizontal = true)
        {
            displayTm1637->showLevel(level, horizontal);
        }
    };
} // namespace IotZoo

#endif
#endif // USE_TM1637_4