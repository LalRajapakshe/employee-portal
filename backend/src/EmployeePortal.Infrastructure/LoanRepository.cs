using System.Data;
using System.Reflection;
using EmployeePortal.Application;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace EmployeePortal.Infrastructure;

public sealed class LoanRepository : ILoanRepository
{
    private readonly LoanOptions _options;

    public LoanRepository(IOptions<LoanOptions> options)
    {
        _options = options.Value;
    }

    public Task<LoanPolicyDto> GetPolicyAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(new LoanPolicyDto(
            MaximumAmount: _options.MaximumAmount,
            MaximumMonths: _options.MaximumMonths,
            InterestRate: _options.InterestRate,
            RequirePermanentEmployee: _options.RequirePermanentEmployee,
            MinimumServiceMonths: _options.MinimumServiceMonths,
            WaitingMonthsAfterCompletion: _options.WaitingMonthsAfterCompletion,
            FirstApproverRole: _options.FirstApproverRole.ToUpperInvariant(),
            SecondApproverRole: _options.SecondApproverRole.ToUpperInvariant(),
            CurrencyCode: _options.CurrencyCode));

    public async Task<LoanRequestDto?> GetByIdAsync(Guid requestId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
SELECT
    RequestId,
    RequestNumber,
    EmployeeCode,
    EmployeeName,
    RequestedAmount,
    InterestRate,
    InstallmentMonths,
    MonthlyInstallment,
    TotalRepayableAmount,
    OutstandingBalance,
    CurrencyCode,
    Reason,
    Status,
    PendingStageNumber,
    PendingWithRole,
    PayrollHandoffStatus,
    CreatedAtUtc,
    UpdatedAtUtc,
    SubmittedAtUtc,
    ApprovedAtUtc,
    CompletedAtUtc
FROM portal.LoanRequests
WHERE RequestId = @RequestId;";

        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection) { CommandType = CommandType.Text };
        command.Parameters.AddWithValue("@RequestId", requestId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        var repaymentSchedule = await GetRepaymentScheduleAsync(requestId, cancellationToken);
        var workflowActions = await GetWorkflowActionsAsync(requestId, cancellationToken);

        return new LoanRequestDto(
            RequestId: reader.GetGuid(reader.GetOrdinal("RequestId")),
            RequestNumber: ReadString(reader, "RequestNumber"),
            EmployeeCode: ReadString(reader, "EmployeeCode"),
            EmployeeName: ReadString(reader, "EmployeeName"),
            RequestedAmount: reader.GetDecimal(reader.GetOrdinal("RequestedAmount")),
            InterestRate: reader.GetDecimal(reader.GetOrdinal("InterestRate")),
            InstallmentMonths: reader.GetInt32(reader.GetOrdinal("InstallmentMonths")),
            MonthlyInstallment: reader.GetDecimal(reader.GetOrdinal("MonthlyInstallment")),
            TotalRepayableAmount: reader.GetDecimal(reader.GetOrdinal("TotalRepayableAmount")),
            OutstandingBalance: reader.GetDecimal(reader.GetOrdinal("OutstandingBalance")),
            CurrencyCode: ReadString(reader, "CurrencyCode"),
            Reason: ReadNullableString(reader, "Reason"),
            Status: ReadString(reader, "Status"),
            PendingStageNumber: reader.GetInt32(reader.GetOrdinal("PendingStageNumber")),
            PendingWithRole: ReadNullableString(reader, "PendingWithRole"),
            PayrollHandoffStatus: ReadString(reader, "PayrollHandoffStatus"),
            CreatedAtUtc: ReadDateTimeOffset(reader, "CreatedAtUtc"),
            UpdatedAtUtc: ReadDateTimeOffset(reader, "UpdatedAtUtc"),
            SubmittedAtUtc: ReadNullableDateTimeOffset(reader, "SubmittedAtUtc"),
            ApprovedAtUtc: ReadNullableDateTimeOffset(reader, "ApprovedAtUtc"),
            CompletedAtUtc: ReadNullableDateTimeOffset(reader, "CompletedAtUtc"),
            RepaymentSchedule: repaymentSchedule,
            WorkflowActions: workflowActions,
            ValidationMessages: Array.Empty<string>());
    }

    public async Task<IReadOnlyList<LoanSummaryDto>> ListForEmployeeAsync(string employeeCode, CancellationToken cancellationToken = default)
    {
        const string sql = @"
SELECT
    RequestId,
    RequestNumber,
    RequestedAmount,
    InstallmentMonths,
    MonthlyInstallment,
    OutstandingBalance,
    Status,
    PayrollHandoffStatus,
    CreatedAtUtc,
    SubmittedAtUtc
FROM portal.LoanRequests
WHERE EmployeeCode = @EmployeeCode
ORDER BY CreatedAtUtc DESC;";

        var results = new List<LoanSummaryDto>();
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection) { CommandType = CommandType.Text };
        command.Parameters.AddWithValue("@EmployeeCode", employeeCode);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new LoanSummaryDto(
                RequestId: reader.GetGuid(reader.GetOrdinal("RequestId")),
                RequestNumber: ReadString(reader, "RequestNumber"),
                RequestedAmount: reader.GetDecimal(reader.GetOrdinal("RequestedAmount")),
                InstallmentMonths: reader.GetInt32(reader.GetOrdinal("InstallmentMonths")),
                MonthlyInstallment: reader.GetDecimal(reader.GetOrdinal("MonthlyInstallment")),
                OutstandingBalance: reader.GetDecimal(reader.GetOrdinal("OutstandingBalance")),
                Status: ReadString(reader, "Status"),
                PayrollHandoffStatus: ReadString(reader, "PayrollHandoffStatus"),
                CreatedAtUtc: ReadDateTimeOffset(reader, "CreatedAtUtc"),
                SubmittedAtUtc: ReadNullableDateTimeOffset(reader, "SubmittedAtUtc")));
        }

        return results;
    }

    public async Task<IReadOnlyList<ApprovalInboxItemDto>> ListPendingApprovalsAsync(IReadOnlyList<string> roles, CancellationToken cancellationToken = default)
    {
        var upperRoles = roles.Select(x => x.ToUpperInvariant()).ToArray();
        if (upperRoles.Length == 0)
        {
            return Array.Empty<ApprovalInboxItemDto>();
        }

        var sql = $@"
SELECT
    RequestId,
    RequestNumber,
    EmployeeCode,
    EmployeeName,
    RequestedAmount,
    Status,
    PendingStageNumber,
    PendingWithRole,
    SubmittedAtUtc,
    CreatedAtUtc
FROM portal.LoanRequests
WHERE PendingWithRole IN ({string.Join(",", upperRoles.Select((_, index) => $"@Role{index}"))})
ORDER BY ISNULL(SubmittedAtUtc, CreatedAtUtc) DESC;";

        var results = new List<ApprovalInboxItemDto>();
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection) { CommandType = CommandType.Text };
        for (var i = 0; i < upperRoles.Length; i++)
        {
            command.Parameters.AddWithValue($"@Role{i}", upperRoles[i]);
        }

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new ApprovalInboxItemDto(
                reader.GetGuid(reader.GetOrdinal("RequestId")),
                ModuleCode: "LOAN",
                ReadString(reader, "RequestNumber"),
                ReadString(reader, "EmployeeCode"),
                ReadString(reader, "EmployeeName"),
                reader.GetDecimal(reader.GetOrdinal("RequestedAmount")),
                ReadString(reader, "Status"),
                reader.GetInt32(reader.GetOrdinal("PendingStageNumber")),
                ReadNullableString(reader, "PendingWithRole") ?? string.Empty,
                ReadNullableDateTimeOffset(reader, "SubmittedAtUtc") ?? ReadDateTimeOffset(reader, "CreatedAtUtc"),
                Summary: $"{ReadString(reader, "EmployeeName")} requested {reader.GetDecimal(reader.GetOrdinal("RequestedAmount")):0.00} {_options.CurrencyCode}"));
        }

        return results;
    }

    public async Task<LoanRequestDto> SaveDraftAsync(LoanRequestDto request, CancellationToken cancellationToken = default)
    {
        const string sql = @"
INSERT INTO portal.LoanRequests
(
    RequestId, RequestNumber, EmployeeCode, EmployeeName, RequestedAmount, InterestRate,
    InstallmentMonths, MonthlyInstallment, TotalRepayableAmount, OutstandingBalance,
    CurrencyCode, Reason, Status, PendingStageNumber, PendingWithRole, PayrollHandoffStatus,
    CreatedAtUtc, UpdatedAtUtc, SubmittedAtUtc, ApprovedAtUtc, CompletedAtUtc
)
VALUES
(
    @RequestId, @RequestNumber, @EmployeeCode, @EmployeeName, @RequestedAmount, @InterestRate,
    @InstallmentMonths, @MonthlyInstallment, @TotalRepayableAmount, @OutstandingBalance,
    @CurrencyCode, @Reason, @Status, @PendingStageNumber, @PendingWithRole, @PayrollHandoffStatus,
    @CreatedAtUtc, @UpdatedAtUtc, @SubmittedAtUtc, @ApprovedAtUtc, @CompletedAtUtc
);";

        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        await using (var command = new SqlCommand(sql, connection, (SqlTransaction)transaction) { CommandType = CommandType.Text })
        {
            MapLoanRequestParameters(command, request);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        await ReplaceRepaymentScheduleAsync(connection, (SqlTransaction)transaction, request.RequestId, request.RepaymentSchedule, cancellationToken);
        await SyncWorkflowArtifactsAsync(connection, (SqlTransaction)transaction, request.RequestId, request.WorkflowActions, cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        return request;
    }

    public async Task<LoanRequestDto> UpdateAsync(LoanRequestDto request, CancellationToken cancellationToken = default)
    {
        const string sql = @"
UPDATE portal.LoanRequests
SET
    RequestedAmount = @RequestedAmount,
    InterestRate = @InterestRate,
    InstallmentMonths = @InstallmentMonths,
    MonthlyInstallment = @MonthlyInstallment,
    TotalRepayableAmount = @TotalRepayableAmount,
    OutstandingBalance = @OutstandingBalance,
    CurrencyCode = @CurrencyCode,
    Reason = @Reason,
    Status = @Status,
    PendingStageNumber = @PendingStageNumber,
    PendingWithRole = @PendingWithRole,
    PayrollHandoffStatus = @PayrollHandoffStatus,
    UpdatedAtUtc = @UpdatedAtUtc,
    SubmittedAtUtc = @SubmittedAtUtc,
    ApprovedAtUtc = @ApprovedAtUtc,
    CompletedAtUtc = @CompletedAtUtc
WHERE RequestId = @RequestId;";

        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        await using (var command = new SqlCommand(sql, connection, (SqlTransaction)transaction) { CommandType = CommandType.Text })
        {
            MapLoanRequestParameters(command, request);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        await ReplaceRepaymentScheduleAsync(connection, (SqlTransaction)transaction, request.RequestId, request.RepaymentSchedule, cancellationToken);
        await SyncWorkflowArtifactsAsync(connection, (SqlTransaction)transaction, request.RequestId, request.WorkflowActions, cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        return request;
    }

    public async Task<IReadOnlyList<LoanRepaymentScheduleItemDto>> GetRepaymentScheduleAsync(Guid requestId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
SELECT ScheduleItemId, InstallmentNumber, DueDate, OpeningBalance, PrincipalComponent, InterestComponent, InstallmentAmount, ClosingBalance, Status
FROM portal.LoanRepaymentSchedules
WHERE RequestId = @RequestId
ORDER BY InstallmentNumber;";

        var results = new List<LoanRepaymentScheduleItemDto>();
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection) { CommandType = CommandType.Text };
        command.Parameters.AddWithValue("@RequestId", requestId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var dueDate = reader.GetDateTime(reader.GetOrdinal("DueDate"));
            results.Add(new LoanRepaymentScheduleItemDto(
                ScheduleItemId: reader.GetGuid(reader.GetOrdinal("ScheduleItemId")),
                InstallmentNumber: reader.GetInt32(reader.GetOrdinal("InstallmentNumber")),
                DueDateUtc: new DateTimeOffset(DateTime.SpecifyKind(dueDate, DateTimeKind.Utc)),
                OpeningBalance: reader.GetDecimal(reader.GetOrdinal("OpeningBalance")),
                PrincipalComponent: reader.GetDecimal(reader.GetOrdinal("PrincipalComponent")),
                InterestComponent: reader.GetDecimal(reader.GetOrdinal("InterestComponent")),
                InstallmentAmount: reader.GetDecimal(reader.GetOrdinal("InstallmentAmount")),
                ClosingBalance: reader.GetDecimal(reader.GetOrdinal("ClosingBalance")),
                Status: ReadString(reader, "Status")));
        }

        return results;
    }

    public async Task<bool> HasActiveLoanAsync(string employeeCode, CancellationToken cancellationToken = default)
    {
        const string sql = @"
SELECT COUNT(1)
FROM portal.LoanRequests
WHERE EmployeeCode = @EmployeeCode
  AND Status IN ('PENDING_FIRST_APPROVAL', 'PENDING_SECOND_APPROVAL', 'APPROVED', 'ACTIVE', 'DISBURSED')
  AND OutstandingBalance > 0;";

        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection) { CommandType = CommandType.Text };
        command.Parameters.AddWithValue("@EmployeeCode", employeeCode);
        var count = (int)(await command.ExecuteScalarAsync(cancellationToken) ?? 0);
        return count > 0;
    }

    public async Task<LoanCompletionInfoDto?> GetMostRecentCompletedLoanAsync(string employeeCode, CancellationToken cancellationToken = default)
    {
        const string sql = @"
SELECT TOP (1) CompletedAtUtc
FROM portal.LoanRequests
WHERE EmployeeCode = @EmployeeCode
  AND CompletedAtUtc IS NOT NULL
ORDER BY CompletedAtUtc DESC;";

        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection) { CommandType = CommandType.Text };
        command.Parameters.AddWithValue("@EmployeeCode", employeeCode);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new LoanCompletionInfoDto(ReadNullableDateTimeOffset(reader, "CompletedAtUtc"));
    }

    public async Task<decimal> GetOutstandingBalanceAsync(string employeeCode, CancellationToken cancellationToken = default)
    {
        const string sql = @"
SELECT ISNULL(SUM(OutstandingBalance), 0)
FROM portal.LoanRequests
WHERE EmployeeCode = @EmployeeCode
  AND Status IN ('PENDING_FIRST_APPROVAL', 'PENDING_SECOND_APPROVAL', 'APPROVED', 'ACTIVE', 'DISBURSED');";

        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection) { CommandType = CommandType.Text };
        command.Parameters.AddWithValue("@EmployeeCode", employeeCode);
        var value = await command.ExecuteScalarAsync(cancellationToken);
        return value is decimal decimalValue ? decimalValue : Convert.ToDecimal(value ?? 0m);
    }

    public async Task<IReadOnlyList<NotificationItemDto>> ListNotificationsAsync(IReadOnlyList<string> recipientKeys, CancellationToken cancellationToken = default)
    {
        var keys = recipientKeys.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        if (keys.Length == 0)
        {
            return Array.Empty<NotificationItemDto>();
        }

        var sql = $@"
SELECT TOP (20) NotificationId, RecipientUserName, Title, Message, Severity, IsRead, LinkUrl, CreatedAtUtc
FROM portal.Notifications
WHERE RecipientUserName IN ({string.Join(",", keys.Select((_, index) => $"@Recipient{index}"))})
ORDER BY CreatedAtUtc DESC;";

        var results = new List<NotificationItemDto>();
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection) { CommandType = CommandType.Text };
        for (var i = 0; i < keys.Length; i++)
        {
            command.Parameters.AddWithValue($"@Recipient{i}", keys[i]);
        }

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new NotificationItemDto(
                NotificationId: reader.GetGuid(reader.GetOrdinal("NotificationId")),
                RecipientUserName: ReadString(reader, "RecipientUserName"),
                Title: ReadString(reader, "Title"),
                Message: ReadString(reader, "Message"),
                Severity: ReadString(reader, "Severity"),
                IsRead: reader.GetBoolean(reader.GetOrdinal("IsRead")),
                CreatedAtUtc: ReadDateTimeOffset(reader, "CreatedAtUtc"),
                LinkUrl: ReadNullableString(reader, "LinkUrl")));
        }

        return results;
    }

    public async Task AddNotificationsAsync(IEnumerable<NotificationItemDto> notifications, CancellationToken cancellationToken = default)
    {
        const string sql = @"
INSERT INTO portal.Notifications(NotificationId, RecipientUserName, Title, Message, Severity, IsRead, LinkUrl, CreatedAtUtc)
VALUES (@NotificationId, @RecipientUserName, @Title, @Message, @Severity, @IsRead, @LinkUrl, @CreatedAtUtc);";

        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);
        foreach (var item in notifications)
        {
            await using var command = new SqlCommand(sql, connection) { CommandType = CommandType.Text };
            command.Parameters.AddWithValue("@NotificationId", item.NotificationId);
            command.Parameters.AddWithValue("@RecipientUserName", item.RecipientUserName);
            command.Parameters.AddWithValue("@Title", item.Title);
            command.Parameters.AddWithValue("@Message", item.Message);
            command.Parameters.AddWithValue("@Severity", item.Severity);
            command.Parameters.AddWithValue("@IsRead", item.IsRead);
            command.Parameters.AddWithValue("@LinkUrl", (object?)item.LinkUrl ?? DBNull.Value);
            command.Parameters.AddWithValue("@CreatedAtUtc", item.CreatedAtUtc.UtcDateTime);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    private async Task<IReadOnlyList<WorkflowActionLogDto>> GetWorkflowActionsAsync(Guid requestId, CancellationToken cancellationToken)
    {
        const string sql = @"
SELECT WorkflowActionId, ModuleCode, RequestId, StageNumber, ActionCode, PerformedBy, PerformedRole, Comments, ActionAtUtc, ResultingStatus
FROM portal.WorkflowActionLogs
WHERE RequestId = @RequestId AND ModuleCode = 'LOAN'
ORDER BY ActionAtUtc, StageNumber;";

        var results = new List<WorkflowActionLogDto>();
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection) { CommandType = CommandType.Text };
        command.Parameters.AddWithValue("@RequestId", requestId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var dto = CreateWorkflowActionLogDto(reader);
            if (dto is not null)
            {
                results.Add(dto);
            }
        }

        return results;
    }

    private async Task ReplaceRepaymentScheduleAsync(SqlConnection connection, SqlTransaction transaction, Guid requestId, IReadOnlyList<LoanRepaymentScheduleItemDto> schedule, CancellationToken cancellationToken)
    {
        await using (var delete = new SqlCommand("DELETE FROM portal.LoanRepaymentSchedules WHERE RequestId = @RequestId;", connection, transaction))
        {
            delete.Parameters.AddWithValue("@RequestId", requestId);
            await delete.ExecuteNonQueryAsync(cancellationToken);
        }

        const string insertSql = @"
INSERT INTO portal.LoanRepaymentSchedules
(ScheduleItemId, RequestId, InstallmentNumber, DueDate, OpeningBalance, PrincipalComponent, InterestComponent, InstallmentAmount, ClosingBalance, Status)
VALUES
(@ScheduleItemId, @RequestId, @InstallmentNumber, @DueDate, @OpeningBalance, @PrincipalComponent, @InterestComponent, @InstallmentAmount, @ClosingBalance, @Status);";

        foreach (var item in schedule)
        {
            await using var insert = new SqlCommand(insertSql, connection, transaction);
            insert.Parameters.AddWithValue("@ScheduleItemId", item.ScheduleItemId);
            insert.Parameters.AddWithValue("@RequestId", requestId);
            insert.Parameters.AddWithValue("@InstallmentNumber", item.InstallmentNumber);
            insert.Parameters.AddWithValue("@DueDate", item.DueDateUtc.UtcDateTime.Date);
            insert.Parameters.AddWithValue("@OpeningBalance", item.OpeningBalance);
            insert.Parameters.AddWithValue("@PrincipalComponent", item.PrincipalComponent);
            insert.Parameters.AddWithValue("@InterestComponent", item.InterestComponent);
            insert.Parameters.AddWithValue("@InstallmentAmount", item.InstallmentAmount);
            insert.Parameters.AddWithValue("@ClosingBalance", item.ClosingBalance);
            insert.Parameters.AddWithValue("@Status", item.Status);
            await insert.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    private async Task SyncWorkflowArtifactsAsync(SqlConnection connection, SqlTransaction transaction, Guid requestId, IReadOnlyList<WorkflowActionLogDto> actions, CancellationToken cancellationToken)
    {
        const string existsSql = "SELECT COUNT(1) FROM portal.WorkflowActionLogs WHERE WorkflowActionId = @WorkflowActionId;";
        const string insertWorkflowSql = @"
INSERT INTO portal.WorkflowActionLogs
(WorkflowActionId, ModuleCode, RequestId, StageNumber, ActionCode, PerformedBy, PerformedRole, Comments, ActionAtUtc, ResultingStatus)
VALUES
(@WorkflowActionId, @ModuleCode, @RequestId, @StageNumber, @ActionCode, @PerformedBy, @PerformedRole, @Comments, @ActionAtUtc, @ResultingStatus);";
        const string insertApprovalSql = @"
INSERT INTO portal.LoanApprovalLogs
(ApprovalLogId, RequestId, StageNumber, ActionCode, ActionBy, ActionRole, Comments, ActionAtUtc, ResultingStatus)
VALUES
(@ApprovalLogId, @RequestId, @StageNumber, @ActionCode, @ActionBy, @ActionRole, @Comments, @ActionAtUtc, @ResultingStatus);";

        foreach (var action in actions)
        {
            if (!TryExtractWorkflowActionRow(action, requestId, out var row))
            {
                continue;
            }

            await using (var exists = new SqlCommand(existsSql, connection, transaction))
            {
                exists.Parameters.AddWithValue("@WorkflowActionId", row.WorkflowActionId);
                var count = (int)(await exists.ExecuteScalarAsync(cancellationToken) ?? 0);
                if (count > 0)
                {
                    continue;
                }
            }

            await using (var insert = new SqlCommand(insertWorkflowSql, connection, transaction))
            {
                insert.Parameters.AddWithValue("@WorkflowActionId", row.WorkflowActionId);
                insert.Parameters.AddWithValue("@ModuleCode", row.ModuleCode);
                insert.Parameters.AddWithValue("@RequestId", row.RequestId);
                insert.Parameters.AddWithValue("@StageNumber", row.StageNumber);
                insert.Parameters.AddWithValue("@ActionCode", row.ActionCode);
                insert.Parameters.AddWithValue("@PerformedBy", row.PerformedBy);
                insert.Parameters.AddWithValue("@PerformedRole", (object?)row.PerformedRole ?? DBNull.Value);
                insert.Parameters.AddWithValue("@Comments", (object?)row.Comments ?? DBNull.Value);
                insert.Parameters.AddWithValue("@ActionAtUtc", row.ActionAtUtc.UtcDateTime);
                insert.Parameters.AddWithValue("@ResultingStatus", row.ResultingStatus);
                await insert.ExecuteNonQueryAsync(cancellationToken);
            }

            if (row.StageNumber > 0)
            {
                await using var insertApproval = new SqlCommand(insertApprovalSql, connection, transaction);
                insertApproval.Parameters.AddWithValue("@ApprovalLogId", row.WorkflowActionId);
                insertApproval.Parameters.AddWithValue("@RequestId", row.RequestId);
                insertApproval.Parameters.AddWithValue("@StageNumber", row.StageNumber);
                insertApproval.Parameters.AddWithValue("@ActionCode", row.ActionCode);
                insertApproval.Parameters.AddWithValue("@ActionBy", row.PerformedBy);
                insertApproval.Parameters.AddWithValue("@ActionRole", (object?)row.PerformedRole ?? DBNull.Value);
                insertApproval.Parameters.AddWithValue("@Comments", (object?)row.Comments ?? DBNull.Value);
                insertApproval.Parameters.AddWithValue("@ActionAtUtc", row.ActionAtUtc.UtcDateTime);
                insertApproval.Parameters.AddWithValue("@ResultingStatus", row.ResultingStatus);
                await insertApproval.ExecuteNonQueryAsync(cancellationToken);
            }
        }
    }

    private void MapLoanRequestParameters(SqlCommand command, LoanRequestDto request)
    {
        command.Parameters.AddWithValue("@RequestId", request.RequestId);
        command.Parameters.AddWithValue("@RequestNumber", request.RequestNumber);
        command.Parameters.AddWithValue("@EmployeeCode", request.EmployeeCode);
        command.Parameters.AddWithValue("@EmployeeName", request.EmployeeName);
        command.Parameters.AddWithValue("@RequestedAmount", request.RequestedAmount);
        command.Parameters.AddWithValue("@InterestRate", request.InterestRate);
        command.Parameters.AddWithValue("@InstallmentMonths", request.InstallmentMonths);
        command.Parameters.AddWithValue("@MonthlyInstallment", request.MonthlyInstallment);
        command.Parameters.AddWithValue("@TotalRepayableAmount", request.TotalRepayableAmount);
        command.Parameters.AddWithValue("@OutstandingBalance", request.OutstandingBalance);
        command.Parameters.AddWithValue("@CurrencyCode", request.CurrencyCode);
        command.Parameters.AddWithValue("@Reason", (object?)request.Reason ?? DBNull.Value);
        command.Parameters.AddWithValue("@Status", request.Status);
        command.Parameters.AddWithValue("@PendingStageNumber", request.PendingStageNumber);
        command.Parameters.AddWithValue("@PendingWithRole", (object?)request.PendingWithRole ?? DBNull.Value);
        command.Parameters.AddWithValue("@PayrollHandoffStatus", request.PayrollHandoffStatus);
        command.Parameters.AddWithValue("@CreatedAtUtc", request.CreatedAtUtc.UtcDateTime);
        command.Parameters.AddWithValue("@UpdatedAtUtc", request.UpdatedAtUtc.UtcDateTime);
        command.Parameters.AddWithValue("@SubmittedAtUtc", request.SubmittedAtUtc?.UtcDateTime ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@ApprovedAtUtc", request.ApprovedAtUtc?.UtcDateTime ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@CompletedAtUtc", request.CompletedAtUtc?.UtcDateTime ?? (object)DBNull.Value);
    }

    private SqlConnection CreateConnection()
    {
        if (string.IsNullOrWhiteSpace(_options.ConnectionString))
        {
            throw new InvalidOperationException("Loan connection string is not configured.");
        }
        return new SqlConnection(_options.ConnectionString);
    }

    private readonly record struct WorkflowActionRow(
        Guid WorkflowActionId,
        string ModuleCode,
        Guid RequestId,
        int StageNumber,
        string ActionCode,
        string PerformedBy,
        string? PerformedRole,
        string? Comments,
        DateTimeOffset ActionAtUtc,
        string ResultingStatus);

    private static WorkflowActionRow? TryReadWorkflowActionRow(SqlDataReader reader)
        => new(
            WorkflowActionId: reader.GetGuid(reader.GetOrdinal("WorkflowActionId")),
            ModuleCode: ReadString(reader, "ModuleCode"),
            RequestId: reader.GetGuid(reader.GetOrdinal("RequestId")),
            StageNumber: reader.GetInt32(reader.GetOrdinal("StageNumber")),
            ActionCode: ReadString(reader, "ActionCode"),
            PerformedBy: ReadString(reader, "PerformedBy"),
            PerformedRole: ReadNullableString(reader, "PerformedRole"),
            Comments: ReadNullableString(reader, "Comments"),
            ActionAtUtc: ReadDateTimeOffset(reader, "ActionAtUtc"),
            ResultingStatus: ReadString(reader, "ResultingStatus"));

    private static WorkflowActionLogDto? CreateWorkflowActionLogDto(SqlDataReader reader)
    {
        var rowMaybe = TryReadWorkflowActionRow(reader);
        if (rowMaybe is null)
        {
            return null;
        }
        var row = rowMaybe.Value;
        var dtoType = typeof(WorkflowActionLogDto);
        var constructor = dtoType.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
            .OrderByDescending(x => x.GetParameters().Length)
            .FirstOrDefault();
        if (constructor is null)
        {
            return null;
        }

        var values = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            [NormalizeName(nameof(row.WorkflowActionId))] = row.WorkflowActionId,
            [NormalizeName(nameof(row.ModuleCode))] = row.ModuleCode,
            [NormalizeName(nameof(row.RequestId))] = row.RequestId,
            [NormalizeName(nameof(row.StageNumber))] = row.StageNumber,
            [NormalizeName(nameof(row.ActionCode))] = row.ActionCode,
            [NormalizeName(nameof(row.PerformedBy))] = row.PerformedBy,
            [NormalizeName(nameof(row.PerformedRole))] = row.PerformedRole,
            [NormalizeName(nameof(row.Comments))] = row.Comments,
            [NormalizeName(nameof(row.ActionAtUtc))] = row.ActionAtUtc,
            [NormalizeName(nameof(row.ResultingStatus))] = row.ResultingStatus
        };

        var arguments = constructor.GetParameters()
            .Select(parameter => ConvertArgument(values.TryGetValue(NormalizeName(parameter.Name ?? string.Empty), out var value) ? value : null, parameter.ParameterType))
            .ToArray();
        return constructor.Invoke(arguments) as WorkflowActionLogDto;
    }

    private static bool TryExtractWorkflowActionRow(WorkflowActionLogDto action, Guid fallbackRequestId, out WorkflowActionRow row)
    {
        var type = action.GetType();
        var workflowActionId = ReadPropertyOrDefault<Guid>(type, action, Guid.NewGuid(), "WorkflowActionId", "ActionId");
        var moduleCode = ReadPropertyOrDefault<string>(type, action, "LOAN", "ModuleCode");
        var requestId = ReadPropertyOrDefault<Guid>(type, action, fallbackRequestId, "RequestId");
        var stageNumber = ReadPropertyOrDefault<int>(type, action, 0, "StageNumber");
        var actionCode = ReadPropertyOrDefault<string>(type, action, string.Empty, "ActionCode");
        var performedBy = ReadPropertyOrDefault<string>(type, action, string.Empty, "PerformedBy");
        var performedRole = ReadPropertyOrNull<string>(type, action, "PerformedRole", "ActionRole");
        var comments = ReadPropertyOrNull<string>(type, action, "Comments");
        var actionAtUtc = ReadPropertyOrNullable<DateTimeOffset>(type, action, "ActionAtUtc")
                          ?? (ReadPropertyOrNullable<DateTime>(type, action, "ActionAtUtc") is { } actionAtDateTime
                              ? new DateTimeOffset(DateTime.SpecifyKind(actionAtDateTime, DateTimeKind.Utc))
                              : DateTimeOffset.UtcNow);
        var resultingStatus = ReadPropertyOrDefault<string>(type, action, string.Empty, "ResultingStatus");

        if (string.IsNullOrWhiteSpace(actionCode) || string.IsNullOrWhiteSpace(performedBy))
        {
            row = default;
            return false;
        }

        row = new WorkflowActionRow(workflowActionId, moduleCode, requestId, stageNumber, actionCode, performedBy, performedRole, comments, actionAtUtc, resultingStatus);
        return true;
    }

    private static string NormalizeName(string value)
        => value.Replace("_", string.Empty, StringComparison.OrdinalIgnoreCase).ToUpperInvariant();

    private static object? ConvertArgument(object? value, Type targetType)
    {
        var nullableUnderlyingType = Nullable.GetUnderlyingType(targetType);
        if (nullableUnderlyingType is not null)
        {
            if (value is null) return null;
            targetType = nullableUnderlyingType;
        }

        if (targetType == typeof(string))
        {
            return value?.ToString();
        }

        if (targetType == typeof(Guid))
        {
            if (value is Guid guid) return guid;
            return value is null ? Guid.Empty : Guid.Parse(value.ToString()!);
        }

        if (targetType == typeof(int))
        {
            return value is null ? 0 : Convert.ToInt32(value);
        }

        if (targetType == typeof(decimal))
        {
            return value is null ? 0m : Convert.ToDecimal(value);
        }

        if (targetType == typeof(bool))
        {
            return value is not null && Convert.ToBoolean(value);
        }

        if (targetType == typeof(DateTimeOffset))
        {
            return value switch
            {
                DateTimeOffset dto => dto,
                DateTime dt => new DateTimeOffset(DateTime.SpecifyKind(dt, DateTimeKind.Utc)),
                _ => DateTimeOffset.UtcNow
            };
        }

        if (targetType == typeof(DateTime))
        {
            return value switch
            {
                DateTime dt => DateTime.SpecifyKind(dt, DateTimeKind.Utc),
                DateTimeOffset dto => dto.UtcDateTime,
                _ => DateTime.UtcNow
            };
        }

        return value;
    }

    private static T ReadPropertyOrDefault<T>(Type type, object instance, T defaultValue, params string[] candidateNames)
    {
        foreach (var candidateName in candidateNames)
        {
            var property = type.GetProperty(candidateName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (property is null) continue;
            var value = property.GetValue(instance);
            if (value is null) return defaultValue;
            if (value is T typed) return typed;
            return (T)Convert.ChangeType(value, typeof(T));
        }
        return defaultValue;
    }

    private static T? ReadPropertyOrNull<T>(Type type, object instance, params string[] candidateNames) where T : class
    {
        foreach (var candidateName in candidateNames)
        {
            var property = type.GetProperty(candidateName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (property is null) continue;
            return property.GetValue(instance) as T;
        }
        return null;
    }

    private static T? ReadPropertyOrNullable<T>(Type type, object instance, params string[] candidateNames) where T : struct
    {
        foreach (var candidateName in candidateNames)
        {
            var property = type.GetProperty(candidateName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (property is null) continue;
            var value = property.GetValue(instance);
            if (value is T typed) return typed;
        }
        return null;
    }

    private static string ReadString(SqlDataReader reader, string columnName)
        => reader[columnName]?.ToString() ?? string.Empty;

    private static string? ReadNullableString(SqlDataReader reader, string columnName)
        => reader[columnName] == DBNull.Value ? null : reader[columnName]?.ToString();

    private static DateTimeOffset ReadDateTimeOffset(SqlDataReader reader, string columnName)
    {
        var value = reader.GetDateTime(reader.GetOrdinal(columnName));
        return new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc));
    }

    private static DateTimeOffset? ReadNullableDateTimeOffset(SqlDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        if (reader.IsDBNull(ordinal)) return null;
        var value = reader.GetDateTime(ordinal);
        return new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc));
    }
}
