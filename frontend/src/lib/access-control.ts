import type { CurrentUser } from '@/types/current-user';
import { PORTAL_NAVIGATION, type PortalNavigationItem } from '@/config/navigation';

export function hasRole(user: CurrentUser | null | undefined, roleCode: string): boolean {
  if (!user) return false;
  return user.roles.some((role) => role.toUpperCase() === roleCode.toUpperCase());
}

export function hasPermission(user: CurrentUser | null | undefined, permissionCode: string): boolean {
  if (!user) return false;
  return user.permissions.some((permission) => permission.toUpperCase() === permissionCode.toUpperCase());
}

export function hasAnyRole(user: CurrentUser | null | undefined, roleCodes: string[] = []): boolean {
  if (roleCodes.length === 0) return true;
  return roleCodes.some((roleCode) => hasRole(user, roleCode));
}

export function hasAllPermissions(user: CurrentUser | null | undefined, permissionCodes: string[] = []): boolean {
  if (permissionCodes.length === 0) return true;
  return permissionCodes.every((permissionCode) => hasPermission(user, permissionCode));
}

export function canAccessNavigationItem(user: CurrentUser | null | undefined, item: PortalNavigationItem): boolean {
  return hasAnyRole(user, item.requiredRoles) && hasAllPermissions(user, item.requiredPermissions);
}

export function getAllowedNavigation(user: CurrentUser | null | undefined): PortalNavigationItem[] {
  return PORTAL_NAVIGATION.filter((item) => canAccessNavigationItem(user, item));
}
