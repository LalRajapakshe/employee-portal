namespace EmployeePortal.Infrastructure;

public sealed class SalaryAdvanceOptions
{
    public const string SectionName = "SalaryAdvance";
    public string ConnectionString { get; set; } = string.Empty;
    public decimal MaximumAmount { get; set; } = 75000m;
    public bool RequirePermanentEmployee { get; set; } = true;
    public string CurrencyCode { get; set; } = "LKR";
    public string FirstApproverRole { get; set; } = "DIRECTOR";
    public string SecondApproverRole { get; set; } = "HR_ADMIN";
}
