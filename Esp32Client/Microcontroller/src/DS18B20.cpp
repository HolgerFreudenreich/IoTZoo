// --------------------------------------------------------------------------------------------------------------------
//      ____    ______   _____
//     /  _/___/_  __/  /__  / ____  ____
//     / // __ \/ /       / / / __ \/ __ \  P L A Y G R O U N D
//   _/ // /_/ / /       / /_/ /_/ / /_/ /
//  /___/\____/_/       /____|____/\____/   (c) 2025 - 2026 Holger Freudenreich under the MIT licence.
//
// --------------------------------------------------------------------------------------------------------------------
// Connecting 1..64 temperature Sensors DS18B20
// --------------------------------------------------------------------------------------------------------------------
#include "Defines.hpp"
#ifdef USE_DS18B20
#include "DS18B20.hpp"

namespace IotZoo
{
    DS18B20::DS18B20(int deviceIndex, Settings* const settings, MqttClient* const mqttClient, const String& baseTopic, uint8_t pinData,
                     u_int8_t resolution, int transmissionIntervalMs)
        : DeviceBase(deviceIndex, settings, mqttClient, baseTopic)
    {
        Serial.println("Constructor DS18B20");
        pinMode(pinData, OUTPUT);
        // Setup a oneWire instance to communicate with any OneWire devices
        oneWire = new OneWire(pinData);
        Serial.println("Expecting DS18B20 sensors on GPIO " + String(pinData));

        // Pass our oneWire reference to Dallas Temperature sensor
        dallasTemperatureSensors = new DallasTemperature(oneWire);

        dallasTemperatureSensors->setResolution(resolution);
        setInterval(transmissionIntervalMs);

        Serial.println("Start the DS18B20 sensor!");
        // Start the DS18B20 sensor
        dallasTemperatureSensors->begin();
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

    DS18B20::~DS18B20()
    {
        Serial.println("Destructor DS18B20");
        delete dallasTemperatureSensors;
        dallasTemperatureSensors = nullptr;
        delete oneWire;
        oneWire = nullptr;
    }

    /// @brief Let the user know what the device can do.
    /// @param topics
    void DS18B20::addMqttTopicsToRegister(std::vector<Topic>* const topics) const
    {
        Serial.println("Register temperature sensors.");
        // std::list<float> temperatures = ds18B20SensorManager->requestTemperatures();
        int index = 0;
        // for (auto &temperature : temperatures)
        for (int index = 0; index < 10; index++)
        {
            topics->emplace_back(getBaseTopic() + "/ds18b20_manager/0/sensor/" + String(index) + "/celsius",
                                 "The Temperature in °C of Sensor " + String(index), MessageDirection::IotZooClientInbound);
        }
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

    std::vector<float> DS18B20::requestTemperatures()
    {
        std::vector<float>           temperatures;
        DallasTemperature::request_t request = dallasTemperatureSensors->requestTemperatures();

        if (request.result)
        {
            delay(250); // Do not remove this delay!
            for (int i = 0; i < numberOfDevices; i++)
            {
                float temperatureCelsius = dallasTemperatureSensors->getTempCByIndex(i);
                temperatures.push_back(temperatureCelsius);
            }
        }
        else
        {
            Serial.println("DS18B20 is not working.");
        }
        return temperatures;
    }

    void DS18B20::loop()
    {

        if (millis() - getLastPublishedTemperatureMillis() < getInterval())
        {
            return;
        }

        setLastPublishedTemperatureMillis(millis());

        std::vector<float> temperatures = requestTemperatures();
        Serial.println("Count of Temperature sensors: " + String(temperatures.size()));
        int indexTemperatureSensor = 0;

        for (const auto& temperatureCelsius : temperatures)
        {
            String topic = getBaseTopic() + "/ds18b20_manager/0/sensor/" + String(indexTemperatureSensor) + "/celsius";

            Serial.print(topic + "/" + String(temperatureCelsius));
            Serial.println(" ºC");
            if (temperatureCelsius != DEVICE_DISCONNECTED_C)
            {
                mqttClient->publish(topic, String(temperatureCelsius, 1));
            }
            else
            {
                mqttClient->publish(topic, "device is not ready");
            }
            indexTemperatureSensor++;
        }
    }
} // namespace IotZoo

#endif // USE_DS18B20