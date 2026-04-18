'use client';

import Link from 'next/link';
import { useEffect, useState } from 'react';
import type { ApprovalInboxItem } from '@/types/salary-advance';

type ApiResponse = { success: boolean; data?: ApprovalInboxItem[]; message?: string };

export function ApprovalInbox() {
  const [state, setState] = useState<{ loading: boolean; items: ApprovalInboxItem[]; message?: string }>({ loading: true, items: [] });

  useEffect(() => { void load(); }, []);

  async function load() {
    try {
      const response = await fetch('/api/approvals/inbox', { cache: 'no-store' });
      const payload = (await response.json()) as ApiResponse;
      if (!response.ok || !payload.success) {
        setState({ loading: false, items: [], message: payload.message ?? 'Unable to load approvals inbox.' });
        return;
      }
      setState({ loading: false, items: payload.data ?? [] });
    } catch {
      setState({ loading: false, items: [], message: 'Unable to load approvals inbox.' });
    }
  }

  return (
    <div className="card">
      <h3>Approvals Inbox</h3>
      {state.loading ? <p className="small">Loading inbox...</p> : null}
      {state.message ? <p className="small">{state.message}</p> : null}
      {!state.loading && state.items.length === 0 ? <p className="small">No pending approvals.</p> : null}
      {!state.loading && state.items.length > 0 ? (
        <table style={{ width: '100%', borderCollapse: 'collapse', marginTop: 12 }}>
          <thead>
            <tr>
              <th align="left">Request</th>
              <th align="left">Employee</th>
              <th align="left">Amount</th>
              <th align="left">Pending Role</th>
              <th align="left">Submitted</th>
            </tr>
          </thead>
          <tbody>
            {state.items.map((item) => (
              <tr key={item.requestId} style={{ borderTop: '1px solid #e2e8f0' }}>
                <td style={{ padding: '10px 0' }}><Link href={`/salary-advance/${item.requestId}`}>{item.requestNumber}</Link></td>
                <td>{item.employeeName}</td>
                <td>{item.requestedAmount.toFixed(2)}</td>
                <td>{item.pendingWithRole}</td>
                <td>{new Date(item.submittedAtUtc).toLocaleString()}</td>
              </tr>
            ))}
          </tbody>
        </table>
      ) : null}
    </div>
  );
}
