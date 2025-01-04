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
#ifdef USE_BLE_HEART_RATE_SENSOR

#include "BLEHeartRateSensor.hpp"

namespace IotZoo
{
  HeartRateSensor::HeartRateSensor(int deviceIndex, MqttClient *const mqttClient, const String &baseTopic,
                                   uint8_t advertisingTimeOut) : DeviceBase(deviceIndex, mqttClient, baseTopic)
  {
    Serial.println("Constructor HeartRateSensor. advertisingTimeOut: " + String(advertisingTimeOut) + " s");
    charUUID = (NimBLEUUID((uint16_t)0x2A37));
  }

  HeartRateSensor::~HeartRateSensor()
  {
    Serial.println("Deleting BLE HeartRateSensor.");
  }

  /// @brief Let the user know what the device can do.
  /// @param topics
  void HeartRateSensor::addMqttTopicsToRegister(std::vector<Topic> *const topics) const
  {
    topics->push_back(*new Topic(getBaseTopic() + "/pulse/0",
                                 "Heart rate of BLE Heart-Rate-Sensor 0.",
                                 MessageDirection::IotZooClientInbound));
  }

  bool HeartRateSensor::connectToServer(NimBLEAddress pAddress)
  {
    Serial.print("Establishing a BLE connection to ");
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
      Serial.print("Failed to find the service UUID: ");
      Serial.println(serviceUUID.toString().c_str());
      return false;
    }
    Serial.println(" - Found the service.");

    // Obtain a reference to the characteristic in the service of the remote BLE server.
    pRemoteCharacteristic = pRemoteService->getCharacteristic(charUUID);
    if (pRemoteCharacteristic == nullptr)
    {
      Serial.print("Failed to find the characteristic UUID: ");
      Serial.println(charUUID.toString().c_str());
      return false;
    }

    Serial.println(" - Found the characteristic!");

    pRemoteCharacteristic->subscribe(true, notifyCallback);

    // pRemoteCharacteristic->registerForNotify(notifyCallback);

    return true;
  }

  void HeartRateSensor::setup(notify_callback callbackMethod, int scanDuration /*= 60*/)
  {
    Serial.println("Starting BLE heart rate monitor. Scanning for sensor.");
    NimBLEDevice::init("");

    notifyCallback = callbackMethod;

    // Retrieve a Scanner and set the callback we want to use to be informed when we
    // have detected a new device.  Specify that we want active scanning and start the
    // scan to run.
    NimBLEScan *pBLEScan = NimBLEDevice::getScan();
    pBLEScan->setAdvertisedDeviceCallbacks(new AdvertisedDeviceCallbacks());
    pBLEScan->setActiveScan(true);
    Serial.println("BLE scan started");
    NimBLEScanResults results = pBLEScan->start(scanDuration);
    Serial.println("BLE scan done. Found: " + String(results.getCount() + " BLE devices."));
  }

  void HeartRateSensor::loop()
  {
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
  NimBLEUUID HeartRateSensor::serviceUUID = (NimBLEUUID((uint16_t)0x180D));
  bool HeartRateSensor::connected = false;
  bool HeartRateSensor::doConnect = false;
  NimBLEAddress *HeartRateSensor::pServerAddress = NULL;
  NimBLERemoteCharacteristic *HeartRateSensor::pRemoteCharacteristic = NULL;
}

#endif // USE_BLE_HEART_RATE_SENSOR