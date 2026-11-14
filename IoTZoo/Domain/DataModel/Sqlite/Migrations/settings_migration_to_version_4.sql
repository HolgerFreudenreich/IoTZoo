insert or ignore into setting(category, setting_key, setting_value) values ('Mail', 'MailSenderConfig', '{}');

update db_version set version_number = 4;
