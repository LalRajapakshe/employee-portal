namespace EmployeePortal.Domain;

public sealed class PortalUser
{
    public int PortalUserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string EmployeeCode { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
