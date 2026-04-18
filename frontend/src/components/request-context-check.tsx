'use client';

import { useEffect, useState } from 'react';
import { getJson } from '@/services/api-client';

type RequestContextData = {
  userName?: string | null;
  correlationId?: string | null;
  bffCorrelationId?: string | null;
  path?: string | null;
  ipAddress?: string | null;
  userAgent?: string | null;
  method?: string | null;
};

export function RequestContextCheck() {
  const [state, setState] = useState<{
    loading: boolean;
    message?: string;
    data?: RequestContextData;
  }>({ loading: true });

  useEffect(() => {
    let active = true;

    async function load() {
      const result = await getJson<RequestContextData>('/api/test/request-context');
      if (!active) return;

      setState({
        loading: false,
        message: result.success ? undefined : result.message ?? 'Request-context diagnostic failed.',
        data: result.data,
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
        <p className="small">Loading request-context diagnostic...</p>
      </div>
    );
  }

  if (!state.data) {
    return (
      <div className="card">
        <p className="small">{state.message ?? 'No diagnostic data returned.'}</p>
      </div>
    );
  }

  return (
    <div className="card">
      <h3>Request Context Diagnostic</h3>
      <div className="grid" style={{ gap: 10, marginTop: 16 }}>
        <p className="small"><strong>User:</strong> {state.data.userName ?? 'n/a'}</p>
        <p className="small"><strong>Core correlation ID:</strong> {state.data.correlationId ?? 'n/a'}</p>
        <p className="small"><strong>BFF correlation ID:</strong> {state.data.bffCorrelationId ?? 'n/a'}</p>
        <p className="small"><strong>Path:</strong> {state.data.path ?? 'n/a'}</p>
        <p className="small"><strong>Method:</strong> {state.data.method ?? 'n/a'}</p>
        <p className="small"><strong>IP address:</strong> {state.data.ipAddress ?? 'n/a'}</p>
        <p className="small"><strong>User-Agent:</strong> {state.data.userAgent ?? 'n/a'}</p>
      </div>
    </div>
  );
}
