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
        var normalized = sourceValue.Trim();
        return normalized.ToLowerInvariant() switch
        {
            "demo.user" or "emp001" => new EmployeeProfileDto(
                EmployeeCode: "EMP001",
                FullName: "Demo Employee",
                Department: "Finance",
                Designation: "Executive",
                JoinDate: new DateOnly(2024, 1, 15),
                EmploymentStatus: "Active",
                IsPermanent: true,
                OfficialEmail: "employee@example.com"),
            "director.user" or "emp500" => new EmployeeProfileDto(
                EmployeeCode: "EMP500",
                FullName: "Director Approver",
                Department: "Management",
                Designation: "Director",
                JoinDate: new DateOnly(2020, 4, 1),
                EmploymentStatus: "Active",
                IsPermanent: true,
                OfficialEmail: "director@example.com"),
            "hr.admin" or "emp900" => new EmployeeProfileDto(
                EmployeeCode: "EMP900",
                FullName: "HR Administrator",
                Department: "Human Resources",
                Designation: "HR Admin",
                JoinDate: new DateOnly(2019, 7, 10),
                EmploymentStatus: "Active",
                IsPermanent: true,
                OfficialEmail: "hr.admin@example.com"),
            _ => new EmployeeProfileDto(
                EmployeeCode: isEmployeeCode ? normalized : "EMP001",
                FullName: isEmployeeCode ? "Demo Employee" : normalized,
                Department: "Finance",
                Designation: "Executive",
                JoinDate: new DateOnly(2024, 1, 15),
                EmploymentStatus: "Active",
                IsPermanent: true,
                OfficialEmail: "employee@example.com")
        };
    }
}
