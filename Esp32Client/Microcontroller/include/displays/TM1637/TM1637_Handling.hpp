// --------------------------------------------------------------------------------------------------------------------
//      ____    ______   _____
//     /  _/___/_  __/  /__  / ____  ____
//     / // __ \/ /       / / / __ \/ __ \  P L A Y G R O U N D
//   _/ // /_/ / /       / /_/ /_/ / /_/ /
//  /___/\____/_/       /____|____/\____/   (c) 2025 Holger Freudenreich under the MIT licence.
//
// --------------------------------------------------------------------------------------------------------------------
// Firmware for ESP8266 and ESP32 Microcontrollers
// --------------------------------------------------------------------------------------------------------------------
#include "Defines.hpp"
#if defined(USE_TM1637_4) || defined(USE_TM1637_6)
#ifndef __TM1637_HANDLING_HPP__
#define __TM1637_HANDLING_HPP__

#include "DeviceHandlingBase.hpp"
#include "TM1637.hpp"

namespace IotZoo
{
    /// @brief Holds a vector of TM1637 displays. Is used as base class for TM1637_4_Handling and TM1637_6_Handling.
    class TM1637_Handling : public DeviceHandlingBase
    {
    public:
        TM1637_Handling(Tm1637DisplayType tm1637DisplayType);

        void setup();

        virtual void onIotZooClientUnavailable() override;

        void addMqttTopicsToRegister(std::vector<Topic> *const topics) const;

        /// @brief Data received to display on a TM1637 4 digit display.
        /// @param rawData: data in json format or unformatted.
        static void onReceivedDataTm1637_Number(const String &rawData, int deviceIndex);

        static void callbackMqttOnReceivedDataTm1637_Number(const String &topic, const String &message);

        static void callMqttbackOnReceivedDataTm1637Text(const String &topic, const String &message);

        static void callbackMqttOnReceivedDataTm1637Level(const String &topic, const String &message);

        void addDevice(const String &baseTopic, int deviceIndex,
                       int clkPin, int dioPin, bool flipDisplay, const String &serverDownText);

        static TM1637 *getDisplayByDeviceIndex(int index);

    protected:
        static std::vector<IotZoo::TM1637> displays1637; // static, because of the static callback functions.
        Tm1637DisplayType tm1637DisplayType;             // all displays in the vector are from the same type.       
    };
}
#endif // __TM1637_HANDLING_HPP__
#endif // #if defined(USE_TM1637_4) || defined(USE_TM1637_6)