# Performance Test Assets (WEB-017)

## Goal
Validate list/filter/dashboard behavior and protect the p95 target of less than 2 seconds.

## Tooling
- k6 script: `k6-device-platform.js`
- Inputs via env vars:
  - `API_BASE_URL`
  - `ACCESS_TOKEN` (OIDC JWT with support read policy)

## Example command
```bash
k6 run tests/performance/k6-device-platform.js -e API_BASE_URL=https://localhost:7279 -e ACCESS_TOKEN=<token>
```

## Expected thresholds
- `http_req_failed < 1%`
- `http_req_duration p95 < 2000ms`

## Scope
- `GET /api/devices`
- `GET /api/devices/dashboard/{environment}`

## Notes
- Run independently per environment (`demo`, `qa`, `prod`).
- Keep test token in secure secret storage.
