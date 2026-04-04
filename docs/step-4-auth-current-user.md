# Step 4 - Core Backend Layer hardening + auth/current-user flow

## What this step adds
- Core Backend Layer exception handling middleware
- Core Backend Layer login, logout, and current-user endpoints
- Demo-first authentication path backed by appsettings, with a clear repository abstraction for Portal DB-backed auth later
- Audit and error log service stubs
- UI Service / BFF Layer session cookie handling
- Frontend login and current-user screens
- Profile route updated to read the signed-in user from the BFF session cookie

## Core backend endpoints
- `POST /api/auth/login`
- `POST /api/auth/logout`
- `GET /api/auth/me`
- `GET /api/employees/me`
- `GET /health`

## Demo accounts
These are configured in `backend/src/EmployeePortal.Api/appsettings.json`.

1. `demo.user / Password@123`
2. `hr.admin / Password@123`

## Portal DB-backed auth later
When the Portal DB connection string is supplied in `PortalAuth:ConnectionString`, the repository reads from:
- `portal.PortalUsers`
- `portal.EmployeePortalProfile`
- `portal.UserRoleMappings`
- `portal.Roles`
- `portal.RolePermissionMappings`
- `portal.Permissions`

## SQL seed
Run `database/01-baseline/003_seed_demo_portal_users.sql` after the baseline scripts if you want demo users in SQL Server too.
