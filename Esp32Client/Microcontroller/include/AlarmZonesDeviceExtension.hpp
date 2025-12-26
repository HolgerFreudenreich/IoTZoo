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
#ifdef USE_WS2818_PIXEL_MATRIX
#ifdef USE_WS2818

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

    class AlarmZonesDeviceExtension : public DeviceExtension
    {
      public:
        AlarmZonesDeviceExtension(DeviceBase* const deviceBase);

        void onMqttConnectionEstablished();

        /// @brief Let the user know what the device can do.
        /// @param topics
        void addMqttTopicsToRegister(std::vector<Topic>* const topics) const;

      protected:
        uint getAlarmLevel(const String& subject);

        void onAlarmReceived(const String& subject);
    };
} // namespace IotZoo

#endif // USE_WS2818
#endif // USE_WS2818_PIXEL_MATRIX