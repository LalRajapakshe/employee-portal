using System.Collections.Concurrent;
using System.Data;
using System.Reflection;
using EmployeePortal.Application;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace EmployeePortal.Infrastructure;

public sealed class SalaryAdvanceRepository : ISalaryAdvanceRepository
{
    private static readonly ConcurrentDictionary<Guid, SalaryAdvanceRequestDto> Requests = new();
    private static readonly ConcurrentDictionary<Guid, NotificationItemDto> Notifications = new();

    private readonly SalaryAdvanceOptions _options;

    public SalaryAdvanceRepository(IOptions<SalaryAdvanceOptions> options)
    {
        _options = options.Value;
    }

    private bool UseSql => !string.IsNullOrWhiteSpace(_options.ConnectionString);

    public Task<SalaryAdvancePolicyDto> GetPolicyAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(new SalaryAdvancePolicyDto(
            MaximumAmount: _options.MaximumAmount,
            RequirePermanentEmployee: _options.RequirePermanentEmployee,
            FirstApproverRole: _options.FirstApproverRole.ToUpperInvariant(),
            SecondApproverRole: _options.SecondApproverRole.ToUpperInvariant(),
            CurrencyCode: _options.CurrencyCode));

    public async Task<SalaryAdvanceRequestDto?> GetByIdAsync(Guid requestId, CancellationToken cancellationToken = default)
    {
        if (!UseSql)
        {
            Requests.TryGetValue(requestId, out var existingRequest);
            return existingRequest;
        }

        await using var connection = await OpenConnectionAsync(cancellationToken);
        const string sql = @"
SELECT
    RequestId,
    RequestNumber,
    EmployeeCode,
    EmployeeName,
    RequestedAmount,
    CurrencyCode,
    Reason,
    Status,
    PendingStageNumber,
    PendingWithRole,
    PayrollHandoffStatus,
    CreatedAtUtc,
    UpdatedAtUtc,
    SubmittedAtUtc
FROM portal.SalaryAdvanceRequests
WHERE RequestId = @RequestId;";

        await using var command = new SqlCommand(sql, connection)
        {
            CommandType = CommandType.Text
        };
        command.Parameters.AddWithValue("@RequestId", requestId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        var request = MapRequest(reader, Array.Empty<WorkflowActionLogDto>());
        await reader.CloseAsync();

        var workflowActions = await LoadWorkflowActionsAsync(connection, requestId, cancellationToken);
        return request with { WorkflowActions = workflowActions };
    }

    public async Task<IReadOnlyList<SalaryAdvanceSummaryDto>> ListForEmployeeAsync(string employeeCode, CancellationToken cancellationToken = default)
    {
        if (!UseSql)
        {
            var memoryResults = Requests.Values
                .Where(x => string.Equals(x.EmployeeCode, employeeCode, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(x => x.CreatedAtUtc)
                .Select(x => new SalaryAdvanceSummaryDto(
                    x.RequestId,
                    x.RequestNumber,
                    x.RequestedAmount,
                    x.Status,
                    x.PayrollHandoffStatus,
                    x.CreatedAtUtc,
                    x.SubmittedAtUtc))
                .ToArray();

            return memoryResults;
        }

        await using var connection = await OpenConnectionAsync(cancellationToken);
        const string sql = @"
SELECT
    RequestId,
    RequestNumber,
    RequestedAmount,
    Status,
    PayrollHandoffStatus,
    CreatedAtUtc,
    SubmittedAtUtc
FROM portal.SalaryAdvanceRequests
WHERE EmployeeCode = @EmployeeCode
ORDER BY CreatedAtUtc DESC;";

        await using var command = new SqlCommand(sql, connection)
        {
            CommandType = CommandType.Text
        };
        command.Parameters.AddWithValue("@EmployeeCode", employeeCode);

        var results = new List<SalaryAdvanceSummaryDto>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new SalaryAdvanceSummaryDto(
                RequestId: reader.GetGuid(reader.GetOrdinal("RequestId")),
                RequestNumber: reader["RequestNumber"]?.ToString() ?? string.Empty,
                RequestedAmount: reader.GetDecimal(reader.GetOrdinal("RequestedAmount")),
                Status: reader["Status"]?.ToString() ?? string.Empty,
                PayrollHandoffStatus: reader["PayrollHandoffStatus"]?.ToString() ?? string.Empty,
                CreatedAtUtc: ReadDateTimeOffset(reader, "CreatedAtUtc"),
                SubmittedAtUtc: ReadNullableDateTimeOffset(reader, "SubmittedAtUtc")));
        }

        return results;
    }

    public async Task<IReadOnlyList<ApprovalInboxItemDto>> ListPendingApprovalsAsync(IReadOnlyList<string> roles, CancellationToken cancellationToken = default)
    {
        if (!UseSql)
        {
            var roleSet = new HashSet<string>(roles.Select(x => x.ToUpperInvariant()), StringComparer.OrdinalIgnoreCase);
            var memoryResults = Requests.Values
                .Where(x => !string.IsNullOrWhiteSpace(x.PendingWithRole) && roleSet.Contains(x.PendingWithRole))
                .OrderByDescending(x => x.SubmittedAtUtc ?? x.CreatedAtUtc)
                .Select(x => new ApprovalInboxItemDto(
                    x.RequestId,
                    ModuleCode: "SALARY_ADVANCE",
                    x.RequestNumber,
                    x.EmployeeCode,
                    x.EmployeeName,
                    x.RequestedAmount,
                    x.Status,
                    x.PendingStageNumber,
                    x.PendingWithRole ?? string.Empty,
                    x.SubmittedAtUtc ?? x.CreatedAtUtc,
                    Summary: $"{x.EmployeeName} requested {x.RequestedAmount:0.00} {x.CurrencyCode}"))
                .ToArray();

            return memoryResults;
        }

        var normalizedRoles = roles
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim().ToUpperInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (normalizedRoles.Length == 0)
        {
            return Array.Empty<ApprovalInboxItemDto>();
        }

        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = new SqlCommand
        {
            Connection = connection,
            CommandType = CommandType.Text
        };

        var roleParameters = new List<string>();
        for (var i = 0; i < normalizedRoles.Length; i++)
        {
            var parameterName = $"@Role{i}";
            roleParameters.Add(parameterName);
            command.Parameters.AddWithValue(parameterName, normalizedRoles[i]);
        }

        command.CommandText = $@"
SELECT
    RequestId,
    RequestNumber,
    EmployeeCode,
    EmployeeName,
    RequestedAmount,
    CurrencyCode,
    Status,
    PendingStageNumber,
    PendingWithRole,
    CreatedAtUtc,
    SubmittedAtUtc
FROM portal.SalaryAdvanceRequests
WHERE PendingWithRole IS NOT NULL
  AND UPPER(PendingWithRole) IN ({string.Join(", ", roleParameters)})
ORDER BY COALESCE(SubmittedAtUtc, CreatedAtUtc) DESC;";

        var results = new List<ApprovalInboxItemDto>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var createdAtUtc = ReadDateTimeOffset(reader, "CreatedAtUtc");
            var submittedAtUtc = ReadNullableDateTimeOffset(reader, "SubmittedAtUtc");
            var requestedAmount = reader.GetDecimal(reader.GetOrdinal("RequestedAmount"));
            var employeeName = reader["EmployeeName"]?.ToString() ?? string.Empty;
            var currencyCode = reader["CurrencyCode"]?.ToString() ?? string.Empty;

            results.Add(new ApprovalInboxItemDto(
                reader.GetGuid(reader.GetOrdinal("RequestId")),
                ModuleCode: "SALARY_ADVANCE",
                reader["RequestNumber"]?.ToString() ?? string.Empty,
                reader["EmployeeCode"]?.ToString() ?? string.Empty,
                employeeName,
                requestedAmount,
                reader["Status"]?.ToString() ?? string.Empty,
                reader.GetInt32(reader.GetOrdinal("PendingStageNumber")),
                reader["PendingWithRole"]?.ToString() ?? string.Empty,
                submittedAtUtc ?? createdAtUtc,
                Summary: $"{employeeName} requested {requestedAmount:0.00} {currencyCode}"));
        }

        return results;
    }

    public async Task<SalaryAdvanceRequestDto> SaveDraftAsync(SalaryAdvanceRequestDto request, CancellationToken cancellationToken = default)
    {
        if (!UseSql)
        {
            Requests[request.RequestId] = request;
            return request;
        }

        await using var connection = await OpenConnectionAsync(cancellationToken);
        await InsertRequestAsync(connection, request, cancellationToken);
        await UpsertWorkflowActionsAsync(connection, request.RequestId, request.WorkflowActions, cancellationToken);
        return request;
    }

    public async Task<SalaryAdvanceRequestDto> UpdateAsync(SalaryAdvanceRequestDto request, CancellationToken cancellationToken = default)
    {
        if (!UseSql)
        {
            Requests[request.RequestId] = request;
            return request;
        }

        await using var connection = await OpenConnectionAsync(cancellationToken);
        var affectedRows = await UpdateRequestAsync(connection, request, cancellationToken);
        if (affectedRows == 0)
        {
            await InsertRequestAsync(connection, request, cancellationToken);
        }

        await UpsertWorkflowActionsAsync(connection, request.RequestId, request.WorkflowActions, cancellationToken);
        return request;
    }

    public async Task<IReadOnlyList<NotificationItemDto>> ListNotificationsAsync(IReadOnlyList<string> recipientKeys, CancellationToken cancellationToken = default)
    {
        if (!UseSql)
        {
            var keys = new HashSet<string>(recipientKeys.Where(x => !string.IsNullOrWhiteSpace(x)), StringComparer.OrdinalIgnoreCase);
            var memoryResults = Notifications.Values
                .Where(x => keys.Contains(x.RecipientUserName))
                .OrderByDescending(x => x.CreatedAtUtc)
                .Take(20)
                .ToArray();
            return memoryResults;
        }

        var normalizedKeys = recipientKeys
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (normalizedKeys.Length == 0)
        {
            return Array.Empty<NotificationItemDto>();
        }

        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = new SqlCommand
        {
            Connection = connection,
            CommandType = CommandType.Text
        };

        var keyParameters = new List<string>();
        for (var i = 0; i < normalizedKeys.Length; i++)
        {
            var parameterName = $"@Recipient{i}";
            keyParameters.Add(parameterName);
            command.Parameters.AddWithValue(parameterName, normalizedKeys[i]);
        }

        command.CommandText = $@"
SELECT TOP (20)
    NotificationId,
    RecipientUserName,
    Title,
    Message,
    Severity,
    IsRead,
    LinkUrl,
    CreatedAtUtc
FROM portal.Notifications
WHERE RecipientUserName IN ({string.Join(", ", keyParameters)})
ORDER BY CreatedAtUtc DESC;";

        var results = new List<NotificationItemDto>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new NotificationItemDto(
                NotificationId: reader.GetGuid(reader.GetOrdinal("NotificationId")),
                RecipientUserName: reader["RecipientUserName"]?.ToString() ?? string.Empty,
                Title: reader["Title"]?.ToString() ?? string.Empty,
                Message: reader["Message"]?.ToString() ?? string.Empty,
                Severity: reader["Severity"]?.ToString() ?? "INFO",
                IsRead: reader["IsRead"] is bool isRead && isRead,
                CreatedAtUtc: ReadDateTimeOffset(reader, "CreatedAtUtc"),
                LinkUrl: reader["LinkUrl"] == DBNull.Value ? null : reader["LinkUrl"]?.ToString()));
        }

        return results;
    }

    public async Task AddNotificationsAsync(IEnumerable<NotificationItemDto> notifications, CancellationToken cancellationToken = default)
    {
        if (!UseSql)
        {
            foreach (var item in notifications)
            {
                Notifications[item.NotificationId] = item;
            }

            return;
        }

        await using var connection = await OpenConnectionAsync(cancellationToken);
        foreach (var item in notifications)
        {
            const string sql = @"
IF EXISTS (SELECT 1 FROM portal.Notifications WHERE NotificationId = @NotificationId)
BEGIN
    UPDATE portal.Notifications
    SET RecipientUserName = @RecipientUserName,
        Title = @Title,
        Message = @Message,
        Severity = @Severity,
        IsRead = @IsRead,
        LinkUrl = @LinkUrl,
        CreatedAtUtc = @CreatedAtUtc
    WHERE NotificationId = @NotificationId;
END
ELSE
BEGIN
    INSERT INTO portal.Notifications (
        NotificationId,
        RecipientUserName,
        Title,
        Message,
        Severity,
        IsRead,
        LinkUrl,
        CreatedAtUtc)
    VALUES (
        @NotificationId,
        @RecipientUserName,
        @Title,
        @Message,
        @Severity,
        @IsRead,
        @LinkUrl,
        @CreatedAtUtc);
END";

            await using var command = new SqlCommand(sql, connection)
            {
                CommandType = CommandType.Text
            };
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

    private async Task<SqlConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        var connection = new SqlConnection(_options.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }

    private static async Task InsertRequestAsync(SqlConnection connection, SalaryAdvanceRequestDto request, CancellationToken cancellationToken)
    {
        const string sql = @"
INSERT INTO portal.SalaryAdvanceRequests (
    RequestId,
    RequestNumber,
    EmployeeCode,
    EmployeeName,
    RequestedAmount,
    CurrencyCode,
    Reason,
    Status,
    PendingStageNumber,
    PendingWithRole,
    PayrollHandoffStatus,
    CreatedAtUtc,
    UpdatedAtUtc,
    SubmittedAtUtc)
VALUES (
    @RequestId,
    @RequestNumber,
    @EmployeeCode,
    @EmployeeName,
    @RequestedAmount,
    @CurrencyCode,
    @Reason,
    @Status,
    @PendingStageNumber,
    @PendingWithRole,
    @PayrollHandoffStatus,
    @CreatedAtUtc,
    @UpdatedAtUtc,
    @SubmittedAtUtc);";

        await using var command = new SqlCommand(sql, connection)
        {
            CommandType = CommandType.Text
        };
        AddRequestParameters(command, request);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<int> UpdateRequestAsync(SqlConnection connection, SalaryAdvanceRequestDto request, CancellationToken cancellationToken)
    {
        const string sql = @"
UPDATE portal.SalaryAdvanceRequests
SET RequestNumber = @RequestNumber,
    EmployeeCode = @EmployeeCode,
    EmployeeName = @EmployeeName,
    RequestedAmount = @RequestedAmount,
    CurrencyCode = @CurrencyCode,
    Reason = @Reason,
    Status = @Status,
    PendingStageNumber = @PendingStageNumber,
    PendingWithRole = @PendingWithRole,
    PayrollHandoffStatus = @PayrollHandoffStatus,
    CreatedAtUtc = @CreatedAtUtc,
    UpdatedAtUtc = @UpdatedAtUtc,
    SubmittedAtUtc = @SubmittedAtUtc
WHERE RequestId = @RequestId;";

        await using var command = new SqlCommand(sql, connection)
        {
            CommandType = CommandType.Text
        };
        AddRequestParameters(command, request);
        return await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static void AddRequestParameters(SqlCommand command, SalaryAdvanceRequestDto request)
    {
        command.Parameters.AddWithValue("@RequestId", request.RequestId);
        command.Parameters.AddWithValue("@RequestNumber", request.RequestNumber);
        command.Parameters.AddWithValue("@EmployeeCode", request.EmployeeCode);
        command.Parameters.AddWithValue("@EmployeeName", request.EmployeeName);
        command.Parameters.AddWithValue("@RequestedAmount", request.RequestedAmount);
        command.Parameters.AddWithValue("@CurrencyCode", request.CurrencyCode);
        command.Parameters.AddWithValue("@Reason", (object?)request.Reason ?? DBNull.Value);
        command.Parameters.AddWithValue("@Status", request.Status);
        command.Parameters.AddWithValue("@PendingStageNumber", request.PendingStageNumber);
        command.Parameters.AddWithValue("@PendingWithRole", (object?)request.PendingWithRole ?? DBNull.Value);
        command.Parameters.AddWithValue("@PayrollHandoffStatus", request.PayrollHandoffStatus);
        command.Parameters.AddWithValue("@CreatedAtUtc", request.CreatedAtUtc.UtcDateTime);
        command.Parameters.AddWithValue("@UpdatedAtUtc", request.UpdatedAtUtc.UtcDateTime);
        command.Parameters.AddWithValue("@SubmittedAtUtc", request.SubmittedAtUtc?.UtcDateTime ?? (object)DBNull.Value);
    }

    private async Task<IReadOnlyList<WorkflowActionLogDto>> LoadWorkflowActionsAsync(SqlConnection connection, Guid requestId, CancellationToken cancellationToken)
    {
        const string sql = @"
SELECT
    WorkflowActionId,
    ModuleCode,
    RequestId,
    StageNumber,
    ActionCode,
    PerformedBy,
    PerformedRole,
    Comments,
    ActionAtUtc,
    ResultingStatus
FROM portal.WorkflowActionLogs
WHERE RequestId = @RequestId
ORDER BY ActionAtUtc ASC, WorkflowActionId ASC;";

        await using var command = new SqlCommand(sql, connection)
        {
            CommandType = CommandType.Text
        };
        command.Parameters.AddWithValue("@RequestId", requestId);

        var results = new List<WorkflowActionLogDto>();
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

    private async Task UpsertWorkflowActionsAsync(SqlConnection connection, Guid requestId, IEnumerable<WorkflowActionLogDto> workflowActions, CancellationToken cancellationToken)
    {
        foreach (var action in workflowActions)
        {
            if (!TryExtractWorkflowActionRow(action, requestId, out var row))
            {
                continue;
            }

            const string sql = @"
IF EXISTS (SELECT 1 FROM portal.WorkflowActionLogs WHERE WorkflowActionId = @WorkflowActionId)
BEGIN
    UPDATE portal.WorkflowActionLogs
    SET ModuleCode = @ModuleCode,
        RequestId = @RequestId,
        StageNumber = @StageNumber,
        ActionCode = @ActionCode,
        PerformedBy = @PerformedBy,
        PerformedRole = @PerformedRole,
        Comments = @Comments,
        ActionAtUtc = @ActionAtUtc,
        ResultingStatus = @ResultingStatus
    WHERE WorkflowActionId = @WorkflowActionId;
END
ELSE
BEGIN
    INSERT INTO portal.WorkflowActionLogs (
        WorkflowActionId,
        ModuleCode,
        RequestId,
        StageNumber,
        ActionCode,
        PerformedBy,
        PerformedRole,
        Comments,
        ActionAtUtc,
        ResultingStatus)
    VALUES (
        @WorkflowActionId,
        @ModuleCode,
        @RequestId,
        @StageNumber,
        @ActionCode,
        @PerformedBy,
        @PerformedRole,
        @Comments,
        @ActionAtUtc,
        @ResultingStatus);
END";

            await using var command = new SqlCommand(sql, connection)
            {
                CommandType = CommandType.Text
            };
            command.Parameters.AddWithValue("@WorkflowActionId", row.WorkflowActionId);
            command.Parameters.AddWithValue("@ModuleCode", row.ModuleCode);
            command.Parameters.AddWithValue("@RequestId", row.RequestId);
            command.Parameters.AddWithValue("@StageNumber", row.StageNumber);
            command.Parameters.AddWithValue("@ActionCode", row.ActionCode);
            command.Parameters.AddWithValue("@PerformedBy", row.PerformedBy);
            command.Parameters.AddWithValue("@PerformedRole", (object?)row.PerformedRole ?? DBNull.Value);
            command.Parameters.AddWithValue("@Comments", (object?)row.Comments ?? DBNull.Value);
            command.Parameters.AddWithValue("@ActionAtUtc", row.ActionAtUtc.UtcDateTime);
            command.Parameters.AddWithValue("@ResultingStatus", row.ResultingStatus);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    private static SalaryAdvanceRequestDto MapRequest(SqlDataReader reader, IReadOnlyList<WorkflowActionLogDto> workflowActions)
    {
        return new SalaryAdvanceRequestDto(
            RequestId: reader.GetGuid(reader.GetOrdinal("RequestId")),
            RequestNumber: reader["RequestNumber"]?.ToString() ?? string.Empty,
            EmployeeCode: reader["EmployeeCode"]?.ToString() ?? string.Empty,
            EmployeeName: reader["EmployeeName"]?.ToString() ?? string.Empty,
            RequestedAmount: reader.GetDecimal(reader.GetOrdinal("RequestedAmount")),
            CurrencyCode: reader["CurrencyCode"]?.ToString() ?? string.Empty,
            Reason: reader["Reason"] == DBNull.Value ? null : reader["Reason"]?.ToString(),
            Status: reader["Status"]?.ToString() ?? string.Empty,
            PendingStageNumber: reader.GetInt32(reader.GetOrdinal("PendingStageNumber")),
            PendingWithRole: reader["PendingWithRole"] == DBNull.Value ? null : reader["PendingWithRole"]?.ToString(),
            PayrollHandoffStatus: reader["PayrollHandoffStatus"]?.ToString() ?? string.Empty,
            CreatedAtUtc: ReadDateTimeOffset(reader, "CreatedAtUtc"),
            UpdatedAtUtc: ReadDateTimeOffset(reader, "UpdatedAtUtc"),
            SubmittedAtUtc: ReadNullableDateTimeOffset(reader, "SubmittedAtUtc"),
            WorkflowActions: workflowActions,
            ValidationMessages: Array.Empty<string>());
    }

    private static DateTimeOffset ReadDateTimeOffset(SqlDataReader reader, string columnName)
    {
        var value = reader.GetDateTime(reader.GetOrdinal(columnName));
        return new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc));
    }

    private static DateTimeOffset? ReadNullableDateTimeOffset(SqlDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        if (reader.IsDBNull(ordinal))
        {
            return null;
        }

        var value = reader.GetDateTime(ordinal);
        return new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc));
    }

    private static SalaryAdvanceRepository.WorkflowActionRow? TryReadWorkflowActionRow(SqlDataReader reader)
    {
        return new WorkflowActionRow(
            WorkflowActionId: reader.GetGuid(reader.GetOrdinal("WorkflowActionId")),
            ModuleCode: reader["ModuleCode"]?.ToString() ?? "SALARY_ADVANCE",
            RequestId: reader.GetGuid(reader.GetOrdinal("RequestId")),
            StageNumber: reader.GetInt32(reader.GetOrdinal("StageNumber")),
            ActionCode: reader["ActionCode"]?.ToString() ?? string.Empty,
            PerformedBy: reader["PerformedBy"]?.ToString() ?? string.Empty,
            PerformedRole: reader["PerformedRole"] == DBNull.Value ? null : reader["PerformedRole"]?.ToString(),
            Comments: reader["Comments"] == DBNull.Value ? null : reader["Comments"]?.ToString(),
            ActionAtUtc: ReadDateTimeOffset(reader, "ActionAtUtc"),
            ResultingStatus: reader["ResultingStatus"]?.ToString() ?? string.Empty);
    }

    private static WorkflowActionLogDto? CreateWorkflowActionLogDto(SqlDataReader reader)
    {
        var rowMaybe = TryReadWorkflowActionRow(reader);
        if (rowMaybe is null)
        {
            return null;
        }

        var row = rowMaybe.Value;

        var dtoType = typeof(WorkflowActionLogDto);
        var constructor = dtoType
            .GetConstructors(BindingFlags.Public | BindingFlags.Instance)
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
        var moduleCode = ReadPropertyOrDefault<string>(type, action, "SALARY_ADVANCE", "ModuleCode");
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
            row = default!;
            return false;
        }

        row = new WorkflowActionRow(
            WorkflowActionId: workflowActionId,
            ModuleCode: moduleCode,
            RequestId: requestId,
            StageNumber: stageNumber,
            ActionCode: actionCode,
            PerformedBy: performedBy,
            PerformedRole: performedRole,
            Comments: comments,
            ActionAtUtc: actionAtUtc,
            ResultingStatus: resultingStatus);
        return true;
    }

    private static T ReadPropertyOrDefault<T>(Type type, object instance, T defaultValue, params string[] candidateNames)
    {
        foreach (var candidateName in candidateNames)
        {
            var property = type.GetProperty(candidateName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (property is null)
            {
                continue;
            }

            var value = property.GetValue(instance);
            if (value is null)
            {
                return defaultValue;
            }

            if (value is T typed)
            {
                return typed;
            }

            if (typeof(T) == typeof(DateTimeOffset) && value is DateTime dateTimeValue)
            {
                object converted = new DateTimeOffset(DateTime.SpecifyKind(dateTimeValue, DateTimeKind.Utc));
                return (T)converted;
            }

            return (T)Convert.ChangeType(value, Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T));
        }

        return defaultValue;
    }

    private static T? ReadPropertyOrNullable<T>(Type type, object instance, params string[] candidateNames)
        where T : struct
    {
        foreach (var candidateName in candidateNames)
        {
            var property = type.GetProperty(candidateName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (property is null)
            {
                continue;
            }

            var value = property.GetValue(instance);
            if (value is null)
            {
                return null;
            }

            if (value is T typed)
            {
                return typed;
            }

            if (typeof(T) == typeof(DateTimeOffset) && value is DateTime dateTimeValue)
            {
                object converted = new DateTimeOffset(DateTime.SpecifyKind(dateTimeValue, DateTimeKind.Utc));
                return (T)converted;
            }

            return (T)Convert.ChangeType(value, typeof(T));
        }

        return null;
    }

    private static string? ReadPropertyOrNull<T>(Type type, object instance, params string[] candidateNames)
    {
        foreach (var candidateName in candidateNames)
        {
            var property = type.GetProperty(candidateName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (property is null)
            {
                continue;
            }

            var value = property.GetValue(instance);
            if (value is null)
            {
                return null;
            }

            return value.ToString();
        }

        return null;
    }

    private static object? ConvertArgument(object? value, Type targetType)
    {
        if (value is null || value == DBNull.Value)
        {
            return targetType.IsValueType && Nullable.GetUnderlyingType(targetType) is null
                ? Activator.CreateInstance(targetType)
                : null;
        }

        var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (underlyingType.IsInstanceOfType(value))
        {
            return value;
        }

        if (underlyingType == typeof(DateTimeOffset) && value is DateTime dateTimeValue)
        {
            return new DateTimeOffset(DateTime.SpecifyKind(dateTimeValue, DateTimeKind.Utc));
        }

        if (underlyingType == typeof(Guid) && value is string guidText)
        {
            return Guid.Parse(guidText);
        }

        if (underlyingType.IsEnum)
        {
            return Enum.Parse(underlyingType, value.ToString() ?? string.Empty, ignoreCase: true);
        }

        return Convert.ChangeType(value, underlyingType);
    }

    private static string NormalizeName(string value)
        => new(value.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());

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
}
