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
#ifdef USE_HW040

#include "HW040/HW040.hpp"
#include "HW040/HW040Helper.hpp"
#include "MqttClient.hpp"

namespace IotZoo
{
    RotaryEncoder::RotaryEncoder(MqttClient *mqttClient, int deviceIndex, const String &baseTopic,
                                 int boundaryMinValue,
                                 int boundaryMaxValue,
                                 bool circleValues,
                                 int acceleration,
                                 uint8_t encoderSteps,
                                 uint8_t encoderAPin,
                                 uint8_t encoderBPin,
                                 int encoderButtonPin,
                                 int encoderVccPin) : AiEsp32RotaryEncoder(encoderAPin, encoderBPin, encoderButtonPin, encoderVccPin, encoderSteps),
                                                      DeviceBase(deviceIndex, mqttClient, baseTopic)

    {
        Serial.println("constructor RotaryEncoder");
        lastTimeButtonDown = 0;
        wasButtonDown = false;
        topicEncoderValue = baseTopic + "/rotary_encoder/" + String(deviceIndex) + "/value";

        // We must initialize the rotary encoder (attachInterrupt).
        setup(HW040Helper::onInterruptTriggered);

        setBoundaries(boundaryMinValue, boundaryMaxValue, circleValues);

        if (0 == acceleration)
        {
            disableAcceleration(); // acceleration is enabled by default - disable if you dont need it
        }
        else
        {
            setAcceleration(acceleration); // or set the value - larger number = more accelearation; 0 or 1 means disabled acceleration
        }
        begin();
    }

    RotaryEncoder::~RotaryEncoder()
    {
        Serial.println("Deleting RotaryEncoder with deviceIndex " + String(getDeviceIndex()));
    }

    void RotaryEncoder::onReceivedRotaryEncoderValue(const String &strValue)
    {
        long value = std::stol(strValue.c_str());
        setEncoderValue(value - 1);
    }

    /// @brief The MQTT connection is established. Now subscribe to the topics. An existing MQTT connection is a prerequisite for a subscription.
    /// @param mqttClient
    /// @param baseTopic
    void RotaryEncoder::onMqttConnectionEstablished()
    {
        String topic = baseTopic + "/rotary_encoder/" + String(getDeviceIndex()) + "/set_value";
        if (mqttClient->subscribe(topic, [=](const String &payload)
                                  { onReceivedRotaryEncoderValue(payload); }))
        {
            Serial.println("Subscribed topic: " + topic);
        }
    }

    /// @brief Let the user know what the device can do.
    /// @param topics
    void RotaryEncoder::addMqttTopicsToRegister(std::vector<Topic> *const topics) const
    {
        topics->push_back(*new Topic(getBaseTopic() + "/rotary_encoder/" + String(deviceIndex) + "/set_value",
                                     "Sets the value of the rotary encoder " + String(deviceIndex) + ".",
                                     MessageDirection::IotZooClientOutbound));

        topics->push_back(*new Topic(getBaseTopic() + "/rotary_encoder/" + String(deviceIndex) + "/button_pressed",
                                     "Button of rotary encoder " + String(deviceIndex) + " has been pressed.",
                                     MessageDirection::IotZooClientInbound));

        topics->push_back(*new Topic(getBaseTopic() + "/rotary_encoder/" + String(deviceIndex) + "/value",
                                     "Value of Rotary encoder " + String(deviceIndex) + " changed.",
                                     MessageDirection::IotZooClientInbound));
    }

    void RotaryEncoder::setLastTimeButtonDown(unsigned long lastTimeButtonDown)
    {
        this->lastTimeButtonDown = lastTimeButtonDown;
        Serial.println("lastTimeButtonDown is set to " + String(lastTimeButtonDown) + " for RotaryEncoder.");
    }

    unsigned long RotaryEncoder::getLastTimeButtonDown() const
    {
        return lastTimeButtonDown;
    }

    void RotaryEncoder::setWasButtonDown(bool wasButtonDown)
    {
        this->wasButtonDown = wasButtonDown;
    }

    bool RotaryEncoder::getWasButtonDown() const
    {
        return this->wasButtonDown;
    }

    void RotaryEncoder::loop()
    {
        try
        {
            if (NULL == mqttClient)
            {
                Serial.println("mqttClient is NULL!");
                return;
            }

            // bool isClicked = encoder->isEncoderButtonClicked(20);
            bool isClicked = isEncoderButtonDown();

            if (isClicked)
            {
                if (!getWasButtonDown())
                {
                    String millisTmp = String(millis());
                    Serial.println("Button of encoder " + String(deviceIndex) + " is pressed at " + millisTmp + ".");
                    String topicButtonPressed = baseTopic + "/rotary_encoder/" + String(deviceIndex) + "/button_pressed";
                    mqttClient->publish(topicButtonPressed, millisTmp);
                }
            }

            setWasButtonDown(isClicked);

            // don't do anything unless value changed.
            if (encoderChanged())
            {
                Serial.print("Value encoder " + String(deviceIndex) + ": ");
                long rotaryEncoderValue = readEncoder();

                Serial.println(rotaryEncoderValue);

                mqttClient->publish(topicEncoderValue, String(rotaryEncoderValue));
            }
        }
        catch (const std::exception &e)
        {
            Serial.println(e.what());
        }
    }
}
#endif // USE_HW040