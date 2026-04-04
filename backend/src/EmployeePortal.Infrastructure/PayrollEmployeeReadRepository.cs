using System.Data;
using EmployeePortal.Application;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace EmployeePortal.Infrastructure;

public sealed class PayrollEmployeeReadRepository : IEmployeeReadRepository
{
    private readonly PayrollReadOptions _options;

    public PayrollEmployeeReadRepository(IOptions<PayrollReadOptions> options)
    {
        _options = options.Value;
    }

    public Task<EmployeeProfileDto?> GetEmployeeProfileByUserNameAsync(string userName, CancellationToken cancellationToken = default)
        => ExecuteSingleAsync(_options.GetEmployeeProfileByUserNameProcedure, "@UserName", userName, cancellationToken);

    public Task<EmployeeProfileDto?> GetEmployeeProfileByEmployeeCodeAsync(string employeeCode, CancellationToken cancellationToken = default)
        => ExecuteSingleAsync(_options.GetEmployeeProfileByEmployeeCodeProcedure, "@EmployeeCode", employeeCode, cancellationToken);

    private async Task<EmployeeProfileDto?> ExecuteSingleAsync(
        string procedureName,
        string parameterName,
        string parameterValue,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.ConnectionString))
        {
            return CreateFallbackProfile(parameterValue, parameterName == "@EmployeeCode");
        }

        await using var connection = new SqlConnection(_options.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(procedureName, connection)
        {
            CommandType = CommandType.StoredProcedure
        };
        command.Parameters.AddWithValue(parameterName, parameterValue);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new EmployeeProfileDto(
            EmployeeCode: reader["EmployeeCode"]?.ToString() ?? string.Empty,
            FullName: reader["FullName"]?.ToString() ?? string.Empty,
            Department: reader["DepartmentName"]?.ToString(),
            Designation: reader["DesignationName"]?.ToString(),
            JoinDate: reader["JoinDate"] is DateTime joinDate ? DateOnly.FromDateTime(joinDate) : null,
            EmploymentStatus: reader["EmploymentStatus"]?.ToString(),
            IsPermanent: reader["IsPermanent"] is bool isPermanent && isPermanent,
            OfficialEmail: reader["OfficialEmail"]?.ToString());
    }

    private static EmployeeProfileDto CreateFallbackProfile(string sourceValue, bool isEmployeeCode)
    {
        var employeeCode = isEmployeeCode ? sourceValue : "EMP001";
        var name = isEmployeeCode ? "Demo Employee" : sourceValue;

        return new EmployeeProfileDto(
            EmployeeCode: employeeCode,
            FullName: name,
            Department: "Finance",
            Designation: "Executive",
            JoinDate: new DateOnly(2024, 1, 15),
            EmploymentStatus: "Active",
            IsPermanent: true,
            OfficialEmail: "employee@example.com");
    }
}
