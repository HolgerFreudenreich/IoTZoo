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
#ifdef USE_OLED_SSD1306
#include "./displays/SSD1306.hpp"

namespace IotZoo
{
    OledSsd1306Display::OledSsd1306Display(int deviceIndex, Settings* const settings, MqttClient* mqttClient, const String& baseTopic, u_int8_t i2cAddress)
        : DeviceBase(deviceIndex, settings, mqttClient, baseTopic)
    {
        Serial.println("Constructor OledSsd1306Display, deviceIndex: " + String(deviceIndex));
        oled = new SSD1306AsciiWire();
        setupDisplay(i2cAddress);
    }

    OledSsd1306Display::~OledSsd1306Display()
    {
        Serial.println("Destructor OledSsd1306Display, deviceIndex: " + String(deviceIndex));
    }

    /// @brief Let the user know what the device can do.
    /// @param topics
    void OledSsd1306Display::addMqttTopicsToRegister(std::vector<Topic>* const topics) const
    {
        for (int line = 0; line < 6; line++)
        {
            String topicLine = getBaseTopic() + "/oled/" + String(getDeviceIndex()) + "/line/" + String(line) + "/text";
            topics->emplace_back(topicLine, "Payload: text", MessageDirection::IotZooClientOutbound);
        }
        String topicInvertDisplay = getBaseTopic() + "/oled/" + String(getDeviceIndex()) + "/invert";
        topics->emplace_back(topicInvertDisplay, "Payload: 1: invert; 0: normal", MessageDirection::IotZooClientOutbound);
        String topicClearDisplay = getBaseTopic() + "/oled/" + String(getDeviceIndex()) + "/invert";
        topics->emplace_back(topicClearDisplay, "Clears the display.", MessageDirection::IotZooClientOutbound);
    }

    /// @brief Subscribe to Topics
    void OledSsd1306Display::onMqttConnectionEstablished()
    {
        Serial.println("OledSsd1306Display::onMqttConnectionEstablished");
        if (mqttCallbacksAreRegistered)
        {
            Serial.println("Reconnection -> nothing to do.");
            return;
        }
        for (int line = 0; line < 6; line++)
        {
            String topicLine = getBaseTopic() + "/oled/" + String(getDeviceIndex()) + "/line/" + String(line) + "/text";

            mqttClient->subscribe(topicLine, [=](const String& payload) { setTextLine(line, payload); });
        }
        String topicInvertDisplay = getBaseTopic() + "/oled/" + String(getDeviceIndex()) + "/invert";
        mqttClient->subscribe(topicInvertDisplay,
                              [=](const String& payload)
                              {
                                  bool invert = payload == "1";
                                  oled->invertDisplay(invert);
                              });

        String topicClearDisplay = getBaseTopic() + "/oled/" + String(getDeviceIndex()) + "/clear";
        mqttClient->subscribe(topicClearDisplay, [=](const String& payload) { oled->clear(); });
    }

    void OledSsd1306Display::onIotZooClientUnavailable()
    {
        oled->clear();
    }

    // ------------------------------------------------------------------------------------------------
    // Prints the text <@see text> in lineNumber <@lineNumber>.
    // ------------------------------------------------------------------------------------------------
    void OledSsd1306Display::setTextLine(u_int8_t lineNumber, const String& text)
    {
        if (lineNumber > 6)
        {
            Serial.println("Invalid line number " + String(lineNumber));
            return;
        }
        Serial.println(text + " on line number " + String(lineNumber));
        oled->setCursor(0, lineNumber);
        oled->clearToEOL();
        oled->print(text);
    }

    void OledSsd1306Display::setupDisplay(uint8_t i2cAddress)
    {
        Wire.begin();
        Wire.setClock(400000L);

        oled->begin(&Adafruit128x64, i2cAddress);

        oled->setFont(lcd5x7); // Verdana12_bold

        oled->clear();
        oled->setCursor(0, 1);

        // oled.set2X();
        // oled.invertDisplay(true);
        oled->setContrast(8);

        setTextLine(1, "I");
        setTextLine(2, "love");
        setTextLine(3, "IotZoo!");
    }

} // namespace IotZoo

#endif // USE_OLED_SSD1306