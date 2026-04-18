# Step 8 - Audit/Error Logging Baseline + Test Readiness

This step adds correlation-aware logging and a protected diagnostics path so Sprint 1 can be validated before moving into business modules.

## What changed

### Core Backend Layer
- Added `RequestCorrelationMiddleware` to accept or generate `X-Correlation-Id` and return it on every response.
- Extended auth/profile audit logging with:
  - source layer
  - correlation ID
  - request path
  - status code
  - IP address
  - user-agent
- Extended exception/error logging with:
  - user name
  - request path
  - IP address
  - user-agent
  - status code
- Added `GET /api/diagnostics/request-context`.

### UI Service / BFF Layer
- All main auth/profile routes now generate or forward a correlation ID.
- Core API failures surface a correlation ID when available.
- Added `/api/test/request-context`.

### Frontend Layer
- Added protected page `/test/request-context`.
- Profile flow test now displays correlation IDs for each step.

## Apply order
1. Run SQL patch `database/01-baseline/004_extend_audit_error_logs.sql`
2. Replace updated backend files
3. Add new middleware file
4. Add new frontend diagnostic files
5. Replace updated frontend routes and helpers
6. Start backend and frontend
7. Test login, profile flow, and request context diagnostics

## Suggested smoke tests
- Login as `demo.user`
- Open `/test/profile-flow`
- Open `/test/request-context`
- Confirm correlation IDs are shown
- Confirm `portal.AuditLogs` rows are being created for login/current-user/profile calls
- Trigger one backend exception manually and confirm `portal.ErrorLogs` is written
