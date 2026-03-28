// --------------------------------------------------------------------------------------------------------------------
//      ____    ______   _____
//     /  _/___/_  __/  /__  / ____  ____
//     / // __ \/ /       / / / __ \/ __ \  P L A Y G R O U N D
//   _/ // /_/ / /       / /_/ /_/ / /_/ /
//  /___/\____/_/       /____|____/\____/   (c) 2025 - 2026 Holger Freudenreich under the MIT licence.
//
// --------------------------------------------------------------------------------------------------------------------
// Private extension without a public use case
// --------------------------------------------------------------------------------------------------------------------
#include "Defines.hpp"

#pragma once

#include "Arduino.h"
#include "pocos/Topic.hpp"
#include "DeviceBase.hpp"

namespace IotZoo
{
    class DeviceExtension
    {
      public:
        DeviceExtension(DeviceBase* const deviceBase)
        {
            this->deviceBase = deviceBase;
        }

      protected:
        const DeviceBase* deviceBase;
    };

} // namespace IotZoo