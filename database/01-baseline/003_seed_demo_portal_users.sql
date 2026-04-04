USE EmployeePortal;
GO

DECLARE @EmployeeRoleId INT = (SELECT RoleId FROM portal.Roles WHERE RoleCode = 'EMPLOYEE');
DECLARE @HrAdminRoleId INT = (SELECT RoleId FROM portal.Roles WHERE RoleCode = 'HR_ADMIN');
DECLARE @SysAdminRoleId INT = (SELECT RoleId FROM portal.Roles WHERE RoleCode = 'SYS_ADMIN');
DECLARE @AuthPermissionId INT = (SELECT PermissionId FROM portal.Permissions WHERE PermissionCode = 'AUTH.LOGIN');
DECLARE @ProfilePermissionId INT = (SELECT PermissionId FROM portal.Permissions WHERE PermissionCode = 'PROFILE.VIEW');
DECLARE @DashboardPermissionId INT = (SELECT PermissionId FROM portal.Permissions WHERE PermissionCode = 'DASHBOARD.VIEW');
DECLARE @AdminPermissionId INT = (SELECT PermissionId FROM portal.Permissions WHERE PermissionCode = 'ADMIN.ACCESS');

MERGE portal.PortalUsers AS target
USING (VALUES
  ('demo.user', '{plain}Password@123', 'EMP001', 'employee@example.com'),
  ('hr.admin', '{plain}Password@123', 'EMP900', 'hr.admin@example.com')
) AS source(UserName, PasswordHash, EmployeeCode, Email)
ON target.UserName = source.UserName
WHEN NOT MATCHED THEN
  INSERT (UserName, PasswordHash, EmployeeCode, Email)
  VALUES (source.UserName, source.PasswordHash, source.EmployeeCode, source.Email)
WHEN MATCHED THEN
  UPDATE SET
    PasswordHash = source.PasswordHash,
    EmployeeCode = source.EmployeeCode,
    Email = source.Email,
    UpdatedAt = SYSUTCDATETIME();
GO

MERGE portal.EmployeePortalProfile AS target
USING (VALUES
  ('EMP001', 'Demo Employee', 'Starter employee account', 'employee@example.com', 1),
  ('EMP900', 'HR Administrator', 'Starter HR admin account', 'hr.admin@example.com', 1)
) AS source(EmployeeCode, PortalDisplayName, PortalRoleNote, NotificationEmail, IsPortalEnabled)
ON target.EmployeeCode = source.EmployeeCode
WHEN NOT MATCHED THEN
  INSERT (EmployeeCode, PortalDisplayName, PortalRoleNote, NotificationEmail, IsPortalEnabled)
  VALUES (source.EmployeeCode, source.PortalDisplayName, source.PortalRoleNote, source.NotificationEmail, source.IsPortalEnabled)
WHEN MATCHED THEN
  UPDATE SET
    PortalDisplayName = source.PortalDisplayName,
    PortalRoleNote = source.PortalRoleNote,
    NotificationEmail = source.NotificationEmail,
    IsPortalEnabled = source.IsPortalEnabled,
    UpdatedAt = SYSUTCDATETIME();
GO

INSERT INTO portal.UserRoleMappings (PortalUserId, RoleId)
SELECT pu.PortalUserId, @EmployeeRoleId
FROM portal.PortalUsers pu
WHERE pu.UserName = 'demo.user'
  AND NOT EXISTS (
    SELECT 1 FROM portal.UserRoleMappings m WHERE m.PortalUserId = pu.PortalUserId AND m.RoleId = @EmployeeRoleId
  );

INSERT INTO portal.UserRoleMappings (PortalUserId, RoleId)
SELECT pu.PortalUserId, @HrAdminRoleId
FROM portal.PortalUsers pu
WHERE pu.UserName = 'hr.admin'
  AND NOT EXISTS (
    SELECT 1 FROM portal.UserRoleMappings m WHERE m.PortalUserId = pu.PortalUserId AND m.RoleId = @HrAdminRoleId
  );

INSERT INTO portal.UserRoleMappings (PortalUserId, RoleId)
SELECT pu.PortalUserId, @SysAdminRoleId
FROM portal.PortalUsers pu
WHERE pu.UserName = 'hr.admin'
  AND NOT EXISTS (
    SELECT 1 FROM portal.UserRoleMappings m WHERE m.PortalUserId = pu.PortalUserId AND m.RoleId = @SysAdminRoleId
  );
GO

INSERT INTO portal.RolePermissionMappings (RoleId, PermissionId)
SELECT @EmployeeRoleId, x.PermissionId
FROM (VALUES (@AuthPermissionId), (@ProfilePermissionId), (@DashboardPermissionId)) AS x(PermissionId)
WHERE x.PermissionId IS NOT NULL
  AND NOT EXISTS (
    SELECT 1 FROM portal.RolePermissionMappings rpm WHERE rpm.RoleId = @EmployeeRoleId AND rpm.PermissionId = x.PermissionId
  );

INSERT INTO portal.RolePermissionMappings (RoleId, PermissionId)
SELECT @HrAdminRoleId, x.PermissionId
FROM (VALUES (@AuthPermissionId), (@ProfilePermissionId), (@DashboardPermissionId), (@AdminPermissionId)) AS x(PermissionId)
WHERE x.PermissionId IS NOT NULL
  AND NOT EXISTS (
    SELECT 1 FROM portal.RolePermissionMappings rpm WHERE rpm.RoleId = @HrAdminRoleId AND rpm.PermissionId = x.PermissionId
  );

INSERT INTO portal.RolePermissionMappings (RoleId, PermissionId)
SELECT @SysAdminRoleId, x.PermissionId
FROM (VALUES (@AuthPermissionId), (@ProfilePermissionId), (@DashboardPermissionId), (@AdminPermissionId)) AS x(PermissionId)
WHERE x.PermissionId IS NOT NULL
  AND NOT EXISTS (
    SELECT 1 FROM portal.RolePermissionMappings rpm WHERE rpm.RoleId = @SysAdminRoleId AND rpm.PermissionId = x.PermissionId
  );
GO
