import { AppShell } from '@/components/app-shell';
import { LoanDetailPage } from '@/components/loan-detail-page';
import { requirePortalSession } from '@/lib/require-session';

export default async function LoanRequestPage({ params }: { params: Promise<{ requestId: string }> }) {
  const user = await requirePortalSession({ requiredPermissions: ['LOAN.VIEW_SELF'] });
  const { requestId } = await params;

  return (
    <AppShell initialUser={user} pageTitle="Loan Request" pageDescription="View request details, schedule, workflow history, and submit the request.">
      <LoanDetailPage requestId={requestId} />
    </AppShell>
  );
}
