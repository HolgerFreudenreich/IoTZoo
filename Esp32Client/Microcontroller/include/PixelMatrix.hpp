// --------------------------------------------------------------------------------------------------------------------
//      ____    ______   _____
//     /  _/___/_  __/  /__  / ____  ____
//     / // __ \/ /       / / / __ \/ __ \  P L A Y G R O U N D
//   _/ // /_/ / /       / /_/ /_/ / /_/ /
//  /___/\____/_/       /____|____/\____/   (c) 2025 - 2026 Holger Freudenreich under the MIT licence.
//
// --------------------------------------------------------------------------------------------------------------------
// Connect WS2812, WS2818 Leds with microcontrollers in a simple way.
// --------------------------------------------------------------------------------------------------------------------
// WS2818 Adafruit_NeoPixel arranged as Pixel-Matrix.
// --------------------------------------------------------------------------------------------------------------------
#include "Defines.hpp"

#pragma once

#ifdef USE_WS2818_PIXEL_MATRIX

#include "WS2818.hpp"

namespace IotZoo
{
    enum PixelMatrixExtensions
    {
        None       = 0,
        AlarmZones = 1 // Private extension without a public use case
    };

    class AlarmZonesDeviceExtension;

    class PixelMatrix : public WS2818
    {
      public:
        PixelMatrix(int deviceIndex, Settings* const settings, MqttClient* const mqttClient, const String& baseTopic, int pin,
                    uint numberOfLedsPerColumn, uint numberOfLedsPerRow, PixelMatrixExtensions extensions);

        ~PixelMatrix() override;

        // @brief The MQTT connection is established. Now subscribe to the topics. An existing MQTT connection is a prerequisite
        /// for a subscription.
        /// @param mqttClient
        /// @param baseTopic
        void onMqttConnectionEstablished() override;

        /// @brief Let the user know what the device can do.
        /// @param topics
        void addMqttTopicsToRegister(std::vector<Topic>* const topics) const override;

        uint GetNumberOfLedsPerColumn() const
        {
            return numberOfLedsPerColumn;
        }

        uint GetNumberOfLedsPerRow() const
        {
            return numberOfLedsPerRow;
        }

      private:
        PixelMatrixExtensions      pixelMatrixExtensions     = PixelMatrixExtensions::None;
        AlarmZonesDeviceExtension* alarmZonesDeviceExtension = nullptr;
        uint                       numberOfLedsPerColumn     = 0;
        uint                       numberOfLedsPerRow        = 0;
    };    
}

#endif // USE_WS2818_PIXEL_MATRIX