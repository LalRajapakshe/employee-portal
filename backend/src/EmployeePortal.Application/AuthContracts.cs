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
    string? UserAgent
);

public sealed record ErrorLogEntry(
    string? ErrorCode,
    string ErrorMessage,
    string? StackTrace,
    string SourceLayer,
    string? CorrelationId
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
                new AuditLogEntry("AUTH.LOGIN_FAILED", "PortalUser", null, userName, "Missing username or password.", null, null),
                cancellationToken);

            return (false, "Username and password are required.", null);
        }

        var user = await _authUserRepository.FindByUserNameAsync(userName, cancellationToken);
        if (user is null || !user.IsActive)
        {
            await _auditLogService.WriteAsync(
                new AuditLogEntry("AUTH.LOGIN_FAILED", "PortalUser", null, userName, "User not found or inactive.", null, null),
                cancellationToken);

            return (false, "Invalid username or password.", null);
        }

        if (!_passwordVerifier.Verify(request.Password, user.PasswordHash))
        {
            await _auditLogService.WriteAsync(
                new AuditLogEntry("AUTH.LOGIN_FAILED", "PortalUser", user.PortalUserId.ToString(), userName, "Password verification failed.", null, null),
                cancellationToken);

            return (false, "Invalid username or password.", null);
        }

        await _authUserRepository.RecordLastLoginAsync(user.UserName, cancellationToken);
        await _auditLogService.WriteAsync(
            new AuditLogEntry("AUTH.LOGIN_SUCCESS", "PortalUser", user.PortalUserId.ToString(), user.UserName, "User logged in successfully.", null, null),
            cancellationToken);

        return (true, null, ToCurrentUser(user));
    }

    public async Task<CurrentUserDto?> GetCurrentUserAsync(CancellationToken cancellationToken = default)
    {
        var userName = _currentUserAccessor.GetUserName();
        if (string.IsNullOrWhiteSpace(userName))
        {
            return null;
        }

        var user = await _authUserRepository.FindByUserNameAsync(userName, cancellationToken);
        return user is null || !user.IsActive ? null : ToCurrentUser(user);
    }

    private static CurrentUserDto ToCurrentUser(AuthenticatedPortalUser user)
        => new(
            UserName: user.UserName,
            DisplayName: user.DisplayName,
            EmployeeCode: user.EmployeeCode,
            Email: user.Email,
            Roles: user.Roles,
            Permissions: user.Permissions,
            IsAuthenticated: true);
}
