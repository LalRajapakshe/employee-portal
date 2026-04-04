import { AppShell } from '@/components/app-shell';
import { requirePortalSession } from '@/lib/require-session';

export default async function AdminPage() {
  const user = await requirePortalSession({ requiredPermissions: ['ADMIN.ACCESS'] });

  return (
    <AppShell
      initialUser={user}
      pageTitle="Admin Area"
      pageDescription="Role-based access sample page for HR admin and system admin users."
    >
      <div className="card">
        <h3>Admin Access Confirmed</h3>
        <p className="small">
          This placeholder page proves the Frontend Layer respects permissions coming from the Core Backend Layer.
        </p>
      </div>
    </AppShell>
  );
}
