import { AppShell } from '@/components/app-shell';
import { NotificationsPanel } from '@/components/notifications-panel';
import { requirePortalSession } from '@/lib/require-session';
import { PERMISSIONS } from '@/config/permissions';

export default async function NotificationsPage() {
  const user = await requirePortalSession({ requiredPermissions: [PERMISSIONS.NOTIFICATION_VIEW] });
  return (
    <AppShell initialUser={user} pageTitle="Notifications" pageDescription="Workflow and request lifecycle notifications for Sprint 2.">
      <NotificationsPanel />
    </AppShell>
  );
}
