'use client';

import { useEffect, useState } from 'react';
import { getJson } from '@/services/api-client';

type FlowStep = {
  name: string;
  success: boolean;
  message: string;
  correlationId?: string;
};

type ProfileFlowData = {
  checkedAtUtc: string;
  sessionUserName: string;
  correlationId?: string;
  steps: FlowStep[];
};

type ViewState = {
  loading: boolean;
  message?: string;
  result?: {
    success: boolean;
    data?: ProfileFlowData;
    message?: string;
  };
};

export function ProfileFlowCheck() {
  const [state, setState] = useState<ViewState>({ loading: true });

  useEffect(() => {
    let active = true;

    async function load() {
      const result = await getJson<ProfileFlowData>('/api/test/profile-flow');
      if (!active) {
        return;
      }

      setState({
        loading: false,
        result,
        message: result.success ? undefined : result.message ?? 'Profile flow check failed.',
      });
    }

    load();
    return () => {
      active = false;
    };
  }, []);

  if (state.loading) {
    return (
      <div className="card">
        <p className="small">Running end-to-end checks...</p>
      </div>
    );
  }

  if (!state.result?.data) {
    return (
      <div className="card">
        <p className="small">{state.message ?? 'No test result returned.'}</p>
      </div>
    );
  }

  return (
    <div className="card">
      <h3>Profile Flow Check</h3>
      <p className="small">Session user: {state.result.data.sessionUserName}</p>
      <p className="small">Checked at (UTC): {state.result.data.checkedAtUtc}</p>
      <p className="small">BFF correlation ID: {state.result.data.correlationId ?? 'n/a'}</p>
      <div className="grid" style={{ gap: 12, marginTop: 16 }}>
        {state.result.data.steps.map((step) => (
          <div key={step.name} className="card" style={{ margin: 0, borderColor: step.success ? '#16a34a' : '#dc2626' }}>
            <strong>{step.name}</strong>
            <p className="small" style={{ marginTop: 8 }}>
              {step.success ? 'PASS' : 'FAIL'} - {step.message}
            </p>
            <p className="small">Correlation ID: {step.correlationId ?? 'n/a'}</p>
          </div>
        ))}
      </div>
    </div>
  );
}
