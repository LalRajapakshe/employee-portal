USE EmployeePortal;
GO

IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'portal_payroll')
BEGIN
    EXEC('CREATE SCHEMA portal_payroll AUTHORIZATION dbo');
END;
GO

CREATE OR ALTER VIEW portal_payroll.vw_EmployeeMasterRead
AS
    SELECT
        CAST(e.Id AS nvarchar(50)) AS EmployeeCode,
        e.FullName,
        CAST(d.Id AS nvarchar(50)) AS DepartmentCode,
        d.Name AS DepartmentName,
        CAST(g.Id AS nvarchar(50)) AS DesignationCode,
        g.Name AS DesignationName,
        CAST(e.JoinDate AS date) AS JoinDate,
        CAST(ed.JobStatusId AS nvarchar(50)) AS EmploymentStatus,
        CAST(CASE WHEN ed.JobStatusId <> 3 THEN 1 ELSE 0 END AS bit) AS IsPermanent,
        e.Email AS OfficialEmail,
        CAST(rs.ReportingManagerEmployeeCode AS nvarchar(50)) AS ReportingManagerEmployeeCode,
        CAST(rs.DirectorEmployeeCode AS nvarchar(50)) AS DirectorEmployeeCode,
        CAST(CASE WHEN ed.JobStatusId <> 3 THEN 1 ELSE 0 END AS bit) AS IsActive
    FROM [SlimDB].dbo.HREmployee e
    INNER JOIN [SlimDB].dbo.HREmpDepartment ed
        ON ed.EmpId = e.Id
    INNER JOIN [SlimDB].dbo.HRDepartment d
        ON d.Id = ed.DepartmentId
    INNER JOIN [SlimDB].dbo.HRDesignation g
        ON g.Id = ed.DesignationId
    LEFT JOIN [SlimDB].dbo.HREmployeeResponseHeads rs
        ON rs.EmpId = e.Id;
GO

CREATE OR ALTER PROCEDURE portal_payroll.usp_GetEmployeeProfileByEmployeeCode
    @EmployeeCode nvarchar(50)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (1)
        EmployeeCode,
        FullName,
        DepartmentCode,
        DepartmentName,
        DesignationCode,
        DesignationName,
        JoinDate,
        EmploymentStatus,
        IsPermanent,
        OfficialEmail,
        ReportingManagerEmployeeCode,
        DirectorEmployeeCode,
        IsActive
    FROM portal_payroll.vw_EmployeeMasterRead
    WHERE EmployeeCode = @EmployeeCode;
END;
GO

CREATE OR ALTER PROCEDURE portal_payroll.usp_GetEmployeeProfileByUserName
    @UserName nvarchar(150)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (1)
        p.EmployeeCode,
        p.FullName,
        p.DepartmentCode,
        p.DepartmentName,
        p.DesignationCode,
        p.DesignationName,
        p.JoinDate,
        p.EmploymentStatus,
        p.IsPermanent,
        p.OfficialEmail,
        p.ReportingManagerEmployeeCode,
        p.DirectorEmployeeCode,
        p.IsActive
    FROM portal.PortalUsers u
    INNER JOIN portal_payroll.vw_EmployeeMasterRead p
        ON p.EmployeeCode = u.EmployeeCode
    WHERE u.UserName = @UserName
      AND u.IsActive = 1;
END;
GO