-- Project 3 Device Platform - Supabase migration DOWN
-- Version: 20260814_001

drop table if exists audit_entries;
drop table if exists user_roles;
drop table if exists device_config_overrides;
drop table if exists environment_configs;
drop table if exists device_assignments;
drop table if exists devices;
drop table if exists registers;
drop table if exists branches;
drop table if exists organizations;
