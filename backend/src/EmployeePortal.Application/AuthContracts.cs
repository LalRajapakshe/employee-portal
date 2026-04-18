namespace EmployeePortal.Application;

public sealed record LoginRequestDto(string UserName, string Password);

public sealed record CurrentUserDto(
    string UserName,
    string DisplayName,
    string EmployeeCode,
    string? Email,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions,
    bool IsAuthenticated
);

public sealed record AuthenticatedPortalUser(
    int PortalUserId,
    string UserName,
    string PasswordHash,
    string EmployeeCode,
    string DisplayName,
    string? Email,
    bool IsActive,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions
);

public sealed record AuditLogEntry(
    string EventType,
    string? EntityName,
    string? EntityId,
    string? PerformedBy,
    string? Details,
    string? IpAddress,
    string? UserAgent,
    string SourceLayer,
    string? CorrelationId,
    string? RequestPath,
    int? StatusCode
);

public sealed record ErrorLogEntry(
    string? ErrorCode,
    string ErrorMessage,
    string? StackTrace,
    string SourceLayer,
    string? CorrelationId,
    string? UserName,
    string? RequestPath,
    string? IpAddress,
    string? UserAgent,
    int? StatusCode
);

public interface IAuthUserRepository
{
    Task<AuthenticatedPortalUser?> FindByUserNameAsync(string userName, CancellationToken cancellationToken = default);
    Task RecordLastLoginAsync(string userName, CancellationToken cancellationToken = default);
}

public interface IPasswordVerifier
{
    bool Verify(string providedPassword, string storedPasswordHash);
}

public interface IAuthenticationService
{
    Task<(bool Success, string? Message, CurrentUserDto? User)> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default);
    Task<CurrentUserDto?> GetCurrentUserAsync(CancellationToken cancellationToken = default);
}

public interface IAuditLogService
{
    Task WriteAsync(AuditLogEntry entry, CancellationToken cancellationToken = default);
}

public interface IErrorLogService
{
    Task WriteAsync(ErrorLogEntry entry, CancellationToken cancellationToken = default);
}

public sealed class AuthenticationService : IAuthenticationService
{
    private readonly IAuthUserRepository _authUserRepository;
    private readonly ICurrentUserAccessor _currentUserAccessor;
    private readonly IPasswordVerifier _passwordVerifier;
    private readonly IAuditLogService _auditLogService;

    public AuthenticationService(
        IAuthUserRepository authUserRepository,
        ICurrentUserAccessor currentUserAccessor,
        IPasswordVerifier passwordVerifier,
        IAuditLogService auditLogService)
    {
        _authUserRepository = authUserRepository;
        _currentUserAccessor = currentUserAccessor;
        _passwordVerifier = passwordVerifier;
        _auditLogService = auditLogService;
    }

    public async Task<(bool Success, string? Message, CurrentUserDto? User)> LoginAsync(
        LoginRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var userName = request.UserName?.Trim();
        if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(request.Password))
        {
            await _auditLogService.WriteAsync(
                BuildAuditLog(
                    eventType: "AUTH.LOGIN_FAILED",
                    entityId: null,
                    performedBy: userName,
                    details: "Missing username or password.",
                    statusCode: 400),
                cancellationToken);

            return (false, "Username and password are required.", null);
        }

        var user = await _authUserRepository.FindByUserNameAsync(userName, cancellationToken);
        if (user is null || !user.IsActive)
        {
            await _auditLogService.WriteAsync(
                BuildAuditLog(
                    eventType: "AUTH.LOGIN_FAILED",
                    entityId: null,
                    performedBy: userName,
                    details: "User not found or inactive.",
                    statusCode: 401),
                cancellationToken);

            return (false, "Invalid username or password.", null);
        }

        if (!_passwordVerifier.Verify(request.Password, user.PasswordHash))
        {
            await _auditLogService.WriteAsync(
                BuildAuditLog(
                    eventType: "AUTH.LOGIN_FAILED",
                    entityId: user.PortalUserId.ToString(),
                    performedBy: userName,
                    details: "Password verification failed.",
                    statusCode: 401),
                cancellationToken);

            return (false, "Invalid username or password.", null);
        }

        await _authUserRepository.RecordLastLoginAsync(user.UserName, cancellationToken);
        await _auditLogService.WriteAsync(
            BuildAuditLog(
                eventType: "AUTH.LOGIN_SUCCESS",
                entityId: user.PortalUserId.ToString(),
                performedBy: user.UserName,
                details: "User logged in successfully.",
                statusCode: 200),
            cancellationToken);

        return (true, null, ToCurrentUser(user));
    }

    public async Task<CurrentUserDto?> GetCurrentUserAsync(CancellationToken cancellationToken = default)
    {
        var userName = _currentUserAccessor.GetUserName();
        if (string.IsNullOrWhiteSpace(userName))
        {
            await _auditLogService.WriteAsync(
                BuildAuditLog(
                    eventType: "AUTH.CURRENT_USER_SKIPPED",
                    entityId: null,
                    performedBy: null,
                    details: "Current user lookup skipped because no user name was supplied.",
                    statusCode: 401),
                cancellationToken);

            return null;
        }

        var user = await _authUserRepository.FindByUserNameAsync(userName, cancellationToken);
        if (user is null || !user.IsActive)
        {
            await _auditLogService.WriteAsync(
                BuildAuditLog(
                    eventType: "AUTH.CURRENT_USER_NOT_FOUND",
                    entityId: null,
                    performedBy: userName,
                    details: "Current user lookup failed.",
                    statusCode: 404),
                cancellationToken);

            return null;
        }

        await _auditLogService.WriteAsync(
            BuildAuditLog(
                eventType: "AUTH.CURRENT_USER_SUCCESS",
                entityId: user.PortalUserId.ToString(),
                performedBy: user.UserName,
                details: "Current user resolved successfully.",
                statusCode: 200),
            cancellationToken);

        return ToCurrentUser(user);
    }

    private AuditLogEntry BuildAuditLog(string eventType, string? entityId, string? performedBy, string details, int statusCode)
        => new(
            EventType: eventType,
            EntityName: "PortalUser",
            EntityId: entityId,
            PerformedBy: performedBy,
            Details: details,
            IpAddress: _currentUserAccessor.GetIpAddress(),
            UserAgent: _currentUserAccessor.GetUserAgent(),
            SourceLayer: "Core Backend Layer",
            CorrelationId: _currentUserAccessor.GetCorrelationId(),
            RequestPath: _currentUserAccessor.GetPath(),
            StatusCode: statusCode);

    private static CurrentUserDto ToCurrentUser(AuthenticatedPortalUser user)
        => new(
            UserName: user.UserName,
            DisplayName: user.DisplayName,
            EmployeeCode: user.EmployeeCode,
            Email: user.Email,
            Roles: NormalizeCodes(user.Roles),
            Permissions: NormalizeCodes(user.Permissions),
            IsAuthenticated: true);

    private static IReadOnlyList<string> NormalizeCodes(IReadOnlyList<string> values)
        => values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim().ToUpperInvariant())
            .Distinct(StringComparer.Ordinal)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();
}
