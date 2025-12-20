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
// --------------------------------------------------------------------------------------------------------------------
// Includes
// --------------------------------------------------------------------------------------------------------------------
#include "ConnectionSettings.hpp"
#include "Defines.hpp"

#include <Arduino.h>
#include <ArduinoJson.h>
// #include <pins_arduino.h>
#include "pocos/Microcontroller.hpp"
#include "pocos/Topic.hpp"

#include <WiFi.h>
#include <list>

#ifdef ERASE_FLASH
#include "nvs_flash.h"
#endif

#ifdef USE_REST_SERVER
#include "WebServer.h"

#include <HTTPClient.h>
// Web server running on port 80
WebServer webServer(80);
#endif

// Rotary encoder
#ifdef USE_HW040
#include "HW040/HW040Handling.hpp"
#endif

#ifdef USE_STEPPER_MOTOR
#include "StepperMotor.hpp"
IotZoo::StepperMotor* stepperMotor = nullptr;
#endif

#ifdef USE_LED_AND_KEY
#include "TM1638.hpp"
IotZoo::TM1638* tm1638 = nullptr;
#endif

#ifdef USE_WS2818
#include "WS2818.hpp"
IotZoo::WS2818* ws2812 = nullptr;
#ifdef USE_WS2818_PIXEL_MATRIX
#include "PixelMatrix.hpp"
#endif
#endif

#ifdef USE_BLE_HEART_RATE_SENSOR
#include "BLEHeartRateSensor.hpp"
IotZoo::HeartRateSensor* heartRateSensor = nullptr;
void                     connectToHeartRateSensor(int advertisingTimeout = 30);
#endif

#ifdef USE_HC_SR501
#include "HRSR501Handling.hpp"
IotZoo::HRSR501Handling motionDetectorsHrsc501Handling;
#endif

#ifdef USE_RD_03D
#include "Rd03D.hpp"
IotZoo::Rd03D* rd03d = nullptr;
#endif

#ifdef USE_BUZZER
#include "Buzzer.hpp"
IotZoo::Buzzer* buzzer = nullptr;
#endif

#ifdef ARDUINO_ESP32_DEV
#include "Settings.hpp"
using namespace IotZoo;
Settings* settings = nullptr;
#endif

#ifdef USE_TRAFFIC_LIGHT_LEDS
#include "TrafficLight.hpp"
std::vector<TrafficLight> trafficLightLeds{};
#endif

#ifdef USE_REMOTE_GPIOS
#include "RemoteGpio.hpp"
std::vector<RemoteGpio> remoteGpios{};
#endif

#ifdef USE_KY025
const int digitalPinReedContact = 19;

long          reedContactCounter = 0;
unsigned long lastRotationMillis = 0;
double        oldRpm             = -1.0;
double        rpm                = 0.0;
#endif

#ifdef USE_HB0014
const int     digitalPinInfraredLed   = 34;
int           digitalValueInfrared    = 0; // digital readings
int           digitalValueOldInfrared = 0;
unsigned long lastMillisInfrared      = millis();
#endif

#ifdef USE_OLED_SSD1306
#include "./displays/SSD1306.hpp"
OledSsd1306Display* oled1306 = nullptr;
#endif

#ifdef USE_LCD_160X
#include "./displays/LCDDisplay.hpp"
LcdDisplay* lcdDisplay = nullptr;
#endif

#ifdef USE_HT1621
#include "./displays/HT1621.hpp"
IotZoo::HT1621* ht1621 = nullptr;
#endif

#ifdef USE_KEYPAD
#include "ButtonMatrixHandling.hpp"
using namespace IotZoo;
IotZoo::ButtonMatrixHandling buttonMatrixHandling;
#endif

#ifdef ESP8266
#include <ESP8266HTTPClient.h>
#include <ESP8266WiFi.h>
#include <WiFiClient.h>
#else
#include <WiFi.h>
#endif

#ifdef USE_BUTTON
#include "ButtonHandling.hpp"
ButtonHandling buttonHandling;
#endif

#ifdef USE_AUDIO_STREAMER
#include "AudioStreamer.hpp"
AudioStreamer* audioStreamer;
#endif

#ifdef USE_GPS
#include "Gps.hpp"
Gps* gps = nullptr;
#endif

#ifdef USE_SWITCH
#include "Switch.hpp"
std::vector<Switch> switches{};
#endif

// --------------------------------------------------------------------------------------------------------------------
// Global variables
// --------------------------------------------------------------------------------------------------------------------
String firmwareVersion = "0.2.1";

bool          doRestart         = false;
unsigned long aliveCounter      = 0;
unsigned long lastAliveTime     = 0;
unsigned long lastLoopStartTime = millis();

unsigned long lastServerAliveMillis = millis();
long          loopCounter           = 0;
long          loopDurationMs        = 0;

static const uint8_t LED_BUILTIN = 2;

// --------------------------------------------------------------------------------------------------------------------
// usings
// --------------------------------------------------------------------------------------------------------------------
using namespace std;
using namespace IotZoo;

String macAddress;

bool topicsRegistered = false;

/// @brief Restarts the microcontroller.
void restart()
{
    Serial.println("*** RESTART NOW!!! ***");
    ESP.restart();
}

/// @brief Get the MAC Address of the microcontroller.
/// @return
String getMacAddress()
{
    // It would be even possible to set the MAC address!
    macAddress = WiFi.macAddress();
    Serial.println("MAC: " + macAddress);
    return macAddress;
}

/// @brief Gets the board type of the microcontroller.
/// @return
String identifyBoard()
{
    // do not change because this is part of the MQTT topic. The MQTT topic is case sensitive!
#ifdef ARDUINO_ESP32_DEV
    return "esp32";
#else
    return "esp8266";
#endif
}

#ifdef USE_DS18B20
#include "DS18B20.hpp"
IotZoo::DS18B20* ds18B20SensorManager = nullptr;
#endif

#ifdef USE_MQTT
#undef USE_MQTT2
#endif

#ifdef USE_MQTT
#include "MqttClient.hpp"
MqttClient* mqttClient = nullptr;
#endif

#ifdef USE_MQTT2
#include "MqttClient2.hpp"
MqttClient* mqttClient = nullptr;
#endif

#ifdef USE_TM1637_4
#include "./displays/TM1637/TM1637_4_Handling.hpp"
IotZoo::TM1637_4_Handling* tm1637_4Handling;
#endif // USE_TM1637_4

#ifdef USE_TM1637_6
#include "./displays/TM1637/TM1637_6_Handling.hpp"
IotZoo::TM1637_6_Handling tm1637_6Handling;
#endif // USE_TM1637_6

#ifdef USE_MAX7219
#include "./displays/Max7219.hpp"
Max7219* max7219;
#endif // USE_MAX7219

#if defined(USE_MQTT) || defined(USE_MQTT2)
const String NamespaceNameFallback = "iotzoo";
/// @brief Get the base MQTT Topic.
/// @return
String getBaseTopic()
{
    String namespaceName = settings->getNamespaceName(NamespaceNameFallback);
    if (namespaceName.length() > 0)
    {
        namespaceName += "/";
    }
    String projectName = settings->getProjectName(ProjectNameFallback);
    if (projectName.length() > 0)
    {
        projectName += "/";
    }
    // Serial.println("baseTopic: " + projectName + identifyBoard() + "/" + macAddress);

    return namespaceName + projectName + identifyBoard() + "/" + macAddress;
}

void publishError(const String& errMsg)
{
    String topic = getBaseTopic() + "/error";
    mqttClient->publish(topic, errMsg);
}

bool deserializeStaticJsonAndPublishError(JsonDocument& jsonDocument, const String& json)
{
    DeserializationError error = deserializeJson(jsonDocument, json);
    if (error)
    {
        if (DeserializationError::NoMemory == error)
        {
            publishError("Max data length exeeded! (" + String(json.length()) + " > " + String(jsonDocument.capacity()) +
                         ") Error: " + String(error.c_str()));
        }
        else
        {
            publishError("DeserializeJson() of '" + json + "' failed: " + String(error.c_str()));
        }

        return false;
    }
    return true;
}

#endif

void onReceivedConnectedDevicesConfiguration(const String& json)
{
    Serial.println("*** onReceivedConfiguration ***");
    Serial.println(json);
    settings->saveDeviceConfigurations(json);
}

bool onReceivedMicrocontrollerConfiguration(const String& json)
{
    try
    {
        Serial.println("*** onReceivedMicrocontrollerConfiguration (via MQTT): " + json);
        Serial.println(json);
        if (json.length() == 0)
        {
            return false;
        }
        StaticJsonDocument<400> jsonDocument;
        if (!deserializeStaticJsonAndPublishError(jsonDocument, json))
        {
            return false;
        }
        String namespaceName = jsonDocument["NamespaceName"].as<String>();
        String projectName   = jsonDocument["ProjectName"].as<String>();
        String ipMqttBroker  = jsonDocument["IpMqttBroker"].as<String>();
        bool   ok0           = settings->setNamespaceName(namespaceName);
        bool   ok1           = settings->setProjectName(projectName);
        bool   ok2           = settings->setMqttBrokerIp(ipMqttBroker);
        if (ok0 && ok1 && ok2)
        {
            Serial.println("Successful saved settings");
            return true;
        }
    }
    catch (const std::exception& e)
    {
        Serial.println(e.what());
    }
    return false;
}

#ifdef USE_HW040
IotZoo::HW040Handling hw040Handling;
#endif

#if defined(USE_MQTT) || defined(USE_MQTT2)

String serializeTopic(const Topic& topic)
{
    StaticJsonDocument<512> doc;
    doc["Topic"] = topic.TopicName;
    // doc["ProjectName"] = settings.getProjectName(ProjectNameFallback); // is included in microcontroller
    doc["Description"]      = topic.Description;
    doc["Persist"]          = topic.Persist;   // Persist false: Do not insert in table topic_history.
    doc["MessageDirection"] = topic.Direction; // From the perspective of the IotZooClient.
    String jsonTopic;
    size_t size = serializeJson(doc, jsonTopic);
    // Serial.println("Serialized topic " + jsonTopic + " size: " + String(size) + " bytes.");
    return jsonTopic;
}

String serializeMicrocontroller()
{
    Microcontroller microcontroller;
    microcontroller.MacAddress      = macAddress;
    microcontroller.IpAddress       = WiFi.localIP().toString();
    microcontroller.IpMqttBroker    = settings->getMqttBrokerIp(MqttBrokerIpFallback);
    microcontroller.BoardType       = identifyBoard();
    microcontroller.ProjectName     = settings->getProjectName(ProjectNameFallback);
    microcontroller.NamespaceName   = settings->getNamespaceName("iotzoo");
    microcontroller.FirmwareVersion = firmwareVersion;
    StaticJsonDocument<256> doc;
    doc["NamespaceName"] = microcontroller.NamespaceName;
    doc["ProjectName"]   = microcontroller.ProjectName;
    doc["BoardType"]     = microcontroller.BoardType;
    // doc["Description"] = microcontroller.Description;
    doc["IpAddress"]           = microcontroller.IpAddress;
    doc["MacAddress"]          = microcontroller.MacAddress;
    doc["IpMqttBroker"]        = microcontroller.IpMqttBroker;
    doc["FirmwareVersion"]     = microcontroller.FirmwareVersion;
    doc["MicrocontrollerType"] = identifyBoard();
    String json;
    size_t size = serializeJson(doc, json);
    // Serial.println("Serialized topic " + json + " size: " + String(size) + " bytes.");
    return json;
}

void AddMicrocontrollerNestedJsonObject(JsonDocument* jsonDocument)
{
    JsonObject jsonObjectMicrocontroller         = jsonDocument->createNestedObject("Microcontroller");
    jsonObjectMicrocontroller["MacAddress"]      = WiFi.getHostname();
    jsonObjectMicrocontroller["IpAddress"]       = WiFi.localIP();
    jsonObjectMicrocontroller["ProjectName"]     = settings->getProjectName(ProjectNameFallback);
    jsonObjectMicrocontroller["FirmwareVersion"] = firmwareVersion;
    jsonObjectMicrocontroller["BoardType"]       = identifyBoard();
}

void AddAliveNestedJsonObject(JsonDocument* jsonDocument)
{
    JsonObject jsonObjectAlive           = jsonDocument->createNestedObject("Alive");
    jsonObjectAlive["AliveCounter"]      = aliveCounter;
    jsonObjectAlive["LoopCounter"]       = loopCounter;
    jsonObjectAlive["LoopDurationMs"]    = loopDurationMs;
    jsonObjectAlive["ReconnectionCount"] = mqttClient->getConnectionEstablishedCount() - 1;
    jsonObjectAlive["AliveIntervalMs"]   = settings->getAliveIntervalMillis();
}

void AddSupportedDevicesNestedJsonObject(JsonDocument* jsonDocument)
{
    JsonObject jsonObjectSupportedDevices = jsonDocument->createNestedObject("SupportedDevices");
#ifdef USE_HW040
    jsonObjectSupportedDevices["HW-040"] = true;
#else
    jsonObjectSupportedDevices["HW-040"] = false;
#endif
#ifdef USE_KEYPAD
    jsonObjectSupportedDevices["KeyPad"] = true;
#else
    jsonObjectSupportedDevices["KeyPad"] = false;
#endif
#ifdef USE_REMOTE_GPIOS
    jsonObjectSupportedDevices["REMOTE_GPIOS"] = true;
#else
    jsonObjectSupportedDevices["REMOTE_GPIOS"] = false;
#endif
#ifdef USE_TRAFFIC_LIGHT_LEDS
    jsonObjectSupportedDevices["TRAFFIC_LIGHT_LEDS"] = true;
#else
    jsonObjectSupportedDevices["TRAFFIC_LIGHT_LEDS"] = false;
#endif
#ifdef USE_DS18B20
    jsonObjectSupportedDevices["DS18B20"] = true;
#else
    jsonObjectSupportedDevices["DS18B20"] = false;
#endif
#ifdef USE_WS2818
    jsonObjectSupportedDevices["WS2818"] = true;
#else
    jsonObjectSupportedDevices["WS2818"] = false;
#endif
#ifdef USE_BUTTON
    jsonObjectSupportedDevices["BUTTON"] = true;
#else
    jsonObjectSupportedDevices["BUTTON"] = false;
#endif
#ifdef USE_AUDIO_STREAMER
    jsonObjectSupportedDevices["AUDIO_STREAMER"] = true;
#else
    jsonObjectSupportedDevices["AUDIO_STREAMER"] = false;
#endif
#ifdef USE_GPS
    jsonObjectSupportedDevices["GPS"] = true;
#else
    jsonObjectSupportedDevices["GPS"] = false;
#endif
#ifdef USE_BUZZER
    jsonObjectSupportedDevices["BUZZER"] = true;
#else
    jsonObjectSupportedDevices["BUZZER"] = false;
#endif
#ifdef USE_SWITCH
    jsonObjectSupportedDevices["SWITCH"] = true;
#else
    jsonObjectSupportedDevices["SWITCH"] = false;
#endif
#ifdef USE_TM1637_4
    jsonObjectSupportedDevices["TM1637_4"] = true;
#else
    jsonObjectSupportedDevices["TM1637_4"] = false;
#endif
#ifdef USE_TM1637_6
    jsonObjectSupportedDevices["TM1637_6"] = true;
#else
    jsonObjectSupportedDevices["TM1637_6"] = false;
#endif
#ifdef USE_LED_AND_KEY
    jsonObjectSupportedDevices["TM1638LedAndKey"] = true;
#else
    jsonObjectSupportedDevices["TM1638LedAndKey"] = false;
#endif
#ifdef USE_HT1621
    jsonObjectSupportedDevices["HT1621"] = true;
#else
    jsonObjectSupportedDevices["HT1621"] = false;
#endif
#ifdef USE_MAX7219
    jsonObjectSupportedDevices["MAX7219"] = true;
#else
    jsonObjectSupportedDevices["MAX7219"] = false;
#endif
#ifdef USE_LCD_160X
    jsonObjectSupportedDevices["LCD160x"] = true;
#else
    jsonObjectSupportedDevices["LCD160x"] = false;
#endif
#ifdef USE_HC_SR501
    jsonObjectSupportedDevices["HC-SR501"] = true;
#else
    jsonObjectSupportedDevices["HC-SR501"] = false;
#endif
#ifdef USE_HC_SR501
    jsonObjectSupportedDevices["RD-03D"] = true;
#else
    jsonObjectSupportedDevices["RD-03D"] = false;
#endif
#ifdef USE_REST_SERVER
    jsonObjectSupportedDevices["RestServer"] = true;
#else
    jsonObjectSupportedDevices["RestServer"] = false;
#endif
#ifdef USE_BLE_HEART_RATE_SENSOR
    jsonObjectSupportedDevices["BleHeartRate"] = true;
#else
    jsonObjectSupportedDevices["BleHeartRate"] = false;
#endif
}

void pushTopicToIotZooClient(const Topic& topic)
{
    String jsonTopic               = serializeTopic(topic);
    String topicRegisterKnownTopic = getBaseTopic() + "/register_known_topic";

    mqttClient->publish(topicRegisterKnownTopic, jsonTopic);
}

void pushTopicsToIotZooClient(std::vector<Topic>& topics)
{
    Serial.println("*** register_microcontroller:");
    String topicRegisterMicrocontroller = getBaseTopic() + "/register_microcontroller";

    String jsonMicrocontroller = serializeMicrocontroller();
    jsonMicrocontroller.remove(jsonMicrocontroller.length() - 1);
    jsonMicrocontroller += ", \"KnownTopics\": [";

    int size = topics.size();
    if (size > 64) // We are limited to 16 kb. This is the set buffer size of the MQTT client. If this size is exceeded,
                   // the message will not be delivered.
    {
        Serial.println("sorry, to much data. We are limited to 16 kb.");
        size = 64;
    }

    topics.begin();

    for (int index = 0; index < size; index++)
    {
        auto topic = topics.begin();
        std::advance(topic, index);

        String jsonTopic = serializeTopic(*topic);

        if (index != size - 1)
        {
            jsonTopic += ",\r\n";
        }

        jsonMicrocontroller += jsonTopic;
    }
    jsonMicrocontroller += "] }";

    Serial.println(jsonMicrocontroller);
    if (mqttClient->publish(topicRegisterMicrocontroller, jsonMicrocontroller,
                            false)) // retain should be false
    {
        Serial.println("Known Topics successfully sent to the IOTZOO client.");
    }
    else
    {
        publishError("Unable to send topic " + topicRegisterMicrocontroller);
    }

    if (topics.size() > 64)
    {
        Serial.println("Forwarding the remaining topics");
        for (int index = 64; index < topics.size(); index++)
        {
            auto topic = topics.begin();
            std::advance(topic, index);

            pushTopicToIotZooClient(*topic);
        }
    }
}

void publishDeviceConfigurations()
{
    try
    {
        String topicDevices        = getBaseTopic() + "/device_config";
        String deviceConfiguration = settings->loadDeviceConfigurations();
        Serial.println("publishing device configuration: " + deviceConfiguration);
        mqttClient->publish(topicDevices, deviceConfiguration);
    }
    catch (const std::exception& e)
    {
        Serial.println(e.what());
    }
}

void publishErrorMessage(const String& errorMessage)
{
    String topicError = getBaseTopic() + "/error";
    mqttClient->publish(topicError, errorMessage);
}

/// @brief Create Json for alive message.
/// @return Json for alive message
String createAliveJson()
{
    StaticJsonDocument<4096> jsonDocument; // on stack
    // DynamicJsonDocument jsonDocument(4096); // on heap

    AddMicrocontrollerNestedJsonObject(&jsonDocument);
    AddAliveNestedJsonObject(&jsonDocument);
    AddSupportedDevicesNestedJsonObject(&jsonDocument);

    String json;
    serializeJson(jsonDocument, json);
    return json;
}
#endif

// ------------------------------------------------------------------------------------------------
// Periodically send an alive message.
// ------------------------------------------------------------------------------------------------

#if defined(USE_MQTT) || defined(USE_MQTT2)
void publishAliveMessage()
{
    Serial.println("publishAliveMessage");

    aliveCounter++;
    String topicAlive = getBaseTopic() + "/alive";
    String json       = createAliveJson();
    mqttClient->publish(topicAlive, json);

    lastAliveTime = millis();
}

/// @brief IotZoo acknowledged the alive message.
/// @param rawData
void onAliveAck(const String& rawData)
{
    if (rawData.length() > 0)
    {
        if (rawData != "0")
        {
            digitalWrite(LED_BUILTIN, HIGH); // turn the LED on.
        }
        Serial.println("Received alive_ack: " + rawData + ", millis: " + String(millis()));

        lastServerAliveMillis = millis();
    }
}

// --------------------------------------------------------------------------------------------------------------------
/// @brief /esp32/<mac>/status requested (from IoTZooClient) via MQTT.
/// @param rawData

void onStatusRequested(const String& status)
{
    Serial.println("*** Status requested: " + status);
    if (status == "device_config")
    {
        publishDeviceConfigurations();
    }
    else if (status == "alive")
    {
        publishAliveMessage();
    }
    else
    {
        publishErrorMessage("Do not know what status you want: " + status);
    }
}

// publish an answer to getBaseTopic() + "/settings/load".
void onLoadAndPublishConfigurationByKey(const String& key)
{
    String topic = getBaseTopic() + "/settings/key";
    if (key.length() == 0)
    {
        publishError("key missing!");
        return;
    }

    try
    {
        Serial.println("Loading configuration with key '" + key + "'");

        String value = settings->loadConfiguration(key);
        String json  = "{ \"key\": \"" + key + "\", \"data\": \"" + value + "\" }";
        mqttClient->publish(topic, json);
    }
    catch (const std::exception& e)
    {
        publishError(String(e.what()));
    }
};

#ifdef USE_MQTT2
/// @brief /esp32/<mac>/status requested (from IoTZooClient) via MQTT.
void onStatusRequested2(char* topic, uint8_t* payload, unsigned int length)
{
    Serial.println("Received " + String(topic));
    // Convert the payload into a String
    payload[length] = '\0';
    String rawData((char*)payload);
    rawData.toLowerCase();
    onStatusRequested(rawData);
}

#endif

#endif

// ------------------------------------------------------------------------------------------------
// Is called when MQTT connection is established.
// ------------------------------------------------------------------------------------------------

#ifdef USE_MQTT
void onConnectionEstablished() // do not rename! This method name is forced in EspMQTTClient.h. I do not like this.
{
    Serial.println("onMqttConnectionEstablished! baseTopic: " + getBaseTopic());

    String topicDeviceStatus = getBaseTopic() + "/status";
    mqttClient->subscribe(topicDeviceStatus, onStatusRequested);
    mqttClient->subscribe(macAddress + "/status", onStatusRequested);
    mqttClient->subscribe(getBaseTopic() + "/alive_ack", onAliveAck);

#ifdef USE_LCD_160X
    if (nullptr != lcdDisplay)
    {
        lcdDisplay->onMqttConnectionEstablished();
    }
#endif

#ifdef USE_WS2818
    // It seems that the library only supports one light strip!
    if (nullptr != ws2812)
    {
        ws2812->onMqttConnectionEstablished();
    }
    else
    {
        Serial.println("ws2812 is nullptr!");
    }

#endif

#ifdef USE_HW040
    hw040Handling.onMqttConnectionEstablished(mqttClient, getBaseTopic());
#endif

#ifdef USE_STEPPER_MOTOR
    if (nullptr != stepperMotor)
    {
        stepperMotor->onMqttConnectionEstablished();
    }
#endif

#ifdef USE_OLED_SSD1306
    if (nullptr != oled1306)
    {
        oled1306->onMqttConnectionEstablished();
    }
#endif

#ifdef USE_STEPPER_MOTOR
    if (nullptr != stepperMotor)
    {
        stepperMotor->onMqttConnectionEstablished();
    }
#endif

#ifdef USE_BUTTON
    buttonHandling.onMqttConnectionEstablished();
#endif

#ifdef USE_AUDIO_STREAMER
    if (nullptr != audioStreamer)
    {
        audioStreamer->onMqttConnectionEstablished();
    }
#endif

#ifdef USE_GPS
    if (nullptr != gps)
    {
        gps->onMqttConnectionEstablished();
    }
#endif

#ifdef USE_BUZZER
    if (nullptr != buzzer)
    {
        buzzer->onMqttConnectionEstablished();
    }
#endif

#ifdef USE_TM1637_4
    if (nullptr != tm1637_4Handling)
    {
        tm1637_4Handling->onMqttConnectionEstablished(mqttClient, getBaseTopic());
    }
#endif

#ifdef USE_TM1637_6
    tm1637_6Handling.onMqttConnectionEstablished(settings, mqttClient, getBaseTopic());
#endif

#ifdef USE_LED_AND_KEY
    if (nullptr != tm1638)
    {
        tm1638->onMqttConnectionEstablished();
    }
#endif

#ifdef USE_HT1621
    if (nullptr != ht1621)
    {
        ht1621->onMqttConnectionEstablished();
    }
#endif // USE_HT1621

#ifdef USE_MAX7219
    if (nullptr != max7219)
    {
        max7219->onMqttConnectionEstablished();
    }
#endif // USE_MAX7219

#ifdef USE_REMOTE_GPIOS
    for (auto& remoteGpio : remoteGpios)
    {
        remoteGpio.onMqttConnectionEstablished();
    }
#endif // USE_REMOTE_GPIOS

#ifdef USE_TRAFFIC_LIGHT_LEDS
    for (auto& trafficLight : trafficLightLeds)
    {
        trafficLight.onMqttConnectionEstablished();
    }
#endif

    String topicReboot = getBaseTopic() + "/system";

    mqttClient->subscribe(topicReboot,
                          [&](const String& payload)
                          {
                              if (payload == "reboot")
                              {
                                  restart();
                              }
                          });

    String topicIntervalAlive = getBaseTopic() + "/alive_interval_ms";
    mqttClient->subscribe(topicIntervalAlive,
                          [&](const String& payload)
                          {
                              settings->setAliveIntervalMillis(std::stol(payload.c_str()));
                              Serial.println("alive interval is changed to " + String(settings->getAliveIntervalMillis()));
                          });

    // Save the device configurations.
    String topicSetDeviceConfiguration = getBaseTopic() + "/save_device_config";
    mqttClient->subscribe(topicSetDeviceConfiguration,
                          [&](const String& payload)
                          {
                              Serial.println("Received device configurations -> Save it.");
                              onReceivedConnectedDevicesConfiguration(payload);
                              restart();
                          });

    String topicSaveConfiguration = getBaseTopic() + "/settings/save";
    mqttClient->subscribe(topicSaveConfiguration,
                          [&](const String& json)
                          {
                              try
                              {
                                  Serial.print("Received configuration data -> Save it. ");

                                  StaticJsonDocument<4096> jsonDocument;
                                  if (!deserializeStaticJsonAndPublishError(jsonDocument, json))
                                  {
                                      return;
                                  }

                                  String key  = jsonDocument["key"].as<String>();
                                  String data = jsonDocument["data"].as<String>();
                                  Serial.println("key:" + key + ", date: " + data);
                                  settings->saveConfigurationData(key, data);
                              }
                              catch (const std::exception& e)
                              {
                                  publishError(String(e.what()));
                              }
                          });

    String topicLoadConfiguration = getBaseTopic() + "/settings/load";
    mqttClient->subscribe(topicLoadConfiguration, onLoadAndPublishConfigurationByKey);

    // Save the project configurations.
    String topicSetMicrocontrollerConfiguration = macAddress + "/save_microcontroller_config"; // macAddress instead of getBaseTopic()!
    mqttClient->subscribe(topicSetMicrocontrollerConfiguration,
                          [&](const String& payload)
                          {
                              if (payload.length())
                              {
                                  Serial.println("Received topic + " + topicSetMicrocontrollerConfiguration +
                                                 " -> save the microcontroller configuration.");
                                  if (onReceivedMicrocontrollerConfiguration(payload))
                                  {
                                      // Remove retained message from the server
                                      mqttClient->removeRetainedMessageFromBroker(topicSetMicrocontrollerConfiguration);
                                      restart();
                                  }
                              }
                          });
}

#endif

/**
 * @brief Loads the configuration for the connected devices and instantiates them.
 */
void makeInstanceConfiguredDevices()
{
    Serial.println("Loading device configurations and instantiate devices...");

    String jsonDeviceConfigurations = settings->loadDeviceConfigurations();
    if (0 == jsonDeviceConfigurations.length())
    {
        Serial.println("Not yet configured!");
    }
    else // if (jsonDeviceConfigurations.length() > 0)
    {
        Serial.println("Config: " + jsonDeviceConfigurations);
        DynamicJsonDocument jsonDocument(4096);

        if (!deserializeStaticJsonAndPublishError(jsonDocument, jsonDeviceConfigurations))
        {
            return;
        }

        JsonArray arrDevices = jsonDocument.as<JsonArray>();

        for (JsonVariant value : arrDevices)
        {
            String deviceType  = value["DeviceType"];
            int    deviceIndex = value["DeviceIndex"];
            bool   isEnabled   = value["IsEnabled"];

            Serial.println("DeviceName: '" + deviceType + "', IsEnabled: " + String(isEnabled));

            if (isEnabled)
            {
                JsonArray arrPins       = value["Pins"].as<JsonArray>();
                JsonArray arrProperties = value["PropertyValues"].as<JsonArray>();

#ifdef USE_BUTTON
                if (deviceType == "Button")
                {
                    int buttonPin = arrPins[0]["MicrocontrollerGpoPin"];

                    buttonHandling.addDevice(deviceIndex, settings, mqttClient, getBaseTopic(), buttonPin);

                    Serial.println("Button initialized.");
                }
#endif // USE_BUTTON

#ifdef USE_AUDIO_STREAMER
                if (deviceType == "INMP441")
                {
                    int pinSd  = arrPins[0]["MicrocontrollerGpoPin"];
                    int pinWs  = arrPins[1]["MicrocontrollerGpoPin"];
                    int pinSck = arrPins[2]["MicrocontrollerGpoPin"];

                    u8_t  features = AudioStreamerFeatures::Undefined;
                    u16_t minRms   = 400;
                    for (JsonVariant property : arrProperties)
                    {
                        String propertyName = property["Name"];

                        if (propertyName == "AllowStreaming")
                        {
                            bool allowStreaming = property["Value"] == "true";
                            if (allowStreaming)
                            {
                                features |= AudioStreamerFeatures::Streaming;
                            }
                        }
                        else if (propertyName == "AllowSoundLevel")
                        {
                            bool allowSoundLevel = property["Value"] == "true";
                            if (allowSoundLevel)
                            {
                                features |= AudioStreamerFeatures::SoundLevel;
                            }
                        }
                        else if (propertyName == "MinRms")
                        {
                            u16_t minRms = property["Value"];
                        }
                    }

                    audioStreamer =
                        new IotZoo::AudioStreamer(deviceIndex, settings, mqttClient, getBaseTopic(), features, minRms, pinSd, pinWs, pinSck);

                    Serial.println("AudioStreamer initialized.");
                }
#endif // USE_AUDIO_STREAMER

#ifdef USE_GPS
                if (deviceType == "GPS")
                {
                    Serial.println("GPS is in the configuration.");
                    int pinRx = arrPins[0]["MicrocontrollerGpoPin"];
                    int pinTx = arrPins[1]["MicrocontrollerGpoPin"];

                    gps = new Gps(deviceIndex, settings, mqttClient, getBaseTopic(), pinRx, pinTx);
                }
#endif // USE_BUTTON

#ifdef USE_BUZZER
                if (deviceType == "Buzzer")
                {
                    uint8_t buzzerPin = arrPins[0]["MicrocontrollerGpoPin"];
                    uint8_t ledPin    = arrPins[1]["MicrocontrollerGpoPin"];

                    buzzer = new IotZoo::Buzzer(deviceIndex, settings, mqttClient, getBaseTopic(), buzzerPin, ledPin);

                    Serial.println("Buzzer initialized.");
                }
#endif // USE_BUZZER

#ifdef USE_HT1621
                if (deviceType == "HT1621")
                {
                    uint8_t csPin        = arrPins[0]["MicrocontrollerGpoPin"];
                    uint8_t wsPin        = arrPins[1]["MicrocontrollerGpoPin"];
                    uint8_t dataPin      = arrPins[2]["MicrocontrollerGpoPin"];
                    uint8_t backlightPin = arrPins[3]["MicrocontrollerGpoPin"];

                    ht1621 = new IotZoo::HT1621(deviceIndex, settings, mqttClient, getBaseTopic(), csPin, wsPin, dataPin, backlightPin);
                    if (nullptr != ht1621)
                    {
                        Serial.println("HT1621 6 digit LED Display initialized.");
                    }
                }
#endif // USE_HT1621

#ifdef USE_MAX7219
                if (deviceType == "MAX7219")
                {
                    uint8_t dataPin         = arrPins[0]["MicrocontrollerGpoPin"];
                    uint8_t clkPin          = arrPins[1]["MicrocontrollerGpoPin"];
                    uint8_t csPin           = arrPins[2]["MicrocontrollerGpoPin"];
                    uint8_t numberOfDevices = 1;

                    for (JsonVariant property : arrProperties)
                    {
                        String propertyName = property["Name"];

                        if (propertyName == "numberOfDevices")
                        {
                            numberOfDevices = property["Value"];
                        }
                    }

                    max7219 = new IotZoo::Max7219(deviceIndex, settings, mqttClient, getBaseTopic(), numberOfDevices, dataPin, clkPin, csPin);
                    if (nullptr != max7219)
                    {
                        Serial.println("Max7219 8x8 LED Matrix initialized.");
                    }
                }
#endif // USE_MAX7219

#ifdef USE_SWITCH
                if (deviceType == "Switch")
                {
                    int switchPin = arrPins[0]["MicrocontrollerGpoPin"];
                    switches.emplace_back(deviceIndex, settings, mqttClient, getBaseTopic(), switchPin);
                    Serial.println("Switch initialized.");
                }
#endif // USE_SWITCH

#ifdef USE_KEYPAD
                if (deviceType == "Keypad 4x4")
                {
                    int column3Pin = arrPins[0]["MicrocontrollerGpoPin"];
                    int column2Pin = arrPins[1]["MicrocontrollerGpoPin"];
                    int column1Pin = arrPins[2]["MicrocontrollerGpoPin"];
                    int column0Pin = arrPins[3]["MicrocontrollerGpoPin"];
                    int row0Pin    = arrPins[4]["MicrocontrollerGpoPin"];
                    int row1Pin    = arrPins[5]["MicrocontrollerGpoPin"];
                    int row2Pin    = arrPins[6]["MicrocontrollerGpoPin"];
                    int row3Pin    = arrPins[7]["MicrocontrollerGpoPin"];

                    ButtonMatrix* buttonMatrix = new ButtonMatrix(deviceIndex, settings, mqttClient, getBaseTopic());
                    buttonMatrix->setRowPins(row0Pin, row1Pin, row2Pin, row3Pin);
                    buttonMatrix->setColPins(column0Pin, column1Pin, column2Pin, column3Pin);

                    buttonMatrixHandling.AddDevice(buttonMatrix);

                    Serial.println("Buttonmatrix initialized.");
                }
#endif // USE_KEYPAD

#ifdef USE_STEPPER_MOTOR
                if (deviceType == "28BY48Stepper")
                {
                    int pin1 = arrPins[0]["MicrocontrollerGpoPin"];
                    int pin2 = arrPins[1]["MicrocontrollerGpoPin"];
                    int pin3 = arrPins[2]["MicrocontrollerGpoPin"];
                    int pin4 = arrPins[3]["MicrocontrollerGpoPin"];

                    stepperMotor = new StepperMotor(deviceIndex, settings, mqttClient, getBaseTopic(), pin1, pin2, pin3, pin4);

                    Serial.println("28BY48 Stepper initialized.");
                }
#endif // USE_STEPPER_MOTOR

#ifdef USE_HC_SR501
                // Add 1..3 HC-SR501 motion detectors.
                if (deviceType == "HC-SR501")
                {
                    int pinMotionDetector = arrPins[0]["MicrocontrollerGpoPin"];
                    motionDetectorsHrsc501Handling.addDevice(deviceIndex, settings, mqttClient, getBaseTopic(), pinMotionDetector);
                }
#endif // USE_HC_SR501

#ifdef USE_RD_03D
                if (deviceType == "Rd-03D")
                {
                    uint8_t pinRx = arrPins[0]["MicrocontrollerGpoPin"];
                    uint8_t pinTx = arrPins[1]["MicrocontrollerGpoPin"];

                    u_int16_t timeoutMillis          = 30000;
                    u_int16_t maxDistanceMillimeters = 60000;
                    bool      multiTargetMode        = false;
                    for (JsonVariant property : arrProperties)
                    {
                        String propertyName = property["Name"];

                        if (propertyName == "TimeoutMillis")
                        {
                            timeoutMillis = property["Value"];
                        }
                        else if (propertyName == "MaxDistanceMillimeters")
                        {
                            maxDistanceMillimeters = property["Value"];
                        }
                        else if (propertyName == "MultiTargetMode")
                        {
                            multiTargetMode = property["Value"];
                        }
                    }

                    rd03d = new Rd03D(deviceIndex, settings, mqttClient, getBaseTopic(), pinRx, pinTx, timeoutMillis, maxDistanceMillimeters,
                                      multiTargetMode);
                    Serial.print("Rd-03d configuration added! pinRx: " + String(pinRx) + ", pinTx: " + String(pinTx));
                    Serial.println(", TimeOutMillis: " + String(timeoutMillis) + ", MaxDistanceMillimeters: " + String(maxDistanceMillimeters));
                }
#endif // USE_RD_03D

#ifdef USE_LCD_160X
                // Add USE_LCD_160X display.
                if (deviceType == "LCD160x")
                {
                    Serial.println("Initializing LCD160x display.");
                    uint8_t columns    = 20;
                    uint8_t rows       = 4;
                    uint8_t i2cAddress = 0x27;
                    for (JsonVariant property : arrProperties)
                    {
                        String propertyName  = property["Name"];
                        String propertyValue = property["Value"];

                        if (propertyName == "Columns")
                        {
                            columns = std::stoi(propertyValue.c_str());
                        }
                        else if (propertyName == "Rows")
                        {
                            columns = std::stoi(propertyValue.c_str());
                        }
                        else if (propertyName == "I2CAddress")
                        {
                            i2cAddress = std::stoi(propertyValue.c_str());
                        }
                    }

                    lcdDisplay = new LcdDisplay(deviceIndex, settings, mqttClient, getBaseTopic(),
                                                i2cAddress, // set the LCD address to 0x27
                                                columns, rows);

                    Serial.println("LCD160x configuration added! I2C-Address: " + String(i2cAddress));
                }
#endif // USE_LCD_160X

#ifdef USE_OLED_SSD1306

                // SDA must be connected to pin 21 and SCL to pin 22.
                if (deviceType == "OLED_SSD1306")
                {
                    Serial.println("Initializing OLED_SSD1306 display.");
                    // u_int8_t sclPin = arrPins[0]["MicrocontrollerGpoPin"];
                    // u_int8_t sdaPin = arrPins[1]["MicrocontrollerGpoPin"];
                    u_int8_t i2cAddress = 0x3C;

                    oled1306 = new OledSsd1306Display(deviceIndex, settings, mqttClient, getBaseTopic(), i2cAddress);
                    Serial.println("Oled display SSD1306 initialized! I2C-Address: " + String(i2cAddress));
                }
#endif // USE_OLED_SSD1306

#ifdef USE_DS18B20
                if (deviceType == "DS18B20")
                {
                    // Add 1 DS18B20 temperature sensors manager which can support 1..64 DS18B20 temperature sensors.
                    ds18B20SensorManager = new IotZoo::DS18B20();

                    int datPin = arrPins[0]["MicrocontrollerGpoPin"];

                    int transmissionInterval = 20000;
                    int resolution           = 11;

                    for (JsonVariant property : arrProperties)
                    {
                        String propertyName  = property["Name"];
                        String propertyValue = property["Value"];
                        if (propertyName == "Interval")
                        {
                            transmissionInterval = std::stoi(propertyValue.c_str());
                        }
                        else if (propertyName == "Resolution")
                        {
                            resolution = std::stoi(propertyValue.c_str());
                        }
                        if (transmissionInterval < 5000)
                        {
                            transmissionInterval = 5000;
                        }
                        if (transmissionInterval > 900000) // 15 min
                        {
                            transmissionInterval = 900000;
                        }
                        if (resolution < 9)
                        {
                            resolution = 9;
                        }
                        if (resolution > 11)
                        {
                            resolution = 11;
                        }
                    }
                    if (nullptr != ds18B20SensorManager)
                    {
                        ds18B20SensorManager->setup(datPin, resolution, transmissionInterval);
                    }

                    Serial.println("DS18B20 sensors configuration loaded! Dat Pin is " + String(datPin) + ", Resolution: " + String(resolution) +
                                   ", Transmission interval: " + String(transmissionInterval));
                }
#endif // USE_DS18B20

#ifdef USE_WS2818
                if (deviceType == "NEO")
                {
                    Serial.println("Configuration of NEO pixels...");
                    int dioPin       = arrPins[0]["MicrocontrollerGpoPin"];
                    int numberOfLeds = 256;

                    for (JsonVariant property : arrProperties)
                    {
                        String propertyName  = property["Name"];
                        String propertyValue = property["Value"];
                        if (propertyName == "numberOfLeds")
                        {
                            numberOfLeds = std::stoi(propertyValue.c_str());
                        }
                    }
                    ws2812 = new WS2818(deviceIndex, settings, mqttClient, getBaseTopic(), dioPin, numberOfLeds);
                    Serial.println("Neo pixel configuration loaded! DIO Pin is " + String(dioPin) + ", Leds: " + String(numberOfLeds));
                }
#ifdef USE_WS2818_PIXEL_MATRIX
                else if (deviceType == "PixelMatrix")
                {
                    Serial.println("Configuration of NEO pixel matrix...");
                    int  dioPin                = arrPins[0]["MicrocontrollerGpoPin"];
                    uint numberOfLedsPerColumn = 8;
                    uint numberOfLedsPerRow    = 8;
                    uint extensions            = 0;

                    for (JsonVariant property : arrProperties)
                    {
                        String propertyName  = property["Name"];
                        String propertyValue = property["Value"];
                        if (propertyName == "numberOfLedsPerColumn")
                        {
                            numberOfLedsPerColumn = std::stoi(propertyValue.c_str());
                        }
                        else if (propertyName == "numberOfLedsPerRow")
                        {
                            numberOfLedsPerRow = std::stoi(propertyValue.c_str());
                        }
                        else if (propertyName == "extensions")
                        {
                            extensions = std::stoi(propertyValue.c_str());
                        }
                        extensions = 1;
                    }
                    ws2812 = new PixelMatrix(deviceIndex, settings, mqttClient, getBaseTopic(), dioPin, numberOfLedsPerColumn, numberOfLedsPerRow,
                                             (PixelMatrixExtensions)extensions);
                    Serial.println("Neo pixel matrix configuration loaded! DIO Pin is " + String(dioPin) +
                                   ", numberOfLedsPerColumn: " + String(numberOfLedsPerColumn) +
                                   ", numberOfLedsPerRow: " + String(numberOfLedsPerRow) + ", Extensions: " + String(extensions));
                }
#endif // USE_WS2818_PIXEL_MATRIX
#endif // USE_WS2818

#ifdef USE_TM1637_4
                if (deviceType == "TM1637_4")
                {
                    Serial.println("TM1637_4 display");

                    int clkPin = arrPins[0]["MicrocontrollerGpoPin"];
                    int dioPin = arrPins[1]["MicrocontrollerGpoPin"];

                    bool   flipDisplay = false;
                    String serverDownText;
                    for (JsonVariant property : arrProperties)
                    {
                        String propertyName  = property["Name"];
                        String propertyValue = property["Value"];

                        if (propertyName == "flipDisplay")
                        {
                            propertyValue.toLowerCase();
                            flipDisplay = propertyValue == "true";
                        }
                        else if (propertyName == "serverDownText")
                        {
                            propertyValue.toLowerCase();
                            serverDownText = propertyValue;
                        }
                    }
                    tm1637_4Handling = new TM1637_4_Handling();
                    if (nullptr != tm1637_4Handling)
                    {
                        tm1637_4Handling->addDevice(getBaseTopic(), deviceIndex, clkPin, dioPin, flipDisplay, serverDownText);
                        Serial.println("TM1637_4 display with deviceIndex " + String(deviceIndex) + " initialized! CLK Pin is " + String(clkPin) +
                                       ", DIO Pin is " + String(dioPin) + ", FlipDisplay: " + String(flipDisplay));
                    }
                }
#endif // USE_TM1637_4

#ifdef USE_TM1637_6
                if (deviceType == "TM1637_6")
                {
                    Serial.println("TM1637_6 display");

                    int clkPin = arrPins[0]["MicrocontrollerGpoPin"];
                    int dioPin = arrPins[1]["MicrocontrollerGpoPin"];

                    bool   flipDisplay = false;
                    String serverDownText;
                    for (JsonVariant property : arrProperties)
                    {
                        String propertyName  = property["Name"];
                        String propertyValue = property["Value"];

                        if (propertyName == "flipDisplay")
                        {
                            propertyValue.toLowerCase();
                            flipDisplay = propertyValue == "true";
                        }
                        else if (propertyName == "serverDownText")
                        {
                            propertyValue.toLowerCase();
                            serverDownText = propertyValue;
                        }
                    }

                    tm1637_6Handling.addDevice(getBaseTopic(), deviceIndex, clkPin, dioPin, flipDisplay, serverDownText);

                    Serial.println("TM1637_6 display initialized! CLK Pin is " + String(clkPin) + ", DIO Pin is " + String(dioPin));
                }
#endif // USE_TM1637_6

#ifdef USE_LED_AND_KEY
                if (deviceType == "TM1638")
                {
                    Serial.println("TM1638_8 display");

                    int strobePin = arrPins[0]["MicrocontrollerGpoPin"];
                    int clkPin    = arrPins[1]["MicrocontrollerGpoPin"];
                    int dioPin    = arrPins[2]["MicrocontrollerGpoPin"];

                    tm1638 = new TM1638(deviceIndex, settings, mqttClient, getBaseTopic(), strobePin, clkPin, dioPin);

                    Serial.println("TM1638 display initialized! Strobe Pin is " + String(strobePin) + ", CLK Pin is " + String(clkPin) +
                                   ", DIO Pin is " + String(dioPin));
                }
#endif // USE_LED_AND_KEY

#ifdef USE_REMOTE_GPIOS
                if (deviceType == "Remote GPIO")
                {
                    Serial.println("Remote GPIO");

                    int gpioPin = arrPins[0]["MicrocontrollerGpoPin"];
                    remoteGpios.emplace_back(deviceIndex, settings, mqttClient, getBaseTopic(), gpioPin);
                    Serial.println("Remote GPIO PIN configuration loaded! Pin is " + String(gpioPin) + ". Size remoteGpios: " + remoteGpios.size());
                }
#endif // USE_REMOTE_GPIOS

#ifdef USE_TRAFFIC_LIGHT_LEDS
                if (deviceType == "LEDS Traffic Light")
                {
                    Serial.println("LEDS Traffic Light");

                    int gpioLedRed    = -1;
                    int gpioLedYellow = -1;
                    int gpioLedGreen  = -1;

                    for (JsonVariant property : arrPins)
                    {
                        String propertyName  = property["PinName"];
                        String propertyValue = property["MicrocontrollerGpoPin"];
                        Serial.println("propertyName: " + propertyName + ", propertyValue: " + propertyValue);

                        if (propertyName == "R")
                        {
                            gpioLedRed = std::stoi(propertyValue.c_str());
                            Serial.println("Red Gpio: " + gpioLedRed);
                        }
                        else if (propertyName == "Y")
                        {
                            gpioLedYellow = std::stoi(propertyValue.c_str());
                            Serial.println("Yellow Gpio: " + gpioLedYellow);
                        }
                        else if (propertyName == "G")
                        {
                            gpioLedGreen = std::stoi(propertyValue.c_str());
                            Serial.println("Green Gpio: " + gpioLedGreen);
                        }
                    }

                    if (gpioLedRed != -1 && gpioLedYellow != -1 && gpioLedGreen != -1)
                    {
                        trafficLightLeds.emplace_back(deviceIndex, settings, mqttClient, getBaseTopic(), gpioLedRed, gpioLedYellow, gpioLedGreen);
                    }
                }
#endif // USE_TRAFFIC_LIGHT_LEDS

#ifdef USE_HW040
                if (deviceType == "HW-040")
                {
                    Serial.println("HW-040 rotary encoder");
                    int clkPin = arrPins[0]["MicrocontrollerGpoPin"];
                    int dtPin  = arrPins[1]["MicrocontrollerGpoPin"];
                    int swPin  = arrPins[2]["MicrocontrollerGpoPin"];

                    int  boundaryMinValue = 0;
                    int  boundaryMaxValue = 255;
                    bool circleValues     = false;
                    int  acceleration     = 250;
                    int  encoderSteps     = 2;
                    for (JsonVariant property : arrProperties)
                    {
                        String propertyName  = property["Name"];
                        String propertyValue = property["Value"];

                        if (propertyName == "BoundaryMinValue")
                        {
                            boundaryMinValue = std::stoi(propertyValue.c_str());
                        }

                        if (propertyName == "BoundaryMaxValue")
                        {
                            boundaryMaxValue = std::stoi(propertyValue.c_str());
                        }

                        if (propertyName == "Acceleration")
                        {
                            acceleration = std::stoi(propertyValue.c_str());
                        }

                        if (propertyName == "EncoderSteps")
                        {
                            encoderSteps = std::stoi(propertyValue.c_str());
                        }

                        if (propertyName == "CircleValue")
                        {
                            circleValues = propertyValue == "true";
                        }
                    }

                    hw040Handling.addDevice(deviceIndex, settings, mqttClient, getBaseTopic(), boundaryMinValue, boundaryMaxValue, circleValues,
                                            acceleration, encoderSteps, clkPin, dtPin, swPin, -1);
                    Serial.println("HW-040 rotary encoder initialized! CLK Pin is " + String(clkPin) + ", DT Pin is " + String(dtPin) +
                                   ", MS Pin is " + String(swPin) + ", boundaryMinValue is " + String(boundaryMinValue) + ", boundaryMaxValue is " +
                                   String(boundaryMaxValue) + ", acceleration is " + String(acceleration) + ", circleValues is " +
                                   String(circleValues) + ", encoderSteps is " + String(encoderSteps));
                }
#endif // USE_HW040

#ifdef USE_BLE_HEART_RATE_SENSOR
                if (deviceType == "BleHeartRateSensor")
                {
                    uint8_t advertisingTimeoutSeconds = 30;
                    for (JsonVariant property : arrProperties)
                    {
                        String propertyName = property["Name"];

                        if (propertyName == "AdvertisingTimeoutSeconds")
                        {
                            advertisingTimeoutSeconds = property["Value"].as<uint8_t>();
                        }
                    }

                    heartRateSensor = new HeartRateSensor(deviceIndex, settings, mqttClient, getBaseTopic(), advertisingTimeoutSeconds);
                    connectToHeartRateSensor(advertisingTimeoutSeconds);
                }
#endif // USE_BLE_HEART_RATE_SENSOR
            }
        }
    }
    Serial.println("Configuration is loaded");
}

#if defined(USE_REST_SERVER)
#if defined(USE_MQTT) || defined(USE_MQTT2)
void handleGetAlive()
{
    Serial.println("Get alive");
    String json = createAliveJson();
    webServer.send(200, "application/json", json.c_str());
}
#endif
#endif

#ifdef USE_REST_SERVER
/// @brief http get delivers the configuration of the connected sensors/devices. http://raspberrypi/deviceConfig
void handleGetDeviceConfig()
{
    Serial.println("GET device config");
    String json = settings->loadDeviceConfigurations();
    webServer.send(200, "application/json", json.c_str());
}

/// @brief
/// @param index Index of the GPIO pin.
/// @return State of the GPIO pin at Index @see index.
void handleGetGpioState(int index)
{
#ifdef USE_REMOTE_GPIOS
    auto remoteGpio = remoteGpios.begin();
    std::advance(remoteGpio, index);
    int data = remoteGpio->readDigitalValue();
    Serial.println("GPIO Pin " + String(remoteGpio->getGpioPin()) + " is in state " + String(data));
    webServer.send(200, "text/plain", String(data));
#endif
}

void handleGetGpio0State()
{
    handleGetGpioState(0);
}

void handleGetGpio1State()
{
    handleGetGpioState(1);
}

void handleGetGpio2State()
{
    handleGetGpioState(2);
}

void handleGetGpio3State()
{
    handleGetGpioState(3);
}

void handleGetGpio4State()
{
    handleGetGpioState(4);
}

#ifdef USE_REST_SERVER

/// @brief Blazor app sends device config.
void handlePostDeviceConfig()
{
    Serial.println("Received device config (POST).");
    if (webServer.hasArg("plain") == false)
    {
        // handle error here
        Serial.println("rest problem");
    }
    String body = webServer.arg("plain");
    settings->saveDeviceConfigurations(body);
    Serial.println(body);
    // Respond to the client
    webServer.send(200, "application/json", "{}");
}

/// @brief Blazor app sends microcontroller config.
void handlePostMicrocontrollerConfig()
{
    Serial.println("Received microcontroller config (POST).");

    if (webServer.hasArg("plain") == false)
    {
        // handle error here
        Serial.println("rest problem");
    }
    String body = webServer.arg("plain");
    Serial.println(body);
    // JSON data buffer
    DynamicJsonDocument jsonDocument(1024); // on heap
    if (!deserializeStaticJsonAndPublishError(jsonDocument, body))
    {
        return;
    }

    String mqttBrokerIp  = jsonDocument["IpMqttBroker"];
    String projectName   = jsonDocument["ProjectName"];
    String namespaceName = jsonDocument["NamespaceName"];

    Serial.println("MqttBrokerIp: " + mqttBrokerIp);
    Serial.println("Project: " + projectName);
    if (!settings->setMqttBrokerIp(mqttBrokerIp))
    {
        Serial.println("MqttBrokerIp not saved!");
    }
    if (!settings->setNamespaceName(namespaceName))
    {
        Serial.println("NamespaceName not saved!");
    }
    if (!settings->setProjectName(projectName))
    {
        Serial.println("ProjectName not saved!");
    }
    // Respond to the client
    webServer.send(200, "application/json", "{}");

    doRestart = true;
}

#endif

void connectToWiFi()
{
    Serial.print("Connecting to ");
    Serial.println(ssid);

    WiFi.begin(ssid, password);

    while (WiFi.status() != WL_CONNECTED)
    {
        Serial.print("+");
        delay(250);
    }

    WiFi.setAutoReconnect(true);

    Serial.print("Connected. IP: ");
    Serial.println(WiFi.localIP());
    Serial.println("AutoReconnect: " + String(WiFi.getAutoReconnect()));
}
#endif

#ifdef USE_BLE_HEART_RATE_SENSOR
/// @brief Callback from the heart rate sensor (heart rate changed)
/// @param pBLERemoteCharacteristic
/// @param pData heartRate/Pulse
/// @param length
/// @param isNotify
void notifyCallbackHeartRate(NimBLERemoteCharacteristic* pBLERemoteCharacteristic, uint8_t* data, size_t length, bool isNotify)
{
    if (data[1] > 0)
    {
        Serial.println("Pulse: " + String(data[1]));
    }

#ifdef USE_EXERCISE_BIKE_STANDALONE
#ifdef USE_TM1637_4
    auto displayPulse = displays1637_4.begin();
    displayPulse->showNumber(data[1]);
#endif
#endif

#if defined(USE_MQTT) || defined(USE_MQTT2)
    String topic = getBaseTopic() + "/pulse/0";
#ifdef USE_KY025
    if (rpm > 54.5 && rpm < 56)
    {
        topic = getBaseTopic() + "/pulse/0/pulse_rpm_55";
    }
    else if (rpm > 59.5 && rpm < 61)
    {
        topic = getBaseTopic() + "/pulse/0/pulse_rpm_60";
    }
    else if (rpm > 64.5 && rpm < 66)
    {
        topic = getBaseTopic() + "/pulse/0/pulse_rpm_65";
    }
    else if (rpm > 69.5 && rpm < 71)
    {
        topic = getBaseTopic() + "/pulse/0/pulse_rpm_70";
    }
    else if (rpm > 74.5 && rpm < 76)
    {
        topic = getBaseTopic() + "/pulse/0/pulse_rpm_75";
    }
    else if (rpm > 79.5 && rpm < 81)
    {
        topic = getBaseTopic() + "/pulse/0/pulse_rpm_80";
    }
    else if (rpm > 84.5 && rpm < 86)
    {
        topic = getBaseTopic() + "/pulse/0/pulse_rpm_85";
    }
    else if (rpm > 89.5 && rpm < 91)
    {
        topic = getBaseTopic() + "/pulse/0/pulse_rpm_90";
    }
    else if (rpm > 94.5 && rpm < 96)
    {
        topic = getBaseTopic() + "/pulse/0/pulse_rpm_95";
    }
    else if (rpm > 99.5 && rpm < 100)
    {
        topic = getBaseTopic() + "/pulse/0/pulse_rpm_100";
    }

#endif
    mqttClient->publish(topic.c_str(), String(data[1]).c_str()); // Pulse
#endif
}
#endif // USE_BLE_HEART_RATE_SENSOR

#ifdef USE_KY025
void IRAM_ATTR isr()
{
    // Serial.print("1"); // crashes!!!

    int diff = millis() - lastRotationMillis;
    if (diff > 300.0)
    {
        reedContactCounter++;
        oldRpm = rpm;
        rpm    = 1.0 / (diff / 60000.0);
    }
    lastRotationMillis = millis();
}
#endif

#ifdef USE_BLE_HEART_RATE_SENSOR
void connectToHeartRateSensor(int advertisingTimeout)
{
    String topic = getBaseTopic() + "/pulse/0/scan_started";
#ifdef USE_MQTT2
    mqttClient->publish(topic, "BLE scan started to find advertising heart rate sensor. Timeout is set to " + String(timeOut) + " s.");
#endif
    if (nullptr != heartRateSensor)
    {
        heartRateSensor->setup(notifyCallbackHeartRate, // Callback method, called on heart rate received
                               advertisingTimeout);     // Scan duration
    }
}
#endif // USE_BLE_HEART_RATE_SENSOR

// ------------------------------------------------------------------------------------------------
void setup()
{
    Serial.begin(115200);
    Serial.println("*** SETUP ***");
    pinMode(LED_BUILTIN, OUTPUT);

    randomSeed(micros());

    macAddress = getMacAddress();

#ifdef ERASE_FLASH
    // helps if the config is destroyed ...
    nvs_flash_erase(); // erase the NVS partition and...
    nvs_flash_init();  // initialize the NVS partition.
    Serial.println("Flash is now empty.");
#endif
    settings = new Settings();

#ifdef ERASE_FLASH
    while (true)
        ;
#endif

#if defined(USE_REST_SERVER)
    connectToWiFi();
    // Routing REST Server

    // REST interface
#if defined(USE_MQTT) || defined(USE_MQTT2)
    webServer.on("/alive", HTTP_GET, handleGetAlive);
#endif
    webServer.on("/deviceConfig", HTTP_GET, handleGetDeviceConfig);
    webServer.on("/deviceConfig", HTTP_POST, handlePostDeviceConfig);
    webServer.on("/microcontrollerConfig", HTTP_POST, handlePostMicrocontrollerConfig);
    webServer.on("/gpio/0", HTTP_GET, handleGetGpio0State);
    webServer.on("/gpio/1", HTTP_GET, handleGetGpio1State);
    webServer.on("/gpio/2", HTTP_GET, handleGetGpio2State);
    webServer.on("/gpio/3", HTTP_GET, handleGetGpio3State);
    webServer.on("/gpio/4", HTTP_GET, handleGetGpio4State);
    webServer.begin();
#endif

#ifdef USE_TM1637_4
    if (nullptr != tm1637_4Handling)
    {
        tm1637_4Handling->setup();
    }
#endif

#ifdef USE_TM1637_6
    tm1637_6Handling.setup();
#endif

#if defined(USE_MQTT) || defined(USE_MQTT2)
    char* mqttClientName = new char[18]();

    for (int i = 0; i < 17; i++)
    {
        mqttClientName[i] = macAddress[i];
    }

    String strMqttBrokerIp = settings->getMqttBrokerIp(MqttBrokerIpFallback);
    Serial.println("MQTT Broker Ip: " + strMqttBrokerIp);
    char* mqttBrokerIp = new char[18]();
    for (int i = 0; i < 17; i++)
    {
        mqttBrokerIp[i] = strMqttBrokerIp[i];
    }

    mqttClient = new MqttClient(mqttClientName, ssid, password, mqttBrokerIp, nullptr, nullptr, 1883);

    Serial.println("BaseTopic: " + getBaseTopic());
#ifdef USE_MQTT2
    mqttClient->setup(macAddress);

    String topicDeviceStatus = getBaseTopic() + "/status";
    mqttClient->subscribe(topicDeviceStatus, onStatusRequested2, 0);
#endif

#endif
    makeInstanceConfiguredDevices();
    lastAliveTime = millis() - settings->getAliveIntervalMillis();

#ifdef USE_KY025
    pinMode(digitalPinReedContact, INPUT_PULLUP);
    attachInterrupt(digitalPinReedContact, isr, FALLING);
#endif

#ifdef USE_HB0014
    pinMode(digitalPinInfraredLed, INPUT);
#ifdef USE_OLED_SSD1306
    if (nullptr != oled1306)
    {
        oled1306->setTextLine(1, "?");
        oled1306->setTextLine(2, "Watt");
    }
#endif
#endif
}

#ifdef USE_MQTT

void publishViaMqtt(const String& topicName, const String& payload)
{
    Serial.println(topicName);
    Serial.println(payload);
    mqttClient->publish(topicName, payload);
}
#endif

#ifdef USE_DS18B20
void loopDS18B20()
{
    if (nullptr == ds18B20SensorManager)
    {
        return;
    }
    if (millis() - ds18B20SensorManager->getLastPublishedTemperatureMillis() < ds18B20SensorManager->getInterval())
    {
        return;
    }

    ds18B20SensorManager->setLastPublishedTemperatureMillis(millis());

    if (nullptr != ds18B20SensorManager)
    {
        std::vector<float> temperatures = ds18B20SensorManager->requestTemperatures();
        Serial.println("Count of Temperature sensors: " + String(temperatures.size()));
        int indexTemperatureSensor = 0;

        for (const auto& temperatureCelsius : temperatures)
        {
            String topic = getBaseTopic() + "/ds18b20_manager/0/sensor/" + String(indexTemperatureSensor) + "/celsius";

            Serial.print(topic + "/" + String(temperatureCelsius));
            Serial.println(" ºC");
            if (temperatureCelsius > -55 && temperatureCelsius < 125)
            {
                mqttClient->publish(topic, String(temperatureCelsius, 1));
            }
            indexTemperatureSensor++;
        }
    }
}

#endif

/// @brief Register all from this microcontroller supported topics at the IOTZOO client.
void registerTopics()
{
#ifdef USE_MQTT
    Serial.println("Register Known Topics at the IOTZOO client.");

    String lastWillTopic = getBaseTopic() + "/terminated";
    mqttClient->enableLastWillMessage(lastWillTopic.c_str(), "SHUTDOWN", 0);

    // so now the IOTZOO client knows this microcontroller.
    // ... let's tell it more about the connected devices and what you can do with it...
    std::vector<Topic> topics{};

    topics.emplace_back(getBaseTopic() + "/register_microcontroller", "Registers all the known topics of the microcontroller.",
                        MessageDirection::IotZooClientInbound);
    // necessary? register_microcontroller should be enough.
    topics.emplace_back(getBaseTopic() + "/started", "Microcontroller started", MessageDirection::IotZooClientInbound);

    // Alive message of the microcontroller
    topics.emplace_back(getBaseTopic() + "/alive", "Alive message of the microcontroller", MessageDirection::IotZooClientInbound);

    // Acknowledge fo the alive message from the IotZooClient.
    topics.emplace_back(getBaseTopic() + "/alive_ack", "Alive message of the microcontroller", MessageDirection::IotZooClientOutbound);

    topics.emplace_back(getBaseTopic() + "/terminated", "Microcontroller terminated!", MessageDirection::IotZooClientInbound);
    // settings
    if (settings != nullptr)
    {
        topics.emplace_back(getBaseTopic() + "/settings/load", "Loads data with by the key given in the payload",
                            MessageDirection::IotZooClientOutbound);
        // answer to /settings/load
        topics.emplace_back(getBaseTopic() + "/settings/key", "Answer of /load in json format {\"key\": \"data\"}",
                            MessageDirection::IotZooClientInbound);

        topics.emplace_back(getBaseTopic() + "/settings/save", "{\"key\": \"data\"}", MessageDirection::IotZooClientOutbound);
    }
#ifdef USE_BLE_HEART_RATE_SENSOR
    if (nullptr != heartRateSensor)
    {
        heartRateSensor->addMqttTopicsToRegister(&topics);
    }
#endif

#ifdef USE_STEPPER_MOTOR
    if (nullptr != stepperMotor)
    {
        stepperMotor->addMqttTopicsToRegister(&topics);
    }
#endif

#ifdef USE_BUTTON
    buttonHandling.addMqttTopicsToRegister(&topics);
#endif

#ifdef USE_BUZZER
    if (nullptr != buzzer)
    {
        buzzer->addMqttTopicsToRegister(&topics);
    }
#endif

#ifdef USE_SWITCH
    for (auto& buttonSwitch : switches)
    {
        buttonSwitch.addMqttTopicsToRegister(&topics);
    }
#endif

#ifdef USE_REMOTE_GPIOS
    for (auto& remoteGpio : remoteGpios)
    {
        remoteGpio.addMqttTopicsToRegister(&topics);
    }
#endif

#ifdef USE_LCD_160X
    if (nullptr != lcdDisplay)
    {
        lcdDisplay->addMqttTopicsToRegister(&topics);
    }
#endif

#ifdef USE_OLED_SSD1306
    if (nullptr != oled1306)
    {
        oled1306->addMqttTopicsToRegister(&topics);
    }
#endif

#ifdef USE_TM1637_4
    if (nullptr != tm1637_4Handling)
    {
        tm1637_4Handling->addMqttTopicsToRegister(&topics);
    }
#endif

#ifdef USE_TM1637_6
    tm1637_6Handling.addMqttTopicsToRegister(&topics);
#endif

#ifdef USE_HT1621
    if (nullptr != ht1621)
    {
        ht1621->addMqttTopicsToRegister(&topics);
    }
#endif

#ifdef USE_MAX7219
    if (nullptr != max7219)
    {
        max7219->addMqttTopicsToRegister(&topics);
    }
#endif

#ifdef USE_HW040
    hw040Handling.addMqttTopicsToRegister(&topics);
#endif

#ifdef USE_HC_SR501
    motionDetectorsHrsc501Handling.addMqttTopicsToRegister(&topics);
#endif

#ifdef USE_RD_03D
    if (nullptr != rd03d)
    {
        rd03d->addMqttTopicsToRegister(&topics);
    }
#endif // USE_RD_03D

#ifdef USE_LED_AND_KEY
    if (nullptr != tm1638)
    {
        tm1638->addMqttTopicsToRegister(&topics);
    }
#endif

#ifdef USE_KEYPAD
    buttonMatrixHandling.addMqttTopicsToRegister(&topics);
#endif

#ifdef USE_DS18B20
    if (nullptr != ds18B20SensorManager)
    {
        Serial.println("Register temperature sensors.");
        // std::list<float> temperatures = ds18B20SensorManager->requestTemperatures();
        int index = 0;
        // for (auto &temperature : temperatures)
        for (int index = 0; index < 10; index++)
        {
            topics.emplace_back(getBaseTopic() + "/ds18b20_manager/0/sensor/" + String(index) + "/celsius",
                                "The Temperature in °C of Sensor " + String(index), MessageDirection::IotZooClientInbound);
        }
    }
#endif

#ifdef USE_WS2818
    if (nullptr != ws2812)
    {
        ws2812->addMqttTopicsToRegister(&topics);
    }
#endif

#ifdef USE_AUDIO_STREAMER
    if (nullptr != audioStreamer)
    {
        audioStreamer->addMqttTopicsToRegister(&topics);
    }
#endif

#ifdef USE_TRAFFIC_LIGHT_LEDS
    for (auto& trafficLight : trafficLightLeds)
    {
        trafficLight.addMqttTopicsToRegister(&topics);
    }
#endif

    pushTopicsToIotZooClient(topics);
#endif // USE_MQTT
    topicsRegistered = true;
}

/// @brief Called when the IotZoo Client has not responded to the alive message.
void onIotZooClientUnavailable()
{
    Serial.println("*** IoT-Zoo client is unavailable! Please start it! ***");

    String jsonMicrocontroller = serializeMicrocontroller();

    mqttClient->publish("i_am_lost", jsonMicrocontroller);
    // server dead?
#ifdef USE_TM1637_4
    if (nullptr != tm1637_4Handling)
    {
        tm1637_4Handling->onIotZooClientUnavailable();
    }
#endif

#ifdef USE_TM1637_6
    tm1637_6Handling.onIotZooClientUnavailable();
#endif

#ifdef USE_LED_AND_KEY
    if (nullptr != tm1638)
    {
        tm1638->onIotZooClientUnavailable();
    }
#endif // USE_LED_AND_KEY

#ifdef USE_HT1621
    if (nullptr != ht1621)
    {
        ht1621->onIotZooClientUnavailable();
    }
#endif

#ifdef USE_OLED_SSD1306
    if (nullptr != oled1306)
    {
        oled1306->onIotZooClientUnavailable();
    }
#endif

#ifdef USE_LCD_160X
    if (nullptr != lcdDisplay)
    {
        lcdDisplay->onIotZooClientUnavailable();
    }
#endif

#ifdef USE_STEPPER_MOTOR
    if (nullptr != stepperMotor)
    {
        stepperMotor->onIotZooClientUnavailable();
    }
#endif
}

// ------------------------------------------------------------------------------------------------
// The loop.
// ------------------------------------------------------------------------------------------------
void loop()
{
    try
    {
        Serial.print("_");
        lastLoopStartTime = millis();
        loopCounter++;

        if (doRestart)
        {
            restart();
        }

#if defined(USE_MQTT) || defined(USE_MQTT2)
        mqttClient->loop();
        if (millis() - lastLoopStartTime > 10000)
        {
            Serial.print("BROKEN MQTT");
            restart();
        }
        if (!mqttClient->isConnected())
        {
            Serial.print(".");
            delay(50);
            return;
        }

        if (!topicsRegistered)
        {
            registerTopics();
            String topic = getBaseTopic() + "/started";
            mqttClient->publish(topic, "STARTED");
        }
#endif

#ifdef USE_BLE_HEART_RATE_SENSOR
        if (nullptr != heartRateSensor)
        {
            heartRateSensor->loop();
        }
#endif // USE_BLE_HEART_RATE_SENSOR

#ifdef USE_BUTTON
        buttonHandling.loop();
#endif

#ifdef USE_AUDIO_STREAMER
        if (nullptr != audioStreamer)
        {
            audioStreamer->loop();
        }
#endif

#ifdef USE_WS2818
        if (nullptr != ws2812)
        {
            ws2812->loop();
        }
#endif

#ifdef USE_GPS
        if (nullptr != gps)
        {
            gps->loop();
        }
#endif

#ifdef USE_SWITCH
        for (auto& buttonSwitch : switches)
        {
            buttonSwitch.loop();
        }
#endif

#ifdef USE_LED_AND_KEY
        if (nullptr != tm1638)
        {
            tm1638->loop();
        }
#endif // USE_LED_AND_KEY

#ifdef USE_KY025
        if (millis() - lastRotationMillis < 10000)
        {
            Serial.println(reedContactCounter);
        }

#ifdef USE_TM1637_4
        TM1637* tm1637 = tm1637_4Handling->getDisplayByDeviceIndex(2);
        if (nullptr != tm1637)
        {
            tm1637->showNumber(reedContactCounter, true);
        }
#if defined(USE_MQTT) || defined(USE_MQTT2)
        String topic = getBaseTopic() + "/rpm/0";
        if (rpm > 0)
        {
            Serial.println(String(rpm) + " rpm");
        }
        mqttClient->publish(topic.c_str(), String(rpm, 0).c_str());
#endif
#endif // USE_KY025

#ifdef USE_EXERCISE_BIKE_STANDALONE
        if (millis() - lastRotationMillis > 3000)
        {
            rpm = 0.0;
        }

#ifdef USE_TM1637_4
        auto displayRpm = tm1637_4Handling.displays1637.begin();
        std::advance(displayRpm, 1);
        // displayRpm->showNumber(reedContactCounter);
        displayRpm->showNumber((int)rpm);
#endif
#endif

#if defined(USE_MQTT) || defined(USE_MQTT2)
        if (oldRpm != rpm)
        {
            String topic = getBaseTopic() + "/reed_contact_counter/0";
            mqttClient->publish(topic.c_str(), String(reedContactCounter).c_str());
        }
#endif

#endif

#ifdef USE_REST_SERVER
        webServer.handleClient();
#endif

#ifdef USE_MQTT2
        if (!mqttClient->isConnected())
        {
            mqttClient->connectToBroker(macAddress);
        }
#endif

        // >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
        // The preconditions are fulfilled (MQTT connected).

#ifdef USE_HW040
        hw040Handling.loop();
#endif

#ifdef USE_STEPPER_MOTOR
        if (nullptr != stepperMotor)
        {
            stepperMotor->loop();
        }
#endif

#ifdef USE_DS18B20
        loopDS18B20();
#endif // USE_DS18B20

#ifdef USE_HB0014
        digitalValueInfrared = digitalRead(digitalPinInfraredLed);
        // Serial.println(String(millis()) + " Reed contact: " + String(digitalValueReedContact));
        if (digitalValueInfrared == HIGH && digitalValueOldInfrared == LOW)
        {
            long diff = millis() - lastMillisInfrared;
#ifdef USE_OLED_SSD1306
            if (nullptr != oled1306)
            {
                oled1306->setTextLine(3, String(diff) + " ms");
            }
#endif
            if (diff > 30)
            {
                // Umrechnen in Watt

                // 10000 Impulse entsprechen 1 KW/h.

                // Hochrechnen auf 10000 Impulse = 1000 Watt pro Stunde
                double watt = 360000.0 / diff;
                Serial.println(String(watt) + " watt");
#ifdef USE_OLED_SSD1306
                if (nullptr != oled1306)
                {
                    oled1306->setTextLine(1, String(watt, 0));
                }
#endif
                String topic = getBaseTopic() + "/power/0";
                mqttClient->publish(topic, String(watt, 0));
                lastMillisInfrared = millis();
            }
        }
        digitalValueOldInfrared = digitalValueInfrared;
#endif

#ifdef USE_KEYPAD
        buttonMatrixHandling.loop();
#endif

#ifdef USE_HC_SR501
        motionDetectorsHrsc501Handling.loop();
#endif

#ifdef USE_RD_03D
        if (nullptr != rd03d)
        {
            rd03d->loop();
        }
#endif // USE_RD_03D

#if defined(USE_MQTT) || defined(USE_MQTT2) && !defined(USE_BLE_HEART_RATE_SENSOR)
        try
        {
            if (millis() - lastAliveTime > settings->getAliveIntervalMillis())
            {
                publishAliveMessage();
            }
        }
        catch (const std::exception& e)
        {
            Serial.println(e.what()); // Exception handling does only work with build_flags -DPIO_FRAMEWORK_ARDUINO_ENABLE_EXCEPTIONS
        }
#endif // USE_HC_SR501

        loopDurationMs = millis() - lastLoopStartTime;

        if (millis() - lastServerAliveMillis > (settings->getAliveIntervalMillis() * 2))
        {
            onIotZooClientUnavailable();
            lastServerAliveMillis = millis();
        }

        if (loopDurationMs < 100)
        {
            delay(100 - loopDurationMs);
        }
        digitalWrite(LED_BUILTIN, LOW); // turn the LED off to indicate that the device is offline.
    }
    catch (const std::exception& e)
    {
        publishError(e.what());
        delay(1000);
    }
}

// --- end of main.cpp
