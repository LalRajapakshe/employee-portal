import { cookies } from 'next/headers';
import type { CurrentUser } from '@/types/current-user';

export const PORTAL_SESSION_COOKIE = 'portal_session';

export async function getPortalSession(): Promise<CurrentUser | null> {
  const cookieStore = await cookies();
  const rawValue = cookieStore.get(PORTAL_SESSION_COOKIE)?.value;
  if (!rawValue) {
    return null;
  }

  try {
    return JSON.parse(Buffer.from(rawValue, 'base64url').toString('utf8')) as CurrentUser;
  } catch {
    return null;
  }
}

export function encodePortalSession(user: CurrentUser): string {
  return Buffer.from(JSON.stringify(user), 'utf8').toString('base64url');
}
