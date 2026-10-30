-- This script migrates the settings to version 2. It updates the MqttBrokerSettings to use the internal MQTT broker with default values.
update setting set setting_value = '{"UseInternalMqttBroker":true,"Ip":"","Port":1883,"User":null,"Password":null}' where category = 'MqttBrokerSettings' and setting_key = 'MqttBrokerSettings';

update setting set setting_value = 'true' where category = 'UiSettings' and setting_key = 'IsDarkMode';

update db_version set version_number = 2;
