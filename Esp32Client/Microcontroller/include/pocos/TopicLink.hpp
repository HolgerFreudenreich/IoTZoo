#pragma once
#include <WString.h>

namespace IotZoo
{
    struct TopicLink
    {
        TopicLink(const String& topic, const String& expression, const String& targetTopic, const String& targetPayload)
            : TriggeringTopic(topic), Expression(expression), TargetTopic(targetTopic), TargetPayload(targetPayload)
        {
            Serial.println("Constructor TopicLink. TriggeringTopic: " + TriggeringTopic +
                           ", Expression: " + Expression + ", TargetTopic: " + TargetTopic + ", TargetPayload: " + TargetPayload);
        }

        TopicLink(const String& topic, const String& targetTopic) : TriggeringTopic(topic), TargetTopic(targetTopic)
        {
            Serial.println("Constructor TopicLink. TriggeringTopic: " + String(TriggeringTopic.c_str()) +
                           ", TargetTopic: " + String(TargetTopic.c_str()));
        }

        String TriggeringTopic; // Triggering Topic, e.g. "iotzoo/esp32/reed_contact/0/rpm"
        String TargetTopic;     // Target Topic, e.g. "iotzoo/esp32/TM1637_4/0/number"

        String Expression; // json
        // If empty or "input", the payload of the received TriggeringTopic message will be published to TargetTopic.
        String TargetPayload;

        String Payload; // Payload to publish to TargetTopic when Topic is received, e.g. "44.6"
    };
} // namespace IotZoo