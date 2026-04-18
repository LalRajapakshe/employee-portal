import { AppShell } from '@/components/app-shell';
import { ProfileFlowCheck } from '@/components/profile-flow-check';
import { requirePortalSession } from '@/lib/require-session';

export default async function ProfileFlowPage() {
  const user = await requirePortalSession({ requiredPermissions: ['PROFILE.VIEW'] });

  return (
    <AppShell
      initialUser={user}
      pageTitle="Profile Flow Test"
      pageDescription="Protected end-to-end path to verify BFF routing, current-user resolution, and Payroll employee profile read access."
    >
      <ProfileFlowCheck />
    </AppShell>
  );
}
