'use client';

import Link from 'next/link';
import { useEffect, useState } from 'react';
import type { SalaryAdvancePolicy, SalaryAdvanceSummary } from '@/types/salary-advance';

type ApiResponse = {
  success: boolean;
  data?: {
    policy?: SalaryAdvancePolicy | null;
    items?: SalaryAdvanceSummary[];
  };
  message?: string;
};

export function SalaryAdvanceHome() {
  const [state, setState] = useState<{ loading: boolean; policy?: SalaryAdvancePolicy | null; items: SalaryAdvanceSummary[]; message?: string }>({ loading: true, items: [] });

  useEffect(() => {
    void load();
  }, []);

  async function load() {
    try {
      const response = await fetch('/api/salary-advance', { cache: 'no-store' });
      const payload = (await response.json()) as ApiResponse;
      if (!response.ok || !payload.success) {
        setState({ loading: false, items: [], message: payload.message ?? 'Unable to load salary advance data.' });
        return;
      }
      setState({ loading: false, policy: payload.data?.policy, items: payload.data?.items ?? [] });
    } catch {
      setState({ loading: false, items: [], message: 'Unable to load salary advance data.' });
    }
  }

  return (
    <div className="grid">
      <div className="card">
        <div style={{ display: 'flex', justifyContent: 'space-between', gap: 12, alignItems: 'center' }}>
          <div>
            <h3>Salary Advance Policy</h3>
            <p className="small">Configurable maximum amount and approval path for Sprint 2.</p>
          </div>
          <Link href="/salary-advance/new">Create request</Link>
        </div>
        {state.loading ? <p className="small">Loading policy...</p> : null}
        {state.policy ? (
          <div className="grid grid-3" style={{ marginTop: 12 }}>
            <div><strong>Max Amount</strong><p className="small">{state.policy.maximumAmount.toFixed(2)} {state.policy.currencyCode}</p></div>
            <div><strong>Approvals</strong><p className="small">{state.policy.firstApproverRole} → {state.policy.secondApproverRole}</p></div>
            <div><strong>Eligibility</strong><p className="small">{state.policy.requirePermanentEmployee ? 'Permanent employees only' : 'Open to all active employees'}</p></div>
          </div>
        ) : null}
      </div>

      <div className="card">
        <h3>My Salary Advance Requests</h3>
        {state.message ? <p className="small">{state.message}</p> : null}
        {state.loading ? <p className="small">Loading requests...</p> : null}
        {!state.loading && state.items.length === 0 ? <p className="small">No salary advance requests yet.</p> : null}
        {!state.loading && state.items.length > 0 ? (
          <table style={{ width: '100%', borderCollapse: 'collapse', marginTop: 12 }}>
            <thead>
              <tr>
                <th align="left">Request</th>
                <th align="left">Amount</th>
                <th align="left">Status</th>
                <th align="left">Payroll</th>
                <th align="left">Submitted</th>
              </tr>
            </thead>
            <tbody>
              {state.items.map((item) => (
                <tr key={item.requestId} style={{ borderTop: '1px solid #e2e8f0' }}>
                  <td style={{ padding: '10px 0' }}><Link href={`/salary-advance/${item.requestId}`}>{item.requestNumber}</Link></td>
                  <td>{item.requestedAmount.toFixed(2)}</td>
                  <td>{item.status}</td>
                  <td>{item.payrollHandoffStatus}</td>
                  <td>{item.submittedAtUtc ? new Date(item.submittedAtUtc).toLocaleString() : '-'}</td>
                </tr>
              ))}
            </tbody>
          </table>
        ) : null}
      </div>
    </div>
  );
}
