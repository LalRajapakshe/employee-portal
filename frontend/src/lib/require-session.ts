import { redirect } from 'next/navigation';
import { getPortalSession } from '@/lib/session';
import { hasAllPermissions, hasAnyRole } from '@/lib/access-control';
import type { CurrentUser } from '@/types/current-user';

type GuardOptions = {
  requiredPermissions?: string[];
  requiredRoles?: string[];
};

export async function requirePortalSession(options?: GuardOptions): Promise<CurrentUser> {
  const session = await getPortalSession();

  if (!session?.userName) {
    redirect('/login');
  }

  const permissionsOk = hasAllPermissions(session, options?.requiredPermissions);
  const rolesOk = hasAnyRole(session, options?.requiredRoles);

  if (!permissionsOk || !rolesOk) {
    redirect('/unauthorized');
  }

  return session;
}
