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
#include "Settings.hpp"
#endif

#ifndef __DEFINES_HPP__
#include "Defines.hpp"
#endif

namespace IotZoo
{
    Settings::Settings()
    {
        Serial.println("Constructor Settings");

        if (!preferences.begin(NamespaceNameConfig)) // create namespace config if it does not exist yet.
        {
            Serial.println("Settings failure!");
        }
        preferences.end();
    }

    Settings::~Settings()
    {
        Serial.println("Destructor Settings");
    }

    void Settings::saveConfiguration(const String& json)
    {
        if (json.length() == 0)
        {
            return;
        }
        preferences.begin(NamespaceNameConfig, false);
        int bytesWritten = preferences.putString("devices", json);
        Serial.println("Bytes:" + String(bytesWritten));
        preferences.end();
    }

    String Settings::loadConfiguration()
    {
        Serial.println("load configuration");
        String json;

        preferences.begin(NamespaceNameConfig, true);
        json = preferences.getString("devices");

        preferences.end();

        return json;
    }

    // gets the interval for sending alive message via mqtt.
    long Settings::getAliveIntervalMillis()
    {
        preferences.begin(NamespaceNameConfig, true);
        long interval = preferences.getLong("interval_alive", 15000);
        preferences.end();
        return interval;
    }

    void Settings::setAliveIntervalMillis(long interval)
    {
        preferences.begin(NamespaceNameConfig, false);
        preferences.putLong("interval_alive", interval);
        preferences.end();
    }

    bool Settings::storeData(const String& name, const String& data)
    {
        try
        {
            Serial.println("storeData to '" + name + "' data: '" + data + "'");
            int size = 0;
            if (preferences.begin(NamespaceNameConfig, false))
            {
                size = preferences.putString(name.c_str(), data);
                preferences.end();
            }
            return size == data.length();
        }
        catch (const std::exception& e)
        {
            Serial.println(e.what()); // Exception handling does only work with build_flags -DPIO_FRAMEWORK_ARDUINO_ENABLE_EXCEPTIONS
            return false;
        }
    }

    String Settings::getDataString(const String& key, const String& fallbackValue, bool printLog /*= true*/)
    {
        if (printLog)
        {
            Serial.println("getData '" + key + "', fallback is '" + fallbackValue + "'");
        }

        String data;
        try
        {
            if (preferences.begin(NamespaceNameConfig, true))
            {
                data = preferences.getString(key.c_str(),
                                             fallbackValue); // fallback
            }
            preferences.end();
            if (data == "null")
            {
                Serial.println("Using fallback '" + fallbackValue + "'!");
                return fallbackValue;
            }
        }
        catch (const std::exception& e)
        {
            Serial.println(e.what()); // Exception handling does only work with build_flags -DPIO_FRAMEWORK_ARDUINO_ENABLE_EXCEPTIONS
            return fallbackValue;
        }
        if (data.length() == 0)
        {
            Serial.println("Using fallback '" + fallbackValue + "'!");
            return fallbackValue;
        }
        if (printLog)
        {
            Serial.println(key + ": " + data);
        }
        return data;
    }

    bool Settings::setMqttBrokerIp(const String& data)
    {
        return storeData("MqttBrokerIp", data);
    }

    String Settings::getMqttBrokerIp(const String& fallbackValue)
    {
        return getDataString("MqttBrokerIp", fallbackValue);
    }

    /// @brief Sets the Namespace Name. This is a prefix of every MQTT Topic. <NamespaceName>/<ProjectName>/
    /// @param namespaceName
    bool Settings::setNamespaceName(const String& namespaceName)
    {
        return storeData("NamespaceName", namespaceName);
    }

    String Settings::getNamespaceName(const String& fallbackValue)
    {
        return getDataString("NamespaceName", fallbackValue, false);
    }

    /// @brief Sets the Project Name. Each microcontroller is assigned to exactly one project.
    /// @param projectName
    bool Settings::setProjectName(const String& projectName)
    {
        return storeData("ProjectName", projectName);
    }

    String Settings::getProjectName(const String& fallbackValue)
    {
        return getDataString("ProjectName", fallbackValue, false);
    }

    long Settings::getIntervalTemperatureSensorsMillis()
    {
        preferences.begin(NamespaceNameConfig, true);
        long interval = preferences.getLong("interval_temperature_sensors", 30000);
        preferences.end();
        return interval;
    }

    void Settings::setIntervalTemperatureSensorsMillis(long interval)
    {
        preferences.begin(NamespaceNameConfig, false);
        preferences.putLong("interval_temperature_sensors", interval);
        preferences.end();
    }
} // namespace IotZoo