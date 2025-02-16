// --------------------------------------------------------------------------------------------------------------------
//      ____    ______   _____
//     /  _/___/_  __/  /__  / ____  ____
//     / // __ \/ /       / / / __ \/ __ \  P L A Y G R O U N D
//   _/ // /_/ / /       / /_/ /_/ / /_/ /
//  /___/\____/_/       /____|____/\____/   (c) 2025 Holger Freudenreich under the MIT licence.
//
// --------------------------------------------------------------------------------------------------------------------
// Connecting 1..64 temperature Sensors DS18B20
// --------------------------------------------------------------------------------------------------------------------
#include "Defines.hpp"
#ifdef USE_DS18B20
#include "DS18B20.hpp"

namespace IotZoo
{
    DS18B20::DS18B20()
    {
        Serial.println("Constructor DS18B20");
    }

    DS18B20::~DS18B20()
    {
        Serial.println("Destructor DS18B20");
        delete dallasTemperatureSensors;
        dallasTemperatureSensors = NULL;
        delete oneWire;
        oneWire = NULL;
    }

    void DS18B20::printDeviceAddress(DeviceAddress deviceAddress)
    {
        for (uint8_t i = 0; i < 8; i++)
        {
            if (deviceAddress[i] < 16)
            {
                Serial.print("0");
            }
            Serial.print(deviceAddress[i], HEX);
        }
        Serial.println();
    }

    std::list<float> DS18B20::requestTemperatures()
    {
        std::list<float> list;
        dallasTemperatureSensors->requestTemperatures();
        for (int i = 0; i < numberOfDevices; i++)
        {
            list.push_back(dallasTemperatureSensors->getTempCByIndex(i));
        }
        return list;
    }

    /// @brief
    /// @param gpioNumber GPIO where the DS18B20 is connected to.
    /// @param resolution resolution of a device to 9, 10, 11, or 12 bits.
    /// @param transmissionIntervalMs Interval at which the temperatures are sent via MQTT.
    void DS18B20::setup(int gpioNumber, u_int8_t resolution, int transmissionIntervalMs)
    {
        // Setup a oneWire instance to communicate with any OneWire devices
        oneWire = new OneWire(gpioNumber);
        Serial.println("Expecting DS18B20 sensors on GPIO " + String(gpioNumber));
        // Pass our oneWire reference to Dallas Temperature sensor
        dallasTemperatureSensors = new DallasTemperature(oneWire);

        dallasTemperatureSensors->setResolution(resolution);
        setInterval(transmissionIntervalMs);

        Serial.println("Start the DS18B20 sensor!");
        // Start the DS18B20 sensor
        dallasTemperatureSensors->begin();
        delay(200);
        numberOfDevices = dallasTemperatureSensors->getDeviceCount();
        Serial.print("Found ");
        Serial.print(numberOfDevices, DEC);
        Serial.println(" temperature sensors.");

        // Loop through each device, print out address
        for (int i = 0; i < numberOfDevices; i++)
        {
            // We'll use this variable to store a found device address
            DeviceAddress tempDeviceAddress;

            // Search the wire for address
            if (dallasTemperatureSensors->getAddress(tempDeviceAddress, i))
            {
                Serial.print("Found DS18B20 temperature sensor ");
                Serial.print(i, DEC);
                Serial.print(" with address: ");
                printDeviceAddress(tempDeviceAddress);
            }
            else
            {
                Serial.print("Found ghost device at ");
                Serial.print(i, DEC);
                Serial.print(" but could not detect address. Check power and cabling");
            }
        }
    }
} // namespace IotZoo

#endif // USE_DS18B20