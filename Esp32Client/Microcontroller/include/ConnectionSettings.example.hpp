// --------------------------------------------------------------------------------------------------------------------
// Local connection settings template.
//
// Copy this file to ConnectionSettings.hpp and set the values for your device.
// ConnectionSettings.hpp is ignored by Git and must never be committed.
// --------------------------------------------------------------------------------------------------------------------

#ifndef __CONNECTION_SETTINGS_HPP__
#define __CONNECTION_SETTINGS_HPP__

#include <Arduino.h>

// WIFI
const char *ssid = "your-wifi-ssid";
const char *password = "your-wifi-password";

// MQTT Broker
const String MqttBrokerIpFallback = "your-mqtt-broker";

// URL prefix for each MQTT Topic.
const String ProjectNameFallback = "example1";

#endif // __CONNECTION_SETTINGS_HPP__
