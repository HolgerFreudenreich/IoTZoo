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
#include <Arduino.h>
#include "HCSR501.hpp"
#include "HRSR501Helper.hpp"

namespace IotZoo
{
    HCSC501::HCSC501(int deviceIndex, MqttClient *const mqttClient, const String &baseTopic,
                     uint8_t pinMotionDetector) : DeviceBase(deviceIndex, mqttClient, baseTopic)
    {
        Serial.println("Constructor HCSC501 pinMotionDetector: " + String(pinMotionDetector));
        this->pinMotionDetector = pinMotionDetector;
        pinMode(pinMotionDetector, INPUT_PULLDOWN);
        setup(HRSR501Helper::readInterrupt);
    }

    HCSC501::~HCSC501()
    {
        Serial.println("Destructor HCSC501 with index " + String(index));
    }

    bool HCSC501::isTriggered()
    {
        bool isTriggered = isTriggered = motionDetectorCounterRising > oldMotionDetectorCounterRising;
        if (isTriggered)
        {
            Serial.println("Motion detector " + String(index) + " triggered! " + String(lastMillisMotionDetectorRising));
            oldMotionDetectorCounterRising = motionDetectorCounterRising;
        }
        return isTriggered;
    }

    unsigned long HCSC501::getCounterRising() const
    {
        return motionDetectorCounterRising;
    }

    void HCSC501::setup(void (*ISR_callback)(void))
    {
        Serial.println("attach interrupt for MotionDetector with deviceIndex " + String(deviceIndex) + ", pin: " + String(pinMotionDetector));
        attachInterrupt(pinMotionDetector, ISR_callback, RISING);
    }

    void IRAM_ATTR HCSC501::isrMotionDetectorRising()
    {
        if (millis() - lastMillisMotionDetectorRising > 100)
        {
            oldMotionDetectorCounterRising = motionDetectorCounterRising;
            motionDetectorCounterRising++;
            lastMillisMotionDetectorRising = millis();
        }
    }

    /// @brief Let the user know what the device can do.
    /// @param topics
    void HCSC501::addMqttTopicsToRegister(std::vector<Topic> *const topics) const
    {
        topics->push_back(*new Topic(getBaseTopic() + "/motion_detector/" + String(getDeviceIndex()) + "/triggered",
                                     "Motion detector " + String(getDeviceIndex()) + "triggered",
                                     MessageDirection::IotZooClientInbound));
    }

    int HCSC501::getIndex() const
    {
        return index;
    }

    void HCSC501::loop()
    {
        String topicMotionDetectorTriggered = getBaseTopic() + "/motion_detector/" + String(getDeviceIndex()) + "/triggered";

        if (isTriggered())
        {
            mqttClient->publish(topicMotionDetectorTriggered, String(getCounterRising()));
        }
    }
}

#endif // USE_HC_SR501