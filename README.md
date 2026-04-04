# Employee Portal - Sprint 1 Foundation

This repository contains the Sprint 1 foundation for the Employee Portal application.

## Application Architecture
- Frontend Layer: Next.js + React + TypeScript
- UI Service / BFF Layer: Next.js API routes
- Core Backend Layer: C# (.NET) Web API
- Data and Integration Layer: Microsoft SQL Server + Payroll read integration

## Current scope in this starter
- Project/repository structure
- Frontend foundation scaffold
- Backend solution folder scaffold
- Portal database baseline scripts
- Payroll integration read-view placeholders
- Environment template files

## Repository structure
- `frontend/` - Next.js frontend + BFF starter
- `backend/` - .NET solution skeleton
- `database/` - MSSQL scripts
- `docs/` - sprint and setup notes

## Important note
The execution environment used to prepare this starter does not include the .NET SDK.
The backend files are scaffolded and ready to open on a development machine with the .NET SDK installed.

## Suggested next steps
1. Open `database/01-baseline/001_create_portal_baseline.sql`
2. Review and align schema names, DB names, and naming conventions
3. Confirm Payroll source objects for employee master access
4. Run frontend install and startup in the `frontend` folder
5. Open `backend/` in Visual Studio / Rider / VS Code and complete the API implementation


## Step 3 added
- Payroll employee master read SQL contract under `database/02-payroll-integration/003_create_payroll_read_contract.sql`
- Core Backend employee profile service and repository stub
- BFF route at `frontend/src/app/api/profile/me/route.ts`
- Profile page at `frontend/src/app/profile/page.tsx`

### Demo flow
1. Start the Core Backend Layer on `http://localhost:5000`.
2. Start the Frontend Layer.
3. Open `/profile`.
4. The BFF forwards to the Core Backend with `X-Portal-User`.

When `PayrollRead:ConnectionString` is empty, the backend returns a fallback demo profile so the slice can be tested before live Payroll connectivity is finalized.

## Step 4 status
- Auth login/current-user flow scaffold added
- Demo-first authentication is available through backend appsettings
- Next.js BFF now stores a `portal_session` cookie after successful login
- Profile calls use the signed-in BFF session instead of a fixed environment user



