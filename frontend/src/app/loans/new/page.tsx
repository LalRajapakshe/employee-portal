import { AppShell } from '@/components/app-shell';
import { LoanForm } from '@/components/loan-form';
import { requirePortalSession } from '@/lib/require-session';

export default async function LoanNewPage() {
  const user = await requirePortalSession({ requiredPermissions: ['LOAN.CREATE'] });

  return (
    <AppShell initialUser={user} pageTitle="Create Loan Request" pageDescription="Create and save an employee loan draft with repayment preview.">
      <LoanForm />
    </AppShell>
  );
}
