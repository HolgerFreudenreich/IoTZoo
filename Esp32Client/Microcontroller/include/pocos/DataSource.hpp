#pragma once

#include <WString.h>


enum class DataLinkType
{
    Unknown = -1,
    Mqtt,
    Internal
};

// Purpose: Purpose: To link devices without creating a rule in the IoT client.
// The device should also be able to function without the IoT client.
// An MQTT broker is still required; it would also be conceivable to replace it, for example,
// with direct communication between the devices.
struct DataSource
{
    DataSource(const String& topic, const String& method, const DataLinkType dataLinkType) : Topic(topic), Method(method), DataLinkType(dataLinkType)
    {
         Serial.println("Constructor DataSource. Topic: " + Topic + ", method: " + Method);
    }

    ~DataSource()
    {
        Serial.println("Destructor DataSource. Topic: " + Topic + ", method: " + Method);
    }

    // The topic to subscribe to. E.g. "iotzoo/picea/esp32/E4:65:B8:B0:45:B4/reed_contact/0/counter"
    String Topic;
    // The method to call on the device when a message is received for the topic. E.g. "showNumber"
    String Method;

    DataLinkType DataLinkType;
};