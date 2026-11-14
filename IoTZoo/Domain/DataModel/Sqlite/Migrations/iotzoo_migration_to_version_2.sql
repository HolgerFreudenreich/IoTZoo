-- This script migrates the IoTZoo database to version 2. It adds the font size to the dashboard items.
alter table dashboard_item add column font_size integer not null default 14;

update db_version set version_number = 2;
