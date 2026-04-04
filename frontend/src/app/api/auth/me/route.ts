import { NextResponse } from 'next/server';
import { getPortalSession } from '@/lib/session';

export async function GET() {
  const coreApiBaseUrl = process.env.CORE_API_BASE_URL ?? 'http://localhost:5000';
  const session = await getPortalSession();

  if (!session?.userName) {
    return NextResponse.json({ success: false, message: 'Not authenticated.' }, { status: 401 });
  }

  try {
    const response = await fetch(`${coreApiBaseUrl}/api/auth/me`, {
      method: 'GET',
      headers: {
        'Content-Type': 'application/json',
        'X-Portal-User': session.userName,
      },
      cache: 'no-store',
    });

    if (response.ok) {
      const payload = await response.json();
      return NextResponse.json(payload, { status: response.status });
    }

    return NextResponse.json({ success: false, message: 'Current user could not be resolved.' }, { status: response.status });
  } catch {
    return NextResponse.json({ success: true, data: session }, { status: 200 });
  }
}
