import { NextResponse } from 'next/server';
import { PORTAL_SESSION_COOKIE } from '@/lib/session';

export async function POST() {
  const response = NextResponse.json({ success: true, message: 'Logged out.' });
  response.cookies.set(PORTAL_SESSION_COOKIE, '', {
    httpOnly: true,
    sameSite: 'lax',
    path: '/',
    secure: false,
    expires: new Date(0),
  });

  return response;
}
