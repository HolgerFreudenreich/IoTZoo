// --------------------------------------------------------------------------------------------------------------------
//      ____    ______   _____
//     /  _/___/_  __/  /__  / ____  ____
//     / // __ \/ /       / / / __ \/ __ \  P L A Y G R O U N D
//   _/ // /_/ / /       / /_/ /_/ / /_/ /
//  /___/\____/_/       /____|____/\____/   (c) 2025 - 2026 Holger Freudenreich under the MIT licence.
//
// --------------------------------------------------------------------------------------------------------------------
// GPS support
// --------------------------------------------------------------------------------------------------------------------
#include "Defines.hpp"
#ifdef USE_GPS
#include "Gps.hpp"

namespace IotZoo
{
    Gps::Gps(int deviceIndex, Settings* const settings, MqttClient* const mqttClient, const String& baseTopic, uint8_t pinRx, uint8_t pinTx,
             uint32_t baud)
        : DeviceBase(deviceIndex, settings, mqttClient, baseTopic)
    {
        Serial.println("Constructor Gps. pinRx: " + String(pinRx) + ", pinTx: " + String(pinTx) + ", baud: " + String(baud));
        this->pinRx    = pinRx;
        this->pinTx    = pinTx;
        softwareSerial = new SoftwareSerial(pinRx, pinTx);
        softwareSerial->begin(baud);
    }

    Gps::~Gps()
    {
        Serial.println("Constructor Gps");
        delete softwareSerial;
    }

    /// @brief Let the user know what the device can do.
    /// @param topics
    void Gps::addMqttTopicsToRegister(std::vector<Topic>* const topics) const
    {
        String examplePayload = "{\"lat\": 52.63, \"lon\": 9.61, \"alt\": 32.1, \"dateTimeUtc\": \"2025-10-05 17:30:36\"}";

        topics->emplace_back(getBaseTopic() + "/gps/position" + String(deviceIndex), examplePayload, MessageDirection::IotZooClientInbound);
    }

    void Gps::loop()
    {
        smartDelay(1000);

        if (millis() > 5000 && gps.charsProcessed() < 10)
        {
            Serial.println(F("No GPS data received: check wiring"));
        }

        if (gps.location.isValid())
        {
            String payload = "{ \"Lat\": " + String(gps.location.lat(), 3U) + ", \"Lon\": " + String(gps.location.lng(), 3U) +
                             ", \"Alt\": " + String(gps.altitude.meters(), 1U);

            if (gps.date.isValid() && gps.time.isValid())
            {
                char sz[64] = {};
                sprintf(sz, "%02d-%02d-%02d %02d:%02d:%02d", gps.date.year(), gps.date.month(), gps.date.day(), gps.time.hour(), gps.time.minute(),
                        gps.time.second());
                payload += ", \"DateTimeUtc\": \"" + String(sz) + "\"";
            }
            payload += "}";
            mqttClient->publish(getBaseTopic() + "/gps/position" + String(deviceIndex), payload);
        }
    }

    // This custom version of delay() ensures that the gps object
    // is being "fed".
    void Gps::smartDelay(unsigned long ms)
    {
        unsigned long start = millis();
        do
        {
            while (softwareSerial->available())
            {
                gps.encode(softwareSerial->read());
            }
        } while (millis() - start < ms);
    }

} // namespace IotZoo

#endif
