import { AppShell } from '@/components/app-shell';
import { SalaryAdvanceForm } from '@/components/salary-advance-form';
import { requirePortalSession } from '@/lib/require-session';
import { PERMISSIONS } from '@/config/permissions';

export default async function NewSalaryAdvancePage({ searchParams }: { searchParams: Promise<{ requestId?: string | string[] }> }) {
  const user = await requirePortalSession({ requiredPermissions: [PERMISSIONS.SALARY_ADVANCE_CREATE] });
  const resolved = await searchParams;
  const requestId = Array.isArray(resolved.requestId) ? resolved.requestId[0] : resolved.requestId;

  return (
    <AppShell initialUser={user} pageTitle={requestId ? 'Edit Salary Advance Draft' : 'New Salary Advance'} pageDescription="Start the request as draft, then submit it into the shared workflow engine.">
      <SalaryAdvanceForm requestId={requestId} />
    </AppShell>
  );
}
