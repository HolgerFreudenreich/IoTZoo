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
#if defined(USE_TM1637_4) || defined(USE_TM1637_6)
#include "./displays/TM1637/TM1637Helper.hpp"
#include "./displays/TM1637/TM1637_Handling.hpp"

namespace IotZoo
{
    TM1637_Handling::TM1637_Handling(Tm1637DisplayType tm1637DisplayType) : DeviceHandlingBase()
    {
        this->tm1637DisplayType = tm1637DisplayType;
    }

    void TM1637_Handling::setup()
    {
        int index = 0;
        for (auto& display : displays1637)
        {
            display.clear();
            display.setBrightness(0x0A, true); // 0x0f = max brightness
            String text = String(index);
            display.showString(text.c_str());
            index++;
        }
    }

    void TM1637_Handling::onIotZooClientUnavailable()
    {
        for (auto& display : displays1637)
        {
            display.onIotZooClientUnavailable();
        }
    }

#ifdef USE_INTERNAL_MQTT
    void TM1637_Handling::subscribeToInternalMqttTopics(InternalMqttClient* internalMqttClient, const String& baseTopic)
    {
        debug("TM1637_Handling subscribing to internal MQTT topics...");

        // Which devices want to subscribe to this topic? We don't know that here.
        // Subscribe to all available topics.
        for (auto& display : displays1637)
        {
            for (const Topic& topic : display.getTopics())
            {
                if (topic.Direction == static_cast<int>(MessageDirection::IotZooClientOutbound))
                {
                    String topicName = topic.TopicName;
                    debug("Subscribing to internal MQTT topic: " + topicName);
                    InternalTopic internalTopic(topic.TopicName.c_str());
                    internalMqttClient->subscribe(internalTopic);
                }
            }
        }
    }
#endif // USE_INTERNAL_MQTT

    // Subscribe to external MQTT topics.
    void TM1637_Handling::onMqttConnectionEstablished(MqttClient* mqttClient, const String& baseTopic)
    {
        debug("TM1637_4_Handling::onMqttConnectionEstablished");
        if (callbacksAreRegistered)
        {
            debug("Reconnection -> nothing to do.");
            return;
        }

        this->mqttClient = mqttClient;
        if (nullptr != mqttClient)
        {
            debug("MQTT client is available. Registering callbacks for TM1637_4_Handling...");

            for (auto& display : displays1637)
            {
                String topicTm1637 = baseTopic + "/tm1637_4/" + String(display.getDeviceIndex()) + "/number";
                mqttClient->subscribe(topicTm1637, callbackMqttOnReceivedDataTm1637Number);
            }

            for (auto& display : displays1637)
            {
                String topicTm1637 = baseTopic + "/tm1637_4/" + String(display.getDeviceIndex()) + "/time";
                mqttClient->subscribe(topicTm1637, callbackMqttOnReceivedDataTm1637Number);
            }

            for (auto& display : displays1637)
            {
                String topicTm1637 = baseTopic + "/tm1637_4/" + String(display.getDeviceIndex()) + "/text";
                mqttClient->subscribe(topicTm1637, callMqttbackOnReceivedDataTm1637Text);
            }

            for (auto& display : displays1637)
            {
                String topicTm1637 = baseTopic + "/tm1637_4/" + String(display.getDeviceIndex()) + "/level";
                mqttClient->subscribe(topicTm1637, callbackMqttOnReceivedDataTm1637Level);
            }

            for (auto& display : displays1637)
            {
                String topicTm1637 = baseTopic + "/tm1637_4/" + String(display.getDeviceIndex()) + "/temperature";
                mqttClient->subscribe(topicTm1637, callbackMqttOnReceivedDataTm1637Number);
            }
        }
        Serial.println(".");
        callbacksAreRegistered = true;
    }

    void TM1637_Handling::addMqttTopicsToRegister(std::vector<Topic>* const topics) const
    {
        for (auto& display : displays1637)
        {
            display.addMqttTopicsToRegister(topics);
        }
    }

    /// @brief Data received to display on a TM1637 display.
    /// @param rawData: data in json format or unformatted.
    void TM1637_Handling::onReceivedDataTm1637_Number(const String& rawData, int deviceIndex)
    {
        debug("onReceivedDataTm1637_Number " + rawData);

        TM1637* display = getDisplayByDeviceIndex(deviceIndex);

        if (nullptr != display)
        {
            int index = 0;

            String data(rawData);
            data.trim();
            data.toLowerCase();

            bool containsColon = false;
            if (data[0] != '{' && data.indexOf(":") > 0)
            {
                containsColon = true;
                data.replace(":", ""); // needed to display Time like 10:23
            }

            int  number           = 0;
            bool showLeadingZeros = false;
            int  displayLength    = display->getDefaultDisplayLength();
            int  position         = 0;
            int  dots             = 0;

            if (containsColon)
            {
                dots = 64; // 01000000
            }
            else
            {
                IotZoo::TM1637Helper tm1637Helper(data);
                dots = tm1637Helper.getDots();
            }

            display->setBrightness(0x0A, true); // 0x0f = max brightness. Do not delete this, the display may be turned off.

            try
            {
                number = std::stoi(data.c_str());
            }
            catch (const std::exception& e)
            {
                Serial.println("Unable to convert to a number!");
            }

            debug("device index: " + String(deviceIndex) + "; number: " + String(number) + "; LeadingZeros: " + String(showLeadingZeros) +
                  "; displayLength: " + String(displayLength) + "; position: " + String(position) + "; dots: " + String(dots));
            display->showNumberDec(number, dots, showLeadingZeros, displayLength, position);
        }
    }

    void TM1637_Handling::callbackMqttOnReceivedDataTm1637Number(const String& topic, const String& message)
    {
        debug("callbackMqttOnReceivedDataTm1637Number topic: " + topic + " message: " + message);

        int indexEnd = topic.lastIndexOf("/");
        debug("indexEnd: " + String(indexEnd));
        if (indexEnd >= 0)
        {
            int deviceIndex = topic.c_str()[indexEnd - 1] - '0'; // at least 10 (0 - 9).
            debug("deviceIndex: " + String(deviceIndex));
            debug("topic: " + String(topic.c_str()[indexEnd - 1]));
            onReceivedDataTm1637_Number(message, deviceIndex);
        }
    }

    /// @brief Data received to display on a TM1637 display.
    /// @param rawData: data in json format or unformatted.
    void TM1637_Handling::onReceivedDataTm1637_Temperature(const String& rawData, int deviceIndex)
    {
        debug("onReceivedDataTm1637_Temperature " + rawData);

        TM1637* display = getDisplayByDeviceIndex(deviceIndex);

        if (nullptr != display)
        {
            String strTemperature(rawData);
            strTemperature.trim();
            debug("device index: " + String(deviceIndex) + "; temperature: " + strTemperature);
            display->showString(strTemperature.c_str(), display->getDefaultDisplayLength());
        }
    }

    void TM1637_Handling::callbackMqttOnReceivedDataTm1637Temperature(const String& topic, const String& message)
    {
        try
        {
            String t(message);
            t.trim();

            int indexDot = t.indexOf(".");
            if (indexDot > 0)
            {
                t = t.substring(0, indexDot + 2); // one decimal place
            }

            t += "°C";

            IotZoo::TM1637Helper tm1637Helper(t);

            if (t.length() == 5)
            {
                t = " " + t;
            }
            else if (t.length() == 4)
            {
                t = "  " + t;
            }

            int indexEnd = topic.lastIndexOf("/");
            Serial.println("indexEnd: " + String(indexEnd));
            if (indexEnd >= 0)
            {
                int deviceIndex = topic.c_str()[indexEnd - 1] - '0'; // at least 10 (0 - 9).
                Serial.println(deviceIndex);
                Serial.println(topic.c_str()[indexEnd - 1]);

                auto display = displays1637.begin();
                if (deviceIndex > 0)
                {
                    std::advance(display, deviceIndex);
                }
                display->setBrightness(0x0c, true); // 0x0f = max brightness. Do not delete this, the display may be turned off.
                display->showString(t.c_str(), 6U, 0, tm1637Helper.getDots());
            }
        }
        catch (const std::exception& e)
        {
        }
    }

    void TM1637_Handling::callMqttbackOnReceivedDataTm1637Text(const String& topic, const String& message)
    {
        debug("callMqttbackOnReceivedDataTm1637Text topic: " + topic + " message: " + message);

        int indexEnd = topic.lastIndexOf("/");
        debug("indexEnd: " + String(indexEnd));
        if (indexEnd >= 0)
        {
            int deviceIndex = topic.c_str()[indexEnd - 1] - '0'; // at least 10 (0 - 9).
            Serial.println(deviceIndex);
            Serial.println(topic.c_str()[indexEnd - 1]);

            TM1637* display = getDisplayByDeviceIndex(deviceIndex);

            if (nullptr != display)
            {
                display->setBrightness(0x0A, true); // 0x0f = max brightness. Do not delete this, the display may be turned off.
                Serial.println(message);
                display->showString(message.c_str(), display->getDefaultDisplayLength());
            }
        }
    }

    TM1637* TM1637_Handling::getDisplayByDeviceIndex(int index)
    {
        for (auto& display : displays1637)
        {
            if (display.getDeviceIndex() == index)
            {
                return &display;
            }
        }
        debug("TM1637 display with index " + String(index) + " not found!");
        return nullptr;
    }

    /// @brief Incoming MqttMessage to indicate a level between 0 and 100.
    /// @param topic
    /// @param message
    void TM1637_Handling::callbackMqttOnReceivedDataTm1637Level(const String& topic, const String& message)
    {
        debug("callbackMqttOnReceivedDataTm1637Level topic: " + topic + " message: " + message);

        int indexEnd = topic.lastIndexOf("/");
        Serial.println("indexEnd: " + String(indexEnd));
        if (indexEnd >= 0)
        {
            int deviceIndex = topic.c_str()[indexEnd - 1] - '0'; // at least 10 (0 - 9).
            debug(deviceIndex);
            debug(topic.c_str()[indexEnd - 1]);

            TM1637* display = getDisplayByDeviceIndex(deviceIndex);

            String displayType = display->getDisplayType() == Tm1637DisplayType::Digits4
                                     ? "4"
                                     : (display->getDisplayType() == Tm1637DisplayType::Digits6 ? "6" : "undefined");
            String settingsKey = "tm1637_" + displayType + "/" + String(display->getDeviceIndex()) + "/lf";

            Settings settings;

            String strLevelFactor = "1";
            strLevelFactor        = settings.getDataString(settingsKey, "1", false);
            debug("settingsKey: " + settingsKey + " levelFactor: " + strLevelFactor);
            float levelFactor = 1.0f;
            try
            {
                levelFactor = std::stof(strLevelFactor.c_str());
            }
            catch (const std::exception& e)
            {
                debug("Unable to convert levelFactor to a number! Using default value 1.0");
            }

            debug("Using levelFactor: " + String(levelFactor));
            int level = 0;
            try
            {
                level = std::stoi(message.c_str());
                level = static_cast<int>(level * levelFactor);
            }
            catch (const std::exception& e)
            {
                debug("Unable to convert to a number!");
            }

            if (nullptr != display)
            {
                display->setBrightness(0x0A, true); // 0x0f = max brightness. Do not delete this, the display may be turned off before.

                display->showLevel(level, false);
            }
        }
    }

#ifdef USE_INTERNAL_MQTT
    static void onInternalReceivedData(const InternalMqttClient* /* srce */, const InternalTopic& topic, const char* payload, size_t /* length */)
    {
        String strTopic = String(topic.c_str());
        if (strTopic.endsWith("/number"))
        {
            TM1637_Handling::callbackMqttOnReceivedDataTm1637Number(topic.c_str(), String(payload));
        }
        else if (strTopic.endsWith("/text"))
        {
            debug("Received internal MQTT message for text: " + String(payload));
            TM1637_Handling::callMqttbackOnReceivedDataTm1637Text(topic.c_str(), String(payload));
        }
        else if (strTopic.endsWith("/level"))
        {
            TM1637_Handling::callbackMqttOnReceivedDataTm1637Level(topic.c_str(), String(payload));
        }
        else if (strTopic.endsWith("/temperature"))
        {
            TM1637_Handling::callbackMqttOnReceivedDataTm1637Temperature(topic.c_str(), String(payload));
        }
    }

    void TM1637_Handling::setInternalCallback(InternalMqttClient* const internalMqttClient)
    {
        debug("Setting internal MQTT callback for TM1637_Handling...");

        if (internalMqttClient == nullptr)
        {
            Serial.println("Internal MQTT client is not available. Cannot set internal callbacks for TM1637_Handling.");
            return;
        }

        internalMqttClient->setCallback(onInternalReceivedData);
    }

#endif // USE_INTERNAL_MQTT
    DeviceBase& TM1637_Handling::addDevice(const String& baseTopic, int deviceIndex, int clkPin, int dioPin, bool flipDisplay,
                                           const String& serverDownText)
    {
        debug("Adding TM1637 device with base topic: " + baseTopic + ", device index: " + String(deviceIndex) + ", clkPin: " + String(clkPin) +
              ", dioPin: " + String(dioPin) + ", flipDisplay: " + String(flipDisplay) + ", serverDownText: " + serverDownText);
        TM1637& display =
            displays1637.emplace_back(deviceIndex, nullptr, mqttClient, baseTopic, tm1637DisplayType, clkPin, dioPin, flipDisplay, serverDownText);

        return display;
    }

    // Initialize static members
    std::vector<IotZoo::TM1637> TM1637_Handling::displays1637{};
} // namespace IotZoo
#endif