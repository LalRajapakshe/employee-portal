'use client';

import { useEffect, useState } from 'react';
import Link from 'next/link';
import type { NotificationItem } from '@/types/salary-advance';

type ApiResponse = {
  success: boolean;
  data?: NotificationItem[];
  message?: string;
};

export function NotificationsPanel() {
  const [state, setState] = useState<{ loading: boolean; items: NotificationItem[]; message?: string }>({ loading: true, items: [] });

  useEffect(() => {
    void load();
  }, []);

  async function load() {
    try {
      const response = await fetch('/api/notifications', { cache: 'no-store' });
      const payload = (await response.json()) as ApiResponse;
      if (!response.ok || !payload.success) {
        setState({ loading: false, items: [], message: payload.message ?? 'Unable to load notifications.' });
        return;
      }
      setState({ loading: false, items: payload.data ?? [] });
    } catch {
      setState({ loading: false, items: [], message: 'Unable to load notifications.' });
    }
  }

  return (
    <div className="card">
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <h3>Notifications</h3>
        <Link href="/notifications">View all</Link>
      </div>
      {state.loading ? <p className="small">Loading notifications...</p> : null}
      {state.message ? <p className="small">{state.message}</p> : null}
      {!state.loading && state.items.length === 0 ? <p className="small">No notifications yet.</p> : null}
      <div className="grid" style={{ marginTop: 12 }}>
        {state.items.slice(0, 5).map((item) => (
          <div key={item.notificationId} style={{ border: '1px solid #e2e8f0', borderRadius: 12, padding: 12 }}>
            <strong>{item.title}</strong>
            <p className="small" style={{ marginTop: 6 }}>{item.message}</p>
            {item.linkUrl ? <Link href={item.linkUrl}>Open</Link> : null}
          </div>
        ))}
      </div>
    </div>
  );
}
