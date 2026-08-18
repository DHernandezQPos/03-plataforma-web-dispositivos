-- Project 3 Device Platform - Supabase migration UP
-- Version: 20260814_001

create extension if not exists pgcrypto;

create table if not exists organizations (
    organization_id uuid primary key default gen_random_uuid(),
    code text not null unique,
    name text not null,
    environment text not null,
    created_at_utc timestamptz not null default now()
);

create table if not exists branches (
    branch_id uuid primary key default gen_random_uuid(),
    organization_id uuid not null references organizations(organization_id),
    code text not null,
    name text not null,
    created_at_utc timestamptz not null default now(),
    unique (organization_id, code)
);

create table if not exists registers (
    register_id uuid primary key default gen_random_uuid(),
    branch_id uuid not null references branches(branch_id),
    code text not null,
    name text not null,
    created_at_utc timestamptz not null default now(),
    unique (branch_id, code)
);

create table if not exists devices (
    device_id text primary key,
    merchant_id text not null,
    branch_id text not null,
    register_id text not null,
    environment text not null,
    status text not null,
    updated_at_utc timestamptz not null default now()
);

create table if not exists device_assignments (
    assignment_id uuid primary key default gen_random_uuid(),
    device_id text not null references devices(device_id),
    merchant_id text not null,
    branch_id text not null,
    register_id text not null,
    active boolean not null default true,
    assigned_at_utc timestamptz not null default now()
);

create table if not exists environment_configs (
    config_id uuid primary key default gen_random_uuid(),
    environment text not null,
    config_key text not null,
    config_value jsonb not null,
    version integer not null,
    updated_at_utc timestamptz not null default now(),
    unique (environment, config_key, version)
);

create table if not exists device_config_overrides (
    override_id uuid primary key default gen_random_uuid(),
    device_id text not null references devices(device_id),
    config_key text not null,
    config_value jsonb not null,
    version integer not null,
    updated_at_utc timestamptz not null default now(),
    unique (device_id, config_key, version)
);

create table if not exists user_roles (
    user_role_id uuid primary key default gen_random_uuid(),
    user_id text not null,
    role text not null,
    environment text not null,
    created_at_utc timestamptz not null default now(),
    unique (user_id, role, environment)
);

create table if not exists audit_entries (
    audit_id bigint generated always as identity primary key,
    actor text not null,
    action text not null,
    entity text not null,
    entity_id text not null,
    environment text not null,
    metadata jsonb null,
    utc timestamptz not null default now()
);

create unique index if not exists ux_devices_environment_device
    on devices (environment, device_id);

create index if not exists ix_devices_merchant_status
    on devices (merchant_id, status);

create index if not exists ix_device_assignments_device_active
    on device_assignments (device_id, active);

create index if not exists ix_audit_entries_entity_entityid_utc
    on audit_entries (entity, entity_id, utc desc);
