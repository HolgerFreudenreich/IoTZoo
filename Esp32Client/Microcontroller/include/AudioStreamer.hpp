// --------------------------------------------------------------------------------------------------------------------
//      ____    ______   _____
//     /  _/___/_  __/  /__  / ____  ____
//     / // __ \/ /       / / / __ \/ __ \
//   _/ // /_/ / /       / /_/ /_/ / /_/ /
//  /___/\____/_/       /____|____/\____/ (c) 2025 Holger Freudenreich under the MIT licence.
// --------------------------------------------------------------------------------------------------------------------
// Connect a INMP441 microphone with a microcontrollers in a simple way.
// --------------------------------------------------------------------------------------------------------------------
#include "Defines.hpp"
#ifdef USE_AUDIO_STREAMER

#include "DeviceBase.hpp"

#include <Arduino.h>
#include <driver/i2s.h>

namespace IotZoo
{
// I2S Pins (Default, können durch Konstruktor überschrieben werden)
#define I2S_WS 22  // LRCK/WS
#define I2S_SCK 15 // BCLK
#define I2S_SD 35  // DATA_IN

#define SAMPLE_RATE 16000
#define CHUNK_SIZE (int)(SAMPLE_RATE * 0.5) // memory is rare! more than 0.8 is not possible

    enum AudioStreamerFeatures
    {
        Undefined  = 0,
        Streaming  = 1,
        SoundLevel = 2
    };

    class AudioStreamer : public DeviceBase
    {
      public:
        AudioStreamer(int deviceIndex, Settings* const settings, MqttClient* const mqttClient, const String& baseTopic, u8_t features, u16_t minRms,
                      uint8_t pinSd = I2S_SD, uint8_t pinWs = I2S_WS, uint8_t pinSck = I2S_SCK);

        void loop();

        void addMqttTopicsToRegister(std::vector<Topic>* const topics) const;

        void onMqttConnectionEstablished();

      private:
        i2s_config_t i2sConfig = {
            .mode                 = (i2s_mode_t)(I2S_MODE_MASTER | I2S_MODE_RX),
            .sample_rate          = 16000,
            .bits_per_sample      = I2S_BITS_PER_SAMPLE_16BIT, // Why ever does I2S_BITS_PER_SAMPLE_32BIT not work in PlatformIO, only in Arduino IDE.
            .channel_format       = I2S_CHANNEL_FMT_ONLY_LEFT,
            .communication_format = I2S_COMM_FORMAT_STAND_I2S,
            .intr_alloc_flags     = ESP_INTR_FLAG_LEVEL1,
            .dma_buf_count        = 4,
            .dma_buf_len          = 256,
            .use_apll             = false,
        };

        size_t           bufferIndex = 0;
        i2s_pin_config_t pinConfig;
        int32_t          i2sBuffer[CHUNK_SIZE];
        int16_t          pcm16Buffer[CHUNK_SIZE];
        int16_t          chunkBuffer[CHUNK_SIZE];
        u8_t             features;
        u16_t            minRms;
    };

} // namespace IotZoo

#endif // USE_AUDIO_STREAMER
