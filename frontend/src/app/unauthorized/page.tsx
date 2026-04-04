import Link from 'next/link';

export default function UnauthorizedPage() {
  return (
    <main className="container">
      <div className="card" style={{ maxWidth: 640, margin: '64px auto' }}>
        <h1>Unauthorized</h1>
        <p className="small">
          You are signed in, but your current role or permission set does not allow access to this page.
        </p>
        <div style={{ display: 'flex', gap: 12, marginTop: 12 }}>
          <Link href="/">Return to dashboard</Link>
          <Link href="/login">Switch user</Link>
        </div>
      </div>
    </main>
  );
}
