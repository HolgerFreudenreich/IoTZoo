// --------------------------------------------------------------------------------------------------------------------
//      ____    ______   _____
//     /  _/___/_  __/  /__  / ____  ____
//     / // __ \/ /       / / / __ \/ __ \  P L A Y G R O U N D
//   _/ // /_/ / /       / /_/ /_/ / /_/ /
//  /___/\____/_/       /____|____/\____/   (c) 2025 Holger Freudenreich under the MIT licence.
//
// --------------------------------------------------------------------------------------------------------------------
// Connect WS2812, WS2818 Leds with microcontrollers in a simple way.
// --------------------------------------------------------------------------------------------------------------------
// WS2818 Adafruit_NeoPixel arranged as Pixel-Matrix.
// --------------------------------------------------------------------------------------------------------------------
#include "PixelMatrix.hpp"
#include "AlarmZonesDeviceExtension.hpp"

#ifdef USE_WS2818_PIXEL_MATRIX

namespace IotZoo
{
    PixelMatrix::PixelMatrix(int deviceIndex, Settings* const settings, MqttClient* const mqttClient, const String& baseTopic, int pin,
                             uint numberOfLedsPerColumn, uint numberOfLedsPerRow, PixelMatrixExtensions extensions)
        : WS2818(deviceIndex, settings, mqttClient, baseTopic, pin, numberOfLedsPerColumn * numberOfLedsPerRow)
    {
        Serial.println("Constructor PixelMatrix");
        this->numberOfLedsPerColumn = numberOfLedsPerColumn;
        this->numberOfLedsPerRow    = numberOfLedsPerRow;
        deviceName                  = "pixel_matrix";
        pixelMatrixExtensions       = extensions;
        if (pixelMatrixExtensions == PixelMatrixExtensions::AlarmZones)
        {
            alarmZonesDeviceExtension = new AlarmZonesDeviceExtension(this);
        }
    }

    PixelMatrix::~PixelMatrix()
    {
    }

    // @brief The MQTT connection is established. Now subscribe to the topics. An existing MQTT connection is a prerequisite
    /// for a subscription.
    /// @param mqttClient
    /// @param baseTopic
    void PixelMatrix::onMqttConnectionEstablished()
    {
        Serial.println("PixelMatrix::onMqttConnectionEstablished");

        WS2818::onMqttConnectionEstablished();

        if (pixelMatrixExtensions == PixelMatrixExtensions::AlarmZones && nullptr != alarmZonesDeviceExtension)
        {
            alarmZonesDeviceExtension->onMqttConnectionEstablished();
        }
    }

    void PixelMatrix::addMqttTopicsToRegister(std::vector<Topic>* const topics) const
    {
        WS2818::addMqttTopicsToRegister(topics);
        if (pixelMatrixExtensions == PixelMatrixExtensions::AlarmZones && nullptr != alarmZonesDeviceExtension)
        {
            alarmZonesDeviceExtension->addMqttTopicsToRegister(topics);
        }
    }
} // namespace IotZoo
#endif // USE_WS2818_PIXEL_MATRIX