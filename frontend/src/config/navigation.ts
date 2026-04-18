import { PORTAL_ROUTES } from '@/config/routes';

export type PortalNavigationItem = {
  href: string;
  label: string;
  requiredPermissions?: string[];
  requiredRoles?: string[];
};

export const PORTAL_NAVIGATION: PortalNavigationItem[] = PORTAL_ROUTES
  .filter((route) => route.showInNavigation !== false)
  .map((route) => ({
    href: route.href,
    label: route.label,
    requiredPermissions: route.requiredPermissions,
    requiredRoles: route.requiredRoles,
  }));
