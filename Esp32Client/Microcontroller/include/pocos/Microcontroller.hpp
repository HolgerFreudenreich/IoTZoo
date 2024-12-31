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

#ifndef __MICROCONTROLLER_HPP__
#define __MICROCONTROLLER_HPP__

#include <WString.h>

namespace IotZoo
{
    struct Microcontroller
    {
        String MicrocontrollerType;
        String MacAddress;
        String IpAddress;
        String IpMqttBroker;
        String NamespaceName;
        String ProjectName;
        String BoardType;
        String FirmwareVersion;
        String Description;
    };
}

#endif // __MICROCONTROLLER_HPP__