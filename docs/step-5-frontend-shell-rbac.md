# Step 5 - Frontend Shell + Role-Based Access Integration

This step strengthens the Frontend Layer while keeping the Application Architecture unchanged:

- Frontend Layer
- UI Service / BFF Layer
- Core Backend Layer
- Data and Integration Layer

## What was added

### Frontend Layer
- Portal shell with sidebar navigation
- Role-aware and permission-aware navigation filtering
- Protected dashboard page
- Protected profile page
- Admin placeholder page protected by `ADMIN.ACCESS`
- Unauthorized page
- Login redirect support
- Session-based route protection helper

### UI Service / BFF Layer
- Existing auth/profile routes continue to be used
- Session cookie now drives page access consistently

## Current access model
- Unauthenticated users are redirected to `/login`
- Authenticated users without required permissions are redirected to `/unauthorized`
- Navigation only displays links the current user can access

## Demo behavior
- `demo.user` can access Dashboard and My Profile
- `hr.admin` can access Dashboard, My Profile, and Admin
