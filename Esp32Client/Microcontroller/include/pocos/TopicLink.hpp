#pragma once
#include <WString.h>
#include <string>

namespace IotZoo
{
    struct TopicLink
    {
        TopicLink(const String& topic, const String& targetTopic) : TriggeringTopic(topic), TargetTopic(targetTopic)
        {
            Serial.println("Constructor TopicLink. TriggeringTopic: " + String(TriggeringTopic.c_str()) + ", TargetTopic: " + String(TargetTopic.c_str()));
        }
        
        String TriggeringTopic; // Triggering Topic, e.g. "iotzoo/esp32/reed_contact/0/rpm"
        String TargetTopic; // Target Topic, e.g. "iotzoo/esp32/TM1637_4/0/number"

        String Payload; // Payload to publish to TargetTopic when Topic is received, e.g. "44.6"
    };
} // namespace IotZoo