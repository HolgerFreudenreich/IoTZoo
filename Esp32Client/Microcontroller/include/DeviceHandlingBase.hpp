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

#ifndef __DEVICE_HANDLING_BASE_HPP__
#define __DEVICE_HANDLING_BASE_HPP__
#ifdef USE_MQTT
#include "MqttClient.hpp"
#endif
#ifdef USE_MQTT2
#include "MqttClient2.hpp"
#endif
#include "./pocos/Topic.hpp"
#include "InternalMqtt/InternalMqtt.h"
#include "Settings.hpp"

using namespace std;

namespace IotZoo
{
    /// @brief Pure virtual base class of everything that can be connected to the ESP32.
    class DeviceHandlingBase
    {
      public:
        DeviceHandlingBase()
        {
            Serial.println("Constructor DeviceHandlingBase()");
        }

        virtual ~DeviceHandlingBase()
        {
            Serial.println("~DeviceHandlingBase()");
        }

#ifdef USE_INTERNAL_MQTT
         void setInternalMqttClient(InternalMqttClient* const client)
        {
            internalMqttClient = client;
        }
#endif

        /// @brief The IotZooMqtt client is not available, so tell this this user. Providing false information is worse
        /// than not providing any information.
        ///        This method is a suitable point to erase a display or stop something.
        virtual void onIotZooClientUnavailable()
        {
            Serial.println("DeviceHandlingBase override onIotZooClientUnavailable!");
        }

        /// @brief Let the user know what the device can do.
        /// @param topics
        virtual void addMqttTopicsToRegister(std::vector<Topic>* const topics) const = 0;

        /// @brief The MQTT connection is established. Now subscribe to the topics. An existing MQTT connection is a
        /// prerequisite for a subscription.
        /// @param mqttClient
        /// @param baseTopic
        virtual void onMqttConnectionEstablished(MqttClient* mqttClient, const String& baseTopic)
        {
            Serial.println("override onMqttConnectionEstablished!");
        }

        virtual void subscribeToInternalMqttTopics()
        {
            Serial.println("override subscribeToMqttTopics!");
        }

      protected:
        MqttClient* mqttClient             = nullptr;
        Settings*   settings               = nullptr;
        bool        callbacksAreRegistered = false;
#ifdef USE_INTERNAL_MQTT
        InternalMqttClient* internalMqttClient = nullptr;
#endif
    };
} // namespace IotZoo
#endif // __DEVICE_HANDLING_BASE_HPP__