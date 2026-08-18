# Supabase Schema Scripts

## Purpose
These scripts initialize the device platform schema on Supabase PostgreSQL.

## Script order
1. `migrations/20260814_001_platform_initial_schema.up.sql`
2. `migrations/20260814_002_active_assignment_entities.up.sql`
3. `migrations/20260815_003_governance_and_audit_immutability.up.sql`

## Rollback
1. `migrations/20260815_003_governance_and_audit_immutability.down.sql`
2. `migrations/20260814_002_active_assignment_entities.down.sql`
3. `migrations/20260814_001_platform_initial_schema.down.sql`

## Notes
- The API requires `ConnectionStrings:Supabase`.
- Apply migrations independently per environment (demo, qa, prod).
- Keep database credentials outside source control.
