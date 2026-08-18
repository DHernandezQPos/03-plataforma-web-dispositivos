# GO / NO-GO Checklist - Project 3 Device Platform (WEB-018)

## Environment
- [ ] demo
- [ ] qa
- [ ] prod

## Security gates
- [ ] OIDC login and role claims validated.
- [ ] MFA mandatory for admin sensitive operations.
- [ ] RBAC and environment scope checks validated (no IDOR cross-environment access).
- [ ] CSRF/XSS mitigations validated in web forms and config payload handling.
- [ ] Sensitive fields masked in API responses and audit metadata.

## Governance gates
- [ ] Double approval validated for critical changes: publish, rollback, override.
- [ ] Approval by same requester is blocked.
- [ ] Audit entries generated for sensitive operations.
- [ ] Audit immutability trigger applied in database migration 20260815_003.

## Functional gates
- [ ] Device ABM flow validated.
- [ ] CSV import validates row-level errors without stopping full batch.
- [ ] Assignment validates active organization/branch/register target.
- [ ] Config templates support versioning and rollback.
- [ ] Device overrides resolve effective config correctly.
- [ ] Operational dashboard shows consistent counts.
- [ ] Device detail shows assignments, effective config, sessions/transactions signals.
- [ ] Async export job start/status/download validated.

## QA / performance gates
- [x] `dotnet test src/C2C.DevicePlatform.slnx` green in CI-compatible environment (validated 2026-08-18).
- [ ] k6 performance run completed (`tests/performance/k6-device-platform.js`).
- [ ] p95 under target for list/dashboard filters.

## Operational readiness
- [ ] Migration order applied and verified in target environment.
- [ ] Connection and secret configuration validated from secure store.
- [x] Dependency advisory NU1903 remediated in codebase (Microsoft.OpenApi 2.7.5, 2026-08-18).

## Decision
- [ ] GO
- [ ] NO-GO
- Decision owner:
- Decision date (UTC):
- Notes:
