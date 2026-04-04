# Step 3 - Payroll employee master read integration

## Objective
Provide a stable, read-only contract from Payroll to the Employee Portal for employee profile and later rule evaluation.

## Recommended source of truth
Payroll remains the source of truth for:
- Employee code
- Full name
- Department and designation
- Join date
- Employment status
- Permanent flag
- Official email
- Reporting manager / director references

## Integration pattern
Use the agreed Application Architecture:
- Frontend Layer
- UI Service / BFF Layer
- Core Backend Layer
- Data and Integration Layer

### Flow
1. Frontend requests `/api/profile/me`.
2. UI Service / BFF Layer forwards to Core Backend Layer.
3. Core Backend Layer reads Payroll through `portal_payroll` views/procedures.
4. Core Backend Layer maps payroll fields into portal DTOs.
5. BFF returns a UI-safe response.

## Important rules
- No direct Payroll table access from the browser.
- No direct Payroll table access from Next.js API routes.
- No write access to Payroll source tables from the profile feature.
- Grant the portal application only the minimum read permissions required.

## Fields currently included
- EmployeeCode
- FullName
- DepartmentName
- DesignationName
- JoinDate
- EmploymentStatus
- IsPermanent
- OfficialEmail
- ReportingManagerEmployeeCode
- DirectorEmployeeCode
- IsActive

## Next steps after this slice
- Auth/current-user linkage
- Role-based profile access checks
- Approver lookup support
- Salary advance and loan eligibility reuse
