import { AppShell } from '@/components/app-shell';
import { RequestContextCheck } from '@/components/request-context-check';
import { requirePortalSession } from '@/lib/require-session';
import { PERMISSIONS } from '@/config/permissions';

export default async function RequestContextPage() {
  const user = await requirePortalSession({ requiredPermissions: [PERMISSIONS.DASHBOARD_VIEW] });

  return (
    <AppShell
      initialUser={user}
      pageTitle="Request Context Test"
      pageDescription="Protected diagnostics page to verify correlation IDs and backend request context capture."
    >
      <RequestContextCheck />
    </AppShell>
  );
}
