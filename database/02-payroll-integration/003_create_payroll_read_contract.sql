/*
Step 3: Payroll employee master read integration contract
--------------------------------------------------------
Purpose:
- define a safe read-only contract from Payroll to Employee Portal
- isolate portal code from direct dependency on payroll base tables
- provide stable objects for employee profile and future eligibility rules

Instructions:
1. Replace [PayrollDb] and payroll source table/column names with the actual payroll objects.
2. Prefer granting SELECT only on the portal_payroll schema views to the portal app user.
3. Do not grant direct table access to payroll transactional tables from the portal.
*/

IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'portal_payroll')
    EXEC('CREATE SCHEMA portal_payroll');
GO

CREATE OR ALTER VIEW portal_payroll.vw_EmployeeMasterRead
AS
    SELECT
        e.Id,
        e.FullName,
        d.Id AS DepartmentId,
        d.Name AS DepartmentName,
        g.id AS DesignationId,
        g.Name AS DesignationName,
        CAST(e.JoinDate AS date) AS JoinDate,
        ed.JobStatusId,
        CAST(CASE WHEN ed.JobStatusId <> 3  THEN 1 ELSE 0 END AS bit) AS IsPermanent,
        e.Email AS OfficialEmail,
        rs.ReportingManagerEmployeeCode AS ReportingManagerEmployeeCode,
        re.DirectorEmployeeCode AS DirectorEmployeeCode,
        CAST(CASE WHEN ed.JobStatusId <> 3 THEN 1 ELSE 0 END AS bit) AS IsActive
    FROM [PayrollDB].dbo.HREmployee e
    inner JOIN [PayrollDB].dbo.HREmpDepartment ed
        ON ed.EmpId = e.Id
    inner JOIN [PayrollDB].dbo.HRDepartment d
        ON d.Id = ed.DepartmentId
    inner JOIN [PayrollDB].dbo.HRDesignation g
        ON g.Id = ed.DesignationId
    left join [PayrollDB].dbo.HREmployeeResponseHeads rs
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

/* Suggested security pattern
GRANT SELECT ON OBJECT::portal_payroll.vw_EmployeeMasterRead TO [portal_app_user];
GRANT EXECUTE ON OBJECT::portal_payroll.usp_GetEmployeeProfileByEmployeeCode TO [portal_app_user];
GRANT EXECUTE ON OBJECT::portal_payroll.usp_GetEmployeeProfileByUserName TO [portal_app_user];
*/
