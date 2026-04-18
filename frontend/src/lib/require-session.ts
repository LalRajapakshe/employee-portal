import { redirect } from 'next/navigation';
import { getPortalSession } from '@/lib/session';
import { getDefaultAuthorizedRoute, hasAllPermissions, hasAnyPermission, hasAnyRole } from '@/lib/access-control';
import type { CurrentUser } from '@/types/current-user';

type GuardOptions = {
  requiredPermissions?: string[];
  requiredAnyPermissions?: string[];
  requiredRoles?: string[];
};

export async function requirePortalSession(options?: GuardOptions): Promise<CurrentUser> {
  const session = await getPortalSession();

  if (!session?.userName) {
    redirect('/login');
  }

  const permissionsOk = hasAllPermissions(session, options?.requiredPermissions);
  const anyPermissionsOk = hasAnyPermission(session, options?.requiredAnyPermissions);
  const rolesOk = hasAnyRole(session, options?.requiredRoles);

  if (!permissionsOk || !anyPermissionsOk || !rolesOk) {
    const fallbackRoute = getDefaultAuthorizedRoute(session);
    if (fallbackRoute === '/unauthorized') {
      redirect('/unauthorized');
    }

    redirect(fallbackRoute);
  }

  return session;
}
