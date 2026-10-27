// --------------------------------------------------------------------------------------------------------------------
//      ____    ______   _____
//     /  _/___/_  __/  /__  / ____  ____
//     / // __ \/ /       / / / __ \/ __ \ 
//   _/ // /_/ / /       / /_/ /_/ / /_/ /
//  /___/\____/_/       /____|____/\____/ (c) 2025 Holger Freudenreich under the MIT licence.
// --------------------------------------------------------------------------------------------------------------------
// Connect «Things» with microcontrollers in a simple way.
// --------------------------------------------------------------------------------------------------------------------

#ifndef __CONNECTION_SETTINGS_HPP__
#define __CONNECTION_SETTINGS_HPP__

#include <Arduino.h>

// --------------------------------------------------------------------------------------------------------------------
// TO DO: Replace with your values!
// --------------------------------------------------------------------------------------------------------------------

// WIFI
const char *ssid = "Wokwi-GUEST"; // WIFI SSID. If you use WOKWI -> "Wokwi-GUEST"
const char *password = "";        // WIFI password If you use WOKWI no password is required -> ""

// MQTT Broker
const String MqttBrokerIpFallback = "pi5"; // IP of the MQTT Broker.

// URL prefix for each MQTT Topic. 
const String ProjectNameFallback = "example1";

#endif // __CONNECTION_SETTINGS_HPP__