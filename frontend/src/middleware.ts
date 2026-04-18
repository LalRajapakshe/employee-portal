import type { NextRequest } from 'next/server';
import { NextResponse } from 'next/server';
import { PORTAL_SESSION_COOKIE } from '@/lib/session';

const PROTECTED_PREFIXES = ['/', '/profile', '/salary-advance', '/approvals', '/notifications', '/admin', '/test'];
const PUBLIC_PATHS = ['/login', '/unauthorized'];

function isProtectedPath(pathname: string): boolean {
  if (pathname === '/') return true;
  return PROTECTED_PREFIXES.some((prefix) => prefix !== '/' && pathname.startsWith(prefix));
}

function isPublicPath(pathname: string): boolean {
  return PUBLIC_PATHS.some((prefix) => pathname === prefix || pathname.startsWith(`${prefix}/`));
}

export function middleware(request: NextRequest) {
  const { pathname } = request.nextUrl;
  const hasSession = Boolean(request.cookies.get(PORTAL_SESSION_COOKIE)?.value);

  if (pathname === '/login' && hasSession) {
    return NextResponse.redirect(new URL('/', request.url));
  }

  if (!isProtectedPath(pathname) || isPublicPath(pathname)) {
    return NextResponse.next();
  }

  if (hasSession) {
    return NextResponse.next();
  }

  const loginUrl = new URL('/login', request.url);
  loginUrl.searchParams.set('redirect', pathname);
  return NextResponse.redirect(loginUrl);
}

export const config = {
  matcher: ['/', '/profile/:path*', '/salary-advance/:path*', '/approvals/:path*', '/notifications/:path*', '/admin/:path*', '/test/:path*', '/login'],
};
