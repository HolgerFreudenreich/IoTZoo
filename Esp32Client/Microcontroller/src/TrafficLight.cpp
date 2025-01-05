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
#ifdef USE_TRAFFIC_LIGHT_LEDS

#include "TrafficLight.hpp"

namespace IotZoo
{
  TrafficLight::TrafficLight(int deviceIndex, MqttClient *const mqttClient, const String &baseTopic,
                             u_int8_t pinRedLed, u_int8_t pinYellowLed, u_int8_t pinGreenLed) : DeviceBase(deviceIndex, mqttClient, baseTopic)
  {
    this->pinRedLed = pinRedLed;
    this->pinYellowLed = pinYellowLed;
    this->pinGreenLed = pinGreenLed;

    pinMode(this->pinRedLed, OUTPUT);    // red LED
    pinMode(this->pinYellowLed, OUTPUT); // yellow LED
    pinMode(this->pinGreenLed, OUTPUT);  // green LED

    Serial.println("TrafficLight instantiated with pinRed: " + String(this->pinRedLed) + ", pinYellow: " +
                   String(this->pinYellowLed) + ", pinGreen: " + String(this->pinGreenLed));
  }

  TrafficLight::~TrafficLight()
  {
    Serial.println("Destructor TrafficLight with pinRed: " + String(this->pinRedLed) + ", pinYellow: " +
                   String(this->pinYellowLed) + ", pinGreen: " + String(this->pinGreenLed));
  }

  void TrafficLight::handleTrafficLightPayload(const String &payload)
  {
    Serial.println("handleTrafficLightPayload: " + payload + " received. GPIO Red: " + String(pinRedLed) +
                   ", GPIO Yellow: " + String(pinYellowLed) + ", GPIO Green: " + String(pinGreenLed));
    String payloadLowerCase = payload;
    payloadLowerCase.toLowerCase();
    if (payloadLowerCase == "g" || payloadLowerCase == "0" || payloadLowerCase == "green")
    {
      digitalWrite(pinRedLed, LOW);
      digitalWrite(pinYellowLed, LOW);
      digitalWrite(pinGreenLed, HIGH);
      Serial.println("GREEN");
    }
    else if (payloadLowerCase == "y" || payloadLowerCase == "1" || payloadLowerCase == "yellow")
    {
      digitalWrite(pinRedLed, LOW);
      digitalWrite(pinYellowLed, HIGH);
      digitalWrite(pinGreenLed, LOW);
      Serial.println("YELLOW");
    }
    else if (payloadLowerCase == "r" || payloadLowerCase == "2" || payloadLowerCase == "red")
    {
      digitalWrite(pinRedLed, HIGH);   // RED LED
      digitalWrite(pinYellowLed, LOW); // YELLOW LED
      digitalWrite(pinGreenLed, LOW);  // GREEN LED
      Serial.println("RED");
    }
  }

  /// @brief Let the user know what the device can do.
  /// @param topics
  void TrafficLight::addMqttTopicsToRegister(std::vector<Topic> *const topics) const
  {
    topics->push_back(*new Topic(getBaseTopic() + "/traffic_light/" + String(getDeviceIndex()),
                                 "Payload: To turn green led on (and yellow and red off): g, 0, green, To turn yellow led on (and green and red off): y, 1, yellow, To turn red led on (and green and yellow off): r, 2, red",
                                 MessageDirection::IotZooClientOutbound));
  }

  /// @brief The MQTT connection is established. Now subscribe to the topics. An existing MQTT connection is a prerequisite for a subscription.
  /// @param mqttClient
  /// @param baseTopic
  void TrafficLight::onMqttConnectionEstablished()
  {
    Serial.println("TrafficLight::onMqttConnectionEstablished");
    if (mqttCallbacksAreRegistered)
    {
      Serial.println("Reconnection -> nothing to do.");
      return;
    }
    String topicTrafficLightLeds = getBaseTopic() + "/traffic_light/" + String(getDeviceIndex());
    mqttClient->subscribe(topicTrafficLightLeds, [&](const String &payload)
                          { handleTrafficLightPayload(payload); });
  }
}

#endif // USE_TRAFFIC_LIGHT_LEDS