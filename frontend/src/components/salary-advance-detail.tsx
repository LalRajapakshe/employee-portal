'use client';

import Link from 'next/link';
import { useState } from 'react';
import { useRouter } from 'next/navigation';
import type { CurrentUser } from '@/types/current-user';
import type { SalaryAdvanceRequest } from '@/types/salary-advance';
import { PERMISSIONS } from '@/config/permissions';

export function SalaryAdvanceDetail({ user, request }: { user: CurrentUser; request: SalaryAdvanceRequest }) {
  const router = useRouter();
  const [message, setMessage] = useState<string>('');
  const [comment, setComment] = useState<string>('');
  const [loading, setLoading] = useState<boolean>(false);

  const canEdit = request.status === 'DRAFT' || request.status === 'SENT_BACK';
  const canApproveStage1 = user.permissions.includes(PERMISSIONS.SALARY_ADVANCE_APPROVE_STAGE1) && request.pendingStageNumber === 1;
  const canApproveStage2 = user.permissions.includes(PERMISSIONS.SALARY_ADVANCE_APPROVE_STAGE2) && request.pendingStageNumber === 2;

  async function runAction(actionCode: string) {
    setLoading(true);
    setMessage('');
    try {
      const response = await fetch(`/api/salary-advance/${request.requestId}/actions`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ actionCode, comments: comment }),
      });
      const payload = await response.json();
      if (!response.ok || !payload.success) {
        setMessage(payload.message ?? 'Unable to apply workflow action.');
        return;
      }
      router.refresh();
    } catch {
      setMessage('Unable to apply workflow action.');
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="grid">
      <div className="card">
        <div style={{ display: 'flex', justifyContent: 'space-between', gap: 12, alignItems: 'center' }}>
          <div>
            <h3>{request.requestNumber}</h3>
            <p className="small">Status: {request.status} {request.pendingWithRole ? `• Pending with ${request.pendingWithRole}` : ''}</p>
          </div>
          <div style={{ display: 'flex', gap: 12, flexWrap: 'wrap' }}>
            <Link href="/salary-advance">Back to list</Link>
            <Link href={`/salary-advance/${request.requestId}/print`}>Print view</Link>
            {canEdit ? <Link href={`/salary-advance/new?requestId=${request.requestId}`}>Edit draft</Link> : null}
          </div>
        </div>
        <div className="grid grid-3" style={{ marginTop: 12 }}>
          <div><strong>Employee</strong><p className="small">{request.employeeName} ({request.employeeCode})</p></div>
          <div><strong>Amount</strong><p className="small">{request.requestedAmount.toFixed(2)} {request.currencyCode}</p></div>
          <div><strong>Payroll</strong><p className="small">{request.payrollHandoffStatus}</p></div>
        </div>
        <div style={{ marginTop: 12 }}>
          <strong>Reason / Remarks</strong>
          <p className="small">{request.reason || '-'}</p>
        </div>
        {request.validationMessages?.length ? request.validationMessages.map((item) => <p key={item} className="small">{item}</p>) : null}
      </div>

      {(canApproveStage1 || canApproveStage2) ? (
        <div className="card">
          <h3>Approval Actions</h3>
          <textarea value={comment} onChange={(e) => setComment(e.target.value)} rows={4} style={{ width: '100%', padding: 10, marginTop: 8 }} placeholder="Comments" />
          {message ? <p className="small">{message}</p> : null}
          <div style={{ display: 'flex', gap: 12, flexWrap: 'wrap', marginTop: 12 }}>
            <button type="button" disabled={loading} onClick={() => runAction('APPROVE')}>Approve</button>
            <button type="button" disabled={loading} onClick={() => runAction('REJECT')}>Reject</button>
            <button type="button" disabled={loading} onClick={() => runAction('SEND_BACK')}>Send Back</button>
          </div>
        </div>
      ) : null}

      <div className="card">
        <h3>Workflow History</h3>
        {request.workflowActions.length === 0 ? <p className="small">No workflow actions yet.</p> : null}
        <div className="grid" style={{ marginTop: 12 }}>
          {request.workflowActions.map((action) => (
            <div key={action.actionId} style={{ border: '1px solid #e2e8f0', borderRadius: 12, padding: 12 }}>
              <strong>{action.actionCode}</strong>
              <p className="small">Stage {action.stageNumber} • {action.performedBy} • {new Date(action.actionAtUtc).toLocaleString()}</p>
              <p className="small">Resulting status: {action.resultingStatus}</p>
              {action.comments ? <p className="small">Comments: {action.comments}</p> : null}
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}
