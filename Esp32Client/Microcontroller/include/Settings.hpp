// --------------------------------------------------------------------------------------------------------------------
//      ____    ______   _____
//     /  _/___/_  __/  /__  / ____  ____
//     / // __ \/ /       / / / __ \/ __ \  P L A Y G R O U N D
//   _/ // /_/ / /       / /_/ /_/ / /_/ /
//  /___/\____/_/       /____|____/\____/   (c) 2025 Holger Freudenreich under the MIT licence.
//
// --------------------------------------------------------------------------------------------------------------------
// Save Settings permanently in flash memory.
// --------------------------------------------------------------------------------------------------------------------
#ifndef __SETTINGS_HPP__
#define __SETTINGS_HPP__

// To save data permanently in the flash.
#include <Preferences.h>

namespace IotZoo
{
    /*
    enum Pin
    {
      GPIO_0 = 1 << 0,
      GPIO_1 = 1 << 1,
      GPIO_2 = 1 << 2,
      GPIO_3 = 1 << 3,
      GPIO_4 = 1 << 4,
      GPIO_5 = 1 << 5,
      GPIO_6_SharedWithFlashDoNotUse = 1 << 6,
      GPIO_7_SharedWithFlashDoNotUse = 1 << 7,
      GPIO_8_SharedWithFlashDoNotUse = 1 << 8,
      GPOI_9_SharedWithFlashDoNotUse = 1 << 9,
      GPIO_10_SharedWithFlashDoNotUse = 1 << 10,
      GPIO_11_SharedWithFlashDoNotUse = 1 << 11,
      GPIO_12 = 1 << 12,
      GPIO_13 = 1 << 13,
      GPIO_14 = 1 << 14,
      GPIO_15 = 1 << 15,
      GPIO_16 = 1 << 16,
      GPIO_17 = 1 << 17,
      GPIO_18 = 1 << 18,
      GPIO_19 = 1 << 19,
      GPIO_20 = 1 << 20,
      GPIO_21 = 1 << 21,
      GPIO_22 = 1 << 22,
      GPIO_23 = 1 << 23,
      GPIO_24 = 1 << 24,
      GPIO_25 = 1 << 25,
      GPIO_26 = 1 << 26,
      GPIO_27 = 1 << 27,

      // GPIO_34 = 2 ^ 34, // Input Only
      // GPIO_35 = 2 ^ 35, // Input Only
      // GPIO_36 = 2 ^ 36, // Input Only
      // GPIO_37 = 2 ^ 37, // Input Only
      // GPIO_38 = 2 ^ 38, // Input Only
      // GPIO_39 = 2 ^ 39, // Input Only

      Pin3V = 1 << 29,
      Pin5V = 1 << 30,
      Undefined = 1 << 31
    };

    */

    class Settings
    {
        const String ProjectNameKey      = "ProjectName";
        const char*  NamespaceNameConfig = "config";

      public:
        Settings();

        virtual ~Settings();

        void saveConfigurationData(const String& key, const String& json);
        void saveDeviceConfigurations(const String& json);

        String loadConfiguration(const String& key);
        String loadDeviceConfigurations();

        // gets the interval for sending alive message via mqtt.
        long getAliveIntervalMillis();

        void setAliveIntervalMillis(long interval);

        bool storeData(const String& key, const String& data);

        String getDataString(const String& key, const String& fallbackValue, bool printLog = true);

        bool setMqttBrokerIp(const String& data);

        String getMqttBrokerIp(const String& fallbackValue);

        /// @brief Sets the Namespace Name. This is a prefix of every MQTT Topic. <NamespaceName>/<ProjectName>/
        /// @param namespaceName
        bool setNamespaceName(const String& namespaceName);

        String getNamespaceName(const String& fallbackValue);

        /// @brief Sets the Project Name. Each microcontroller is assigned to exactly one project.
        /// @param projectName
        bool setProjectName(const String& projectName);

        String getProjectName(const String& fallbackValue);

        long getIntervalTemperatureSensorsMillis();

        void setIntervalTemperatureSensorsMillis(long interval);

      protected:
        Preferences preferences;
    };
} // namespace IotZoo

#endif