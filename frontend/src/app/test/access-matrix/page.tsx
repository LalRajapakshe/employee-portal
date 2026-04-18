import { AppShell } from '@/components/app-shell';
import { AccessMatrix } from '@/components/access-matrix';
import { requirePortalSession } from '@/lib/require-session';
import { PERMISSIONS } from '@/config/permissions';

export default async function AccessMatrixPage() {
  const user = await requirePortalSession({ requiredPermissions: [PERMISSIONS.DASHBOARD_VIEW] });

  return (
    <AppShell
      initialUser={user}
      pageTitle="Role-Based Access Matrix"
      pageDescription="End-to-end route access evaluation based on current roles and permissions."
    >
      <AccessMatrix user={user} />
    </AppShell>
  );
}
