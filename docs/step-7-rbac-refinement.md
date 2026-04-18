# Step 7 - Role-Based Access End-to-End Refinement

This step tightens RBAC across the Frontend Layer, UI Service / BFF Layer, and Core Backend Layer.

## Main improvements

- Centralized permission constants in `frontend/src/config/permissions.ts`
- Centralized route definitions in `frontend/src/config/routes.ts`
- Navigation now derives from route definitions
- Access checks normalize role and permission codes before comparison
- Middleware protects route prefixes instead of only exact paths
- Middleware redirects authenticated users away from `/login`
- `requirePortalSession` redirects unauthorized users to the first allowed route instead of always sending them to `/unauthorized`
- Session cookie payload is sanitized before being trusted
- Added `/test/access-matrix` page to verify route-level access end to end
- Core Backend now normalizes roles and permissions before returning current user data

## Manual verification

1. Sign in as `demo.user`
2. Confirm Dashboard, My Profile, Profile Flow Test, and Access Matrix are visible
3. Confirm Admin is not visible
4. Open `/admin` directly and confirm the app redirects away from it
5. Open `/test/access-matrix` and verify route decisions match the current user's permissions
6. Sign in as `hr.admin`
7. Confirm Admin is visible and accessible
