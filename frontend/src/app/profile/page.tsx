import { AppShell } from '@/components/app-shell';
import { ProfileContent } from '@/components/profile-content';
import { requirePortalSession } from '@/lib/require-session';
import { PERMISSIONS } from '@/config/permissions';

export default async function ProfilePage() {
  const user = await requirePortalSession({ requiredPermissions: [PERMISSIONS.PROFILE_VIEW] });

  return (
    <AppShell
      initialUser={user}
      pageTitle="My Profile"
      pageDescription="Read-only employee data sourced through the Payroll read integration contract."
    >
      <ProfileContent />
    </AppShell>
  );
}
