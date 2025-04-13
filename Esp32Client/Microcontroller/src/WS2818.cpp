// --------------------------------------------------------------------------------------------------------------------
//      ____    ______   _____
//     /  _/___/_  __/  /__  / ____  ____
//     / // __ \/ /       / / / __ \/ __ \  P L A Y G R O U N D
//   _/ // /_/ / /       / /_/ /_/ / /_/ /
//  /___/\____/_/       /____|____/\____/   (c) 2025 Holger Freudenreich under the MIT licence.
//
// --------------------------------------------------------------------------------------------------------------------
// Connect WS2812, WS2818 Leds with microcontrollers in a simple way.
// --------------------------------------------------------------------------------------------------------------------
// WS2818 Adafruit_NeoPixel
// --------------------------------------------------------------------------------------------------------------------
#include "Defines.hpp"
#ifdef USE_WS2818
#include "WS2818.hpp"

namespace IotZoo
{
    WS2818::WS2818(int deviceIndex, MqttClient* const mqttClient, const String& baseTopic, int pin, int numberOfLeds)
        : DeviceBase(deviceIndex, mqttClient, baseTopic)
    {
        Serial.println("Constructor WS2818");
        dioPin             = pin;
        this->numberOfLeds = numberOfLeds;
        pixels             = new Adafruit_NeoPixel(numberOfLeds, dioPin, (NEO_GRB + NEO_KHZ800));
        setup();
    }

    WS2818::~WS2818()
    {
        Serial.println("Destructor WS2818");
        delete pixels;
        pixels = NULL;
    }

    void WS2818::setup()
    {
        Serial.println("WS2818 setup. DIN Pin is " + String(dioPin));
        pixels->begin();

        setPixelColorRGB(255, 0, 0, 0, 25);
        delay(125);
        setPixelColorRGB(0, 255, 0, 1, 25);
        delay(125);
        setPixelColorRGB(0, 0, 255, 2, 25);
        delay(125);
        setPixelColorRGB(0, 0, 0, 0, 0);
    }

    /// @brief Example: iotzoo/esp32/08:D1:F9:E0:31:78/neo/0/setPixelColorRGB
    /// @param json
    void WS2818::setPixelColorRGB(const String& json)
    {
        Serial.println("setPixelColorRGB rawData: " + String(json)); // {"r": 0, "g": 125, "b": 0, "index": 15, "length": 1, "brightness": 33}
        u_int8_t  r          = 0;
        u_int8_t  g          = 0;
        u_int8_t  b          = 0;
        u_int16_t startIndex = 0;
        u32_t     length     = 1;
        u_int8_t  brightness = 0;

        StaticJsonDocument<200> jsonDocument;

        DeserializationError error = deserializeJson(jsonDocument, json);
        if (error)
        {
            publishError("deserializeJson() of '" + String(json) + "' failed: " + String(error.f_str()));
        }

        r          = jsonDocument["r"].as<u_int8_t>();
        g          = jsonDocument["g"].as<u_int8_t>();
        b          = jsonDocument["b"].as<u_int8_t>();
        startIndex = jsonDocument["index"].as<u_int16_t>();
        brightness = jsonDocument["brightness"].as<u32_t>();

        length = jsonDocument["length"].as<u32_t>();
        if (length < 1)
        {
            length = 1;
        }

        Serial.println("setPixelColor r: " + String(r) + ", g: " + String(g) + ", b, " + String(b) + ", startIndex: " + String(startIndex) +
                       ", brightness: " + brightness + ", length: " + String(length));

        for (u_int16_t index = startIndex; index < startIndex + length; index++)
        {
            setPixelColorRGB(r, g, b, index, brightness);
        }
    }

    /// @brief Example: iotzoo/esp32/08:D1:F9:E0:31:78/neo/0/setPixelColor
    /// @param json
    void WS2818::setPixelColor(const String& json)
    {
        Serial.println("setPixelColor rawData: " + String(json)); // {"color": 1106052, "index": 15, "length": 1, "brightness": 33}
        u_int32_t color;

        u_int16_t startIndex;
        u_int16_t length     = 1;
        u_int8_t  brightness = 2; // < 2 means off

        StaticJsonDocument<200> jsonDocument;

        DeserializationError error = deserializeJson(jsonDocument, json);
        if (error)
        {
            publishError("deserializeJson() of '" + String(json) + "' failed: " + String(error.f_str()));
        }

        color = jsonDocument["color"].as<u_int32_t>();

        startIndex = jsonDocument["index"].as<u_int16_t>();
        brightness = jsonDocument["brightness"].as<u32_t>();

        length = jsonDocument["length"].as<u_int16_t>();
        if (length < 1)
        {
            length = 1;
        }
        if (brightness > 0 and brightness < 2)
        {
            brightness = 2;
        }

        Serial.println("setPixelColor color: " + String(color) + ", startIndex: " + String(startIndex) + ", brightness: " + brightness +
                       ", length: " + String(length));

        for (u_int16_t index = startIndex; index < startIndex + length; index++)
        {
            setPixelColor(color, index, brightness);
        }
    }

    /// @brief Example: iotzoo/esp32/08:D1:F9:E0:31:78/neo/0/setPixelColor
    /// @param json
    void WS2818::setPixelColorHex(const String& json)
    {
        Serial.println("setPixelColorHex rawData: " + String(json)); // {"color": "0x10E084", "index": 15, "length": 1, "brightness": 33}
        String colorHex;

        u_int16_t startIndex;
        u_int16_t length     = 1;
        u_int8_t  brightness = 2; // 0 means off

        StaticJsonDocument<200> jsonDocument;

        DeserializationError error = deserializeJson(jsonDocument, json);
        if (error)
        {
            publishError("deserializeJson() of '" + String(json) + "' failed: " + String(error.f_str()));
        }

        colorHex = jsonDocument["color"].as<String>();
        if (colorHex.startsWith("#"))
        {
            colorHex = colorHex.substring(1);
        }

        startIndex = jsonDocument["index"].as<u_int16_t>();
        brightness = jsonDocument["brightness"].as<u32_t>();

        if (brightness > 0 and brightness < 2)
        {
            brightness = 2;
        }

        length = jsonDocument["length"].as<u_int16_t>();
        if (length < 1)
        {
            length = 1;
        }

        u_int32_t color = stoi(colorHex.c_str(), 0, 16);

        Serial.println("setPixelColor colorHex: " + String(colorHex) + ", color: " + String(color) + ", startIndex: " + String(startIndex) +
                       ", brightness: " + brightness + ", length: " + String(length));

        for (u_int16_t index = startIndex; index < startIndex + length; index++)
        {
            setPixelColor(color, index, brightness);
        }
    }

    /// @brief Let the user know what the device can do.
    /// @param topics
    void WS2818::addMqttTopicsToRegister(std::vector<Topic>* const topics) const
    {
        topics->push_back(*new Topic(getBaseTopic() + "/neo/0/setPixelColorRGB",
                                     "{\"r\": 0, \"g\": 125, \"b\": 0, \"index\": 15, \"length\": 1, \"brightness\": 35}",
                                     MessageDirection::IotZooClientOutbound));

        topics->push_back(*new Topic(getBaseTopic() + "/neo/0/setPixelColorHex",
                                     "{\"color\": \"#10E084\", \"index\": 0, \"length\": 10, \"brightness\": 10}",
                                     MessageDirection::IotZooClientOutbound));

        topics->push_back(*new Topic(getBaseTopic() + "/neo/0/setPixelColor",
                                     "{\"color\": \"1106052\", \"index\": 0, \"length\": 10, \"brightness\": 10}",
                                     MessageDirection::IotZooClientOutbound));
    }

    /// @brief The MQTT connection is established. Now subscribe to the topics. An existing MQTT connection is a prerequisite
    /// for a subscription.
    /// @param mqttClient
    /// @param baseTopic
    void WS2818::onMqttConnectionEstablished()
    {
        Serial.println("WS2818::onMqttConnectionEstablished");
        if (mqttCallbacksAreRegistered)
        {
            Serial.println("Reconnection -> nothing to do.");
            return;
        }
        String topic = getBaseTopic() + "/neo/" + String(deviceIndex) + "/setPixelColorRGB";

        mqttClient->subscribe(topic, [&](const String& json) { setPixelColorRGB(json); });

        Serial.println("LED strip subscribed to topic " + topic);

        topic = getBaseTopic() + "/neo/" + String(deviceIndex) + "/setPixelColor";

        mqttClient->subscribe(topic, [&](const String& json) { setPixelColor(json); });

        Serial.println("LED strip subscribed to topic " + topic);

        topic = getBaseTopic() + "/neo/" + String(deviceIndex) + "/setPixelColorHex";

        mqttClient->subscribe(topic, [&](const String& json) { setPixelColorHex(json); });
        Serial.println("LED strip subscribed to topic " + topic);
    }

    void WS2818::setPixelColorRGB(uint8_t r, uint8_t g, uint8_t b, uint16_t index, uint8_t brightness /* = 20*/)
    {
        setPixelColor(pixels->Color(r, g, b), index, brightness);
    }

    void WS2818::setPixelColor(uint32_t color, uint16_t index, uint8_t brightness /* = 20*/)
    {
        Serial.println("setPixelColor(color:" + String(color) + ", index: " + String(index) + ", brightness: " + String(brightness) + ")");
        if (pixels->getBrightness() != brightness)
        {
            pixels->setBrightness(brightness);
        }
        pixels->setPixelColor(index, color);
        pixels->show();
    }
} // namespace IotZoo
#endif // USE_WS2818