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
#if defined(USE_TM1637_4) || defined(USE_TM1637_6)

#include "./displays/TM1637/TM1637Display.hpp"

#ifdef USE_TM1637_4
#include "./displays/TM1637/TM1637Display4Digits.hpp"
#endif
#ifdef USE_TM1637_6
#include "./displays/TM1637/TM1637Display6Digits.hpp"
#endif

namespace IotZoo
{
    /// @brief We have 2 diplay types: 4 digits and 6 digits. Unfortunately, these are derived from different classes that do not inherit from each other.
    /// This class represents either a 4 digit display or a 6 digit display dependent on displayType.

    TM1637Display::TM1637Display(MqttClient *mqttClient, int deviceIndex, const String &baseTopic, Tm1637DisplayType displayType, uint8_t pinClk, uint8_t pinDio,
                                 bool flipDisplay, const String &serverDownText) : TM1637DisplayBase(deviceIndex, mqttClient, baseTopic)
    {
#ifdef USE_TM1637_4
        if (displayType == Tm1637DisplayType::Digits4)
        {
            tm1637Display = new TM1637Display4Digits(deviceIndex, pinClk, pinDio, mqttClient, baseTopic);
        }
#endif
#ifdef USE_TM1637_6
        if (displayType == Tm1637DisplayType::Digits6)
        {
            tm1637Display = new TM1637Display6Digits(deviceIndex, pinClk, pinDio, mqttClient, baseTopic);
        }
        tm1637Display->flipDisplay(flipDisplay);
        tm1637Display->setServerDownText(serverDownText);

#endif
        tm1637Display->begin();
    }

    void TM1637Display::begin()
    {
        tm1637Display->begin();
    }

    Tm1637DisplayType TM1637Display::getDisplayType() const
    {
        return tm1637Display->getDisplayType();
    }

    void TM1637Display::addMqttTopicsToRegister(std::vector<Topic> *const topics) const
    {
        tm1637Display->addMqttTopicsToRegister(topics);
    }

    int TM1637Display::getDefaultDisplayLength() const
    {
        return tm1637Display->getDefaultDisplayLength();
    }

    void TM1637Display::onIotZooClientUnavailable()
    {
        return tm1637Display->onIotZooClientUnavailable();
    }

    /// @brief Sets the orientation of the display.
    /// @param flip flip Flip display upside down true/false. Setting this parameter to true will cause the rendering on digits to be displayed upside down.
    void TM1637Display::flipDisplay(bool flip)
    {
        tm1637Display->flipDisplay(flip);
    }

    /// @brief Returns the orientation of the display.
    /// @return True = Display has been flipped (upside down)
    bool TM1637Display::isDisplayFlipped() const
    {
        return tm1637Display->isDisplayFlipped();
    }

    //! Sets the brightness of the display.
    //!
    //! The setting takes effect when a command is given to change the data being
    //! displayed.
    //!
    //! @param brightness A number from 0 (lowest brightness) to 7 (highest brightness)
    //! @param on Turn display on or off
    void TM1637Display::setBrightness(uint8_t brightness, bool on)
    {
        tm1637Display->setBrightness(brightness, on);
    }

    /// @brief  Clears the display
    void TM1637Display::clear()
    {
        tm1637Display->clear();
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
    void TM1637Display::showNumber(int num, bool leading_zero, uint8_t length, uint8_t pos)
    {
        tm1637Display->showNumber(num, leading_zero, length, pos);
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
    void TM1637Display::showNumberDec(int num, uint8_t dots, bool leading_zero, uint8_t length, uint8_t pos)
    {
        tm1637Display->showNumberDec(num, dots, leading_zero, length, pos);
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
    void TM1637Display::showString(const char s[], uint8_t length, uint8_t pos, uint8_t dots)
    {
        tm1637Display->showString(s, length, pos, dots);
    }

    //! Display a Level Indicator (both orientations)
    //!
    //! Illuminate LEDs to provide a visual indicator of level (horizontal or vertical orientation)
    //!
    //! @param level A value between 0 and 100 (representing percentage)
    //! @param horizontal Boolean (true/false) where true = horizontal, false = vertical
    void TM1637Display::showLevel(unsigned int level, bool horizontal)
    {
        tm1637Display->showLevel(level, horizontal);
    }
}

#endif // defined(USE_TM1637_4) || defined(USE_TM1637_6)