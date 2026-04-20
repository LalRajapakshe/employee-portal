import { AppShell } from '@/components/app-shell';
import { LoanHome } from '@/components/loan-home';
import { requirePortalSession } from '@/lib/require-session';

export default async function LoansPage() {
  const user = await requirePortalSession({ requiredPermissions: ['LOAN.VIEW_SELF'] });

  return (
    <AppShell initialUser={user} pageTitle="Employee Loans" pageDescription="Check eligibility, create, submit, track, and print employee loan requests.">
      <LoanHome />
    </AppShell>
  );
}
