-- This script migrates the settings to version 3. It adds the setting for the maximum number of received MQTT Explorer topics.
insert into setting(category, setting_key, setting_value) values ('UiSettings', 'MqttExplorerMaxReceivedTopics', '75');

update db_version set version_number = 3;
