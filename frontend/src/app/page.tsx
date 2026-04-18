import Link from 'next/link';
import { AppShell } from '@/components/app-shell';
import { NotificationsPanel } from '@/components/notifications-panel';
import { requirePortalSession } from '@/lib/require-session';
import { PERMISSIONS } from '@/config/permissions';

export default async function HomePage() {
  const user = await requirePortalSession({ requiredPermissions: [PERMISSIONS.DASHBOARD_VIEW] });

  return (
    <AppShell
      initialUser={user}
      pageTitle="Dashboard"
      pageDescription="Sprint 2 introduces the shared workflow engine, notifications, print support, and the Salary Advance module."
    >
      <div className="grid grid-3">
        <div className="card">
          <h3>Salary Advance</h3>
          <p className="small">Create drafts, submit requests, and track payroll handoff readiness.</p>
          <Link href="/salary-advance">Open module</Link>
        </div>
        <div className="card">
          <h3>Approvals Inbox</h3>
          <p className="small">First and second approval actions are routed by the reusable workflow engine.</p>
          <Link href="/approvals">Open inbox</Link>
        </div>
        <div className="card">
          <h3>Diagnostics</h3>
          <p className="small">Use the Sprint 1 test pages to confirm session, RBAC, and request correlation are still healthy.</p>
          <div style={{ display: 'grid', gap: 8 }}>
            <Link href="/test/profile-flow">Profile Flow Test</Link>
            <Link href="/test/request-context">Request Context Test</Link>
          </div>
        </div>
      </div>

      <div className="grid grid-2" style={{ marginTop: 16 }}>
        <div className="card">
          <h3>Signed-In User</h3>
          <p><strong>{user.displayName}</strong> ({user.userName})</p>
          <p className="small">Employee Code: {user.employeeCode}</p>
          <p className="small">Roles: {user.roles.join(', ') || '-'}</p>
          <p className="small">Permissions: {user.permissions.join(', ') || '-'}</p>
          <div style={{ display: 'flex', gap: 12, marginTop: 12, flexWrap: 'wrap' }}>
            <Link href="/profile">Open My Profile</Link>
            <Link href="/salary-advance">Salary Advances</Link>
            {user.permissions.includes(PERMISSIONS.APPROVAL_INBOX_VIEW) ? <Link href="/approvals">Approvals</Link> : null}
          </div>
        </div>
        <NotificationsPanel />
      </div>
    </AppShell>
  );
}
