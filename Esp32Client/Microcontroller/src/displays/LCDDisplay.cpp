// --------------------------------------------------------------------------------------------------------------------
//      ____    ______   _____
//     /  _/___/_  __/  /__  / ____  ____
//     / // __ \/ /       / / / __ \/ __ \  P L A Y G R O U N D
//   _/ // /_/ / /       / /_/ /_/ / /_/ /
//  /___/\____/_/       /____|____/\____/   (c) 2025 Holger Freudenreich under the MIT licence.
//
// --------------------------------------------------------------------------------------------------------------------
// Firmware for ESP8266 and ESP32 Microcontrollers
// Support for LCD1602, LCD2004
// --------------------------------------------------------------------------------------------------------------------
#include "Defines.hpp"
#ifdef USE_LCD_160X
#include "./displays/LCDDisplay.hpp"
#include <ArduinoJson.h>

namespace IotZoo
{
    LcdDisplay::LcdDisplay(u_int8_t address, u_int8_t cols, u_int8_t rows,
                           int deviceIndex, MqttClient *mqttClient, const String &baseTopic) : DeviceBase(deviceIndex, mqttClient, baseTopic)
    {
        Serial.println("Constructor LcdDisplay");
        uint8_t heart[8] = {0x0, 0xa, 0x1f, 0x1f, 0xe, 0x4, 0x0};
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
        Serial.println("Destructor LcdDisplay");

        delete lcd;
        lcd = NULL;
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
    }

    void LcdDisplay::setCursor(uint8_t col, uint8_t row)
    {
        lcd->setCursor(col, row);
    }

    /// @brief Let the user know what the device can do.
    /// @param topics
    void LcdDisplay::addMqttTopicsToRegister(std::vector<Topic> *const topics) const
    {
        topics->push_back(*new Topic(getBaseTopic() + "/lcd160x/" + getDeviceIndex(),
                                     "Payload: {'text': 'IoT Zoo', 'clear': true, 'x':1, 'y': 0}",
                                     MessageDirection::IotZooClientOutbound));

        topics->push_back(*new Topic(getBaseTopic() + "/lcd160x/ " + getDeviceIndex() + "/backlight",
                                     "Payload : 0 (off); 1 (on)",
                                     MessageDirection::IotZooClientOutbound));
    }

    /// @brief The MQTT connection is established. Now subscribe to the topics. An existing MQTT connection is a prerequisite for a subscription.
    /// @param mqttClient
    /// @param baseTopic
    void LcdDisplay::onMqttConnectionEstablished()
    {
        Serial.println("LcdDisplay::onMqttConnectionEstablished");
        if (mqttCallbacksAreRegistered)
        {
            Serial.println("Reconnection -> nothing to do.");
            return;
        }
        subscribeSetLcd160xData();
        subscribeSetBacklight();

        mqttCallbacksAreRegistered = true;
    }

    void LcdDisplay::onIotZooClientUnavailable()
    {
        lcd->clear();
        lcd->print("*** OFFLINE! ***");
    }

    size_t LcdDisplay::write(uint8_t data)
    {
        return lcd->write(data);
    }

    void LcdDisplay::setLcd160xBacklight(const String &rawData)
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

    // {"text": "IoT Zoo", "clear": true, "x":1, "y": 0}
    void LcdDisplay::setLcd160xData(const String &json)
    {
        Serial.println("setLcd160xData: " + json);

        String text;
        bool doClear = true;

        u_int8_t x = 0;
        u_int8_t y = 0;

        if (json.startsWith("{"))
        {
            StaticJsonDocument<256> jsonDocument; // on stack

            DeserializationError error = deserializeJson(jsonDocument, json);
            if (error)
            {
                Serial.print(F("deserializeJson() failed: "));
                Serial.println(error.f_str());                
                return;
            }

            text = jsonDocument["text"].as<String>();
            doClear = jsonDocument["clear"].as<bool>();
            x = jsonDocument["x"].as<u_int8_t>();
            y = jsonDocument["y"].as<u_int8_t>();
        }
        else
        {
            text = json;
        }

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

        Serial.println(text);
        if (doClear)
        {
            clear();
        }

        setCursor(x, y);
        print(text);
    }

    void LcdDisplay::subscribeSetLcd160xData()
    {
        String topicLcd160x = getBaseTopic() + "/lcd160x/" + getDeviceIndex();
        Serial.println("Subscribe Topic " + topicLcd160x);
        mqttClient->subscribe(topicLcd160x, [=](const String &json)
                              { setLcd160xData(json); });
    }

    void LcdDisplay::subscribeSetBacklight()
    {
        String topicLcd160x = getBaseTopic() + "/lcd160x/ " + getDeviceIndex() + "/backlight";
        Serial.println("Subscribe Topic " + topicLcd160x);
        mqttClient->subscribe(topicLcd160x, [=](const String &rawData)
                              { setLcd160xBacklight(rawData); });
    }
}

#endif // USE_LCD_160X