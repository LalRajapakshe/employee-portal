import { AppShell } from '@/components/app-shell';
import { LoanForm } from '@/components/loan-form';
import { requirePortalSession } from '@/lib/require-session';

export default async function LoanEditPage({ params }: { params: Promise<{ requestId: string }> }) {
  const user = await requirePortalSession({ requiredPermissions: ['LOAN.CREATE'] });
  const { requestId } = await params;

  return (
    <AppShell initialUser={user} pageTitle="Edit Loan Draft" pageDescription="Update a saved employee loan draft.">
      <LoanForm requestId={requestId} />
    </AppShell>
  );
}
