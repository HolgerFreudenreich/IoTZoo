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

#include "RD03D.hpp"
#include <ArduinoJson.h>

namespace IotZoo
{
    Rd03D::Rd03D(int deviceIndex, MqttClient *const mqttClient, const String &baseTopic,
                 uint8_t pinRx, uint8_t pinTx,
                 u_int16_t timeoutMillis, u_int16_t maxDistanceMillimeters, bool multiTargetMode)
        : DeviceBase(deviceIndex, mqttClient, baseTopic)
    {
        Serial.print("Constructor Rd03D, pinRx: " + String(pinRx) + ", pinTx: " + String(pinTx));
        Serial.println("timeoutMillis: " + String(timeoutMillis) + ", maxDistanceMillimeters: " + String(maxDistanceMillimeters) + ", multiTargetMode: " + String(multiTargetMode));
        this->pinRx = pinRx;
        this->pinTx = pinTx;
        this->multiTargetMode = multiTargetMode;
        this->timeoutMillis = timeoutMillis;
        if (this->timeoutMillis < 1000)
        {
            this->timeoutMillis = 1000;
        }
        this->maxDistanceMillimeters = maxDistanceMillimeters;
        if (this->maxDistanceMillimeters > 6000)
        {
            //this->maxDistanceMillimeters = 6000;
        }
        topicDistanceTarget1 = baseTopic + "/rd03d/0/target/0/distance_mm";
        topicMovementChangeTarget1 = baseTopic + "/rd03d/0/target/0";
        if (multiTargetMode)
        {
            topicDistanceTarget2 = baseTopic + "/rd03d/0/target/1/distance_mm";
            topicMovementChangeTarget2 = baseTopic + "/rd03d/0/target/1";
            topicDistanceTarget3 = baseTopic + "/rd03d/0/target/2/distance_mm";
            topicMovementChangeTarget3 = baseTopic + "/rd03d/0/target/2";
        }
        topicMovementDetected = baseTopic + "/rd03d/0/movement_detected";
        topicCountOfDetectedPeopleInRange = baseTopic + "/rd03d/0/count_of_people_in_range";
        setup();
    }

    Rd03D::~Rd03D()
    {
        Serial.println("Destructor Rd03D, pinRx: " + String(pinRx) + ", pinTx: " + String(pinTx));
        this->pinRx = pinRx;
    }

    /// @brief Let the user know what the device can do.
    /// @param topics
    void Rd03D::addMqttTopicsToRegister(std::vector<Topic> *const topics) const
    {
        topics->push_back(*new Topic(topicDistanceTarget1,
                                     "Sends the distance to human 1 in mm.",
                                     MessageDirection::IotZooClientInbound));
        if (multiTargetMode)
        {
            topics->push_back(*new Topic(topicDistanceTarget2,
                                         "Sends the distance to human 2 in mm.",
                                         MessageDirection::IotZooClientInbound));

            topics->push_back(*new Topic(topicDistanceTarget3,
                                         "Sends the distance to human 3 in mm.",
                                         MessageDirection::IotZooClientInbound));
        }
        topics->push_back(*new Topic(topicMovementChangeTarget1,
                                     "Sends movement change data in json format for target 1.",
                                     MessageDirection::IotZooClientInbound));
        if (multiTargetMode)
        {
            topics->push_back(*new Topic(topicMovementChangeTarget2,
                                         "Sends movement change data in json format for target 2.",
                                         MessageDirection::IotZooClientInbound));

            topics->push_back(*new Topic(topicMovementChangeTarget3,
                                         "Sends movement change data in json format for target 3.",
                                         MessageDirection::IotZooClientInbound));
        }

        topics->push_back(*new Topic(topicMovementDetected,
                                     "1 = movement detected, 2 = no movement detected past 30 seconds.",
                                     MessageDirection::IotZooClientInbound));

        topics->push_back(*new Topic(topicCountOfDetectedPeopleInRange,
                                     "Number of people in range [0-3].",
                                     MessageDirection::IotZooClientInbound));
    }

    void Rd03D::loop()
    {
        bool peopleDetected = false;
        // Evaluation of the data.
        while (Serial1.available())
        {
            serialBufferCountTmp = Serial1.read(); // reads the first byte of the incomming data.

            serialBuffer[serialBufferCount++] = serialBufferCountTmp;

            // Prevent buffer overflow
            if (serialBufferCount >= sizeof(serialBuffer))
            {
                serialBufferCount = sizeof(serialBuffer) - 1;
            }

            // Check for end of frame (0xCC, 0x55) and extract data for target1, target2, target3.
            if (processData())
            {
                int countOfDetectedPeople = 0;

                if (target1.distanceMillimeters < maxDistanceMillimeters &&
                    target1.distanceMillimeters > 0)
                {
                    Serial.println("Target 1 Distance: " + String(target1.distanceMillimeters));
                    target1IsMoving = true;
                    millisTarget1Moved = millis();

                    countOfDetectedPeople++;

                    mqttClient->publish(topicDistanceTarget1, String(target1.distanceMillimeters));
                    mqttClient->publish(topicMovementChangeTarget1, serializeTarget(target1));
                }
                else
                {
                    // Serial.println("Target 1 out of range of (" + String(maxDistanceMillimeters) + ") mm: " + String(target1.distanceMillimeters) + " mm.");
                    //  target1IsMoving = false; sometimes we got wrong data from the sensor, so out of range must not mean that the target is not there or moving.
                }

                if (target2.distanceMillimeters < maxDistanceMillimeters &&
                    target2.distanceMillimeters > 0)
                {
                    Serial.println("Target 2 Distance: " + String(target2.distanceMillimeters));
                    countOfDetectedPeople++;

                    mqttClient->publish(topicDistanceTarget2, String(target2.distanceMillimeters));
                    mqttClient->publish(topicMovementChangeTarget2, serializeTarget(target2));

                    millisTarget2Moved = millis();
                }
                else
                {
                    // Serial.println("Target 2 out of range of (" + String(maxDistanceMillimeters) + ") mm: " + String(target2.distanceMillimeters) + " mm.");
                }

                if (target3.distanceMillimeters < maxDistanceMillimeters &&
                    target3.distanceMillimeters > 0)
                {
                    Serial.println("Target 3 Distance: " + String(target3.distanceMillimeters));
                    countOfDetectedPeople++;

                    mqttClient->publish(topicDistanceTarget3, String(target3.distanceMillimeters));
                    mqttClient->publish(topicMovementChangeTarget3, serializeTarget(target2));

                    millisTarget3Moved = millis();
                }
                else
                {
                    // Serial.println("Target 3 out of range of (" + String(maxDistanceMillimeters) + ") mm: " + String(target3.distanceMillimeters) + " mm.");
                }

                this->countOfPeopleInRange = countOfDetectedPeople;
            }
        }

        if (millis() - millisTarget1Moved > timeoutMillis)
        {
            // No movement for a long time
            target1IsMoving = false;
        }

        if (millis() - millisTarget2Moved > timeoutMillis)
        {
            // No movement for a long time
            target2IsMoving = false;
        }

        if (millis() - millisTarget3Moved > timeoutMillis)
        {
            // No movement for a long time
            target3IsMoving = false;
        }

        publishMovementStatus();
    }

    void Rd03D::publishMovementStatus()
    {
        bool isMovingStatus = target1IsMoving || target2IsMoving || target3IsMoving;
        if (currentIsMovingStatus != isMovingStatus)
        {
            Serial.println("Moving status changed -> target1IsMoving: " + String(target1IsMoving) + ", target2IsMoving: " + String(target2IsMoving) + ", target3IsMoving: " + String(target3IsMoving) + ", Count of People in Range: " + String(countOfPeopleInRange));

            mqttClient->publish(topicMovementDetected, String(isMovingStatus));
            mqttClient->publish(topicCountOfDetectedPeopleInRange, String(countOfPeopleInRange));
            currentIsMovingStatus = isMovingStatus;
        }
    }

    void Rd03D::setup()
    {
        Serial1.setRxBufferSize(1024);
        Serial1.begin(256000, // Baudrate
                      SERIAL_8N1,
                      pinRx,
                      pinTx);
        // Send target detection command
        if (multiTargetMode)
        {
            Serial1.write(Multi_Target_Detection_CMD, sizeof(Multi_Target_Detection_CMD));
            Serial.println("Multi-target detection mode activated.");
        }
        else
        {
            Serial1.write(Single_Target_Detection_CMD, sizeof(Single_Target_Detection_CMD));
            Serial.println("Single-target detection mode activated.");
        }
        serialBufferCount = 0;
        Serial1.flush();
    }

    String Rd03D::serializeTarget(const Target &target)
    {
        StaticJsonDocument<256> doc;
        doc["x"] = target.x;
        doc["y"] = target.y;
        // doc["speedCentimetersPerSecond"] = std::rint(target.speedCentimetersPerSecond);
        doc["distanceMillimeters"] = target.distanceMillimeters;
        doc["angle"] = std::rint(target.angle);
        String json;
        size_t size = serializeJson(doc, json);
        // Serial.println("Serialized target " + json + " size: " + String(size) + " bytes.");
        return json;
    }

    Target Rd03D::getTarget(uint8_t targetIndex)
    {
        Target target;

        int bufferIndex = 4 + targetIndex * 8;

        target.x = (serialBuffer[bufferIndex++] | (serialBuffer[bufferIndex++] << 8)) - 0x200;
        target.y = (serialBuffer[bufferIndex++] | (serialBuffer[bufferIndex++] << 8)) - 0x8000;
        target.speedCentimetersPerSecond = (serialBuffer[bufferIndex++] | (serialBuffer[bufferIndex++] << 8)) - 0x10;
        target.distanceResolutionMillimeters = (serialBuffer[bufferIndex++] | (serialBuffer[bufferIndex++] << 8));
        target.distanceMillimeters = sqrt(pow(target.x, 2) + pow(target.y, 2));
        target.angle = atan2(target.y, target.x) * 180.0 / PI;
        return target;
    }

    /// @brief Processes the received data.
    /// @return true, if at least one target was found; otherwise false.
    bool Rd03D::processData()
    {
        int minBufferCount = 25;
        if (!multiTargetMode)
        {
            minBufferCount = 10;
        }

        if (serialBufferCount > minBufferCount && serialBuffer[serialBufferCount - 1] == 0xCC &&
            serialBuffer[serialBufferCount - 2] == 0x55)
        {
            /* RX_BUF: 0xAA 0xFF 0x03 0x00                   Header
             *  0x05 0x01 0x19 0x82 0x00 0x00 0x68 0x01      target1
             *  0xE3 0x81 0x33 0x88 0x20 0x80 0x68 0x01      target2
             *  0x00 0x00 0x00 0x00 0x00 0x00 0x00 0x00      target3
             *  0x55 0xCC
             */

            // Extract data for Targets
            target1 = getTarget(0);
            target2 = getTarget(1);
            target3 = getTarget(2);
            // Serial.print("Target 1 - Distance: " + String(target1.distanceMillimeters));
            // Serial.print(", Target 2 - Distance: " + String(target2.distanceMillimeters));
            // Serial.println(", Target 3 - Distance: " + String(target3.distanceMillimeters));

            // Reset buffer and counter
            memset(serialBuffer, 0x00, sizeof(serialBuffer));
            serialBufferCount = 0;

            return target1.distanceMillimeters > 0 || target2.distanceMillimeters > 0 || target2.distanceMillimeters > 0;
        }
        return false;
    }
}

#endif // USE_RD_03D