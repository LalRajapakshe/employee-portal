/*
  Sprint 1 - Portal database baseline
  Platform: Microsoft SQL Server
*/

IF DB_ID('EmployeePortal') IS NULL
BEGIN
  CREATE DATABASE EmployeePortal;
END;
GO

USE EmployeePortal;
GO

IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'portal')
BEGIN
  EXEC('CREATE SCHEMA portal');
END;
GO

IF OBJECT_ID('portal.Roles', 'U') IS NULL
BEGIN
  CREATE TABLE portal.Roles (
    RoleId INT IDENTITY(1,1) PRIMARY KEY,
    RoleCode NVARCHAR(50) NOT NULL UNIQUE,
    RoleName NVARCHAR(100) NOT NULL,
    IsActive BIT NOT NULL CONSTRAINT DF_Roles_IsActive DEFAULT (1),
    CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_Roles_CreatedAt DEFAULT (SYSUTCDATETIME()),
    UpdatedAt DATETIME2(0) NULL
  );
END;
GO

IF OBJECT_ID('portal.Permissions', 'U') IS NULL
BEGIN
  CREATE TABLE portal.Permissions (
    PermissionId INT IDENTITY(1,1) PRIMARY KEY,
    PermissionCode NVARCHAR(100) NOT NULL UNIQUE,
    PermissionName NVARCHAR(150) NOT NULL,
    IsActive BIT NOT NULL CONSTRAINT DF_Permissions_IsActive DEFAULT (1),
    CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_Permissions_CreatedAt DEFAULT (SYSUTCDATETIME()),
    UpdatedAt DATETIME2(0) NULL
  );
END;
GO

IF OBJECT_ID('portal.PortalUsers', 'U') IS NULL
BEGIN
  CREATE TABLE portal.PortalUsers (
    PortalUserId INT IDENTITY(1,1) PRIMARY KEY,
    UserName NVARCHAR(100) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(500) NULL,
    EmployeeCode NVARCHAR(50) NOT NULL,
    Email NVARCHAR(255) NULL,
    IsActive BIT NOT NULL CONSTRAINT DF_PortalUsers_IsActive DEFAULT (1),
    LastLoginAt DATETIME2(0) NULL,
    CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_PortalUsers_CreatedAt DEFAULT (SYSUTCDATETIME()),
    UpdatedAt DATETIME2(0) NULL
  );
END;
GO

IF OBJECT_ID('portal.UserRoleMappings', 'U') IS NULL
BEGIN
  CREATE TABLE portal.UserRoleMappings (
    UserRoleMappingId INT IDENTITY(1,1) PRIMARY KEY,
    PortalUserId INT NOT NULL,
    RoleId INT NOT NULL,
    IsActive BIT NOT NULL CONSTRAINT DF_UserRoleMappings_IsActive DEFAULT (1),
    CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_UserRoleMappings_CreatedAt DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT FK_UserRoleMappings_PortalUsers FOREIGN KEY (PortalUserId) REFERENCES portal.PortalUsers(PortalUserId),
    CONSTRAINT FK_UserRoleMappings_Roles FOREIGN KEY (RoleId) REFERENCES portal.Roles(RoleId),
    CONSTRAINT UQ_UserRoleMappings UNIQUE (PortalUserId, RoleId)
  );
END;
GO

IF OBJECT_ID('portal.RolePermissionMappings', 'U') IS NULL
BEGIN
  CREATE TABLE portal.RolePermissionMappings (
    RolePermissionMappingId INT IDENTITY(1,1) PRIMARY KEY,
    RoleId INT NOT NULL,
    PermissionId INT NOT NULL,
    CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_RolePermissionMappings_CreatedAt DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT FK_RolePermissionMappings_Roles FOREIGN KEY (RoleId) REFERENCES portal.Roles(RoleId),
    CONSTRAINT FK_RolePermissionMappings_Permissions FOREIGN KEY (PermissionId) REFERENCES portal.Permissions(PermissionId),
    CONSTRAINT UQ_RolePermissionMappings UNIQUE (RoleId, PermissionId)
  );
END;
GO

IF OBJECT_ID('portal.EmployeePortalProfile', 'U') IS NULL
BEGIN
  CREATE TABLE portal.EmployeePortalProfile (
    EmployeePortalProfileId INT IDENTITY(1,1) PRIMARY KEY,
    EmployeeCode NVARCHAR(50) NOT NULL UNIQUE,
    PortalDisplayName NVARCHAR(150) NULL,
    PortalRoleNote NVARCHAR(250) NULL,
    NotificationEmail NVARCHAR(255) NULL,
    LastLoginAt DATETIME2(0) NULL,
    IsPortalEnabled BIT NOT NULL CONSTRAINT DF_EmployeePortalProfile_IsPortalEnabled DEFAULT (1),
    CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_EmployeePortalProfile_CreatedAt DEFAULT (SYSUTCDATETIME()),
    UpdatedAt DATETIME2(0) NULL
  );
END;
GO

IF OBJECT_ID('portal.AuditLogs', 'U') IS NULL
BEGIN
  CREATE TABLE portal.AuditLogs (
    AuditLogId BIGINT IDENTITY(1,1) PRIMARY KEY,
    EventType NVARCHAR(100) NOT NULL,
    EntityName NVARCHAR(100) NULL,
    EntityId NVARCHAR(100) NULL,
    PerformedBy NVARCHAR(100) NULL,
    EventUtc DATETIME2(0) NOT NULL CONSTRAINT DF_AuditLogs_EventUtc DEFAULT (SYSUTCDATETIME()),
    Details NVARCHAR(MAX) NULL,
    IpAddress NVARCHAR(50) NULL,
    UserAgent NVARCHAR(500) NULL
  );
END;
GO

IF OBJECT_ID('portal.ErrorLogs', 'U') IS NULL
BEGIN
  CREATE TABLE portal.ErrorLogs (
    ErrorLogId BIGINT IDENTITY(1,1) PRIMARY KEY,
    ErrorCode NVARCHAR(100) NULL,
    ErrorMessage NVARCHAR(1000) NOT NULL,
    StackTrace NVARCHAR(MAX) NULL,
    SourceLayer NVARCHAR(100) NULL,
    LoggedUtc DATETIME2(0) NOT NULL CONSTRAINT DF_ErrorLogs_LoggedUtc DEFAULT (SYSUTCDATETIME()),
    CorrelationId NVARCHAR(100) NULL
  );
END;
GO

CREATE INDEX IX_PortalUsers_EmployeeCode ON portal.PortalUsers(EmployeeCode);
CREATE INDEX IX_AuditLogs_EventUtc ON portal.AuditLogs(EventUtc);
CREATE INDEX IX_ErrorLogs_LoggedUtc ON portal.ErrorLogs(LoggedUtc);
GO
