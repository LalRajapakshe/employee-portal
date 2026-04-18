import { SalaryAdvancePrintView } from '@/components/salary-advance-print-view';
import { requirePortalSession } from '@/lib/require-session';
import { PERMISSIONS } from '@/config/permissions';

export default async function SalaryAdvancePrintPage({ params }: { params: Promise<{ requestId: string }> }) {
  await requirePortalSession({ requiredAnyPermissions: [PERMISSIONS.SALARY_ADVANCE_VIEW_SELF, PERMISSIONS.SALARY_ADVANCE_VIEW_ALL, PERMISSIONS.SALARY_ADVANCE_APPROVE_STAGE1, PERMISSIONS.SALARY_ADVANCE_APPROVE_STAGE2] });
  const { requestId } = await params;
  return <SalaryAdvancePrintView requestId={requestId} />;
}
