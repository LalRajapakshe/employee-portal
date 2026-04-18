namespace EmployeePortal.Application;

public interface ICurrentUserAccessor
{
    string? GetUserName();
    IReadOnlyList<string> GetRoles();
    string? GetCorrelationId();
    string? GetIpAddress();
    string? GetUserAgent();
    string? GetPath();
}

public interface IEmployeeReadRepository
{
    Task<EmployeeProfileDto?> GetEmployeeProfileByUserNameAsync(string userName, CancellationToken cancellationToken = default);
    Task<EmployeeProfileDto?> GetEmployeeProfileByEmployeeCodeAsync(string employeeCode, CancellationToken cancellationToken = default);
}

public interface IEmployeeProfileService
{
    Task<EmployeeProfileDto?> GetCurrentEmployeeProfileAsync(CancellationToken cancellationToken = default);
}

public sealed class EmployeeProfileService : IEmployeeProfileService
{
    private readonly IEmployeeReadRepository _employeeReadRepository;
    private readonly ICurrentUserAccessor _currentUserAccessor;
    private readonly IAuditLogService _auditLogService;

    public EmployeeProfileService(
        IEmployeeReadRepository employeeReadRepository,
        ICurrentUserAccessor currentUserAccessor,
        IAuditLogService auditLogService)
    {
        _employeeReadRepository = employeeReadRepository;
        _currentUserAccessor = currentUserAccessor;
        _auditLogService = auditLogService;
    }

    public async Task<EmployeeProfileDto?> GetCurrentEmployeeProfileAsync(CancellationToken cancellationToken = default)
    {
        var userName = _currentUserAccessor.GetUserName();
        if (string.IsNullOrWhiteSpace(userName))
        {
            await _auditLogService.WriteAsync(
                new AuditLogEntry(
                    EventType: "PROFILE.READ_SKIPPED",
                    EntityName: "EmployeeProfile",
                    EntityId: null,
                    PerformedBy: null,
                    Details: "Employee profile lookup skipped because no current user was resolved.",
                    IpAddress: _currentUserAccessor.GetIpAddress(),
                    UserAgent: _currentUserAccessor.GetUserAgent(),
                    SourceLayer: "Core Backend Layer",
                    CorrelationId: _currentUserAccessor.GetCorrelationId(),
                    RequestPath: _currentUserAccessor.GetPath(),
                    StatusCode: 401),
                cancellationToken);

            return null;
        }

        var profile = await _employeeReadRepository.GetEmployeeProfileByUserNameAsync(userName, cancellationToken);

        await _auditLogService.WriteAsync(
            new AuditLogEntry(
                EventType: profile is null ? "PROFILE.READ_NOT_FOUND" : "PROFILE.READ_SUCCESS",
                EntityName: "EmployeeProfile",
                EntityId: profile?.EmployeeCode,
                PerformedBy: userName,
                Details: profile is null
                    ? "Employee profile could not be resolved from Payroll read integration."
                    : $"Employee profile loaded for {profile.EmployeeCode}.",
                IpAddress: _currentUserAccessor.GetIpAddress(),
                UserAgent: _currentUserAccessor.GetUserAgent(),
                SourceLayer: "Core Backend Layer",
                CorrelationId: _currentUserAccessor.GetCorrelationId(),
                RequestPath: _currentUserAccessor.GetPath(),
                StatusCode: profile is null ? 404 : 200),
            cancellationToken);

        return profile;
    }
}
