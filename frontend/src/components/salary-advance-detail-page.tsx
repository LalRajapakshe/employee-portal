'use client';

import { useEffect, useState } from 'react';
import type { CurrentUser } from '@/types/current-user';
import type { SalaryAdvanceRequest } from '@/types/salary-advance';
import { SalaryAdvanceDetail } from '@/components/salary-advance-detail';

export function SalaryAdvanceDetailPage({ user, requestId }: { user: CurrentUser; requestId: string }) {
  const [state, setState] = useState<{ loading: boolean; item?: SalaryAdvanceRequest; message?: string }>({ loading: true });

  useEffect(() => {
    void load();
  }, [requestId]);

  async function load() {
    try {
      const response = await fetch(`/api/salary-advance/${requestId}`, { cache: 'no-store' });
      const payload = await response.json();
      if (!response.ok || !payload.success || !payload.data) {
        setState({ loading: false, message: payload.message ?? 'Unable to load request.' });
        return;
      }
      setState({ loading: false, item: payload.data });
    } catch {
      setState({ loading: false, message: 'Unable to load request.' });
    }
  }

  if (state.loading) return <div className="card"><p className="small">Loading request...</p></div>;
  if (!state.item) return <div className="card"><p className="small">{state.message ?? 'Request not found.'}</p></div>;
  return <SalaryAdvanceDetail user={user} request={state.item} />;
}
