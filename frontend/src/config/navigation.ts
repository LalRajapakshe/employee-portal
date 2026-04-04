export type PortalNavigationItem = {
  href: string;
  label: string;
  requiredPermissions?: string[];
  requiredRoles?: string[];
};

export const PORTAL_NAVIGATION: PortalNavigationItem[] = [
  {
    href: '/',
    label: 'Dashboard',
    requiredPermissions: ['DASHBOARD.VIEW'],
  },
  {
    href: '/profile',
    label: 'My Profile',
    requiredPermissions: ['PROFILE.VIEW'],
  },
  {
    href: '/admin',
    label: 'Admin',
    requiredPermissions: ['ADMIN.ACCESS'],
  },
];
