
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
#ifndef __DEVICE_BASE_HPP__
#define __DEVICE_BASE_HPP__

#include "Defines.hpp"
#ifdef USE_MQTT
#include "MqttClient.hpp"
#endif
#ifdef USE_MQTT2
#include "MqttClient2.hpp"
#endif
#include "./pocos/Topic.hpp"

using namespace std;

namespace IotZoo
{
    /// @brief Pure virtual base class of everything that can be connected to the ESP32.
    class DeviceBase
    {
      public:
        DeviceBase(int deviceIndex, MqttClient* const mqttClient, const String& baseTopic)
            : deviceIndex(deviceIndex), mqttClient(mqttClient), baseTopic(baseTopic)
        {
            Serial.println("Constructor DeviceBase. DeviceIndex: " + String(deviceIndex) + ", baseTopic: " + baseTopic);
        }

        virtual ~DeviceBase()
        {
            Serial.println("Destructor DeviceBase. DeviceIndex: " + String(deviceIndex) + ", baseTopic: " + baseTopic);
        }

        int getDeviceIndex() const
        {
            return deviceIndex;
        }

        virtual void loop()
        {
            Serial.println("do override loop!");
        }

        /// @brief Let the user know what the device can do.
        /// @param topics
        virtual void addMqttTopicsToRegister(std::vector<Topic>* const topics) const = 0;

        /// @brief The MQTT connection is established. Now subscribe to the topics. An existing MQTT connection is a
        /// prerequisite for a subscription.
        /// @param mqttClient
        /// @param baseTopic
        virtual void onMqttConnectionEstablished()
        {
            Serial.println("do override onMqttConnectionEstablished!");
            mqttCallbacksAreRegistered = true;
        }

        /// @brief The IotZooMqtt client is not available, so tell this this user. Providing false information is worse
        /// than not providing any information.
        ///        This method is a suitable point to erase a display or stop something.
        virtual void onIotZooClientUnavailable()
        {
            Serial.println("do override onIotZooClientUnavailable!");
        }

        String getBaseTopic() const
        {
            return baseTopic;
        }

        void publishError(const String& errMsg)
        {
            String topic = getBaseTopic() + "/error";
            mqttClient->publish(topic, errMsg);
        }

      protected:
        MqttClient* mqttClient;
        int         deviceIndex = -1;
        String      baseTopic;
        bool        mqttCallbacksAreRegistered = false;
    };

} // namespace IotZoo
#endif
