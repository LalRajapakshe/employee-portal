import { AppShell } from '@/components/app-shell';
import { ApprovalInbox } from '@/components/approval-inbox';
import { requirePortalSession } from '@/lib/require-session';
import { PERMISSIONS } from '@/config/permissions';

export default async function ApprovalsPage() {
  const user = await requirePortalSession({ requiredPermissions: [PERMISSIONS.APPROVAL_INBOX_VIEW] });
  return (
    <AppShell initialUser={user} pageTitle="Approvals Inbox" pageDescription="Pending Salary Advance approvals routed by the shared workflow engine.">
      <ApprovalInbox />
    </AppShell>
  );
}
