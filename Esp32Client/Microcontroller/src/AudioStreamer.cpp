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
#include "AudioStreamer.hpp"

namespace IotZoo
{
    AudioStreamer::AudioStreamer(int deviceIndex, Settings* const settings, MqttClient* const mqttClient, const String& baseTopic, u8_t features,
                                 u16_t minRms, uint8_t pinSd, uint8_t pinWs, uint8_t pinSck)
        : DeviceBase(deviceIndex, settings, mqttClient, baseTopic), minRms(minRms), features(features)
    {
        Serial.println("Constructor AudioStreamer features: " + String(features) + ", minRms: " + String(minRms) + ", pinSd: " + String(pinSd) +
                       ", pinWs: " + String(pinWs) + ", pinSck: " + String(pinSck));
        pinConfig = {
            .mck_io_num = I2S_PIN_NO_CHANGE, .bck_io_num = I2S_SCK, .ws_io_num = I2S_WS, .data_out_num = I2S_PIN_NO_CHANGE, .data_in_num = I2S_SD};

        memset((void*)i2sBuffer, 0, sizeof(i2sBuffer));
        memset(pcm16Buffer, 0, sizeof(pcm16Buffer));

        i2s_driver_install(I2S_NUM_0, &i2sConfig, 0, nullptr);
        Serial.println("i2s_driver_install ok");
        i2s_set_pin(I2S_NUM_0, &pinConfig);
        Serial.println("i2s_set_pin ok");
        i2s_zero_dma_buffer(I2S_NUM_0);
        Serial.println("Constructor AudioStreamer ok");
    }

    void AudioStreamer::loop()
    {
        size_t bytesRead = 0;
        i2s_read(I2S_NUM_0, i2sBuffer, sizeof(i2sBuffer), &bytesRead, portMAX_DELAY);
        int sampleCount = bytesRead / sizeof(int32_t);

        for (int i = 0; i < sampleCount; i++)
        {
            int16_t pcm    = (int16_t)(i2sBuffer[i] >> 8); // 24->16 bit
            pcm16Buffer[i] = pcm;
        }

        // Collect Chunks.
        for (int i = 0; i < sampleCount; i++)
        {
            chunkBuffer[bufferIndex++] = pcm16Buffer[i];
            if (bufferIndex >= CHUNK_SIZE)
            {
                // Check RMS.
                double sumSq = 0;
                for (int j = 0; j < CHUNK_SIZE; j++)
                {
                    sumSq += chunkBuffer[j] * chunkBuffer[j];
                }
                double rms    = sqrt(sumSq / CHUNK_SIZE);
                String strRms = String(rms, 0);
                Serial.println("RMS: " + strRms);
                if (rms >= minRms)
                {
                    /* wird schlechter
                        // optional normalize
                        double gain = TARGET_RMS / rms;
                        for (int j = 0; j < CHUNK_SIZE; j++)
                        {
                            int32_t v      = (int32_t)(chunkBuffer[j] * gain);
                            chunkBuffer[j] = (int16_t)max(min(v, 32767), -32768);
                        }
                            */
                    if (features & AudioStreamerFeatures::Streaming)
                    {
                        mqttClient->publish("iotzoo/picea/esp32/1C:69:20:EA:3C:E8/audio_stream/0/pcm", (uint8_t*)chunkBuffer,
                                            CHUNK_SIZE * sizeof(int16_t), false);
                    }
                    if (features & AudioStreamerFeatures::SoundLevel)
                    {
                        mqttClient->publish("iotzoo/picea/esp32/1C:69:20:EA:3C:E8/audio_stream/0/sound_level_rms", strRms);
                    }
                }
                bufferIndex = 0; // collect next chunk.
            }
        }
    }

    void AudioStreamer::addMqttTopicsToRegister(std::vector<Topic>* const topics) const
    {
        if (features & AudioStreamerFeatures::Streaming)
        {
            topics->emplace_back(baseTopic + "/audio_stream/0/pcm", "pcm stream", MessageDirection::IotZooClientInbound);
        }
        if (features & AudioStreamerFeatures::SoundLevel)
        {
            topics->emplace_back(baseTopic + "/audio_stream/0/sound_level_rms", "value between " + String(minRms) + "(quiet) and 32767 (extrem loud)",
                                 MessageDirection::IotZooClientInbound);
        }
    }

    void AudioStreamer::onMqttConnectionEstablished()
    {
        Serial.println("AudioStreamer::onMqttConnectionEstablished");
        if (mqttCallbacksAreRegistered)
        {
            Serial.println("Reconnection -> nothing to do.");
            return;
        }
    }

} // namespace IotZoo
#endif // USE_AUDIO_STREAMER
