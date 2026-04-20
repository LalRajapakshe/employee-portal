'use client';

import { useEffect, useState } from 'react';
import Link from 'next/link';
import type { ApprovalInboxItem } from '@/types/loan';

type InboxResponse = { success: boolean; data?: ApprovalInboxItem[]; message?: string };
type ActionResponse = { success: boolean; message?: string; data?: unknown };

export function LoanApprovalInbox() {
  const [state, setState] = useState<{ loading: boolean; items: ApprovalInboxItem[]; message?: string; actingId?: string }>({ loading: true, items: [] });

  useEffect(() => {
    void load();
  }, []);

  async function load() {
    try {
      const response = await fetch('/api/loans/approvals', { cache: 'no-store' });
      const payload = (await response.json()) as InboxResponse;
      if (!response.ok || !payload.success) {
        setState({ loading: false, items: [], message: payload.message ?? 'Unable to load loan approvals.' });
        return;
      }
      setState({ loading: false, items: payload.data ?? [] });
    } catch {
      setState({ loading: false, items: [], message: 'Unable to load loan approvals.' });
    }
  }

  async function act(requestId: string, actionCode: 'APPROVE' | 'REJECT' | 'SEND_BACK') {
    setState((current) => ({ ...current, actingId: requestId, message: undefined }));
    try {
      const response = await fetch(`/api/loans/${requestId}/actions`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ actionCode, comments: '' }),
      });
      const payload = (await response.json()) as ActionResponse;
      if (!response.ok || !payload.success) {
        setState((current) => ({ ...current, actingId: undefined, message: payload.message ?? 'Unable to apply action.' }));
        return;
      }
      await load();
    } catch {
      setState((current) => ({ ...current, actingId: undefined, message: 'Unable to apply action.' }));
    }
  }

  return (
    <div className="card">
      <h2>Loan Approval Inbox</h2>
      {state.message ? <p className="small">{state.message}</p> : null}
      {state.loading ? <p className="small">Loading approval items...</p> : null}
      {!state.loading && state.items.length === 0 ? <p className="small">No loan approvals pending.</p> : null}
      {!state.loading && state.items.length > 0 ? (
        <table style={{ width: '100%', borderCollapse: 'collapse', marginTop: 12 }}>
          <thead>
            <tr>
              <th align="left">Request</th>
              <th align="left">Employee</th>
              <th align="left">Amount</th>
              <th align="left">Pending Role</th>
              <th align="left">Actions</th>
            </tr>
          </thead>
          <tbody>
            {state.items.map((item) => (
              <tr key={item.requestId} style={{ borderTop: '1px solid #e2e8f0' }}>
                <td><Link href={`/loans/${item.requestId}`}>{item.requestNumber}</Link></td>
                <td>{item.employeeName}</td>
                <td>{item.requestedAmount.toFixed(2)}</td>
                <td>{item.pendingWithRole}</td>
                <td style={{ display: 'flex', gap: 8, padding: '10px 0', flexWrap: 'wrap' }}>
                  <button disabled={state.actingId === item.requestId} onClick={() => void act(item.requestId, 'APPROVE')}>Approve</button>
                  <button disabled={state.actingId === item.requestId} onClick={() => void act(item.requestId, 'SEND_BACK')}>Send Back</button>
                  <button disabled={state.actingId === item.requestId} onClick={() => void act(item.requestId, 'REJECT')}>Reject</button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      ) : null}
    </div>
  );
}
