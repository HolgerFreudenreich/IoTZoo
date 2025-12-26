// --------------------------------------------------------------------------------------------------------------------
//      ____    ______   _____
//     /  _/___/_  __/  /__  / ____  ____
//     / // __ \/ /       / / / __ \/ __ \  P L A Y G R O U N D
//   _/ // /_/ / /       / /_/ /_/ / /_/ /
//  /___/\____/_/       /____|____/\____/   (c) 2025 - 2026 Holger Freudenreich under the MIT licence.
//
// --------------------------------------------------------------------------------------------------------------------
// Connect TM1638 8 digit display 'LED&KEY' with microcontrollers in a simple way.
// --------------------------------------------------------------------------------------------------------------------
#include "Defines.hpp"
#ifdef USE_LED_AND_KEY

#include "TM1638.hpp"

namespace IotZoo
{
    TM1638::TM1638(int deviceIndex, Settings* const settings, MqttClient* const mqttClient, const String& baseTopic, uint8_t strobe, uint8_t clock,
                   uint8_t data, bool highfreq)
        : DeviceBase(deviceIndex, settings, mqttClient, baseTopic)
    {
        Serial.println("Constructor TM1638");
        tm1638plus = new TM1638plus(strobe, clock, data, highfreq);
        tm1638plus->displayBegin();
    }

    TM1638::~TM1638()
    {
        delete tm1638plus;
        tm1638plus = nullptr;
    }

    void TM1638::setServerDownText(const String& serverDownText)
    {
        this->serverDownText = serverDownText;
    }

    const String& TM1638::getServerDownText() const
    {
        return serverDownText;
    }

    /// @brief Let the user know what the device can do.
    /// @param topics
    void TM1638::addMqttTopicsToRegister(std::vector<Topic>* const topics) const
    {
        topics->emplace_back(getBaseTopic() + "/ledAndKey/0/button_row/state", "State of the 8 Buttons.", MessageDirection::IotZooClientInbound);

        topics->emplace_back(getBaseTopic() + "/ledAndKey/0/text", "Text to display.", MessageDirection::IotZooClientOutbound);

        topics->emplace_back(getBaseTopic() + "/ledAndKey/0/humber", "Number to display.", MessageDirection::IotZooClientOutbound);
        for (int ledNumber = 0; ledNumber < 8; ledNumber++)
        {
            topics->emplace_back(getBaseTopic() + "/ledAndKey/0/led/" + String(ledNumber), "Payload: 0 = off, 1 = on",
                                 MessageDirection::IotZooClientOutbound);
        }
    }

    /// @brief The MQTT connection is established. Now subscribe to the topics. An existing MQTT connection is a prerequisite
    /// for a subscription.
    /// @param mqttClient
    /// @param baseTopic
    void TM1638::onMqttConnectionEstablished()
    {
        Serial.println("TM1638::onMqttConnectionEstablished");
        if (mqttCallbacksAreRegistered)
        {
            Serial.println("Reconnection -> nothing to do.");
            return;
        }
        for (int ledNumber = 0; ledNumber < 8; ledNumber++)
        {
            String topicLed = getBaseTopic() + "/ledAndKey/0/led/" + String(ledNumber);
            mqttClient->subscribe(topicLed, [=](const String& text) { tm1638plus->setLED(ledNumber, atoi(text.c_str() /* 0 = off, 1 = on*/)); });
        }

        String topicLedAndKeyText = getBaseTopic() + "/ledAndKey/0/text";
        mqttClient->subscribe(topicLedAndKeyText,
                              [=](const String& text)
                              {
                                  Serial.println("topicLedAndKeyText: " + text);
                                  tm1638plus->reset();
                                  tm1638plus->displayText(text.c_str());
                              });

        String topicLedAndKeyNumber = getBaseTopic() + "/ledAndKey/0/number";
        mqttClient->subscribe(topicLedAndKeyNumber,
                              [=](const String& payload)
                              {
                                  Serial.println("topicLedAndKeyNumber: " + payload);
                                  tm1638plus->reset();
                                  tm1638plus->displayIntNum(atoi(payload.c_str()), false, AlignTextType_e::TMAlignTextRight);
                              });

        for (int ledIndex = 0; ledIndex < 8; ledIndex++)
        {
            // subscribeLed(getBaseTopic(), ledIndex);
        }

        String topicLedAndKeyCommand = getBaseTopic() + "/ledAndKey/0/command";
        mqttClient->subscribe(topicLedAndKeyCommand,
                              [=](const String& text)
                              {
                                  if (text == "reset")
                                  {
                                      tm1638plus->reset();
                                  }
                                  /*else if (text == "brightness")
                                  {
                                    tm1638plus.brightness(atoi(text.c_str()));
                                  }*/
                              });
    }

    /// @brief The IotZooMqtt client is not available, so tell this this user. Providing false information is worse than not
    /// providing any information.
    ///        This method is a suitable point to erase a display or stop something.
    void TM1638::onIotZooClientUnavailable()
    {
        tm1638plus->displayText(getServerDownText().c_str());
    }

    void TM1638::loop()
    {
        int8_t buttonsState = tm1638plus->readButtons();

        if (lastButtonsState != buttonsState)
        {
            /* buttons contains a byte with values of button s8s7s6s5s4s3s2s1
             HEX  :  Switch no : Binary
             0x01 : S1 Pressed  0000 0001
             0x02 : S2 Pressed  0000 0010
             0x04 : S3 Pressed  0000 0100
             0x08 : S4 Pressed  0000 1000
             0x10 : S5 Pressed  0001 0000
             0x20 : S6 Pressed  0010 0000
             0x40 : S7 Pressed  0100 0000
             0x80 : S8 Pressed  1000 0000
            */
            // Serial.println(buttonsState, HEX);
            doLeds(buttonsState);
            lastButtonsState = buttonsState;
            tm1638plus->displayIntNum(buttonsState, true, TMAlignTextLeft);
#ifdef USE_MQTT
            String topicLedAndKeyButtonState = getBaseTopic() + "/ledAndKey/button_row/state";
            mqttClient->publish(topicLedAndKeyButtonState, String(buttonsState));
#endif
        }
    }

    void TM1638::doLeds(uint8_t value)
    {
        for (uint8_t ledPosition = 0; ledPosition < 8; ledPosition++)
        {
            tm1638plus->setLED(ledPosition, value & 1);

            value = value >> 1;
        }
    }
} // namespace IotZoo
#endif // USE_LED_AND_KEY