'use client';

import Link from 'next/link';
import { useEffect, useState } from 'react';
import type { LoanDashboardSummary, LoanEligibility, LoanPolicy, LoanSummary } from '@/types/loan';

type ApiResponse = {
  success: boolean;
  data?: {
    policy?: LoanPolicy | null;
    eligibility?: LoanEligibility | null;
    items?: LoanSummary[];
    summary?: LoanDashboardSummary | null;
  };
  message?: string;
};

export function LoanHome() {
  const [state, setState] = useState<{
    loading: boolean;
    policy?: LoanPolicy | null;
    eligibility?: LoanEligibility | null;
    summary?: LoanDashboardSummary | null;
    items: LoanSummary[];
    message?: string;
  }>({ loading: true, items: [] });

  useEffect(() => {
    void load();
  }, []);

  async function load() {
    try {
      const response = await fetch('/api/loans', { cache: 'no-store' });
      const payload = (await response.json()) as ApiResponse;
      if (!response.ok || !payload.success) {
        setState({ loading: false, items: [], message: payload.message ?? 'Unable to load loan data.' });
        return;
      }
      setState({
        loading: false,
        policy: payload.data?.policy,
        eligibility: payload.data?.eligibility,
        summary: payload.data?.summary,
        items: payload.data?.items ?? [],
      });
    } catch {
      setState({ loading: false, items: [], message: 'Unable to load loan data.' });
    }
  }

  return (
    <div className="grid" style={{ gap: 16 }}>
      <div className="card">
        <div style={{ display: 'flex', justifyContent: 'space-between', gap: 12, alignItems: 'center' }}>
          <div>
            <h3>Employee Loan Eligibility</h3>
            <p className="small">Check eligibility, create loan requests, and review repayment summary.</p>
          </div>
          <Link href="/loans/new">Create loan request</Link>
        </div>
        {state.loading ? <p className="small">Loading eligibility...</p> : null}
        {state.eligibility ? (
          <div style={{ marginTop: 12 }}>
            <p><strong>Status:</strong> {state.eligibility.isEligible ? 'Eligible' : 'Not eligible'}</p>
            {state.eligibility.reasons.length > 0 ? (
              <ul>
                {state.eligibility.reasons.map((reason) => <li key={reason}>{reason}</li>)}
              </ul>
            ) : null}
            {state.eligibility.eligibleOnUtc ? <p className="small">Eligible on: {new Date(state.eligibility.eligibleOnUtc).toLocaleDateString()}</p> : null}
          </div>
        ) : null}
      </div>

      <div className="grid grid-3">
        <div className="card">
          <h3>Loan Policy</h3>
          {state.policy ? (
            <div className="small">
              <p>Max Amount: {state.policy.maximumAmount.toFixed(2)} {state.policy.currencyCode}</p>
              <p>Max Months: {state.policy.maximumMonths}</p>
              <p>Interest Rate: {state.policy.interestRate.toFixed(2)}%</p>
            </div>
          ) : <p className="small">Policy not loaded.</p>}
        </div>
        <div className="card">
          <h3>Active Loan</h3>
          {state.summary ? (
            <div className="small">
              <p>Has Active Loan: {state.summary.hasActiveLoan ? 'Yes' : 'No'}</p>
              <p>Active Loan Count: {state.summary.activeLoanCount}</p>
            </div>
          ) : <p className="small">No summary yet.</p>}
        </div>
        <div className="card">
          <h3>Outstanding Balance</h3>
          {state.summary ? <p className="small">{state.summary.outstandingBalance.toFixed(2)} {state.summary.currencyCode}</p> : <p className="small">No outstanding balance.</p>}
        </div>
      </div>

      <div className="card">
        <h3>My Loan Requests</h3>
        {state.message ? <p className="small">{state.message}</p> : null}
        {state.loading ? <p className="small">Loading requests...</p> : null}
        {!state.loading && state.items.length === 0 ? <p className="small">No loan requests yet.</p> : null}
        {!state.loading && state.items.length > 0 ? (
          <table style={{ width: '100%', borderCollapse: 'collapse', marginTop: 12 }}>
            <thead>
              <tr>
                <th align="left">Request</th>
                <th align="left">Amount</th>
                <th align="left">Months</th>
                <th align="left">Monthly</th>
                <th align="left">Outstanding</th>
                <th align="left">Status</th>
              </tr>
            </thead>
            <tbody>
              {state.items.map((item) => (
                <tr key={item.requestId} style={{ borderTop: '1px solid #e2e8f0' }}>
                  <td style={{ padding: '10px 0' }}><Link href={`/loans/${item.requestId}`}>{item.requestNumber}</Link></td>
                  <td>{item.requestedAmount.toFixed(2)}</td>
                  <td>{item.installmentMonths}</td>
                  <td>{item.monthlyInstallment.toFixed(2)}</td>
                  <td>{item.outstandingBalance.toFixed(2)}</td>
                  <td>{item.status}</td>
                </tr>
              ))}
            </tbody>
          </table>
        ) : null}
      </div>
    </div>
  );
}
