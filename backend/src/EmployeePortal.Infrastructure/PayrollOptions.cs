namespace EmployeePortal.Infrastructure;

public sealed class PayrollReadOptions
{
    public const string SectionName = "PayrollRead";

    public string ConnectionString { get; set; } = string.Empty;
    public string GetEmployeeProfileByUserNameProcedure { get; set; } = "portal_payroll.usp_GetEmployeeProfileByUserName";
    public string GetEmployeeProfileByEmployeeCodeProcedure { get; set; } = "portal_payroll.usp_GetEmployeeProfileByEmployeeCode";
}
