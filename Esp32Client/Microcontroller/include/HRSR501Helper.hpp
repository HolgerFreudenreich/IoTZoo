// --------------------------------------------------------------------------------------------------------------------
//      ____    ______   _____
//     /  _/___/_  __/  /__  / ____  ____
//     / // __ \/ /       / / / __ \/ __ \  P L A Y G R O U N D
//   _/ // /_/ / /       / /_/ /_/ / /_/ /
//  /___/\____/_/       /____|____/\____/   (c) 2025 Holger Freudenreich under the MIT licence.
//
// --------------------------------------------------------------------------------------------------------------------
// Connect HC SR501 Motion detectors with microcontrollers in a simple way.
// --------------------------------------------------------------------------------------------------------------------
#include "Defines.hpp"
#ifdef USE_HC_SR501
#ifndef __HRSR501_HELPER_HPP__
#define __HRSR501_HELPER_HPP__

#include "DeviceHandlingBase.hpp"
#include "HCSR501.hpp"

namespace IotZoo
{
    class HRSR501Helper
    {
    public:
        static std::vector<IotZoo::HCSC501> motionSensors;

        static void readInterrupt();
    };
}

#endif // __HRSR501_HELPER_HPP__
#endif // USE_HC_SR501