'use client';

import { useEffect, useState } from 'react';
import { useRouter } from 'next/navigation';
import type { LoanRequest } from '@/types/loan';
import { LoanDetail } from '@/components/loan-detail';

type Props = {
  requestId: string;
};

type LoanResponse = { success: boolean; data?: LoanRequest; message?: string };

export function LoanDetailPage({ requestId }: Props) {
  const router = useRouter();
  const [state, setState] = useState<{ loading: boolean; request?: LoanRequest; message?: string; submitting?: boolean }>({ loading: true });

  useEffect(() => {
    void load();
  }, [requestId]);

  async function load() {
    setState((current) => ({ ...current, loading: true, message: undefined }));
    try {
      const response = await fetch(`/api/loans/${requestId}`, { cache: 'no-store' });
      const payload = (await response.json()) as LoanResponse;
      if (!response.ok || !payload.success || !payload.data) {
        setState({ loading: false, message: payload.message ?? 'Unable to load loan request.' });
        return;
      }
      setState({ loading: false, request: payload.data });
    } catch {
      setState({ loading: false, message: 'Unable to load loan request.' });
    }
  }

  async function submitForApproval() {
    setState((current) => ({ ...current, submitting: true, message: undefined }));
    try {
      const response = await fetch(`/api/loans/${requestId}/submit`, { method: 'POST' });
      const payload = (await response.json()) as LoanResponse;
      if (!response.ok || !payload.success || !payload.data) {
        setState((current) => ({ ...current, submitting: false, message: payload.message ?? 'Unable to submit request.' }));
        return;
      }
      setState({ loading: false, request: payload.data, submitting: false, message: 'Loan request submitted.' });
      router.refresh();
    } catch {
      setState((current) => ({ ...current, submitting: false, message: 'Unable to submit request.' }));
    }
  }

  if (state.loading) {
    return <div className="card"><p className="small">Loading loan request...</p></div>;
  }

  if (!state.request) {
    return <div className="card"><p className="small">{state.message ?? 'Loan request not found.'}</p></div>;
  }

  return <LoanDetail request={state.request} onSubmitForApproval={submitForApproval} submitting={state.submitting} message={state.message} />;
}
