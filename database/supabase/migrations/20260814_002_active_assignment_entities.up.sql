-- Project 3 Device Platform - Supabase migration UP
-- Version: 20260814_002

alter table organizations
    add column if not exists is_active boolean not null default true;

alter table branches
    add column if not exists is_active boolean not null default true;

alter table registers
    add column if not exists is_active boolean not null default true;

create index if not exists ix_organizations_environment_code_active
    on organizations (environment, code, is_active);

create index if not exists ix_branches_organization_code_active
    on branches (organization_id, code, is_active);

create index if not exists ix_registers_branch_code_active
    on registers (branch_id, code, is_active);
