using System.Data;
using EmployeePortal.Application;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace EmployeePortal.Infrastructure;

public sealed class PayrollPeriodRepository : IPayrollPeriodRepository
{
    private readonly PayrollReadOptions _options;

    public PayrollPeriodRepository(
        IOptions<PayrollReadOptions> options)
    {
        _options = options.Value;
    }

    public async Task<IReadOnlyList<PayrollPeriod>> GetPeriodsAsync(
        int branchId,
        CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT
                Id,
                Desctiption
            FROM HRTransactionPeriod
            WHERE PayrollTypeId = @BranchId
            ORDER BY Id DESC;";

        await using var connection =
            new SqlConnection(_options.ConnectionString);

        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection)
        {
            CommandType = CommandType.Text
        };

        command.Parameters.AddWithValue("@BranchId", branchId);

        var periods = new List<PayrollPeriod>();

        await using var reader =
            await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            periods.Add(
                new PayrollPeriod(
                    Id: reader.GetInt32(reader.GetOrdinal("Id")),
                    Description:
                        reader["Desctiption"]?.ToString() ?? string.Empty
                )
            );
        }

        return periods;
    }
}