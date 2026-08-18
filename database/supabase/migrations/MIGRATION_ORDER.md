# Migration Order - Project 3 Device Platform

## Apply order
1. 20260814_001_platform_initial_schema.up.sql
2. 20260814_002_active_assignment_entities.up.sql
3. 20260815_003_governance_and_audit_immutability.up.sql

## Rollback order
1. 20260815_003_governance_and_audit_immutability.down.sql
2. 20260814_002_active_assignment_entities.down.sql
3. 20260814_001_platform_initial_schema.down.sql

## Notes
- Apply per environment (demo, qa, prod).
- Run with controlled change window in production.
- Keep secrets in secure store, not source files.
