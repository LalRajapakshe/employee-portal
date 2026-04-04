export type CurrentUser = {
  userName: string;
  displayName: string;
  employeeCode: string;
  email?: string | null;
  roles: string[];
  permissions: string[];
  isAuthenticated: boolean;
};
