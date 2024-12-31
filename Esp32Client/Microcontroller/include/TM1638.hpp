// --------------------------------------------------------------------------------------------------------------------
//      ____    ______   _____
//     /  _/___/_  __/  /__  / ____  ____
//     / // __ \/ /       / / / __ \/ __ \  P L A Y G R O U N D
//   _/ // /_/ / /       / /_/ /_/ / /_/ /
//  /___/\____/_/       /____|____/\____/   (c) 2025 Holger Freudenreich under the MIT licence.
//
// --------------------------------------------------------------------------------------------------------------------
// Connect TM1638 8 digit display 'LED&KEY' with microcontrollers in a simple way.
// --------------------------------------------------------------------------------------------------------------------
#include "Defines.hpp"
#ifdef USE_LED_AND_KEY

#ifndef __TM1638__HPP__
#define __TM1638__HPP__

#include "DeviceBase.hpp"
#include "MqttClient.hpp"
#include <ArduinoJson.h>
#include <TM1638plus.h>

namespace IotZoo
{
    class TM1638 : public DeviceBase
    {
    public:
        TM1638(int deviceIndex, MqttClient *const mqttClient, const String &baseTopic,
               uint8_t strobe, uint8_t clock, uint8_t data, bool highfreq = true);

        virtual ~TM1638();

        void setServerDownText(const String &serverDownText);

        const String &getServerDownText() const;

        /// @brief Let the user know what the device can do.
        /// @param topics
        virtual void addMqttTopicsToRegister(std::vector<Topic> *const topics) const override;

        /// @brief The MQTT connection is established. Now subscribe to the topics. An existing MQTT connection is a prerequisite for a subscription.
        /// @param mqttClient
        /// @param baseTopic
        virtual void onMqttConnectionEstablished() override;

        /// @brief The IotZooMqtt client is not available, so tell this this user. Providing false information is worse than not providing any information.
        ///        This method is a suitable point to erase a display or stop something.
        virtual void onIotZooClientUnavailable() override;

        void loop() override;

    protected:
        void doLeds(uint8_t value);

    protected:
        TM1638plus *tm1638plus;
        int8_t lastButtonsState;
        String serverDownText = "--------";
    };
}

#endif // __TM1638__HPP__
#endif // USE_LED_AND_KEY