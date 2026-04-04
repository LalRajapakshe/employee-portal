USE EmployeePortal;
GO

MERGE portal.Roles AS target
USING (VALUES
  ('EMPLOYEE', 'Employee'),
  ('DIRECTOR', 'Director'),
  ('HR_USER', 'HR User'),
  ('HR_ADMIN', 'HR Admin'),
  ('PAYROLL_USER', 'Payroll User'),
  ('SYS_ADMIN', 'System Administrator')
) AS source(RoleCode, RoleName)
ON target.RoleCode = source.RoleCode
WHEN NOT MATCHED THEN
  INSERT (RoleCode, RoleName) VALUES (source.RoleCode, source.RoleName);
GO

MERGE portal.Permissions AS target
USING (VALUES
  ('AUTH.LOGIN', 'Login to portal'),
  ('PROFILE.VIEW', 'View employee profile'),
  ('DASHBOARD.VIEW', 'View dashboard'),
  ('ADMIN.ACCESS', 'Access admin area')
) AS source(PermissionCode, PermissionName)
ON target.PermissionCode = source.PermissionCode
WHEN NOT MATCHED THEN
  INSERT (PermissionCode, PermissionName) VALUES (source.PermissionCode, source.PermissionName);
GO
