# Step 6 - BFF Route Hardening and Employee Profile End-to-End Test Path

## What changed

### BFF hardening
- Introduced a shared `callCoreApi` helper for consistent communication with the Core Backend Layer.
- Added timeout handling for core API requests.
- Standardized JSON parsing and upstream error handling.
- Added local session cookie clearing on authentication/authorization failures from core APIs.
- Centralized portal session cookie apply/clear helpers.

### Employee profile end-to-end test path
- Added protected route: `/test/profile-flow`
- Added diagnostic BFF route: `/api/test/profile-flow`
- The test path validates:
  1. Core backend health
  2. Current user resolution through `/api/auth/me`
  3. Employee profile read through `/api/employees/me`
  4. Employee code consistency between current user and employee profile

## Manual test sequence
1. Start the Core Backend Layer.
2. Start the Frontend Layer.
3. Log in as `demo.user` or `hr.admin`.
4. Open `/test/profile-flow`.
5. Confirm all checks return PASS.

## Notes
- The test route is intended for Sprint 1 validation and can be removed or restricted further later.
- The BFF remains thin and does not own business logic.
