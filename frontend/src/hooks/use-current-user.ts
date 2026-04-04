'use client';

import { useEffect, useState } from 'react';
import type { CurrentUser } from '@/types/current-user';

type CurrentUserState = {
  loading: boolean;
  user?: CurrentUser;
  message?: string;
};

export function useCurrentUser(initialUser?: CurrentUser) {
  const [state, setState] = useState<CurrentUserState>(() =>
    initialUser ? { loading: false, user: initialUser } : { loading: true },
  );

  useEffect(() => {
    if (initialUser) {
      return;
    }

    let active = true;

    async function load() {
      try {
        const response = await fetch('/api/auth/me', { cache: 'no-store' });
        const payload = await response.json().catch(() => null);

        if (!active) {
          return;
        }

        if (!response.ok || !payload?.success || !payload?.data) {
          setState({ loading: false, message: payload?.message ?? 'Current user could not be resolved.' });
          return;
        }

        setState({ loading: false, user: payload.data as CurrentUser });
      } catch {
        if (active) {
          setState({ loading: false, message: 'Unable to load current user.' });
        }
      }
    }

    load();
    return () => {
      active = false;
    };
  }, [initialUser]);

  return state;
}
