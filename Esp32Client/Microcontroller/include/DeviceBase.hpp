
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

#include <ArduinoJson.h>
#ifdef ARDUINO_ESP32_DEV
#include "Settings.hpp"
#endif

#ifdef USE_INTERNAL_MQTT
#include "InternalMqtt/InternalMqtt.h"
#include "pocos/TopicLink.hpp"
#endif

using namespace std;

namespace IotZoo
{
    /// @brief Pure virtual base class of everything that can be connected to the ESP32.
    class DeviceBase
    {
      public:
        DeviceBase(int deviceIndex, Settings* const settings, MqttClient* const mqttClient, const String& baseTopic)
            : deviceIndex(deviceIndex), settings(settings), mqttClient(mqttClient), baseTopic(baseTopic)
        {
            Serial.println("Constructor DeviceBase. DeviceIndex: " + String(deviceIndex) + ", baseTopic: " + baseTopic);
        }

        virtual ~DeviceBase()
        {
            Serial.println("Destructor DeviceBase. DeviceIndex: " + String(deviceIndex) + ", baseTopic: " + baseTopic);
        }

#ifdef USE_INTERNAL_MQTT
        void setInternalMqttClient(InternalMqttClient* const client)
        {
            internalMqttClient = client;
        }

        // true: topicLink.Expression is empty or evaluates to true.
        bool EvaluateExpression(const TopicLink& topicLink)
        {
            bool doIt = false;
            if (topicLink.Expression.length() == 0)
            {
                doIt = true; // no expression means "always publish"
            }
            else
            {
                debug("EvaluateExpression. topicLink.Expression: " + topicLink.Expression);

                StaticJsonDocument<512> jsonDocument;

                if (!deserializeStaticJsonAndPublishError(jsonDocument, topicLink.Expression))
                {
                    return false;
                }

                String strOperator    = jsonDocument["Operator"].as<String>();
                double referenceValue = jsonDocument["Value"].as<double>();
                if (strOperator == ">")
                {
                    if (topicLink.Payload.toDouble() > referenceValue)
                    {
                        doIt = true;
                    }
                }
                else if (strOperator == "<")
                {
                    if (topicLink.Payload.toDouble() < referenceValue)
                    {
                        doIt = true;
                    }
                }
                else if (strOperator == "==")
                {
                    if (referenceValue == topicLink.Payload.toDouble())
                    {
                        doIt = true;
                    }
                }
                debug("Payload: " + topicLink.Payload + " " + "Expression operator: " + strOperator + " value: " + referenceValue +
                      ", -> doIt: " + String(doIt));
            }
            return doIt;
        }

        // Override this method to set the topicLink.Payload based on the current device data. This is needed if topicLink.TargetPayload is "input" or
        // empty. topicLink.Payload will be send via internal MQTT.
        // Return true if topicLink.Payload has been set, false otherwise.
        virtual bool setPayloadPropertyOfTopicLink(TopicLink& topicLink)
        {
            debug("DeviceBase::setPayloadPropertyOfTopicLink. topicLink.TriggeringTopic: " + topicLink.TriggeringTopic +
                  ", topicLink.Expression: " + topicLink.Expression + ", topicLink.TargetTopic: " + topicLink.TargetTopic);
            if (!topicLink.TargetPayload.isEmpty())
            {
                topicLink.Payload =
                    topicLink.TargetPayload; // default to configured TargetPayload if TriggeringTopic does not match any known topic.)
                return true;
            }
            return false;
        }

        virtual void publishInternalMqtt()
        {
            debug("DeviceBase::publishInternalMqtt. Count of TopicLinks: " + String(TopicLinks.size()));

            if (nullptr != internalMqttClient)
            {
                // Has an internal component interest on counter changes?
                for (auto& topicLink : TopicLinks)
                {
                    // Should the event take place?
                    bool doPublish = false;

                    if (topicLink.Expression.length() == 0)
                    {
                        doPublish = true; // no expression means "always publish"
                    }
                    if (!setPayloadPropertyOfTopicLink(topicLink))
                    {
                        debug("DeviceBase::publishInternalMqtt. setPayloadPropertyOfTopicLink did not set topicLink.Payload. "
                              "topicLink.TriggeringTopic: " +
                              topicLink.TriggeringTopic + ", topicLink.Expression: " + topicLink.Expression +
                              ", topicLink.TargetTopic: " + topicLink.TargetTopic);
                    }
                    doPublish = EvaluateExpression(topicLink);

                    if (doPublish)
                    {
                        internalMqttClient->publish(topicLink);
                    }
                }
            }
        }

#endif // USE_INTERNAL_MQTT
        int getDeviceIndex() const
        {
            return deviceIndex;
        }

        virtual void loop()
        {
#ifdef USE_INTERNAL_MQTT
            if (nullptr != internalMqttClient)
            {
                publishInternalMqtt();
                internalMqttClient->loop();
            }
#endif // USE_INTERNAL_MQTT
        }

        /// @brief Let the user know what the device can do.
        /// @param topics
        virtual void addMqttTopicsToRegister(std::vector<Topic>* const topics) const
        {
            for (auto& topic : getTopics())
            {
                topics->push_back(topic);
            }
        }

        /// @brief Get the topics this device supports.
        /// @return
        virtual std::vector<Topic> getTopics() const
        {
            return {}; // Return an empty vector by default. Override this method in derived classes to provide actual topics.
        }

        /// @brief The MQTT connection is established. Now subscribe to the topics. An existing MQTT connection is a
        /// prerequisite for a subscription.
        /// @param mqttClient
        /// @param baseTopic
        virtual void onMqttConnectionEstablished()
        {
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

        String getDeviceName() const
        {
            return deviceName;
        }

        int getDeviceIdex() const
        {
            return deviceIndex;
        }

        MqttClient* getMqttClient() const
        {
            return mqttClient;
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

#ifdef USE_INTERNAL_MQTT
        virtual void subscribeToInternalMqttTopics()
        {
            Serial.println("override subscribeToMqttTopics!");
        }

        InternalMqttClient* getInternalMqttClient() const
        {
            return internalMqttClient;
        }

        void setTopicLinks(const std::vector<TopicLink>& links)
        {
            TopicLinks = links;
        }

        vector<TopicLink> getTopicLinks() const
        {
            return TopicLinks;
        }
#endif // USE_INTERNAL_MQTT

      protected:
        MqttClient* mqttClient  = nullptr;
        Settings*   settings    = nullptr;
        int         deviceIndex = -1;
        String      deviceName;
        String      baseTopic;
        bool        mqttCallbacksAreRegistered = false;

#ifdef USE_INTERNAL_MQTT
        InternalMqttClient* internalMqttClient = nullptr;
        vector<TopicLink>   TopicLinks;
#endif
    };

} // namespace IotZoo
#endif
