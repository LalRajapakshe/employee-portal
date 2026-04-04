/*
  Template only.
  Replace Payroll.dbo.EmployeeMaster and related columns with actual payroll objects.
*/

USE EmployeePortal;
GO

IF OBJECT_ID('portal.vw_PayrollEmployeeProfile', 'V') IS NOT NULL
BEGIN
  DROP VIEW portal.vw_PayrollEmployeeProfile;
END;
GO

CREATE VIEW portal.vw_PayrollEmployeeProfile
AS
SELECT
  e.EmployeeCode,
  e.FullName,
  e.DepartmentName,
  e.DesignationName,
  e.JoinDate,
  e.EmploymentStatus,
  CAST(CASE WHEN e.EmploymentType = 'Permanent' THEN 1 ELSE 0 END AS BIT) AS IsPermanent,
  e.OfficialEmail,
  e.ReportingManagerCode,
  e.DirectorCode
FROM Payroll.dbo.EmployeeMaster AS e;
GO
