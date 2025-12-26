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
#ifdef USE_KEYPAD
#include "ButtonMatrix.hpp"

#include <Arduino.h>

namespace IotZoo
{
    ButtonMatrix::ButtonMatrix(int deviceIndex, Settings* const settings, MqttClient* mqttClient, const String& baseTopic) :
     DeviceBase(deviceIndex, settings, mqttClient, baseTopic)
    {
        keyMap       = makeKeymap(hexaKeys);
        customKeypad = new Keypad(keyMap, rowPins, colPins, ROWS, COLS);
    }

    ButtonMatrix::~ButtonMatrix()
    {
        Serial.println("Deleting ButtonMatrix");
    }

    /// @brief Let the user know what the device can do.
    /// @param topics
    void ButtonMatrix::addMqttTopicsToRegister(std::vector<Topic>* const topics) const
    {
        for (int row = 0; row < getCountOfRows(); row++)
        {
            for (int col = 0; col < getCountOfCols(); col++)
            {
                // char* keypadChar = hexaKeys[row, col];
                char keyChar = getKeyMap()[row * getCountOfRows() + col]; // hmm

                topics->emplace_back(getBaseTopic() + "/button_matrix/" + String(deviceIndex) + "/button/" + keyChar,
                                             "Button " + String(keyChar) + " status changed.", MessageDirection::IotZooClientInbound);

                topics->emplace_back(getBaseTopic() + "/button_matrix/" + String(deviceIndex) + "/button/" + keyChar + "/pressed",
                                             "Button " + String(keyChar) + " was pressed. Payload: millis() of the ESP32.",
                                             MessageDirection::IotZooClientInbound);

                topics->emplace_back(getBaseTopic() + "/button_matrix/" + String(deviceIndex) + "/button/" + keyChar + "/hold",
                                             "Button " + String(keyChar) + " was hold. Payload: millis() of the ESP32.",
                                             MessageDirection::IotZooClientInbound);
                topics->emplace_back(getBaseTopic() + "/button_matrix/" + String(deviceIndex) + "/button/" + keyChar + "/released",
                                             "Button " + String(keyChar) + " was released. Payload: millis() of the ESP32.",
                                             MessageDirection::IotZooClientInbound);
            }
        }
    }

    void ButtonMatrix::loop()
    {
        String msg;
        if (getCustomKeypad()->getKeys())
        {
            Serial.println("get keys...");
            for (int i = 0; i < LIST_MAX; i++) // Scan the whole key list.
            {
                if (getCustomKeypad()->key[i].stateChanged) // Only find keys that have changed state.
                {
                    switch (getCustomKeypad()->key[i].kstate)
                    {
                        // Report active key state : IDLE, PRESSED, HOLD, or RELEASED
                    case PRESSED:
                    {
                        msg     = " PRESSED.";
                        Key key = getCustomKeypad()->key[i];
#ifdef USE_MQTT
                        String topicButton = getBaseTopic() + "/button_matrix/" + String(getDeviceIndex()) + "/button/" + String(key.kchar);
                        mqttClient->publish(topicButton, "PRESSED");
                        topicButton = getBaseTopic() + "/button_matrix/" + String(getDeviceIndex()) + "/button/" + String(key.kchar) + "/pressed";
                        mqttClient->publish(topicButton, String(millis()));
#endif
                    }
                    break;
                    case HOLD:
                    {
                        msg     = " HOLD.";
                        Key key = getCustomKeypad()->key[i];
#ifdef USE_MQTT
                        String topicButton = getBaseTopic() + "/button_matrix/" + String(getDeviceIndex()) + "/button/" + String(key.kchar);
                        mqttClient->publish(topicButton, "HOLD");
                        topicButton = getBaseTopic() + "/button_matrix/" + String(getDeviceIndex()) + "/button/" + String(key.kchar) + "/hold";
                        mqttClient->publish(topicButton, String(millis()));
#endif
                    }
                    break;
                    case RELEASED:
                    {
                        msg     = " RELEASED.";
                        Key key = getCustomKeypad()->key[i];
#ifdef USE_MQTT
                        String topicButton = getBaseTopic() + "/button_matrix/" + String(getDeviceIndex()) + "/button/" + String(key.kchar);
                        mqttClient->publish(topicButton, "RELEASED");
                        topicButton = getBaseTopic() + "/button_matrix/" + String(getDeviceIndex()) + "/button/" + String(key.kchar) + "/released";
                        mqttClient->publish(topicButton, String(millis()));
#endif
                    }
                    break;
                    case IDLE:
                    {
                        msg = " IDLE.";
                    }
                    }
                    Serial.print("Key ");
                    Serial.print(getCustomKeypad()->key[i].kchar);
                    Serial.println(msg);
                }
            }
        }
        else
        {
        }
    }
} // namespace IotZoo

#endif // USE_KEYPAD