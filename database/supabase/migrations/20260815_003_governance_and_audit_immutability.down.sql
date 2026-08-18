-- Project 3 Device Platform - Supabase migration DOWN
-- Version: 20260815_003

drop trigger if exists trg_prevent_audit_entries_mutation on audit_entries;
drop function if exists prevent_audit_entries_mutation();

drop index if exists ix_change_approvals_lookup;
drop table if exists change_approvals;
