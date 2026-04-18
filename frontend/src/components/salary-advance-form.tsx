'use client';

import { FormEvent, useEffect, useMemo, useState } from 'react';
import { useRouter } from 'next/navigation';
import type { SalaryAdvancePolicy, SalaryAdvanceRequest } from '@/types/salary-advance';

type Props = {
  requestId?: string;
};

type CollectionResponse = { success: boolean; data?: { policy?: SalaryAdvancePolicy | null } };
type RequestResponse = { success: boolean; data?: SalaryAdvanceRequest; message?: string };

export function SalaryAdvanceForm({ requestId }: Props) {
  const router = useRouter();
  const [policy, setPolicy] = useState<SalaryAdvancePolicy | null>(null);
  const [request, setRequest] = useState<SalaryAdvanceRequest | null>(null);
  const [requestedAmount, setRequestedAmount] = useState<string>('');
  const [reason, setReason] = useState('');
  const [message, setMessage] = useState<string>('');
  const [loading, setLoading] = useState<boolean>(false);
  const [initializing, setInitializing] = useState<boolean>(true);

  useEffect(() => {
    void initialize();
  }, [requestId]);

  async function initialize() {
    setInitializing(true);
    try {
      const collectionResponse = await fetch('/api/salary-advance', { cache: 'no-store' });
      const collectionPayload = (await collectionResponse.json()) as CollectionResponse;
      if (collectionResponse.ok && collectionPayload.success) {
        setPolicy(collectionPayload.data?.policy ?? null);
      }

      if (requestId) {
        const detailResponse = await fetch(`/api/salary-advance/${requestId}`, { cache: 'no-store' });
        const detailPayload = (await detailResponse.json()) as RequestResponse;
        if (detailResponse.ok && detailPayload.success && detailPayload.data) {
          setRequest(detailPayload.data);
          setRequestedAmount(String(detailPayload.data.requestedAmount));
          setReason(detailPayload.data.reason ?? '');
        } else {
          setMessage(detailPayload.message ?? 'Unable to load draft.');
        }
      }
    } catch {
      setMessage('Unable to load salary advance form.');
    } finally {
      setInitializing(false);
    }
  }

  const validationHint = useMemo(() => {
    if (!policy) return null;
    return `Maximum amount: ${policy.maximumAmount.toFixed(2)} ${policy.currencyCode}`;
  }, [policy]);

  async function onSaveDraft(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setLoading(true);
    setMessage('');
    try {
      const payload = { requestedAmount: Number(requestedAmount), reason };
      const response = request
        ? await fetch(`/api/salary-advance/${request.requestId}`, { method: 'PUT', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(payload) })
        : await fetch('/api/salary-advance', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(payload) });
      const data = (await response.json()) as RequestResponse;
      if (!response.ok || !data.success || !data.data) {
        setMessage(data.message ?? 'Unable to save draft.');
        return;
      }
      setRequest(data.data);
      setMessage('Draft saved successfully.');
      router.push(`/salary-advance/${data.data.requestId}`);
      router.refresh();
    } catch {
      setMessage('Unable to save draft.');
    } finally {
      setLoading(false);
    }
  }

  async function onSubmit() {
    const targetRequestId = request?.requestId;
    if (!targetRequestId) {
      setMessage('Save the draft before submitting it for approval.');
      return;
    }
    setLoading(true);
    setMessage('');
    try {
      const response = await fetch(`/api/salary-advance/${targetRequestId}/submit`, { method: 'POST' });
      const data = (await response.json()) as RequestResponse;
      if (!response.ok || !data.success || !data.data) {
        setMessage(data.message ?? 'Unable to submit request.');
        return;
      }
      router.push(`/salary-advance/${data.data.requestId}`);
      router.refresh();
    } catch {
      setMessage('Unable to submit request.');
    } finally {
      setLoading(false);
    }
  }

  if (initializing) {
    return <div className="card"><p className="small">Loading form...</p></div>;
  }

  return (
    <form onSubmit={onSaveDraft} className="card" style={{ display: 'grid', gap: 16 }}>
      <div>
        <h3>{request ? 'Update Salary Advance Draft' : 'New Salary Advance Request'}</h3>
        <p className="small">Create the request, save as draft, then submit it into the two-level approval workflow.</p>
      </div>
      <label>
        <strong>Requested Amount</strong>
        <input value={requestedAmount} onChange={(e) => setRequestedAmount(e.target.value)} type="number" min="0" step="0.01" style={{ width: '100%', padding: 10, marginTop: 6 }} />
        {validationHint ? <p className="small">{validationHint}</p> : null}
      </label>
      <label>
        <strong>Reason / Remarks</strong>
        <textarea value={reason} onChange={(e) => setReason(e.target.value)} rows={5} style={{ width: '100%', padding: 10, marginTop: 6 }} />
      </label>
      {request?.validationMessages?.length ? request.validationMessages.map((item) => <p key={item} className="small">{item}</p>) : null}
      {message ? <p className="small">{message}</p> : null}
      <div style={{ display: 'flex', gap: 12, flexWrap: 'wrap' }}>
        <button type="submit" disabled={loading}>{loading ? 'Saving...' : 'Save Draft'}</button>
        {request ? <button type="button" onClick={onSubmit} disabled={loading}>Submit for Approval</button> : null}
      </div>
    </form>
  );
}
