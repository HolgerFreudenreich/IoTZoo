// --------------------------------------------------------------------------------------------------------------------
//      ____    ______   _____
//     /  _/___/_  __/  /__  / ____  ____
//     / // __ \/ /       / / / __ \/ __ \  P L A Y G R O U N D
//   _/ // /_/ / /       / /_/ /_/ / /_/ /
//  /___/\____/_/       /____|____/\____/   (c) 2025 Holger Freudenreich under the MIT licence.
//
// --------------------------------------------------------------------------------------------------------------------
// Connect «Things» with microcontrollers in a simple way.
// --------------------------------------------------------------------------------------------------------------------
#ifndef __MQTT_CLIENT_HPP___
#define __MQTT_CLIENT_HPP___

#ifdef USE_MQTT

#include "Defines.hpp"
#include "EspmqttClient.h"

#include <Arduino.h>

namespace IotZoo
{
    class MqttClient
    {
      protected:
        EspMQTTClient* mqttClient = nullptr; // Extends the PubSubClient Library, but requires more memory. Has nice debugging messages.

      public:
        MqttClient(const char* mqttClientName, const char* wifiSsid, const char* wifiPassword, const char* mqttServerIp,
                   const char* mqttUsername = nullptr, const char* mqttPassword = nullptr, const short mqttServerPort = 1883, int bufferSize = 16384);

        virtual ~MqttClient();

        void enableLastWillMessage(const String& topic, const String& message,
                                   const bool retain = false); // Must be set before the first loop() call.

        bool publish(const String& topic, const String& payload, bool retain = false);

        /// @brief
        /// @param topic
        /// @param messageReceivedCallback
        /// @param qos // 0 or 1 only
        /// @return
        bool subscribe(const String& topic, MessageReceivedCallback messageReceivedCallback, uint8_t qos = 0);

        bool subscribe(const String& topic, MessageReceivedCallbackWithTopic messageReceivedCallback, uint8_t qos = 0);

        bool unsubscribe(const String& topic);

        unsigned int getConnectionEstablishedCount() const;

        bool isConnected() const;

        void removeRetainedMessageFromBroker(const String& topic);

        /// Main loop, to call at each sketch loop()
        void loop();

      protected:
        bool printSuccess(bool ok);
    };
} // namespace IotZoo
#endif
#endif