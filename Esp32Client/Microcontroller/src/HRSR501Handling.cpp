// --------------------------------------------------------------------------------------------------------------------
//      ____    ______   _____
//     /  _/___/_  __/  /__  / ____  ____
//     / // __ \/ /       / / / __ \/ __ \  P L A Y G R O U N D
//   _/ // /_/ / /       / /_/ /_/ / /_/ /
//  /___/\____/_/       /____|____/\____/   (c) 2025 Holger Freudenreich under the MIT licence.
//
// --------------------------------------------------------------------------------------------------------------------
// Connect HC SR501 Motion detectors with microcontrollers in a simple way.
// --------------------------------------------------------------------------------------------------------------------
#include "Defines.hpp"
#ifdef USE_HC_SR501

#include "HRSR501Handling.hpp"

namespace IotZoo
{
    HRSR501Handling::HRSR501Handling()
    {
        Serial.println("Constructor HRSR501Handling");
    }

    HRSR501Handling::~HRSR501Handling()
    {
        Serial.println("Destructor HRSR501Handling");
    }

    void HRSR501Handling::addDevice(int deviceIndex, Settings* const settings, MqttClient* mqttClient, const String& baseTopic, uint8_t pin1)
    {
        HRSR501Helper::motionSensors.emplace_back(deviceIndex, settings, mqttClient, baseTopic, pin1);
    }

    /// @brief Let the user know what the device can do.
    /// @param topics
    void HRSR501Handling::addMqttTopicsToRegister(std::vector<Topic>* const topics) const
    {
        for (auto& motionSensor : HRSR501Helper::motionSensors)
        {
            motionSensor.addMqttTopicsToRegister(topics);
        }
    }

    void HRSR501Handling::loop()
    {
        // Triggered?
        for (auto& motionSensor : HRSR501Helper::motionSensors)
        {
            motionSensor.loop();
        }
    }
} // namespace IotZoo

#endif // USE_HC_SR501