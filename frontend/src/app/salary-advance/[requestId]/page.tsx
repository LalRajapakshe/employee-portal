import { AppShell } from '@/components/app-shell';
import { SalaryAdvanceDetailPage } from '@/components/salary-advance-detail-page';
import { requirePortalSession } from '@/lib/require-session';
import { PERMISSIONS } from '@/config/permissions';

export default async function SalaryAdvanceDetailPageRoute({ params }: { params: Promise<{ requestId: string }> }) {
  const user = await requirePortalSession({ requiredAnyPermissions: [PERMISSIONS.SALARY_ADVANCE_VIEW_SELF, PERMISSIONS.SALARY_ADVANCE_VIEW_ALL, PERMISSIONS.SALARY_ADVANCE_APPROVE_STAGE1, PERMISSIONS.SALARY_ADVANCE_APPROVE_STAGE2] });
  const { requestId } = await params;

  return (
    <AppShell initialUser={user} pageTitle="Salary Advance Detail" pageDescription="Request details, workflow history, and approval actions.">
      <SalaryAdvanceDetailPage user={user} requestId={requestId} />
    </AppShell>
  );
}
