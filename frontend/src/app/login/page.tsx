import { Suspense } from 'react';
import LoginClient from './LoginClient';

function LoginFallback() {
  return (
    <main className="container">
      <div className="card" style={{ maxWidth: 520, margin: '48px auto' }}>
        <h1>Portal Login</h1>
        <p className="small">Loading login...</p>
      </div>
    </main>
  );
}

export default function LoginPage() {
  return (
    <Suspense fallback={<LoginFallback />}>
      <LoginClient />
    </Suspense>
  );
}
