namespace EmployeePortal.Infrastructure;

public sealed class LoanOptions
{
    public const string SectionName = "Loan";
    public string ConnectionString { get; set; } = string.Empty;
    public decimal MaximumAmount { get; set; } = 500000m;
    public int MaximumMonths { get; set; } = 18;
    public decimal InterestRate { get; set; } = 12.00m;
    public bool RequirePermanentEmployee { get; set; } = true;
    public int MinimumServiceMonths { get; set; } = 6;
    public int WaitingMonthsAfterCompletion { get; set; } = 4;
    public string CurrencyCode { get; set; } = "LKR";
    public string FirstApproverRole { get; set; } = "DIRECTOR";
    public string SecondApproverRole { get; set; } = "HR_ADMIN";
}
