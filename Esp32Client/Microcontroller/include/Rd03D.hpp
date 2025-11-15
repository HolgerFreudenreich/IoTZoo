// --------------------------------------------------------------------------------------------------------------------
//      ____    ______   _____
//     /  _/___/_  __/  /__  / ____  ____
//     / // __ \/ /       / / / __ \/ __ \  P L A Y G R O U N D
//   _/ // /_/ / /       / /_/ /_/ / /_/ /
//  /___/\____/_/       /____|____/\____/   (c) 2025 Holger Freudenreich under the MIT licence.
//
// --------------------------------------------------------------------------------------------------------------------
// Connect Rd-03D 24G Multi-Target Human Motion Detector
// https://docs.ai-thinker.com/_media/rd-03d_v1.0.0_specification.pdf
// --------------------------------------------------------------------------------------------------------------------
#include "Defines.hpp"
#ifdef USE_RD_03D
#ifndef __RD03D_HPP__
#define __RD03D_HPP__

// --------------------------------------------------------------------------------------------------------------------
// Includes
// --------------------------------------------------------------------------------------------------------------------
#include "DeviceBase.hpp"

#include <Arduino.h>

namespace IotZoo
{
    struct Target
    {
      public:
        int16_t  x                             = 0;
        int16_t  y                             = 0;
        int16_t  speedCentimetersPerSecond     = 0;
        uint16_t distanceResolutionMillimeters = 0;
        int16_t  distanceMillimeters           = 0;
        float    angle                         = 0;
    };

    class Rd03D : DeviceBase
    {
      protected:
        uint8_t   pinRx;
        uint8_t   pinTx;
        u_int16_t timeoutMillis;
        uint16_t  maxDistanceMillimeters;
        bool      multiTargetMode;

        uint8_t serialBuffer[64]     = {0};
        int     serialBufferCount    = 0;
        int     serialBufferCountTmp = 0;

        Target target1;
        Target target2;
        Target target3;

        bool currentIsMovingStatus = false;

        bool target1IsMoving = false;
        bool target2IsMoving = false;
        bool target3IsMoving = false;

        u_int8_t countOfPeopleInRange    = 0;
        u_int8_t oldCountOfPeopleInRange = 0;

        long millisTarget1Moved;
        long millisTarget2Moved;
        long millisTarget3Moved;

        String topicDistanceTarget1;
        String topicDistanceTarget2;
        String topicDistanceTarget3;

        String topicMovementChangeTarget1;
        String topicMovementChangeTarget2;
        String topicMovementChangeTarget3;

        String topicMovementDetected;
        String topicCountOfDetectedPeopleInRange;

      protected:
        // Target Detection Commands
        uint8_t Single_Target_Detection_CMD[12] = {0xFD, 0xFC, 0xFB, 0xFA, 0x02, 0x00, 0x80, 0x00, 0x04, 0x03, 0x02, 0x01};
        uint8_t Multi_Target_Detection_CMD[12]  = {0xFD, 0xFC, 0xFB, 0xFA, 0x02, 0x00, 0x90, 0x00, 0x04, 0x03, 0x02, 0x01};

        void setup();

        Target getTarget(uint8_t targetIndex);

        /// @brief publishes if a movement was detected or not.
        void publishMovementStatus();

        /// @brief Processes the received data.
        /// @return true, if at least one target was found; otherwise false.
        bool processData();

        String serializeTarget(const Target& target);

      public:
        Rd03D(int deviceIndex, Settings* const settings, MqttClient* const mqttClient, const String& baseTopic, uint8_t pinRx, uint8_t pinTx,
              u_int16_t timeoutMillis, u_int16_t maxDistanceMillimeters, bool multiTargetMode);

        ~Rd03D() override;

        /// @brief Let the user know what the device can do.
        /// @param topics
        void addMqttTopicsToRegister(std::vector<Topic>* const topics) const override;

        void loop();
    };
} // namespace IotZoo

#endif // __RD03D_HPP__
#endif // USE_RD_03D