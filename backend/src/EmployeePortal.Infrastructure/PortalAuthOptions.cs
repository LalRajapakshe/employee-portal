namespace EmployeePortal.Infrastructure;

public sealed class PortalAuthOptions
{
    public const string SectionName = "PortalAuth";

    public string ConnectionString { get; set; } = string.Empty;
    public List<DemoPortalUserOptions> DemoUsers { get; set; } = [];
}

public sealed class DemoPortalUserOptions
{
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string EmployeeCode { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public bool IsActive { get; set; } = true;
    public List<string> Roles { get; set; } = [];
    public List<string> Permissions { get; set; } = [];
}
