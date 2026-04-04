import { NextRequest, NextResponse } from 'next/server';
import { encodePortalSession, PORTAL_SESSION_COOKIE } from '@/lib/session';
import type { CurrentUser } from '@/types/current-user';

export async function POST(request: NextRequest) {
  const coreApiBaseUrl = process.env.CORE_API_BASE_URL ?? 'http://localhost:5000';

  let body: { userName?: string; password?: string };
  try {
    body = await request.json();
  } catch {
    return NextResponse.json({ success: false, message: 'Invalid login request.' }, { status: 400 });
  }

  try {
    const response = await fetch(`${coreApiBaseUrl}/api/auth/login`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ userName: body.userName, password: body.password }),
      cache: 'no-store',
    });

    const payload = await response.json();
    if (!response.ok || !payload?.success || !payload?.data) {
      return NextResponse.json(
        { success: false, message: payload?.message ?? 'Login failed.' },
        { status: response.status || 400 },
      );
    }

    const user = payload.data as CurrentUser;
    const result = NextResponse.json({ success: true, data: user });
    result.cookies.set(PORTAL_SESSION_COOKIE, encodePortalSession(user), {
      httpOnly: true,
      sameSite: 'lax',
      path: '/',
      secure: false,
      maxAge: 60 * 60 * 8,
    });

    return result;
  } catch {
    return NextResponse.json({ success: false, message: 'Unable to reach Core Backend Layer.' }, { status: 502 });
  }
}
