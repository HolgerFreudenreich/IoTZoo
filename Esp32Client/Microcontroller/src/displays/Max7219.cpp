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
#ifdef USE_MAX7219

#include "displays/Max7219.hpp"

namespace IotZoo
{
    Max7219::Max7219(int deviceIndex, Settings* const settings, MqttClient* mqttClient, const String& baseTopic, u_int8_t numberOfDevices,
                     u_int8_t dataPin, u_int8_t clkPin, u_int8_t csPin)
        : DeviceBase(deviceIndex, settings, mqttClient, baseTopic)
    {
        Serial.println("Constructor Max7219 dataPin: " + String(dataPin) + ", clkPin: " + String(clkPin) + ", csPin: " + String(csPin));

        max7219 = new MD_MAX72XX(MD_MAX72XX::GENERIC_HW, dataPin, clkPin, csPin, numberOfDevices);
        if (nullptr != max7219)
        {
            max7219->begin();
            max7219->clear();
        }
    }

    /// @brief Let the user know what the device can do.
    /// @param topics
    void Max7219::addMqttTopicsToRegister(std::vector<Topic>* const topics) const
    {
        topics->emplace_back(getBaseTopic() + "/max7219/" + String(deviceIndex) + "/setPoint", "{\"row\": 0, \"col\":1, \"on\": true}",
                             MessageDirection::IotZooClientOutbound);
        topics->emplace_back(getBaseTopic() + "/max7219/" + String(deviceIndex) + "/setColumn", "{\"col\":0, \"value\": 1}; value: Bitfield 0-255",
                             MessageDirection::IotZooClientOutbound);
        topics->emplace_back(getBaseTopic() + "/max7219/" + String(deviceIndex) + "/setRow", "{\"row\":0, \"value\": 255}; value: Bitfield 0-255",
                             MessageDirection::IotZooClientOutbound);
        topics->emplace_back(getBaseTopic() + "/max7219/" + String(deviceIndex) + "/clear", "{}", MessageDirection::IotZooClientOutbound);
        topics->emplace_back(getBaseTopic() + "/max7219/" + String(deviceIndex) + "/allOn", "Turns all pixels on",
                             MessageDirection::IotZooClientOutbound);
    }

    /// @brief The MQTT connection is established. Now subscribe to the topics. An existing MQTT connection is a
    /// prerequisite for a subscription.
    /// @param mqttClient
    /// @param baseTopic
    void Max7219::onMqttConnectionEstablished()
    {
        Serial.println("Max7219::onMqttConnectionEstablished");
        if (mqttCallbacksAreRegistered)
        {
            Serial.println("Reconnection -> nothing to do.");
            return;
        }

        mqttClient->subscribe(getBaseTopic() + "/max7219/" + String(deviceIndex) + "/setPoint",
                              [&](const String& json)
                              {
                                  Serial.println("setPoint json: " + json);

                                  StaticJsonDocument<256> jsonDocument;
                                  if (!deserializeStaticJsonAndPublishError(jsonDocument, json))
                                  {
                                      return;
                                  }
                                  u8_t row  = jsonDocument["row"].as<u8_t>();
                                  u8_t col  = jsonDocument["col"].as<u8_t>();
                                  bool isOn = jsonDocument["on"].as<bool>();
                                  max7219->setPoint(row, col, isOn);
                              });

        mqttClient->subscribe(getBaseTopic() + "/max7219/" + String(deviceIndex) + "/setColumn",
                              [&](const String& json)
                              {
                                  Serial.println("setColumn json: " + json);

                                  StaticJsonDocument<256> jsonDocument;
                                  if (!deserializeStaticJsonAndPublishError(jsonDocument, json))
                                  {
                                      return;
                                  }
                                  u8_t col   = jsonDocument["col"].as<u8_t>();
                                  u8_t value = jsonDocument["value"].as<u8_t>();
                                  max7219->setColumn(col, value);
                              });
        mqttClient->subscribe(getBaseTopic() + "/max7219/" + String(deviceIndex) + "/setRow",
                              [&](const String& json)
                              {
                                  Serial.println("setRow json: " + json);

                                  StaticJsonDocument<256> jsonDocument;
                                  if (!deserializeStaticJsonAndPublishError(jsonDocument, json))
                                  {
                                      return;
                                  }
                                  u8_t row   = jsonDocument["row"].as<u8_t>();
                                  u8_t value = jsonDocument["value"].as<u8_t>();
                                  max7219->setRow(row, value);
                              });
        mqttClient->subscribe(getBaseTopic() + "/max7219/" + String(deviceIndex) + "/clear",
                              [&](const String& json)
                              {
                                  Serial.println("clear json: " + json);
                                  max7219->clear();
                              });

        mqttClient->subscribe(getBaseTopic() + "/max7219/" + String(deviceIndex) + "/allOn",
                              [&](const String& json)
                              {
                                  Serial.println("all on. columnCount: " + String(max7219->getColumnCount()));
                                  for (int i = 0; i < max7219->getColumnCount(); i++)
                                  {
                                      max7219->setColumn(i, 255);
                                  }
                              });
    }

} // namespace IotZoo

#endif // USE_MAX7219