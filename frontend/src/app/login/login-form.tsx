'use client';

import { FormEvent, useState } from 'react';
import { useRouter } from 'next/navigation';

type LoginState = {
  loading: boolean;
  message?: string;
};

type LoginFormProps = {
  redirectTo: string;
};

export default function LoginForm({ redirectTo }: LoginFormProps) {
  const router = useRouter();
  const [state, setState] = useState<LoginState>({ loading: false });
  const [userName, setUserName] = useState('demo.user');
  const [password, setPassword] = useState('Password@123');

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
        setState({
          loading: false,
          message: payload?.message ?? 'Login failed.',
        });
        return;
      }

      router.push(redirectTo || '/');
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
          Sprint 2 adds Salary Advance, approvals, notifications, and print flow. Demo users: demo.user, director.user, hr.admin.
        </p>

        <form onSubmit={onSubmit} style={{ display: 'grid', gap: 16 }}>
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
