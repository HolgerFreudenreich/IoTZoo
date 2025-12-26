// --------------------------------------------------------------------------------------------------------------------
//      ____    ______   _____
//     /  _/___/_  __/  /__  / ____  ____
//     / // __ \/ /       / / / __ \/ __ \  P L A Y G R O U N D
//   _/ // /_/ / /       / /_/ /_/ / /_/ /
//  /___/\____/_/       /____|____/\____/   (c) 2025 - 2026 Holger Freudenreich under the MIT licence.
//
// --------------------------------------------------------------------------------------------------------------------
// Firmware for ESP8266 and ESP32 Microcontrollers
// --------------------------------------------------------------------------------------------------------------------
#include "Defines.hpp"
#ifdef USE_BUZZER
#include "ArduinoJson.h"
#include "Buzzer.hpp"

namespace IotZoo
{
    Buzzer::Buzzer(int deviceIndex, Settings* const settings, MqttClient* const mqttClient, const String& baseTopic, 
        uint8_t pinBuzzer, uint8_t pinLed)
        : DeviceBase(deviceIndex, settings, mqttClient, baseTopic)
    {
        Serial.print("Constructor Buzzer ");
        Serial.println(toString());
        if (pinLed > 0)
        {
            buzzer = new ::Buzzer(pinBuzzer, pinLed);
        }
        else
        {
            buzzer = new ::Buzzer(pinBuzzer);
        }
        topicBeep = getBaseTopic() + "/buzzer/" + String(deviceIndex) + "/beep";
    }

    Buzzer::~Buzzer()
    {
        Serial.print("Destructor Buzzer");
        Serial.println(toString());
    }

    void Buzzer::beep(u_int16_t frequencyHz, u_int16_t durationMs)
    {
        Serial.print("Beep frequencyHz: " + String(frequencyHz) + ", durationMs: " + String(durationMs));
        buzzer->sound(frequencyHz, durationMs);
    }

    /// @brief Let the user know what the device can do.
    /// @param topics
    void Buzzer::addMqttTopicsToRegister(std::vector<Topic>* const topics) const
    {
        topics->emplace_back(topicBeep, "[{'FrequencyHz': 1000, 'DurationMs': 100}, {'FrequencyHz': 0, 'DurationMs': "
                             "100}, {'FrequencyHz': 2000, 'DurationMs': 100}]",
                             MessageDirection::IotZooClientOutbound);
    }

    /// @brief The MQTT connection is established. Now subscribe to the topics. An existing MQTT connection is a
    /// prerequisite for a subscription.
    /// @param mqttClient
    /// @param baseTopic
    void Buzzer::onMqttConnectionEstablished()
    {
        Serial.println("Buzzer::onMqttConnectionEstablished");
        if (mqttCallbacksAreRegistered)
        {
            Serial.println("Reconnection -> nothing to do.");
            return;
        }

        mqttClient->subscribe(topicBeep,
                              [&](const String& json)
                              {
                                  Serial.println(topicBeep + ": " + json);
                                  StaticJsonDocument<2048> jsonDocument;

                                  DeserializationError error = deserializeJson(jsonDocument, json);
                                  if (error)
                                  {
                                      publishError("deserializeJson() failed: " + String(error.c_str()));
                                  }

                                  JsonArray arrActions = jsonDocument.as<JsonArray>();
                                  for (JsonVariant value : arrActions)
                                  {
                                      u_int16_t frequencyHz = value["FrequencyHz"].as<u_int16_t>();
                                      u_int16_t durationMs  = value["DurationMs"].as<u_int16_t>();
                                      beep(frequencyHz, durationMs);
                                  }
                              });
    }

    String Buzzer::toString()
    {
        return buzzer->toString();
    }
} // namespace IotZoo

#endif // USE_BUZZER