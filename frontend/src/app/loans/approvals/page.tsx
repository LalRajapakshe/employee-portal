import { AppShell } from '@/components/app-shell';
import { LoanApprovalInbox } from '@/components/loan-approval-inbox';
import { requirePortalSession } from '@/lib/require-session';

export default async function LoanApprovalsPage() {
  const user = await requirePortalSession();

  return (
    <AppShell
      initialUser={user}
      pageTitle="Loan Approvals"
      pageDescription="Director and HR approval queue for employee loans."
    >
      <LoanApprovalInbox />
    </AppShell>
  );
}