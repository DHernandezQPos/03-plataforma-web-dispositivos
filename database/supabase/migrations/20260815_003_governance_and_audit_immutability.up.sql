-- Project 3 Device Platform - Supabase migration UP
-- Version: 20260815_003

create table if not exists change_approvals (
    approval_id uuid primary key default gen_random_uuid(),
    action_type text not null,
    environment text not null,
    resource_key text not null,
    payload_json text not null,
    payload_hash text not null,
    requested_by text not null,
    approved_by text null,
    status text not null,
    created_at_utc timestamptz not null default now(),
    updated_at_utc timestamptz not null default now(),
    constraint chk_change_approvals_status
        check (status in ('pending', 'approved', 'rejected'))
);

create index if not exists ix_change_approvals_lookup
    on change_approvals (action_type, environment, resource_key, payload_hash, status, created_at_utc desc);

create or replace function prevent_audit_entries_mutation()
returns trigger
language plpgsql
as $$
begin
    raise exception 'audit_entries is immutable and cannot be modified';
end;
$$;

drop trigger if exists trg_prevent_audit_entries_mutation on audit_entries;

create trigger trg_prevent_audit_entries_mutation
before update or delete on audit_entries
for each row
execute function prevent_audit_entries_mutation();
