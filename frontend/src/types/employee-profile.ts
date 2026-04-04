export type EmployeeProfile = {
  employeeCode: string;
  fullName: string;
  department?: string | null;
  designation?: string | null;
  joinDate?: string | null;
  employmentStatus?: string | null;
  isPermanent: boolean;
  officialEmail?: string | null;
};
