'use client';

import { FormEvent, useEffect, useMemo, useState } from 'react';
import { useRouter } from 'next/navigation';
import type { LoanEligibility, LoanPolicy, LoanRequest, LoanRepaymentScheduleItem } from '@/types/loan';

type PolicyResponse = { success: boolean; data?: { policy?: LoanPolicy; eligibility?: LoanEligibility }; message?: string };
type LoanResponse = { success: boolean; data?: LoanRequest; message?: string };

type Props = {
  requestId?: string;
};

export function LoanForm({ requestId }: Props) {
  const router = useRouter();
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [message, setMessage] = useState<string | undefined>();
  const [policy, setPolicy] = useState<LoanPolicy | null>(null);
  const [eligibility, setEligibility] = useState<LoanEligibility | null>(null);
  const [requestedAmount, setRequestedAmount] = useState(100000);
  const [installmentMonths, setInstallmentMonths] = useState(12);
  const [reason, setReason] = useState('');

  useEffect(() => {
    void load();
  }, [requestId]);

  async function load() {
    setLoading(true);
    setMessage(undefined);

    try {
      const homeResponse = await fetch('/api/loans', { cache: 'no-store' });
      const homePayload = (await homeResponse.json()) as PolicyResponse;
      if (homeResponse.ok && homePayload.success) {
        setPolicy(homePayload.data?.policy ?? null);
        setEligibility(homePayload.data?.eligibility ?? null);
        if (homePayload.data?.policy) {
          setInstallmentMonths(Math.min(homePayload.data.policy.maximumMonths, installmentMonths));
        }
      }

      if (requestId) {
        const response = await fetch(`/api/loans/${requestId}`, { cache: 'no-store' });
        const payload = (await response.json()) as LoanResponse;
        if (!response.ok || !payload.success || !payload.data) {
          setMessage(payload.message ?? 'Unable to load loan request.');
          setLoading(false);
          return;
        }
        setRequestedAmount(payload.data.requestedAmount);
        setInstallmentMonths(payload.data.installmentMonths);
        setReason(payload.data.reason ?? '');
      }
    } catch {
      setMessage('Unable to load loan data.');
    } finally {
      setLoading(false);
    }
  }

  const repaymentPreview = useMemo(() => {
    if (!policy || requestedAmount <= 0 || installmentMonths <= 0) {
      return { monthlyInstallment: 0, totalRepayableAmount: 0, schedule: [] as LoanRepaymentScheduleItem[] };
    }

    const monthlyRate = policy.interestRate / 100 / 12;
    const monthlyInstallment = monthlyRate === 0
      ? requestedAmount / installmentMonths
      : requestedAmount * ((monthlyRate * Math.pow(1 + monthlyRate, installmentMonths)) / (Math.pow(1 + monthlyRate, installmentMonths) - 1));

    let balance = requestedAmount;
    const schedule: LoanRepaymentScheduleItem[] = [];
    const dueDate = new Date();
    dueDate.setUTCDate(1);
    dueDate.setUTCMonth(dueDate.getUTCMonth() + 1);

    for (let i = 1; i <= installmentMonths; i += 1) {
      const openingBalance = balance;
      const interestComponent = Number((openingBalance * monthlyRate).toFixed(2));
      let principalComponent = Number((monthlyInstallment - interestComponent).toFixed(2));
      let closingBalance = Number((openingBalance - principalComponent).toFixed(2));
      if (i === installmentMonths) {
        principalComponent = Number(openingBalance.toFixed(2));
        closingBalance = 0;
      }
      schedule.push({
        scheduleItemId: `${i}`,
        installmentNumber: i,
        dueDateUtc: new Date(Date.UTC(dueDate.getUTCFullYear(), dueDate.getUTCMonth() + (i - 1), 1)).toISOString(),
        openingBalance: Number(openingBalance.toFixed(2)),
        principalComponent,
        interestComponent,
        installmentAmount: Number(monthlyInstallment.toFixed(2)),
        closingBalance,
        status: 'PENDING',
      });
      balance = closingBalance;
    }

    return {
      monthlyInstallment: Number(monthlyInstallment.toFixed(2)),
      totalRepayableAmount: Number((schedule.reduce((sum, item) => sum + item.installmentAmount, 0)).toFixed(2)),
      schedule,
    };
  }, [policy, requestedAmount, installmentMonths]);

  async function onSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setSaving(true);
    setMessage(undefined);

    try {
      const response = await fetch(requestId ? `/api/loans/${requestId}` : '/api/loans', {
        method: requestId ? 'PUT' : 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ requestedAmount, installmentMonths, reason }),
      });
      const payload = (await response.json()) as LoanResponse;
      if (!response.ok || !payload.success || !payload.data) {
        setMessage(payload.message ?? 'Unable to save draft.');
        return;
      }
      router.push(`/loans/${payload.data.requestId}`);
      router.refresh();
    } catch {
      setMessage('Unable to save draft.');
    } finally {
      setSaving(false);
    }
  }

  if (loading) {
    return <div className="card"><p className="small">Loading loan form...</p></div>;
  }

  return (
    <div className="grid" style={{ gap: 16 }}>
      <div className="card">
        <h2>{requestId ? 'Edit Loan Draft' : 'Create Loan Request'}</h2>
        <p className="small">Eligibility and repayment preview update from the current policy.</p>

        {eligibility ? (
          <div style={{ marginBottom: 16 }}>
            <strong>Eligibility:</strong> {eligibility.isEligible ? 'Eligible' : 'Not eligible'}
            {eligibility.reasons.length > 0 ? (
              <ul>
                {eligibility.reasons.map((reason) => <li key={reason}>{reason}</li>)}
              </ul>
            ) : null}
          </div>
        ) : null}

        {message ? <p className="small">{message}</p> : null}

        <form onSubmit={onSubmit} style={{ display: 'grid', gap: 16 }}>
          <label>
            <strong>Requested Amount</strong>
            <input type="number" value={requestedAmount} onChange={(e) => setRequestedAmount(Number(e.target.value))} style={{ width: '100%', padding: 10, marginTop: 6 }} />
          </label>

          <label>
            <strong>Installment Months</strong>
            <select value={installmentMonths} onChange={(e) => setInstallmentMonths(Number(e.target.value))} style={{ width: '100%', padding: 10, marginTop: 6 }}>
              {Array.from({ length: policy?.maximumMonths ?? 18 }, (_, index) => index + 1).map((month) => (
                <option key={month} value={month}>{month} month(s)</option>
              ))}
            </select>
          </label>

          <label>
            <strong>Reason</strong>
            <textarea value={reason} onChange={(e) => setReason(e.target.value)} style={{ width: '100%', padding: 10, minHeight: 100, marginTop: 6 }} />
          </label>

          <button type="submit" disabled={saving}>{saving ? 'Saving...' : 'Save Draft'}</button>
        </form>
      </div>

      <div className="card">
        <h3>Repayment Summary</h3>
        <div className="small">
          <p>Interest Rate: {policy?.interestRate.toFixed(2) ?? '0.00'}%</p>
          <p>Monthly Installment: {repaymentPreview.monthlyInstallment.toFixed(2)}</p>
          <p>Total Repayable Amount: {repaymentPreview.totalRepayableAmount.toFixed(2)}</p>
        </div>
        <div style={{ overflowX: 'auto', marginTop: 12 }}>
          <table style={{ width: '100%', borderCollapse: 'collapse' }}>
            <thead>
              <tr>
                <th align="left">#</th>
                <th align="left">Due</th>
                <th align="left">Principal</th>
                <th align="left">Interest</th>
                <th align="left">Installment</th>
              </tr>
            </thead>
            <tbody>
              {repaymentPreview.schedule.slice(0, 6).map((item) => (
                <tr key={item.installmentNumber} style={{ borderTop: '1px solid #e2e8f0' }}>
                  <td>{item.installmentNumber}</td>
                  <td>{new Date(item.dueDateUtc).toLocaleDateString()}</td>
                  <td>{item.principalComponent.toFixed(2)}</td>
                  <td>{item.interestComponent.toFixed(2)}</td>
                  <td>{item.installmentAmount.toFixed(2)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
}
