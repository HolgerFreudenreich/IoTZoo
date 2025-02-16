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

#ifdef USE_MQTT
#include "MqttClient.hpp"

namespace IotZoo
{
    MqttClient::MqttClient(const char* mqttClientName, const char* wifiSsid, const char* wifiPassword, const char* mqttServerIp,
                           const char* mqttUsername, const char* mqttPassword, const short mqttServerPort, int bufferSize)
    {
        Serial.println("Constructor MqttClient mqttServerIp: " + String(mqttServerIp) + ":" + String(mqttServerPort));
        mqttClient = new EspMQTTClient(wifiSsid,       // SSID
                                       wifiPassword,   // PWD SSID
                                       mqttServerIp,   // MQTT Broker server ip
                                       mqttUsername,   // MQTT Broker User; Can be omitted if not needed
                                       mqttPassword,   // MQTT Broker Password; Can be omitted if not needed
                                       mqttClientName, // Client name that UNIQUELY identifies your device. macAddress.c_str() does
                                                       // not work! The mqtt client name must be unique otherwise we get disconnects!
                                       mqttServerPort);

        mqttClient->setMaxPacketSize(bufferSize); // default is only 128 bytes! When exeeding the message will not be published!
        mqttClient->enableDebuggingMessages(false);
        // The reconnection should be established after 100 ms.
        mqttClient->setMqttReconnectionAttemptDelay(100);

        mqttClient->setKeepAlive(30);

        mqttClient->enableOTA("IotZoo", // password
                              8266);    // port
        // mqttClient->enableHTTPWebUpdater("IotZoo");
    }

    MqttClient::~MqttClient()
    {
        Serial.println("Destructor MqttClient");
    }

    void MqttClient::enableLastWillMessage(const String& topic, const String& message,
                                           const bool retain) // Must be set before the first loop() call.
    {
        return mqttClient->enableLastWillMessage(topic.c_str(), message.c_str(), retain);
    }

    bool MqttClient::publish(const String& topic, const String& payload, bool retain)
    {
        Serial.println("─┐");
        Serial.print(">>> Publishing topic:\r\n" + topic + "\r\n\r\npayload:\r\n" + payload + "\r\nretain: " + String(retain) +
                     "\r\nMqttBrokerIp: " + this->mqttClient->getMqttServerIp());
        return printSuccess(mqttClient->publish(topic, payload, retain));
    }

    /// @brief
    /// @param topic
    /// @param messageReceivedCallback
    /// @param qos // 0 or 1 only
    /// @return
    bool MqttClient::subscribe(const String& topic, MessageReceivedCallback messageReceivedCallback, uint8_t qos)
    {
        Serial.print("Subscribing topic: " + topic + ", qos: " + String(qos));
        return printSuccess(mqttClient->subscribe(topic, messageReceivedCallback, qos));
    }

    bool MqttClient::subscribe(const String& topic, MessageReceivedCallbackWithTopic messageReceivedCallback, uint8_t qos)
    {
        Serial.print("Subscribing (topic with topic): " + topic + ", qos: " + String(qos));
        return printSuccess(mqttClient->subscribe(topic, messageReceivedCallback, qos));
    }

    bool MqttClient::unsubscribe(const String& topic)
    {
        return mqttClient->unsubscribe(topic);
    }

    unsigned int MqttClient::getConnectionEstablishedCount() const
    {
        return mqttClient->getConnectionEstablishedCount(); // Return the number of time onConnectionEstablished has been
                                                            // called since the beginning.
    }

    bool MqttClient::isConnected() const
    {
        return (mqttClient->isWifiConnected() && mqttClient->isMqttConnected());
    }

    void MqttClient::removeRetainedMessageFromBroker(const String& topic)
    {
        Serial.println("*** Removing topic " + topic);
        mqttClient->publish(topic, "", true);
    }

    /// Main loop
    void MqttClient::loop()
    {
        mqttClient->loop();
    }

    bool MqttClient::printSuccess(bool ok)
    {
        Serial.println("");
        if (ok)
        {
            Serial.println(" -> OK " + String(millis()));
        }
        else
        {
            Serial.println(" -> NOK " + String(millis()));
        }
        Serial.println("─┘");
        return ok;
    }
} // namespace IotZoo
#endif