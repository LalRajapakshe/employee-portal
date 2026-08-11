'use client';

import { FormEvent, useEffect, useState } from 'react';
import { useRouter, useSearchParams } from 'next/navigation';

type LoginState = {
  loading: boolean;
  message?: string;
};
type BranchOption = {
  value: number;
  text: string;
};

export default function LoginClient() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const [state, setState] = useState<LoginState>({ loading: false });
  const [userName, setUserName] = useState('demo.user');
  const [password, setPassword] = useState('Password@123');
  const [branches, setBranches] = useState<BranchOption[]>([]);
  const [branchId, setBranchId] = useState<number | ''>('');

  useEffect(() => {
  const loadBranches = async () => {
    try {
      const response = await fetch('/api/auth/branches');

      if (!response.ok) {
        throw new Error('Failed to load branches');
      }

      const result = await response.json();

      setBranches(result.data ?? []);
    } catch (error) {
      console.error('Failed to load branches:', error);
    }
  };

  loadBranches();
}, []);

  async function onSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setState({ loading: true });

    try {
      const response = await fetch('/api/auth/login', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ userName, password }),
      });

      const payload = await response.json();
      if (!response.ok || !payload?.success) {
        setState({ loading: false, message: payload?.message ?? 'Login failed.' });
        return;
      }

      const redirectTarget = searchParams.get('redirect') || '/';
      router.push(redirectTarget);
      router.refresh();
    } catch {
      setState({ loading: false, message: 'Unable to sign in.' });
    }
  }

  return (
    <main className="container">
      <div className="card" style={{ maxWidth: 520, margin: '48px auto' }}>
        <h1>Portal Login</h1>
        <p className="small">
          Step 5 role-aware frontend shell. Default credentials are prefilled for the scaffold.
        </p>

        <form onSubmit={onSubmit} style={{ display: 'grid', gap: 16 }}>

           <label>
                <strong>Branch</strong>

                <select
                  value={branchId}
                  onChange={(e) =>
                    setBranchId(e.target.value ? Number(e.target.value) : '')
                  }
                  style={{ width: '100%', padding: 10, marginTop: 6 }}
                >
                  <option value="">Select Branch</option>

                  {branches.map((branch) => (
                    <option key={branch.value} value={branch.value}>
                      {branch.text}
                    </option>
                  ))}
                </select>
              </label>

          <label>
            <strong>User Name</strong>
            <input
              value={userName}
              onChange={(e) => setUserName(e.target.value)}
              style={{ width: '100%', padding: 10, marginTop: 6 }}
            />
          </label>

          <label>
            <strong>Password</strong>
            <input
              type="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              style={{ width: '100%', padding: 10, marginTop: 6 }}
            />
          </label>

          {state.message ? <p className="small">{state.message}</p> : null}

          <button type="submit" disabled={state.loading}>
            {state.loading ? 'Signing in...' : 'Sign in'}
          </button>
        </form>
      </div>
    </main>
  );
}
