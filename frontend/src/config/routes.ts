import { PERMISSIONS } from '@/config/permissions';

export type PortalRouteDefinition = {
  href: string;
  label: string;
  requiredPermissions?: string[];
  requiredRoles?: string[];
  showInNavigation?: boolean;
};

export const PORTAL_ROUTES: PortalRouteDefinition[] = [
  { href: '/', label: 'Dashboard', requiredPermissions: [PERMISSIONS.DASHBOARD_VIEW], showInNavigation: true },
  { href: '/profile', label: 'My Profile', requiredPermissions: [PERMISSIONS.PROFILE_VIEW], showInNavigation: true },

  { href: '/salary-advance', label: 'Salary Advances', requiredPermissions: [PERMISSIONS.SALARY_ADVANCE_VIEW_SELF], showInNavigation: true },
  { href: '/salary-advance/new', label: 'New Salary Advance', requiredPermissions: [PERMISSIONS.SALARY_ADVANCE_CREATE], showInNavigation: false },

  { href: '/loans', label: 'Employee Loans', requiredPermissions: [PERMISSIONS.LOAN_VIEW_SELF], showInNavigation: true },
  { href: '/loans/new', label: 'New Loan', requiredPermissions: [PERMISSIONS.LOAN_CREATE], showInNavigation: false },
  { href: '/loans/[requestId]', label: 'Loan Detail', requiredPermissions: [PERMISSIONS.LOAN_VIEW_SELF], showInNavigation: false },
  { href: '/loans/[requestId]/edit', label: 'Edit Loan', requiredPermissions: [PERMISSIONS.LOAN_CREATE], showInNavigation: false },
  { href: '/loans/[requestId]/print', label: 'Print Loan', requiredPermissions: [PERMISSIONS.LOAN_VIEW_SELF], showInNavigation: false },

  { href: '/approvals', label: 'Approvals Inbox', requiredPermissions: [PERMISSIONS.APPROVAL_INBOX_VIEW], showInNavigation: true },
  { href: '/loans/approvals', label: 'Loan Approvals', requiredRoles: ['DIRECTOR', 'HR_ADMIN'], requiredPermissions: [], showInNavigation: true },

  { href: '/notifications', label: 'Notifications', requiredPermissions: [PERMISSIONS.NOTIFICATION_VIEW], showInNavigation: true },
  { href: '/test/profile-flow', label: 'Profile Flow Test', requiredPermissions: [PERMISSIONS.PROFILE_VIEW], showInNavigation: true },
  { href: '/test/request-context', label: 'Request Context Test', requiredPermissions: [PERMISSIONS.DASHBOARD_VIEW], showInNavigation: true },
  { href: '/test/access-matrix', label: 'Access Matrix', requiredPermissions: [PERMISSIONS.DASHBOARD_VIEW], showInNavigation: true },
  { href: '/admin', label: 'Admin', requiredPermissions: [PERMISSIONS.ADMIN_ACCESS], showInNavigation: true },
];