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

#include "HCSR501.hpp"
#include "HRSR501Helper.hpp"
#include "vector"

namespace IotZoo
{
    void HRSR501Helper::readInterrupt()
    {
        for (auto& motionSensor : HRSR501Helper::motionSensors)
        {
            motionSensor.isrMotionDetectorRising();
        }
    }

    // Initialize static members.
    std::vector<IotZoo::HCSC501> HRSR501Helper::motionSensors{};
} // namespace IotZoo

#endif // USE_HC_SR501
