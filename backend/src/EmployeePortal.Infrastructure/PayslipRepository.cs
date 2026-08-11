using System.Data;
using EmployeePortal.Application;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace EmployeePortal.Infrastructure;

public sealed class PayslipRepository : IPayslipRepository
{
    private readonly PayrollReadOptions _options;

    public PayslipRepository(
        IOptions<PayrollReadOptions> options)
    {
        _options = options.Value;
    }

    public async Task<IReadOnlyList<PayslipLine>> GetPayslipAsync(
        int monthId,
        int employeeId,
        CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT
                S.plineNo,
                S.narration,
                S.[amt1] AS Qty,
                S.[amt2] AS Earn,
                S.[amt3] AS Paid
            FROM HRpaySlip S
            INNER JOIN HRProcessHeader P
                ON P.Id = S.processId
            WHERE P.MonthId = @MonthId
              AND S.empId = @EmployeeId
            ORDER BY S.plineNo;";

        await using var connection =
            new SqlConnection(_options.ConnectionString);

        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection)
        {
            CommandType = CommandType.Text
        };

        command.Parameters.AddWithValue("@MonthId", monthId);
        command.Parameters.AddWithValue("@EmployeeId", employeeId);

        var lines = new List<PayslipLine>();

        await using var reader =
            await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            lines.Add(
                new PayslipLine(
                    PlineNo: reader.GetInt32(
                        reader.GetOrdinal("plineNo")),

                    Narration:
                        reader["narration"]?.ToString() ?? string.Empty,

                    Qty:
                        reader["Qty"] == DBNull.Value
                            ? 0
                            : Convert.ToDecimal(reader["Qty"]),

                    Earn:
                        reader["Earn"]?.ToString() ?? string.Empty,

                    Paid:
                        reader["Paid"] == DBNull.Value
                            ? 0
                            : Convert.ToDecimal(reader["Paid"])
                )
            );
        }

        return lines;
    }
}