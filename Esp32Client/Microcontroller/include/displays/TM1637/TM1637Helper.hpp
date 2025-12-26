// --------------------------------------------------------------------------------------------------------------------
//      ____    ______   _____
//     /  _/___/_  __/  /__  / ____  ____
//     / // __ \/ /       / / / __ \/ __ \  P L A Y G R O U N D
//   _/ // /_/ / /       / /_/ /_/ / /_/ /
//  /___/\____/_/       /____|____/\____/   (c) 2025 - 2026 Holger Freudenreich under the MIT licence.
//
// --------------------------------------------------------------------------------------------------------------------
// Connect «Things» with microcontrollers in a simple way.
// --------------------------------------------------------------------------------------------------------------------
#ifndef __TM1637_HELPER_HPP__
#define __TM1637_HELPER_HPP__

#include <Arduino.h>

namespace IotZoo
{
  class TM1637Helper
  {
  private:
    int dots = 0;

    void setDots(int dots)
    {
      this->dots = dots;
    }

  public:
    TM1637Helper(String& data)
    {
      setDots(data);    
    }

    int getDots() const { return dots; }


    void setDots(String &data)
    {
      int indexDot = data.indexOf(".");
      if (indexDot > 0)
      {
        data.replace(".", ""); // needed to display double value like 1.234
      }

      if (data.length() > 6)
      {
        data = data.substring(0, 6);
      }

      Serial.println("Set dot for " + data + ", length: " + String(data.length()) + ", indexDot: " + String(indexDot));

      if (data.length() == 6)
      {
        if (indexDot == 1)
        {
          setDots(128);
        }
        else if (indexDot == 2)
        {
          setDots(64);
        }
        else if (indexDot == 3)
        {
          setDots(32);
        }
        else if (indexDot == 4)
        {
          setDots(16);
        }
        else if (indexDot == 5)
        {
          setDots(8);
        }
      }
      else if (data.length() == 5)
      {
        if (indexDot == 1)
        {
          setDots(64); // setDots(128);
        }
        else if (indexDot == 2)
        {
          setDots(32);
        }
        else if (indexDot == 3)
        {
          setDots(16);
        }
        else if (indexDot == 4)
        {
          setDots(8);
        }
      }
      else if (data.length() == 4)
      {
        if (indexDot == 1)
        {
          setDots(32);
        }
        else if (indexDot == 2)
        {
          setDots(16);
        }
        else if (indexDot == 3)
        {
          setDots(8);
        }
        else if (indexDot == 4)
        {
          setDots(4);
        }
      }
      else if (data.length() == 3)
      {
        if (indexDot == 1)
        {
          setDots(16);
        }
        else if (indexDot == 2)
        {
          setDots(8);
        }
        else if (indexDot == 3)
        {
          setDots(4);
        }
        else if (indexDot == 4)
        {
          setDots(2);
        }
      }
      else if (data.length() == 2)
      {
        if (indexDot == 1)
        {
          setDots(8);
        }
        else if (indexDot == 2)
        {
          setDots(4);
        }
      }
    }
  };
}

#endif