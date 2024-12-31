// --------------------------------------------------------------------------------------------------------------------
//      ____    ______   _____
//     /  _/___/_  __/  /__  / ____  ____
//     / // __ \/ /       / / / __ \/ __ \  P L A Y G R O U N D
//   _/ // /_/ / /       / /_/ /_/ / /_/ /
//  /___/\____/_/       /____|____/\____/   (c) 2025 Holger Freudenreich under the MIT licence.
//
// --------------------------------------------------------------------------------------------------------------------
// Connect «Things» with microcontrollers in a simple way.
// --------------------------------------------------------------------------------------------------------------------
#ifdef USE_REMOTE_GPIOS
#ifndef __REMOTE_GPIO_HPP__
#define __REMOTE_GPIO_HPP__

#include <Arduino.h>

namespace IotZoo
{
  class RemoteGpio
  {
  protected:
    int pinGpio = -1;

  public:
    RemoteGpio(int pin);

    virtual ~RemoteGpio();

    int getGpioPin() const;

    int readDigitalValue() const;

    void handlePayload(const String &rawData);
  };
}

#endif // __REMOTE_GPIO_HPP__
#endif // USE_REMOTE_GPIOS