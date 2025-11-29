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
#ifdef USE_MAX7219

#ifndef MAX_7219_HPP
#define MAX_7219_HPP

#include "DeviceBase.hpp"

#include <MD_MAX72xx.h>

namespace IotZoo
{
    class Max7219 : public DeviceBase
    {
      public:
        Max7219(int deviceIndex, Settings* const settings, MqttClient* mqttClient, const String& baseTopic, u_int8_t numberOfDevices,
                u_int8_t dataPin, u_int8_t clkPin, u_int8_t csPin);

        /// @brief Let the user know what the device can do.
        /// @param topics
        void addMqttTopicsToRegister(std::vector<Topic>* const topics) const override;

        /// @brief The MQTT connection is established. Now subscribe to the topics. An existing MQTT connection is a
        /// prerequisite for a subscription.
        /// @param mqttClient
        /// @param baseTopic
        void onMqttConnectionEstablished() override;

      protected:
        MD_MAX72XX* max7219;
    };

} // namespace IotZoo

#endif // MAX_7219_HPP
#endif // USE_MAX7219

/*

// ----------------------------
// Deine Pins
// ----------------------------
#define DATA_PIN  27   // MOSI
#define CLK_PIN   25   // SCK

#define CS_PIN    26   // SS / LOAD

// ----------------------------
// Hardwaretyp – falls falsch -> Pixel verdreht/spiegelverkehrt
// GENERIC_HW funktioniert bei den meisten Noname-Modulen
// ----------------------------
#define HARDWARE_TYPE MD_MAX72XX::GENERIC_HW
#define NUM_MODULES   1

// Software-SPI Konstruktor
MD_MAX72XX mx(HARDWARE_TYPE, DATA_PIN, CLK_PIN, CS_PIN, NUM_MODULES);

void setup()
{
  mx.begin();
  mx.clear();
  delay(500);

  // ----------------------------
  // Test 1: Alle LEDs EIN
  // ----------------------------
  mx.control(MD_MAX72XX::INTENSITY, 10);   // Helligkeit
  mx.clear();
  for (uint8_t row = 0; row < 8; row++) {
    for (uint8_t col = 0; col < 8; col++) {
      mx.setPoint(row, col, true);
    }
  }
  delay(1500);

  // ----------------------------
  // Test 2: Alle LEDs AUS
  // ----------------------------
  mx.clear();
  delay(1000);

  // ----------------------------
  // Test 3: Einfache Pixel-Testsequenz
  // ----------------------------
  for (uint8_t i = 0; i < 8; i++) {
    mx.setPoint(i, i, true);  // Diagonale
    delay(200);
  }
}

void loop()
{
  // nichts hier — Tests laufen nur einmal
}

*/