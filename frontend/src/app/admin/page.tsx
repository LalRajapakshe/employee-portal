import { AppShell } from '@/components/app-shell';
import { requirePortalSession } from '@/lib/require-session';
import { PERMISSIONS } from '@/config/permissions';

export default async function AdminPage() {
  const user = await requirePortalSession({ requiredPermissions: [PERMISSIONS.ADMIN_ACCESS] });

  return (
    <AppShell
      initialUser={user}
      pageTitle="Admin Area"
      pageDescription="Role-based access sample page for HR admin and system admin users."
    >
      <div className="grid grid-2">
        <div className="card">
          <h3>Admin Access Confirmed</h3>
          <p className="small">
            This placeholder page proves the Frontend Layer respects permissions coming from the Core Backend Layer.
          </p>
        </div>
        <div className="card">
          <h3>Access Source</h3>
          <p className="small">Permission required: {PERMISSIONS.ADMIN_ACCESS}</p>
          <p className="small">Resolved roles: {user.roles.join(', ') || '-'}</p>
        </div>
      </div>
    </AppShell>
  );
}
