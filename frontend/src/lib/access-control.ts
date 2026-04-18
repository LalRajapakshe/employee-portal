import type { CurrentUser } from '@/types/current-user';
import { PORTAL_NAVIGATION, type PortalNavigationItem } from '@/config/navigation';
import { PORTAL_ROUTES, type PortalRouteDefinition } from '@/config/routes';

function normalizeCode(value: string): string {
  return value.trim().toUpperCase();
}

function getRoleSet(user: CurrentUser | null | undefined): Set<string> {
  return new Set((user?.roles ?? []).map(normalizeCode));
}

function getPermissionSet(user: CurrentUser | null | undefined): Set<string> {
  return new Set((user?.permissions ?? []).map(normalizeCode));
}

export function hasRole(user: CurrentUser | null | undefined, roleCode: string): boolean {
  return getRoleSet(user).has(normalizeCode(roleCode));
}

export function hasPermission(user: CurrentUser | null | undefined, permissionCode: string): boolean {
  return getPermissionSet(user).has(normalizeCode(permissionCode));
}

export function hasAnyRole(user: CurrentUser | null | undefined, roleCodes: string[] = []): boolean {
  if (roleCodes.length === 0) return true;
  const roleSet = getRoleSet(user);
  return roleCodes.some((roleCode) => roleSet.has(normalizeCode(roleCode)));
}

export function hasAnyPermission(user: CurrentUser | null | undefined, permissionCodes: string[] = []): boolean {
  if (permissionCodes.length === 0) return true;
  const permissionSet = getPermissionSet(user);
  return permissionCodes.some((permissionCode) => permissionSet.has(normalizeCode(permissionCode)));
}

export function hasAllPermissions(user: CurrentUser | null | undefined, permissionCodes: string[] = []): boolean {
  if (permissionCodes.length === 0) return true;
  const permissionSet = getPermissionSet(user);
  return permissionCodes.every((permissionCode) => permissionSet.has(normalizeCode(permissionCode)));
}

export function canAccessNavigationItem(user: CurrentUser | null | undefined, item: PortalNavigationItem): boolean {
  return hasAnyRole(user, item.requiredRoles) && hasAllPermissions(user, item.requiredPermissions);
}

export function getAllowedNavigation(user: CurrentUser | null | undefined): PortalNavigationItem[] {
  return PORTAL_NAVIGATION.filter((item) => canAccessNavigationItem(user, item));
}

export function canAccessRoute(user: CurrentUser | null | undefined, route: PortalRouteDefinition): boolean {
  return hasAnyRole(user, route.requiredRoles) && hasAllPermissions(user, route.requiredPermissions);
}

export function getAllowedRoutes(user: CurrentUser | null | undefined): PortalRouteDefinition[] {
  return PORTAL_ROUTES.filter((route) => canAccessRoute(user, route));
}

export function getDefaultAuthorizedRoute(user: CurrentUser | null | undefined): string {
  const firstAllowed = getAllowedRoutes(user)[0];
  return firstAllowed?.href ?? '/unauthorized';
}

export function getAccessMatrix(user: CurrentUser | null | undefined) {
  return PORTAL_ROUTES.map((route) => ({
    href: route.href,
    label: route.label,
    allowed: canAccessRoute(user, route),
    requiredPermissions: route.requiredPermissions ?? [],
    requiredRoles: route.requiredRoles ?? [],
  }));
}
