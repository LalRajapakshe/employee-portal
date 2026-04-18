import { AppShell } from '@/components/app-shell';
import { SalaryAdvanceHome } from '@/components/salary-advance-home';
import { requirePortalSession } from '@/lib/require-session';
import { PERMISSIONS } from '@/config/permissions';

export default async function SalaryAdvancePage() {
  const user = await requirePortalSession({ requiredPermissions: [PERMISSIONS.SALARY_ADVANCE_VIEW_SELF] });

  return (
    <AppShell initialUser={user} pageTitle="Salary Advance" pageDescription="Create, save, submit, track, and print salary advance requests.">
      <SalaryAdvanceHome />
    </AppShell>
  );
}
