// --------------------------------------------------------------------------------------------------------------------
//      ____    ______   _____
//     /  _/___/_  __/  /__  / ____  ____
//     / // __ \/ /       / / / __ \/ __ \  P L A Y G R O U N D
//   _/ // /_/ / /       / /_/ /_/ / /_/ /
//  /___/\____/_/       /____|____/\____/   (c) 2025 Holger Freudenreich under the MIT licence.
//
// --------------------------------------------------------------------------------------------------------------------
// Connect up to three HC SR501 Motion detectors with microcontrollers in a simple way.
// --------------------------------------------------------------------------------------------------------------------
#include "Defines.hpp"
#ifdef USE_HC_SR501
#ifndef __HRSR501_HANDLING_HPP__
#define __HRSR501_HANDLING_HPP__

#include "DeviceHandlingBase.hpp"
#include "HCSR501.hpp"
#include "HRSR501Helper.hpp"

namespace IotZoo
{
    class HRSR501Handling : DeviceHandlingBase
    {
    public:
        HRSR501Handling();

        ~HRSR501Handling();

        void addDevice(int deviceIndex, MqttClient *mqttClient, const String &baseTopic, uint8_t pin1);

        /// @brief Let the user know what the device can do.
        /// @param topics
        virtual void addMqttTopicsToRegister(std::vector<Topic> *const topics) const override;

        void loop();
    };
}

#endif // __HRSR501_HANDLING_HPP__
#endif // USE_HC_SR501