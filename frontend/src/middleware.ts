import type { NextRequest } from 'next/server';
import { NextResponse } from 'next/server';
import { PORTAL_SESSION_COOKIE } from '@/lib/session';

const PROTECTED_PATHS = ['/', '/profile', '/admin'];

export function middleware(request: NextRequest) {
  const { pathname } = request.nextUrl;

  if (!PROTECTED_PATHS.includes(pathname)) {
    return NextResponse.next();
  }

  const sessionCookie = request.cookies.get(PORTAL_SESSION_COOKIE)?.value;
  if (sessionCookie) {
    return NextResponse.next();
  }

  const loginUrl = new URL('/login', request.url);
  loginUrl.searchParams.set('redirect', pathname);
  return NextResponse.redirect(loginUrl);
}

export const config = {
  matcher: ['/', '/profile', '/admin'],
};
