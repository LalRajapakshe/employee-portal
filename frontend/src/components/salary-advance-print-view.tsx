'use client';

import { useEffect, useState } from 'react';
import type { SalaryAdvancePrintData } from '@/types/salary-advance';

export function SalaryAdvancePrintView({ requestId }: { requestId: string }) {
  const [state, setState] = useState<{ loading: boolean; item?: SalaryAdvancePrintData; message?: string }>({ loading: true });

  useEffect(() => { void load(); }, [requestId]);

  async function load() {
    try {
      const response = await fetch(`/api/salary-advance/${requestId}/print`, { cache: 'no-store' });
      const payload = await response.json();
      if (!response.ok || !payload.success) {
        setState({ loading: false, message: payload.message ?? 'Unable to load print data.' });
        return;
      }
      setState({ loading: false, item: payload.data });
    } catch {
      setState({ loading: false, message: 'Unable to load print data.' });
    }
  }

  useEffect(() => {
    if (state.item) {
      setTimeout(() => window.print(), 300);
    }
  }, [state.item]);

  if (state.loading) return <main className="container"><p className="small">Loading print view...</p></main>;
  if (state.message || !state.item) return <main className="container"><p className="small">{state.message ?? 'Print data not found.'}</p></main>;

  const item = state.item;
  return (
    <main className="container">
      <div className="card">
        <h1>Salary Advance Request</h1>
        <p><strong>Request Number:</strong> {item.requestNumber}</p>
        <p><strong>Employee:</strong> {item.employeeName} ({item.employeeCode})</p>
        <p><strong>Department / Designation:</strong> {item.department} / {item.designation}</p>
        <p><strong>Amount:</strong> {item.requestedAmount.toFixed(2)} {item.currencyCode}</p>
        <p><strong>Status:</strong> {item.status}</p>
        <p><strong>Reason:</strong> {item.reason || '-'}</p>
        <h3>Workflow History</h3>
        {item.workflowActions.map((action) => (
          <p key={action.actionId} className="small">Stage {action.stageNumber} • {action.actionCode} • {action.performedBy} • {new Date(action.actionAtUtc).toLocaleString()}</p>
        ))}
      </div>
    </main>
  );
}
