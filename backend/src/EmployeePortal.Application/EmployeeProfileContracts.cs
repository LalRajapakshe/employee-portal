namespace EmployeePortal.Application;

public interface ICurrentUserAccessor
{
    string? GetUserName();
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

    public EmployeeProfileService(
        IEmployeeReadRepository employeeReadRepository,
        ICurrentUserAccessor currentUserAccessor)
    {
        _employeeReadRepository = employeeReadRepository;
        _currentUserAccessor = currentUserAccessor;
    }

    public async Task<EmployeeProfileDto?> GetCurrentEmployeeProfileAsync(CancellationToken cancellationToken = default)
    {
        var userName = _currentUserAccessor.GetUserName();
        if (string.IsNullOrWhiteSpace(userName))
        {
            return null;
        }

        return await _employeeReadRepository.GetEmployeeProfileByUserNameAsync(userName, cancellationToken);
    }
}
