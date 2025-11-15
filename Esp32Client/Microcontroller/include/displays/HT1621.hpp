#include "Defines.hpp"
#ifdef USE_HT1621

#ifndef HT1621_HPP
#define HT1621_HPP

#include "DeviceBase.hpp"

#include <HT1621.h>

namespace IotZoo
{
    class HT1621 : public DeviceBase
    {
      public:
        HT1621(int deviceIndex, Settings* const settings, MqttClient* mqttClient, const String& baseTopic, u_int8_t csPin, u_int8_t wrPin,
               u_int8_t dataPin, u_int8_t backlightPin);

        ~HT1621() override;

        /// @brief Let the user know what the device can do.
        /// @param topics
        void addMqttTopicsToRegister(std::vector<Topic>* const topics) const override;

        /// @brief The MQTT connection is established. Now subscribe to the topics. An existing MQTT connection is a
        /// prerequisite for a subscription.
        /// @param mqttClient
        /// @param baseTopic
        void onMqttConnectionEstablished() override;

      protected:
        ::HT1621 lcd; // create an "lcd" object
    };

} // namespace IotZoo

#endif // HT1621_HPP
#endif // USE_HT1621