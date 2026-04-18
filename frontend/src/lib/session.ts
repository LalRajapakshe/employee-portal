import type { NextResponse } from 'next/server';
import { cookies } from 'next/headers';
import type { CurrentUser } from '@/types/current-user';

export const PORTAL_SESSION_COOKIE = 'portal_session';

function normalizeStringArray(values: unknown): string[] {
  if (!Array.isArray(values)) return [];

  return Array.from(
    new Set(
      values
        .filter((value): value is string => typeof value === 'string')
        .map((value) => value.trim())
        .filter(Boolean),
    ),
  );
}

function sanitizePortalSession(value: unknown): CurrentUser | null {
  if (!value || typeof value !== 'object') {
    return null;
  }

  const raw = value as Record<string, unknown>;
  const userName = typeof raw.userName === 'string' ? raw.userName.trim() : '';
  const displayName = typeof raw.displayName === 'string' ? raw.displayName.trim() : '';
  const employeeCode = typeof raw.employeeCode === 'string' ? raw.employeeCode.trim() : '';

  if (!userName || !displayName || !employeeCode) {
    return null;
  }

  return {
    userName,
    displayName,
    employeeCode,
    email: typeof raw.email === 'string' ? raw.email : null,
    roles: normalizeStringArray(raw.roles),
    permissions: normalizeStringArray(raw.permissions),
    isAuthenticated: true,
  };
}

export async function getPortalSession(): Promise<CurrentUser | null> {
  const cookieStore = await cookies();
  const rawValue = cookieStore.get(PORTAL_SESSION_COOKIE)?.value;
  if (!rawValue) {
    return null;
  }

  try {
    const parsed = JSON.parse(Buffer.from(rawValue, 'base64url').toString('utf8'));
    return sanitizePortalSession(parsed);
  } catch {
    return null;
  }
}

export function encodePortalSession(user: CurrentUser): string {
  return Buffer.from(JSON.stringify(sanitizePortalSession(user)), 'utf8').toString('base64url');
}

export function applyPortalSessionCookie(response: NextResponse, user: CurrentUser) {
  response.cookies.set(PORTAL_SESSION_COOKIE, encodePortalSession(user), {
    httpOnly: true,
    sameSite: 'lax',
    path: '/',
    secure: process.env.NODE_ENV === 'production',
    maxAge: 60 * 60 * 8,
  });
}

export function clearPortalSessionCookie(response: NextResponse) {
  response.cookies.set(PORTAL_SESSION_COOKIE, '', {
    httpOnly: true,
    sameSite: 'lax',
    path: '/',
    secure: process.env.NODE_ENV === 'production',
    maxAge: 0,
    expires: new Date(0),
  });
}

export function buildCoreSessionHeaders(session: CurrentUser, correlationId?: string): Record<string, string> {
  return {
    'X-Portal-User': session.userName,
    'X-Portal-Employee-Code': session.employeeCode,
    'X-Portal-Roles': session.roles.join(','),
    ...(correlationId ? { 'X-Correlation-Id': correlationId } : {}),
  };
}
