import Link from 'next/link';
import { AppShell } from '@/components/app-shell';
import { requirePortalSession } from '@/lib/require-session';

export default async function HomePage() {
  const user = await requirePortalSession({ requiredPermissions: ['DASHBOARD.VIEW'] });

  return (
    <AppShell
      initialUser={user}
      pageTitle="Dashboard"
      pageDescription="Frontend Layer shell with role-based navigation and protected pages."
    >
      <div className="grid grid-3">
        <div className="card">
          <h3>Frontend Layer</h3>
          <p className="small">Next.js + React shell, route protection, and role-aware navigation.</p>
        </div>
        <div className="card">
          <h3>UI Service / BFF Layer</h3>
          <p className="small">Thin session-aware routes for auth and profile access.</p>
        </div>
        <div className="card">
          <h3>Core Backend Layer</h3>
          <p className="small">.NET APIs for login, current user, employee profile, and permission data.</p>
        </div>
      </div>

      <div className="card" style={{ marginTop: 16 }}>
        <h3>Signed-In User</h3>
        <p><strong>{user.displayName}</strong> ({user.userName})</p>
        <p className="small">Employee Code: {user.employeeCode}</p>
        <p className="small">Permissions: {user.permissions.join(', ') || '-'}</p>
        <div style={{ display: 'flex', gap: 12, marginTop: 12 }}>
          <Link href="/profile">Open My Profile</Link>
          {user.permissions.includes('ADMIN.ACCESS') ? <Link href="/admin">Open Admin</Link> : null}
        </div>
      </div>
    </AppShell>
  );
}
