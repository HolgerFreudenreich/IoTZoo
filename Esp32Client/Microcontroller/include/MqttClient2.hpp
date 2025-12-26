// --------------------------------------------------------------------------------------------------------------------
//      ____    ______   _____
//     /  _/___/_  __/  /__  / ____  ____
//     / // __ \/ /       / / / __ \/ __ \  P L A Y G R O U N D
//   _/ // /_/ / /       / /_/ /_/ / /_/ /
//  /___/\____/_/       /____|____/\____/   (c) 2025 - 2026 Holger Freudenreich under the MIT licence.
//
// --------------------------------------------------------------------------------------------------------------------
// Connect «Things» with microcontrollers in a simple way.
// --------------------------------------------------------------------------------------------------------------------
#include "Defines.hpp"
#ifdef USE_MQTT2

#ifndef __MQTT_CLIENT_MQTT2_HPP__
#define __MQTT_CLIENT_MQTT2_HPP__


// Needs less memory than USE_MQTT
typedef std::function<void(const String &message)> MessageReceivedCallback;
typedef std::function<void(const String &topicStr, const String &message)> MessageReceivedCallbackWithTopic;

#include <WiFi.h>

#include <PubSubClient.h>

#include <Arduino.h>

namespace IotZoo
{
    class MqttClient
    {
    protected:
        PubSubClient *mqttClient = nullptr;
        WiFiClient wifiClient;

    public:
        MqttClient(const char *mqttClientName,
                   const char *wifiSsid,
                   const char *wifiPassword,
                   const char *mqttServerIp,
                   const char *mqttUsername = nullptr,
                   const char *mqttPassword = nullptr,
                   const short mqttServerPort = 1883,
                   int bufferSize = 4096)
        {

            mqttClient = new PubSubClient(wifiClient);
            WiFi.setAutoReconnect(true);
            WiFi.begin(wifiSsid, wifiPassword);
            while (WiFi.status() != WL_CONNECTED)
            {
                Serial.println("Connecting to WiFi...");
                delay(200);
            }
            Serial.println("Connected to WiFi");

            IPAddress ip;
            ip.fromString(mqttServerIp);
            mqttClient->setServer(ip, mqttServerPort);
            mqttClient->setBufferSize(bufferSize); // Max length of the message. When exeeding the message will not be published or received!
        }

        void enableLastWillMessage(const String &topic, const String &message, const bool retain = false) // Must be set before the first loop() call.
        {
        }

        bool publish(const String &topic, const String &payload, bool retain = false)
        {
            return mqttClient->publish(topic.c_str(), payload.c_str(), retain);
        }

        /// @brief
        /// @param topic
        /// @param messageReceivedCallback
        /// @param qos // 0 or 1 only
        /// @return
        bool subscribe(const String &topic, MessageReceivedCallback messageReceivedCallback, uint8_t qos = 0)
        {
            return mqttClient->subscribe(topic.c_str(), qos);
        }

        bool subscribe(const String &topic, MessageReceivedCallbackWithTopic messageReceivedCallback, uint8_t qos = 0)
        {
            return mqttClient->subscribe(topic.c_str(), qos); // 0 or 1 only
        }

        bool subscribe(const String &topic, MQTT_CALLBACK_SIGNATURE, uint8_t qos = 0)
        {
            bool result = mqttClient->subscribe(topic.c_str(), qos); // 0 or 1 only
            mqttClient->setCallback(callback);
            return result;
        }

        inline unsigned int getConnectionEstablishedCount() const
        {

            return 0;
        }

        inline bool isConnected() const
        {

            return mqttClient->connected();

        }; // Return true if everything is connected

        void connectToBroker(const String &macAddress)
        {
            while (!mqttClient->connected())
            {
                Serial.print("Attempting MQTT connection...");

                char *mqttClientName = new char[18]();

                for (int i = 0; i < 17; i++)
                {
                    mqttClientName[i] = macAddress[i];
                }

                if (mqttClient->connect(mqttClientName))
                {
                    Serial.println("connected");
                    // mqttClient.subscribe(topic);
                    // mqttClient.publish(topic, "Hello from ESP32");
                }
                else
                {
                    Serial.print("failed, rc=");
                    Serial.print(mqttClient->state());
                    Serial.println(" try again in 1 second.");
                    delay(1000);
                }
            }
        }

        void setup(const String &macAddress)
        {
            connectToBroker(macAddress);
        }

        void removeRetainedMessageFromBroker(const String &topic)
        {
            Serial.println("*** Removing topic " + topic);

            mqttClient->publish(topic.c_str(), "", true);
        }

        /// Main loop, to call at each sketch loop()
        void loop()
        {
            mqttClient->loop();
        }
    };

}
#endif // __MQTT_CLIENT_MQTT2_HPP__

#endif // USE_MQTT2