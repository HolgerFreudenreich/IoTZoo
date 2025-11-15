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
    WS2818::WS2818(int deviceIndex, Settings* const settings, MqttClient* const mqttClient, const String& baseTopic, int pin, int numberOfLeds)
        : DeviceBase(deviceIndex, settings, mqttClient, baseTopic)
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
        pixels = nullptr;
    }

    void WS2818::setup()
    {
        Serial.println("WS2818 setup. DIN Pin is " + String(dioPin));

        pixelProperties.resize(this->numberOfLeds);
        for (int index = 0; index < this->numberOfLeds; index++)
        {
            pixelProperties[index].PixelId            = index;
            pixelProperties[index].MillisUntilTurnOff = 0;
        }

        pixels->begin();

        for (int index = 0; index < this->numberOfLeds; index++)
        {
            setPixelColorRgb(255, 0, 0, index, 10);
        }
        pixels->show();
        for (int index = 0; index < this->numberOfLeds; index++)
        {
            setPixelColorRgb(0, 255, 0, index, 10);
        }
        pixels->show();

        for (int index = 0; index < this->numberOfLeds; index++)
        {
            setPixelColorRgb(0, 0, 255, index, 10);
        }
        pixels->show();

        pixels->clear();
        pixels->show();

        Serial.println("WS2818 setup done.");
    }

    void WS2818::loop()
    {
        // Reset pixel color
        for (auto& pixelProperty : pixelProperties)
        {
            // Serial.println("index:" + String(pixelProperty.PixelId) + ", MillisUntilTurnOff: " +
            //                String(pixelProperties[pixelProperty.PixelId].MillisUntilTurnOff) + ", CurrentMillis: " + String(millis()));

            if (pixelProperty.MillisUntilTurnOff == 0)
            {
                continue;
            }
            if (millis() > pixelProperty.MillisUntilTurnOff)
            {
                // turn the pixel off.
                pixels->setPixelColor(pixelProperty.PixelId, 0);
                pixelProperties[pixelProperty.PixelId].MillisUntilTurnOff = 0;
            }
        }
        pixels->show();
    }

    // @brief Example: iotzoo/esp32/08:D1:F9:E0:31:78/neo/0/setPixelColor
    // @param json
    //{
    //                                        "brightness": 10,
    //                                       "color": "#FFFF00",
    //                                        "pixels": [
    //                                           {
    //                                                "color": "#10E084",
    //                                                "index": 0,
    //                                                "length": 10,
    //                                                "brightness": 10,
    //                                                "millisUntilTurnOff": 2000
    //         },
    //         {
    //             "color": "#FFFFFF",
    //             "index": 30,
    //             "length": 8,
    //             "millisUntilTurnOff": 1000
    //         },
    //         {
    //             "index": 50,
    //             "length": 8,
    //             "millisUntilTurnOff": 3000
    //         }
    //     ]
    // })";
    void WS2818::setPixelColor(const String& json)
    {
        try
        {
            Serial.println("setPixelColorHex rawData: " + String(json)); // {"color": "0x10E084", "index": 15, "length": 1, "brightness": 33}
            String colorHexGlobal;

            u_int16_t startIndex;
            u_int16_t length                   = 1;
            u_int8_t  brightness               = 2; // 0 means off
            u32_t     millisUntilTurnOffGlobal = 0;

            StaticJsonDocument<4096> jsonDocument;
            if (!deserializeStaticJsonAndPublishError(jsonDocument, json))
            {
                return;
            }

            JsonArray elements = jsonDocument["pixels"];
            if (nullptr == elements)
            {
                publishError("\"pixels\":[] missing");
                return;
            }

            bool useGlobalRgb = jsonDocument["r"] != nullptr && jsonDocument["g"] != nullptr && jsonDocument["b"] != nullptr;

            auto brightnessProperty = jsonDocument["brightness"];
            if (nullptr != brightnessProperty)
            {
                brightness = brightnessProperty.as<u_int8_t>();
                if (brightness > 0 and brightness < 2)
                {
                    brightness = 2;
                }
            }
            if (useGlobalRgb && jsonDocument["color"] == nullptr)
            {
                u8_t r         = jsonDocument["r"].as<u8_t>();
                u8_t g         = jsonDocument["g"].as<u8_t>();
                u8_t b         = jsonDocument["b"].as<u8_t>();
                colorHexGlobal = pixels->Color(r, g, b);
                Serial.print("using global rgb");
            }
            else
            {
                colorHexGlobal = jsonDocument["color"].as<String>();
            }
            if (colorHexGlobal.startsWith("#"))
            {
                colorHexGlobal = colorHexGlobal.substring(1);
            }

            auto millisUntilTurnOffPropertyGlobal = jsonDocument["millisUntilTurnOff"];
            if (millisUntilTurnOffPropertyGlobal != nullptr)
            {
                millisUntilTurnOffGlobal = millisUntilTurnOffPropertyGlobal.as<uint64_t>();
            }
            for (JsonVariant pixel : elements)
            {
                Serial.print("New pixel ");
                String colorHex;
                auto   colorProperty = pixel["color"];

                bool useRgb = pixel["r"] != nullptr && pixel["g"] != nullptr && pixel["b"] != nullptr;

                if (!useRgb)
                {
                    if (nullptr != colorProperty)
                    {
                        colorHex = pixel["color"].as<String>();
                        if (colorHex.startsWith("#"))
                        {
                            colorHex = colorHex.substring(1);
                        }
                    }
                    else
                    {
                        colorHex = colorHexGlobal;
                    }
                }
                else
                {
                    u8_t r   = pixel["r"].as<u8_t>();
                    u8_t g   = pixel["g"].as<u8_t>();
                    u8_t b   = pixel["b"].as<u8_t>();
                    colorHex = pixels->Color(r, g, b);
                    Serial.print(", r: " + String(r));
                    Serial.print(", g: " + String(g));
                    Serial.print(", b: " + String(b));
                }

                startIndex = pixel["index"].as<u_int16_t>();

                length = pixel["length"].as<u_int16_t>();
                if (length < 1)
                {
                    length = 1;
                }

                auto     millisUntilTurnOffProperty = pixel["millisUntilTurnOff"];
                uint64_t millisUntilTurnOff         = 0;
                if (millisUntilTurnOffProperty != nullptr)
                {
                    millisUntilTurnOff = millisUntilTurnOffProperty.as<uint64_t>();
                }
                else
                {
                    millisUntilTurnOff = millisUntilTurnOffPropertyGlobal;
                }

                u_int32_t color = stoi(colorHex.c_str(), 0, 16);

                Serial.println("setPixelColor colorHex: " + String(colorHex) + ", color: " + String(color) + ", startIndex: " + String(startIndex) +
                               ", brightness: " + brightness + ", length: " + String(length) + ", MillisUntilTurnOff: " + String(millisUntilTurnOff));

                for (u_int16_t index = startIndex; index < startIndex + length; index++)
                {
                    setPixelColor(color, index, brightness, millisUntilTurnOff);
                    if (millisUntilTurnOff > 0)
                    {
                        pixelProperties[index].MillisUntilTurnOff = millisUntilTurnOff + millis();
                    }
                    else
                    {
                        pixelProperties[index].MillisUntilTurnOff = 0;
                    }
                }
            }
            pixels->show();
        }
        catch (const std::exception& e)
        {
            publishError(e.what());
        }
    }

    void WS2818::setPixelsByPreset(const String& presetName)
    {
        try
        {
            Serial.println("setPixelsByPreset(presetName: " + presetName + ")");
            if (nullptr == settings)
            {
                Serial.println("settings is null!");
                return;
            }
            String json = settings->loadConfiguration(presetName);

            if (json.length())
            {
                setPixelColor(json);
            }
            else
            {
                publishError("no data for preset " + presetName + " found!");
            }
        }
        catch (const std::exception& e)
        {
            publishError(e.what());
        }
    }

    /// @brief Let the user know what the device can do.
    /// @param topics
    void WS2818::addMqttTopicsToRegister(std::vector<Topic>* const topics) const
    {
        String jsonExampleColorHex = R"({
                                            "brightness": 10,
                                            "color": "#FFFF00",
                                            "pixels": [
                                                {
                                                    "r": "255",
                                                    "g": "0",
                                                    "b": 125,
                                                    "index": 0,
                                                    "length": 10,
                                                    "brightness": 10,
                                                    "millisUntilTurnOff": 2000
                                                },
                                                {
                                                    "color": "#FFFFFF",
                                                    "index": 30,
                                                    "length": 8,
                                                    "millisUntilTurnOff": 1000
                                                },
                                                {                       
                                                    "index": 50,
                                                    "length": 8,
                                                    "millisUntilTurnOff": 3000
                                                }
                                            ]
                                        })";

        topics->emplace_back(getBaseTopic() + "/neo/0/setPixelColor", jsonExampleColorHex, MessageDirection::IotZooClientOutbound);

        topics->emplace_back(getBaseTopic() + "/neo/0/setPixelsByPreset", "Smiley", MessageDirection::IotZooClientOutbound);
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

        String topic = getBaseTopic() + "/neo/" + String(deviceIndex) + "/setPixelColor";
        mqttClient->subscribe(topic, [&](const String& json) { setPixelColor(json); });
        Serial.println("LED strip subscribed to topic " + topic);

        topic = getBaseTopic() + "/neo/" + String(deviceIndex) + "/setPixelsByPreset";
        mqttClient->subscribe(topic, [&](const String& presetName) { setPixelsByPreset(presetName); });
        Serial.println("LED strip subscribed to topic " + topic);
    }

    void WS2818::setPixelColorRgb(uint8_t r, uint8_t g, uint8_t b, uint16_t index, uint8_t brightness /* = 20*/, uint64_t millisUntilTurnOff /* = 0*/)
    {
        setPixelColor(pixels->Color(r, g, b), index, brightness, millisUntilTurnOff);
    }

    void WS2818::setPixelColor(uint32_t color, uint16_t index, uint8_t brightness /* = 20*/, uint64_t millisUntilTurnOff /* = 0*/)
    {
        Serial.println("setPixelColor(color:" + String(color) + ", index: " + String(index) + ", brightness: " + String(brightness) +
                       ", millisUntilTurnOff: " + String(millisUntilTurnOff) + ")");
        if (index > this->numberOfLeds)
        {
            Serial.println("index out of range");
            return;
        }
        if (brightness != 0 && pixels->getBrightness() != brightness)
        {
            pixels->setBrightness(brightness); // affects all pixels!!!
        }

        pixels->setPixelColor(index, color);
    }
} // namespace IotZoo
#endif // USE_WS2818