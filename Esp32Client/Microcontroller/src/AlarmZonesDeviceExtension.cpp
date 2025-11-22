// --------------------------------------------------------------------------------------------------------------------
//      ____    ______   _____
//     /  _/___/_  __/  /__  / ____  ____
//     / // __ \/ /       / / / __ \/ __ \  P L A Y G R O U N D
//   _/ // /_/ / /       / /_/ /_/ / /_/ /
//  /___/\____/_/       /____|____/\____/   (c) 2025 Holger Freudenreich under the MIT licence.
//
// --------------------------------------------------------------------------------------------------------------------
// Private extension without a public use case
// --------------------------------------------------------------------------------------------------------------------
#include "Defines.hpp"
#ifdef USE_WS2818_PIXEL_MATRIX
#ifdef USE_WS2818

#include "./pocos/Topic.hpp"
#include "AlarmZonesDeviceExtension.hpp"
#include "DeviceBase.hpp"
#include "PixelMatrix.hpp"

namespace IotZoo
{
    AlarmZonesDeviceExtension::AlarmZonesDeviceExtension(DeviceBase* const deviceBase) : DeviceExtension(deviceBase)
    {
        Serial.println("Constructor AlarmZonesDeviceExtension");
    }

    void AlarmZonesDeviceExtension::onMqttConnectionEstablished()
    {
        String topic = deviceBase->getBaseTopic() + "/" + deviceBase->getDeviceName() + "/" + String(deviceBase->getDeviceIndex()) + "/alarm";
        deviceBase->getMqttClient()->subscribe(topic, [&](const String& json) { onAlarmReceived(json); });
        Serial.println("PixelMatrix subscribed to topic " + topic);
    }

    /// @brief Let the user know what the device can do.
    /// @param topics
    void AlarmZonesDeviceExtension::addMqttTopicsToRegister(std::vector<Topic>* const topics) const
    {
        topics->emplace_back(deviceBase->getBaseTopic() + "/" + deviceBase->getDeviceName() + "/" + String(deviceBase->getDeviceIndex()) + "/alarm",
                             "{ \"zone\":\"cam1\", \"level\": 1}", MessageDirection::IotZooClientOutbound);
    }

    uint AlarmZonesDeviceExtension::getAlarmLevel(const String& subject)
    {
        uint level = 0;
        if (subject.indexOf("motion") > -1) // yellow
        {
            level = 1;
        }
        else if (subject.indexOf("animal") > -1) // green
        {
            level = 2;
        }
        else if (subject.indexOf("vehicle") > -1) // blue
        {
            level = 3;
        }
        else if (subject.indexOf("person") > -1) // red
        {
            level = 4;
        }
        Serial.println("level: " + String(level));
        return level;
    }

    void AlarmZonesDeviceExtension::onAlarmReceived(const String& subject)
    {
        Serial.println("onAlarmReceived subject: " + subject);
        uint level      = getAlarmLevel(subject);
        uint brightness = 4;
        if (level == 0)
        {
            return;
        }
        PixelMatrix* pixelMatrix = (PixelMatrix*)deviceBase;

        u32_t color              = 0;
        u32_t millisUntilTurnOff = 60000;

        if (level == 1)
        {
            color = pixelMatrix->getPixels()->Color(255, 175, 0); // motion -> yellow/orange
            return;                                               // ignore motion
        }
        else if (level == 2)
        {
            color = pixelMatrix->getPixels()->Color(0, 255, 0); // animal -> green
        }
        else if (level == 3)
        {
            color = pixelMatrix->getPixels()->Color(0, 0, 255); // vehicle -> blue
        }
        else if (level == 4)
        {
            color = pixelMatrix->getPixels()->Color(255, 0, 0); // person -> red
        }

        if (pixelMatrix->GetNumberOfLedsPerColumn() == 8 && pixelMatrix->GetNumberOfLedsPerRow() == 8)
        {

            if (subject.indexOf("Dachboden") > -1)
            {
                pixelMatrix->setPixelColor(color, 27, 3, brightness, millisUntilTurnOff);
                pixelMatrix->setPixelColor(color, 34, 3, brightness, millisUntilTurnOff);
            }
            else if (subject.indexOf("Schuppen") > -1)
            {
                pixelMatrix->setPixelColor(color, 51, 3, brightness, millisUntilTurnOff);
                pixelMatrix->setPixelColor(color, 58, 3, brightness, millisUntilTurnOff);
            }
            else if (subject.indexOf("Vorne") > -1)
            {
                pixelMatrix->setPixelColor(color, 0, 3, brightness, millisUntilTurnOff);
                pixelMatrix->setPixelColor(color, 13, 3, brightness, millisUntilTurnOff);
                pixelMatrix->setPixelColor(color, 16, 3, brightness, millisUntilTurnOff);
                pixelMatrix->setPixelColor(color, 29, 3, brightness, millisUntilTurnOff);
            }
            else if (subject.indexOf("Hinten") > -1)
            {
                pixelMatrix->setPixelColor(color, 21, 1, brightness, millisUntilTurnOff);
                pixelMatrix->setPixelColor(color, 26, 1, brightness, millisUntilTurnOff);
                pixelMatrix->setPixelColor(color, 37, 1, brightness, millisUntilTurnOff);
                pixelMatrix->setPixelColor(color, 42, 1, brightness, millisUntilTurnOff);
            }
            else if (subject.indexOf("Terrasse") > -1)
            {
                pixelMatrix->setPixelColor(color, 2, 3, brightness, millisUntilTurnOff);
                pixelMatrix->setPixelColor(color, 11, 3, brightness, millisUntilTurnOff);
                pixelMatrix->setPixelColor(color, 18, 3, brightness, millisUntilTurnOff);
            }
            else if (subject.indexOf("Norden") > -1)
            {
                pixelMatrix->setPixelColor(color, 32, 4, brightness, millisUntilTurnOff);
                pixelMatrix->setPixelColor(color, 44, 4, brightness, millisUntilTurnOff);
                pixelMatrix->setPixelColor(color, 48, 4, brightness, millisUntilTurnOff);
                pixelMatrix->setPixelColor(color, 60, 4, brightness, millisUntilTurnOff);
            }
            else if (subject.indexOf("Westen") > -1)
            {
                pixelMatrix->setPixelColor(color, 52, 3, brightness, millisUntilTurnOff);
                pixelMatrix->setPixelColor(color, 41, 3, brightness, millisUntilTurnOff);
                pixelMatrix->setPixelColor(color, 36, 3, brightness, millisUntilTurnOff);
                pixelMatrix->setPixelColor(color, 25, 3, brightness, millisUntilTurnOff);
            }
            else if (subject.indexOf("Osten") > -1)
            {
                pixelMatrix->setPixelColor(color, 21, 3, brightness, millisUntilTurnOff);
                pixelMatrix->setPixelColor(color, 24, 3, brightness, millisUntilTurnOff);
                pixelMatrix->setPixelColor(color, 37, 3, brightness, millisUntilTurnOff);
                pixelMatrix->setPixelColor(color, 40, 3, brightness, millisUntilTurnOff);
                pixelMatrix->setPixelColor(color, 52, 4, brightness, millisUntilTurnOff);
                pixelMatrix->setPixelColor(color, 56, 4, brightness, millisUntilTurnOff);
            }
            else if (subject.indexOf("Feld") > -1)
            {
                pixelMatrix->setPixelColor(color, 7, 1, brightness, millisUntilTurnOff);
                pixelMatrix->setPixelColor(color, 8, 1, brightness, millisUntilTurnOff);
                pixelMatrix->setPixelColor(color, 23, 1, brightness, millisUntilTurnOff);
                pixelMatrix->setPixelColor(color, 24, 1, brightness, millisUntilTurnOff);
                pixelMatrix->setPixelColor(color, 39, 1, brightness, millisUntilTurnOff);
                pixelMatrix->setPixelColor(color, 40, 1, brightness, millisUntilTurnOff);
                pixelMatrix->setPixelColor(color, 55, 1, brightness, millisUntilTurnOff);
                pixelMatrix->setPixelColor(color, 56, 1, brightness, millisUntilTurnOff);
            }
        }
        else if (pixelMatrix->GetNumberOfLedsPerColumn() == 16 && pixelMatrix->GetNumberOfLedsPerRow() == 16)
        {
            if (subject.indexOf("Dachboden") > -1)
            {
                pixelMatrix->setPixelColor(color, 102, 4, brightness, millisUntilTurnOff);
                pixelMatrix->setPixelColor(color, 118, 4, brightness, millisUntilTurnOff);
                pixelMatrix->setPixelColor(color, 134, 4, brightness, millisUntilTurnOff);
                pixelMatrix->setPixelColor(color, 150, 4, brightness, millisUntilTurnOff);
                pixelMatrix->setPixelColor(color, 166, 4, brightness, millisUntilTurnOff);
                pixelMatrix->setPixelColor(color, 182, 4, brightness, millisUntilTurnOff);
            }
            else if (subject.indexOf("Schuppen") > -1)
            {
                pixelMatrix->setPixelColor(color, 93, 3, brightness, millisUntilTurnOff);
                pixelMatrix->setPixelColor(color, 96, 3, brightness, millisUntilTurnOff);
                pixelMatrix->setPixelColor(color, 125, 3, brightness, millisUntilTurnOff);
                pixelMatrix->setPixelColor(color, 128, 3, brightness, millisUntilTurnOff);
                pixelMatrix->setPixelColor(color, 157, 3, brightness, millisUntilTurnOff);
            }
            else if (subject.indexOf("Vorne") > -1)
            {
                pixelMatrix->setPixelColor(color, 6, 10, brightness, millisUntilTurnOff);
                pixelMatrix->setPixelColor(color, 16, 9, brightness, millisUntilTurnOff);
                pixelMatrix->setPixelColor(color, 40, 8, brightness, millisUntilTurnOff);
            }
            else if (subject.indexOf("Hinten") > -1)
            {
                pixelMatrix->setPixelColor(color, 146, 12, brightness, millisUntilTurnOff);
                pixelMatrix->setPixelColor(color, 162, 12, brightness, millisUntilTurnOff);
                pixelMatrix->setPixelColor(color, 178, 12, brightness, millisUntilTurnOff);
            }
            else if (subject.indexOf("Terrasse") > -1)
            {
                pixelMatrix->setPixelColor(color, 77, 3, brightness, millisUntilTurnOff);
                pixelMatrix->setPixelColor(color, 80, 3, brightness, millisUntilTurnOff);
                pixelMatrix->setPixelColor(color, 109, 3, brightness, millisUntilTurnOff);
                pixelMatrix->setPixelColor(color, 112, 3, brightness, millisUntilTurnOff);
                pixelMatrix->setPixelColor(color, 141, 3, brightness, millisUntilTurnOff);
            }
            else if (subject.indexOf("Norden") > -1)
            {
                pixelMatrix->setPixelColor(color, 0, 4, brightness, millisUntilTurnOff);
                pixelMatrix->setPixelColor(color, 28, 4, brightness, millisUntilTurnOff);
                pixelMatrix->setPixelColor(color, 32, 4, brightness, millisUntilTurnOff);
                pixelMatrix->setPixelColor(color, 60, 4, brightness, millisUntilTurnOff);
                pixelMatrix->setPixelColor(color, 64, 4, brightness, millisUntilTurnOff);
            }
            else if (subject.indexOf("Westen") > -1)
            {
                pixelMatrix->setPixelColor(color, 161, 8, brightness, millisUntilTurnOff);
                pixelMatrix->setPixelColor(color, 183, 8, brightness, millisUntilTurnOff);
                pixelMatrix->setPixelColor(color, 193, 8, brightness, millisUntilTurnOff);
                pixelMatrix->setPixelColor(color, 215, 8, brightness, millisUntilTurnOff);
                pixelMatrix->setPixelColor(color, 225, 8, brightness, millisUntilTurnOff);
            }
            else if (subject.indexOf("Osten") > -1)
            {
                pixelMatrix->setPixelColor(color, 168, 6, brightness, millisUntilTurnOff);
                pixelMatrix->setPixelColor(color, 178, 6, brightness, millisUntilTurnOff);
                pixelMatrix->setPixelColor(color, 200, 6, brightness, millisUntilTurnOff);
                pixelMatrix->setPixelColor(color, 210, 6, brightness, millisUntilTurnOff);
                pixelMatrix->setPixelColor(color, 232, 6, brightness, millisUntilTurnOff);
                pixelMatrix->setPixelColor(color, 242, 6, brightness, millisUntilTurnOff);
            }
            else if (subject.indexOf("Feld") > -1)
            {
                pixelMatrix->setPixelColor(color, 240, 16, brightness, millisUntilTurnOff);
            }
        }
        pixelMatrix->getPixels()->show();
    }
} // namespace IotZoo

#endif // USE_WS2818
#endif // USE_WS2818_PIXEL_MATRIX