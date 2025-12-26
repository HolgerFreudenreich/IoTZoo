// --------------------------------------------------------------------------------------------------------------------
//      ____    ______   _____
//     /  _/___/_  __/  /__  / ____  ____
//     / // __ \/ /       / / / __ \/ __ \  P L A Y G R O U N D
//   _/ // /_/ / /       / /_/ /_/ / /_/ /
//  /___/\____/_/       /____|____/\____/   (c) 2025 - 2026 Holger Freudenreich under the MIT licence.
//
// --------------------------------------------------------------------------------------------------------------------
// Firmware for ESP8266 and ESP32 Microcontrollers
// --------------------------------------------------------------------------------------------------------------------
#include "Defines.hpp"
#ifdef USE_BLE_HEART_RATE_SENSOR
#ifndef __BLE_HEARTRATERECEIVER_HPP__
#include "BLEHeartRateReceiver.hpp"
#endif

namespace IotZoo
{
  HeartRateMonitor::HeartRateMonitor()
  {
    charUUID = (NimBLEUUID((uint16_t)0x2A37));
  }

  bool HeartRateMonitor::connectToServer(NimBLEAddress pAddress)
  {
    Serial.print("Forming a connection to ");
    Serial.println(pAddress.toString().c_str());

    NimBLEClient *pClient = NimBLEDevice::createClient();
    Serial.println(" - Created client");

    // Connect to the remove BLE Server.
    pClient->connect(pAddress);
    Serial.println(" - Connected to server");

    // Obtain a reference to the service we are after in the remote BLE server.
    NimBLERemoteService *pRemoteService = pClient->getService(serviceUUID);
    if (pRemoteService == nullptr)
    {
      Serial.print("Failed to find our service UUID: ");
      Serial.println(serviceUUID.toString().c_str());
      return false;
    }
    Serial.println(" - Found our service");

    // Obtain a reference to the characteristic in the service of the remote BLE server.
    pRemoteCharacteristic = pRemoteService->getCharacteristic(charUUID);
    if (pRemoteCharacteristic == nullptr)
    {
      Serial.print("Failed to find our characteristic UUID: ");
      Serial.println(charUUID.toString().c_str());
      return false;
    }

    Serial.println(" - Found our characteristic");

    pRemoteCharacteristic->subscribe(true, notifyCallback);

    // pRemoteCharacteristic->registerForNotify(notifyCallback);

    return true;
  }

  void HeartRateMonitor::setup(notify_callback callbackMethod, int scanDuration /*= 60*/)
  {
    Serial.println("Starting BLE heart rate monitor. Scanning for sensor.");
    NimBLEDevice::init("");

    notifyCallback = callbackMethod;

    // Retrieve a Scanner and set the callback we want to use to be informed when we
    // have detected a new device.  Specify that we want active scanning and start the
    // scan to run for 30 seconds.
    NimBLEScan *pBLEScan = NimBLEDevice::getScan();
    pBLEScan->setAdvertisedDeviceCallbacks(new AdvertisedDeviceCallbacks());
    pBLEScan->setActiveScan(true);
    Serial.println("BLE scan started");
    NimBLEScanResults results = pBLEScan->start(scanDuration);
    Serial.println("BLE scan done. Found: " + String(results.getCount() + " BLE devices."));
  }

  void HeartRateMonitor::loop()
  {
    // Serial.println("*** Heart Rate Loop started ***");

    // If the flag "doConnect" is true then we have scanned for and found the desired
    // BLE Server with which we wish to connect. Now we connect to it.  Once we are
    // connected we set the connected flag to be true.
    if (doConnect == true)
    {
      if (connectToServer(*pServerAddress))
      {
        Serial.println("We are now connected to the BLE Server.");
        connected = true;
        doConnect = false;
      }
      else
      {
        Serial.println("We have failed to connect to the server;");
        doConnect = true;
      }
    }

    const uint8_t notificationOff[] = {0x0, 0x0};
    const uint8_t notificationOn[] = {0x1, 0x0};

    // If we are connected to a peer BLE Server, update the characteristic each time we are reached
    // with the current time since boot.
    if (connected)
    {
      if (onoff)
      {
        // Serial.println("Notifications turned on");
        pRemoteCharacteristic->getDescriptor(BLEUUID((uint16_t)0x2902))->writeValue((uint8_t *)notificationOn, 2, true);
      }
      else
      {
        // Serial.println("Notifications turned off");
        pRemoteCharacteristic->getDescriptor(BLEUUID((uint16_t)0x2902))->writeValue((uint8_t *)notificationOff, 2, true);
      }

      onoff = onoff ? 0 : 1;
    }    
  }

  // Initialize static members.
  NimBLEUUID HeartRateMonitor::serviceUUID = (NimBLEUUID((uint16_t)0x180D));
  bool HeartRateMonitor::connected = false;
  bool HeartRateMonitor::doConnect = false;
  NimBLEAddress *HeartRateMonitor::pServerAddress = nullptr;
  NimBLERemoteCharacteristic *HeartRateMonitor::pRemoteCharacteristic = nullptr;
}

#endif // USE_BLE_HEART_RATE_SENSOR