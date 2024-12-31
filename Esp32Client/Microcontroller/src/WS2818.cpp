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
#include "WS2818.hpp"

namespace IotZoo
{
    WS2818::WS2818(int pin, int numberOfLeds)
    {
        Serial.println("Constructor WS2818");
        dioPin = pin;
        this->numberOfLeds = numberOfLeds;
        pixels = new Adafruit_NeoPixel(numberOfLeds, dioPin, (NEO_GRB + NEO_KHZ800));
    }

    WS2818::~WS2818()
    {
        Serial.println("Destructor WS2818");
        delete pixels;
        pixels = NULL;
    }

    void WS2818::setup()
    {
        Serial.println("WS2818 setup. DIN Pin is " + String(dioPin));
        pixels->begin();
        setPixelColorRGB(150, 0, 0, 0, 5);
        delay(100);
        setPixelColorRGB(0, 150, 0, 0, 5);
        delay(100);
        setPixelColorRGB(0, 0, 150, 0, 5);
        delay(100);
        setPixelColorRGB(0, 0, 0, 0, 5);
    }

    /// @brief Test all leds.
    /// @param r
    /// @param g
    /// @param b
    void WS2818::testPixels(uint8_t r, uint8_t g, uint8_t b, int waitMs)
    {
        Serial.println("testPixels");
        pixels->setPixelColor(index, pixels->Color(r, g, b));
        if (index > 0)
        {
            pixels->setPixelColor(index - 1, pixels->Color(0, 0, 0));
        }
        pixels->show();

        if (index == 0)
        {
            pixels->setPixelColor(numberOfLeds - 1, pixels->Color(0, 0, 0));
        }
        pixels->show();
        delay(waitMs);
        index = index + 1;
        if (index == numberOfLeds)
        {
            index = 0;
        }
    }

    void WS2818::setPixelColorRGB(uint8_t r, uint8_t g, uint8_t b, uint16_t index, uint8_t brightness /* = 20*/)
    {
        setPixelColor(pixels->Color(r, g, b), index, brightness);
    }

    void WS2818::setPixelColor(uint32_t color, uint16_t index, uint8_t brightness /* = 20*/)
    {
        Serial.println("setPixelColor(color:" + String(color) + ", index: " + String(index) + ", brightness: " + String(brightness) + ")");
        if (pixels->getBrightness() != brightness)
        {
            pixels->setBrightness(brightness);
        }
        pixels->setPixelColor(index, color);
        pixels->show();
    }

    // Theater-marquee-style chasing lights. Pass in a color (32-bit value,
    // a la strip.Color(r,g,b) as mentioned above), and a delay time (in ms)
    // between frames.
    void WS2818::theaterChase(uint8_t r, uint8_t g, uint8_t b, int count, int waitMs)
    {
        uint32_t color = pixels->Color(r, g, b);

        for (int b = 0; b < count; b++)
        {                    //  'b' counts from 0 to 2...
            pixels->clear(); //   Set all pixels in RAM to 0 (off)
            // 'c' counts up from 'b' to end of strip in steps of 3...
            for (int c = b; c < pixels->numPixels(); c += 3)
            {
                pixels->setPixelColor(c, color); // Set pixel 'c' to value 'color'
            }
            pixels->show(); // Update strip with new contents
            delay(waitMs);  // Pause for a moment
        }
    }
}
#endif // USE_WS2818