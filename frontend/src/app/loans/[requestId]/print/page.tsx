import { AppShell } from '@/components/app-shell';
import { LoanPrintView } from '@/components/loan-print-view';
import { requirePortalSession } from '@/lib/require-session';

export default async function LoanPrintPage({ params }: { params: Promise<{ requestId: string }> }) {
  const user = await requirePortalSession({ requiredPermissions: ['LOAN.VIEW_SELF'] });
  const { requestId } = await params;

  return (
    <AppShell initialUser={user} pageTitle="Loan Print Form" pageDescription="Print-ready employee loan request and repayment schedule.">
      <LoanPrintView requestId={requestId} />
    </AppShell>
  );
}
