// --------------------------------------------------------------------------------------------------------------------
//      ____    ______   _____
//     /  _/___/_  __/  /__  / ____  ____
//     / // __ \/ /       / / / __ \/ __ \  P L A Y G R O U N D
//   _/ // /_/ / /       / /_/ /_/ / /_/ /
//  /___/\____/_/       /____|____/\____/   (c) 2025 Holger Freudenreich under the MIT licence.
//
// --------------------------------------------------------------------------------------------------------------------
// Connect WS2812, WS2818 Leds with microcontrollers in a simple way.
// --------------------------------------------------------------------------------------------------------------------
// WS2818 Adafruit_NeoPixel
// --------------------------------------------------------------------------------------------------------------------
#include "Defines.hpp"
#ifdef USE_WS2818
#ifndef __WS2818_HPP__
#define __WS2818_HPP__

#include <Adafruit_NeoPixel.h>

namespace IotZoo
{
  class WS2818
  {
  protected:
    Adafruit_NeoPixel *pixels = NULL;

    int index = 0;

    int dioPin;
    int numberOfLeds;

  public:
    WS2818(int pin, int numberOfLeds);

    virtual ~WS2818();

    void setup();

    /// @brief Test all leds.
    /// @param r
    /// @param g
    /// @param b
    void testPixels(uint8_t r, uint8_t g, uint8_t b, int waitMs);

    void setPixelColorRGB(uint8_t r, uint8_t g, uint8_t b, uint16_t index, uint8_t brightness = 20);

    void setPixelColor(uint32_t color, uint16_t index, uint8_t brightness = 20);

    // Theater-marquee-style chasing lights. Pass in a color (32-bit value,
    // a la strip.Color(r,g,b) as mentioned above), and a delay time (in ms)
    // between frames.
    void theaterChase(uint8_t r, uint8_t g, uint8_t b, int count, int waitMs);
  };
}

#endif // __WS2818_HPP__
#endif // USE_WS2818