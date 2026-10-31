// --------------------------------------------------------------------------------------------------------------------
//      ____    ______   _____
//     /  _/___/_  __/  /__  / ____  ____
//     / // __ \/ /       / / / __ \/ __ \  P L A Y G R O U N D
//   _/ // /_/ / /       / /_/ /_/ / /_/ /
//  /___/\____/_/       /____|____/\____/   (c) 2025 - 2026 Holger Freudenreich under the MIT licence.
//
// --------------------------------------------------------------------------------------------------------------------
// Firmware for ESP8266 and ESP32 Microcontrollers
// Support for LCD1602, LCD2004
// --------------------------------------------------------------------------------------------------------------------
#include "Defines.hpp"
#ifdef USE_LCD_160X
#include "./displays/LCDDisplay.hpp"

#include <ArduinoJson.h>
#include <algorithm>

namespace IotZoo
{
    LcdDisplay::LcdDisplay(int deviceIndex, Settings* const settings, MqttClient* mqttClient, const String& baseTopic, u_int8_t address,
                           u_int8_t cols, u_int8_t rows)
        : DeviceBase(deviceIndex, settings, mqttClient, baseTopic), cols(cols), rows(rows), lastText(static_cast<size_t>(cols) * rows)
    {
        debug("Constructor LcdDisplay");
        uint8_t heart[8] = {0x0, 0xa, 0x1f, 0x1f, 0xe, 0x4, 0x0};
        uint8_t euro[8]  = {0x07, 0x08, 0x1e, 0x08, 0x1e, 0x08, 0x07, 0x00};
        /*
        uint8_t bell[8] = {0x4, 0xe, 0xe, 0xe, 0x1f, 0x0, 0x4};
        uint8_t note[8] = {0x2, 0x3, 0x2, 0xe, 0x1e, 0xc, 0x0};

        uint8_t duck[8] = {0x0, 0xc, 0x1d, 0xf, 0xf, 0x6, 0x0};
        uint8_t check[8] = {0x0, 0x1, 0x3, 0x16, 0x1c, 0x8, 0x0};
        uint8_t cross[8] = {0x0, 0x1b, 0xe, 0x4, 0xe, 0x1b, 0x0};
        uint8_t retarrow[8] = {0x1, 0x1, 0x5, 0x9, 0x1f, 0x8, 0x4};
        */
        lcd = new LiquidCrystal_I2C(address, cols, rows);
        lcd->init();
        lcd->backlight();

        lcd->createChar(0, heart);
        lcd->createChar(1, euro);
        /*
        lcd->createChar(1, bell);
        lcd->createChar(2, note);
        lcd->createChar(3, duck);
        lcd->createChar(4, check);
        lcd->createChar(5, cross);
        lcd->createChar(6, retarrow);
        */
        lcd->home();

        lcd->setCursor(1, 0);
        lcd->print("I");
        lcd->write(0); // ♥
        lcd->setCursor(3, 0);
        lcd->print("IoT Zoo!");
    }

    LcdDisplay::~LcdDisplay()
    {
        debug("Destructor LcdDisplay");

        delete lcd;
        lcd = nullptr;
    }

    void LcdDisplay::turnBacklightOn()
    {
        lcd->setBacklight(1);
    }

    void LcdDisplay::turnBacklightOff()
    {
        lcd->setBacklight(0);
    }

    void LcdDisplay::clear()
    {
        lcd->clear();
        std::fill(lastText.begin(), lastText.end(), String());
    }

    void LcdDisplay::setCursor(uint8_t col, uint8_t row)
    {
        lcd->setCursor(col, row);
    }

    /// @brief Let the user know what the device can do.
    /// @param topics
    void LcdDisplay::addMqttTopicsToRegister(std::vector<Topic>* const topics) const
    {
        topics->emplace_back(getBaseTopic() + "/lcd160x/" + getDeviceIndex(),
                             "Payload: {\"text\": \"IoT Zoo\", \"clear\": true, \"x\": 1, \"y\": 0} or "
                             "[{\"clear\": true}, {\"text\": \"Hello\", \"x\": 0, \"y\": 0}, "
                             "{\"text\": \"World\", \"x\": 0, \"y\": 1}]",
                             MessageDirection::IotZooClientOutbound);

        topics->emplace_back(getBaseTopic() + "/lcd160x/" + getDeviceIndex() + "/backlight", "Payload: 0 (off); 1 (on)",
                             MessageDirection::IotZooClientOutbound);

        topics->emplace_back(getBaseTopic() + "/lcd160x/" + getDeviceIndex() + "/clear", "Payload: none",
                             MessageDirection::IotZooClientOutbound);                             
    }

    /// @brief The MQTT connection is established. Now subscribe to the topics. An existing MQTT connection is a prerequisite for a subscription.
    /// @param mqttClient
    /// @param baseTopic
    void LcdDisplay::onMqttConnectionEstablished()
    {
        debug("LcdDisplay::onMqttConnectionEstablished");
        subscribeSetLcd160xData();
        subscribeSetBacklight();
        subscribeClear();

        mqttCallbacksAreRegistered = true;
    }

    void LcdDisplay::onIotZooClientUnavailable()
    {
        clear();
        lcd->print("*** OFFLINE! ***");
    }

    size_t LcdDisplay::write(uint8_t data)
    {
        return lcd->write(data);
    }

    void LcdDisplay::setBacklightStatus(const String& rawData)
    {
        if (rawData == "1")
        {
            turnBacklightOn();
        }
        else
        {
            turnBacklightOff();
        }
    }

    // Accepts a single object or an array of objects with text, clear, x and y properties.
    void LcdDisplay::setLcd160xData(const String& json)
    {
        debug("setLcd160xData: " + json);

        auto updateDisplay = [this](String text, bool doClear, u_int8_t x, u_int8_t y)
        {
            text.replace("€", "\x01");
            text.replace("Ä", "\xE1");
            text.replace("Ö", "\xEF");
            text.replace("Ü", "\xF5");

            text.replace("Σ", "\xF6");
            text.replace("π", "\xF7");

            text.replace("→", "\x7E");
            text.replace("←", "\x7F");

            text.replace("∞", "\x73");

            text.replace("ä", "\xE1");
            text.replace("ö", "\xEF");
            text.replace("ü", "\xF5");
            text.replace("ß", "\xE2");
            text.replace("°", "\xDF");
            text.replace("µ", "\xE4");
            text.replace("Ω", "\xF4");

            debug(text);
            if (doClear)
            {
                clear();
            }

            updateLcd(x, y, text);
        };

        auto updateFromObject = [&updateDisplay](JsonObjectConst value)
        {
            if (!value.containsKey("text"))
            {
                if (value["clear"].as<bool>())
                {
                    updateDisplay("", true, 0, 0);
                }
                return;
            }

            updateDisplay(value["text"].as<String>(), value["clear"].as<bool>(), value["x"].as<u_int8_t>(), value["y"].as<u_int8_t>());
        };

        if (json.startsWith("{") || json.startsWith("["))
        {
            DynamicJsonDocument  jsonDocument(json.length() * 4 + 64);
            DeserializationError error = deserializeJson(jsonDocument, json);
            if (error)
            {
                publishError("deserializeJson() failed: " + String(error.c_str()));
                return;
            }

            if (json.startsWith("["))
            {
                JsonArrayConst values = jsonDocument.as<JsonArrayConst>();
                if (values.isNull())
                {
                    publishError("LCD data must be a JSON object or array of objects");
                    return;
                }

                for (JsonVariantConst value : values)
                {
                    JsonObjectConst object = value.as<JsonObjectConst>();
                    if (object.isNull())
                    {
                        publishError("LCD data array entries must be JSON objects");
                        return;
                    }

                    updateFromObject(object);
                }
            }
            else
            {
                JsonObjectConst object = jsonDocument.as<JsonObjectConst>();
                if (object.isNull())
                {
                    publishError("LCD data must be a JSON object or array of objects");
                    return;
                }

                updateFromObject(object);
            }
        }
        else
        {
            updateDisplay(json, false, 0, 0);
        }
    }

    void LcdDisplay::updateLcd(uint8_t column, uint8_t row, const String& text)
    {
        if (column >= cols || row >= rows)
        {
            return;
        }
        String& previousText = lastText[static_cast<size_t>(row) * cols + column];
        if (previousText != text)
        {
            setCursor(column, row);

            uint8_t length = min((uint8_t)text.length(), (uint8_t)(cols - column));

            print(text.substring(0, length));

            // Overwrite old characters if the new text is shorter.
            for (uint8_t i = length; i < previousText.length(); i++)
            {
                print(' ');
            }
            previousText = text;
        }
    }

    void LcdDisplay::subscribeSetLcd160xData()
    {
        String topicLcd160x = getBaseTopic() + "/lcd160x/" + getDeviceIndex();
        Serial.println("Subscribe Topic " + topicLcd160x);
        mqttClient->subscribe(topicLcd160x, [=](const String& json) { setLcd160xData(json); });
    }

    void LcdDisplay::subscribeSetBacklight()
    {
        String topicLcd160x = getBaseTopic() + "/lcd160x/" + getDeviceIndex() + "/backlight";
        Serial.println("Subscribe Topic " + topicLcd160x);
        mqttClient->subscribe(topicLcd160x, [=](const String& rawData) { setBacklightStatus(rawData); });
    }

    void LcdDisplay::subscribeClear()
    {
        String topicLcd160x = getBaseTopic() + "/lcd160x/" + getDeviceIndex() + "/clear";
        Serial.println("Subscribe Topic " + topicLcd160x);
        mqttClient->subscribe(topicLcd160x, [=](const String& rawData) { clear(); });
    }
} // namespace IotZoo

#endif // USE_LCD_160X