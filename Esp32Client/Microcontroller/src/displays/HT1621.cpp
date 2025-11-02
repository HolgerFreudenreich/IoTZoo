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
#ifdef USE_HT1621

#include "displays/HT1621.hpp"

namespace IotZoo
{
    HT1621::HT1621(u_int8_t csPin, u_int8_t wrPin, u_int8_t dataPin, u_int8_t backlightPin, int deviceIndex, MqttClient* mqttClient,
                   const String& baseTopic)
        : DeviceBase(deviceIndex, mqttClient, baseTopic)
    {
        Serial.println("Constructor HT1621 csPin: " + String(csPin) + ", wrPin: " + String(wrPin) + ", dataPin: " + String(dataPin) +
                       ", backlightPin: " + String(backlightPin) + ", deviceIndex: " + String(deviceIndex) + ", baseTopic: " + baseTopic);

        lcd.begin(csPin, wrPin, dataPin, backlightPin); // initialize the LCD
        lcd.clear();                                    // clear the LCD
    }

    HT1621::~HT1621()
    {
        Serial.print("Destructor HT1621.");
    }

    /// @brief Let the user know what the device can do.
    /// @param topics
    void HT1621::addMqttTopicsToRegister(std::vector<Topic>* const topics) const
    {
        topics->push_back(
            *new Topic(baseTopic + "/ht1621/" + String(deviceIndex) + "/temperature", "Temperature", MessageDirection::IotZooClientOutbound));
        topics->push_back(*new Topic(baseTopic + "/ht1621/" + String(deviceIndex) + "/number", "Number", MessageDirection::IotZooClientOUtbound));
        topics->push_back(
            *new Topic(baseTopic + "/ht1621/" + String(deviceIndex) + "/batteryLevel", "Battery level [0-2]", MessageDirection::IotZooClientOutbound));
    }

    /// @brief The MQTT connection is established. Now subscribe to the topics. An existing MQTT connection is a
    /// prerequisite for a subscription.
    /// @param mqttClient
    /// @param baseTopic
    void HT1621::onMqttConnectionEstablished()
    {
        Serial.println("HT1621::onMqttConnectionEstablished. BaseTopic: " + baseTopic);
        if (mqttCallbacksAreRegistered)
        {
            Serial.println("Reconnection -> nothing to do.");
            return;
        }

        mqttClient->subscribe(baseTopic + "/ht1621/0/temperature",
                              [&](const String& data)
                              {
                                  try
                                  {
                                      Serial.println("HT1621 received temperature data: " + data);
                                      lcd.printCelsius(atof(data.c_str()));
                                  }
                                  catch (...)
                                  {
                                      Serial.println("Data is not a number: " + data);
                                  }
                              });

        mqttClient->subscribe(baseTopic + "/ht1621/0/number",
                              [&](const String& data)
                              {
                                  try
                                  {
                                      Serial.println("HT1621 received number: " + data);
                                      lcd.print(atof(data.c_str()));
                                  }
                                  catch (...)
                                  {
                                      Serial.println("Data is not a number: " + data);
                                  }
                              });
        mqttClient->subscribe(baseTopic + "/ht1621/0/batteryLevel",
                              [&](const String& data)
                              {
                                  try
                                  {
                                      Serial.println("HT1621 received battery level: " + data);
                                      lcd.setBatteryLevel(atoi(data.c_str()));
                                  }
                                  catch (...)
                                  {
                                      Serial.println("Data is not a number: " + data);
                                  }
                              });
    }
} // namespace IotZoo

#endif // USE_HT1621