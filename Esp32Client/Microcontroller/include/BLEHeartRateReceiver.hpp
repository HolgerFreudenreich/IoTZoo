// --------------------------------------------------------------------------------------------------------------------
//      ____    ______   _____
//     /  _/___/_  __/  /__  / ____  ____
//     / // __ \/ /       / / / __ \/ __ \  P L A Y G R O U N D
//   _/ // /_/ / /       / /_/ /_/ / /_/ /
//  /___/\____/_/       /____|____/\____/   (c) 2025 Holger Freudenreich under the MIT licence.
//
// --------------------------------------------------------------------------------------------------------------------
// Connect BlueTooth HeartRateMonitor.
// --------------------------------------------------------------------------------------------------------------------
#include "Defines.hpp"
#ifdef USE_BLE_HEART_RATE_SENSOR
#ifndef __BLE_HEARTRATERECEIVER_HPP__
#define __BLE_HEARTRATERECEIVER_HPP__

// ------------------------------------------------------------------------------------------------
// Receive BLE signals of a Heart Rate Sensor.

// Based on https://github.com/h2zero/NimBLE-Arduino/tree/release/1.4/examples/NimBLE_Client
// ------------------------------------------------------------------------------------------------

#include <NimBLEDevice.h>

namespace IotZoo
{
  class HeartRateMonitor
  {

  protected:
    // The remote service we wish to connect to.
    static NimBLEUUID serviceUUID;
    // The characteristic of the remote service we are interested in.
    NimBLEUUID charUUID;

    static NimBLEAddress *pServerAddress;
    static bool doConnect;
    static bool connected;
    static NimBLERemoteCharacteristic *pRemoteCharacteristic;

    bool onoff = true;

    notify_callback notifyCallback;

  public:
    HeartRateMonitor();

    virtual ~HeartRateMonitor()
    {
      Serial.println("Deleting HeartRateMonitor.");
    }

    bool connectToServer(NimBLEAddress pAddress);

    /// @brief Scan for BLE servers and find the first one that advertises the service we are looking for.
    class AdvertisedDeviceCallbacks : public NimBLEAdvertisedDeviceCallbacks
    {
      // Called for each advertising BLE server.
      void onResult(NimBLEAdvertisedDevice *advertisedDevice)
      {
        Serial.println("BLE Advertised Device found with serviceUUID: " + String(advertisedDevice->getServiceUUID().toString().c_str()));

        // We have found a device, let us now see if it contains the service we are looking for.
        if (advertisedDevice->haveServiceUUID() && advertisedDevice->getServiceUUID().equals(serviceUUID))
        {
          Serial.println("Found our device! Stopping scan for heart rate device.");
          advertisedDevice->getScan()->stop();
          Serial.println("Scan stopped!");
          pServerAddress = new NimBLEAddress(advertisedDevice->getAddress());

          doConnect = true;
        }
      }
    };

    /// @brief
    /// @param callbackMethod
    /// @param scanDuration
    /// @return true, if the device is found, otherwise false
    void setup(notify_callback callbackMethod, int scanDuration = 60);

    void loop();
  };
}
#endif // __BLE_HEARTRATERECEIVER_HPP__
#endif // USE_BLE_HEART_RATE_SENSOR