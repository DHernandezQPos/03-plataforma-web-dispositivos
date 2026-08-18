-- Project 3 Device Platform - Supabase migration DOWN
-- Version: 20260814_002

drop index if exists ix_registers_branch_code_active;
drop index if exists ix_branches_organization_code_active;
drop index if exists ix_organizations_environment_code_active;

alter table registers
    drop column if exists is_active;

alter table branches
    drop column if exists is_active;

alter table organizations
    drop column if exists is_active;
