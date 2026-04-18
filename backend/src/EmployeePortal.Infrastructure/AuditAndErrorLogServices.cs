using System.Data;
using EmployeePortal.Application;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EmployeePortal.Infrastructure;

public sealed class AuditLogService : IAuditLogService
{
    private readonly PortalAuthOptions _options;
    private readonly ILogger<AuditLogService> _logger;

    public AuditLogService(IOptions<PortalAuthOptions> options, ILogger<AuditLogService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task WriteAsync(AuditLogEntry entry, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Audit event {EventType} by {PerformedBy} on {RequestPath} ({StatusCode}) corr={CorrelationId}: {Details}",
            entry.EventType,
            entry.PerformedBy,
            entry.RequestPath,
            entry.StatusCode,
            entry.CorrelationId,
            entry.Details);

        if (string.IsNullOrWhiteSpace(_options.ConnectionString))
        {
            return;
        }

        const string sql = @"
INSERT INTO portal.AuditLogs (
    EventType,
    EntityName,
    EntityId,
    PerformedBy,
    Details,
    IpAddress,
    UserAgent,
    SourceLayer,
    CorrelationId,
    RequestPath,
    StatusCode)
VALUES (
    @EventType,
    @EntityName,
    @EntityId,
    @PerformedBy,
    @Details,
    @IpAddress,
    @UserAgent,
    @SourceLayer,
    @CorrelationId,
    @RequestPath,
    @StatusCode);";

        await using var connection = new SqlConnection(_options.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection)
        {
            CommandType = CommandType.Text
        };
        command.Parameters.AddWithValue("@EventType", entry.EventType);
        command.Parameters.AddWithValue("@EntityName", (object?)entry.EntityName ?? DBNull.Value);
        command.Parameters.AddWithValue("@EntityId", (object?)entry.EntityId ?? DBNull.Value);
        command.Parameters.AddWithValue("@PerformedBy", (object?)entry.PerformedBy ?? DBNull.Value);
        command.Parameters.AddWithValue("@Details", (object?)entry.Details ?? DBNull.Value);
        command.Parameters.AddWithValue("@IpAddress", (object?)entry.IpAddress ?? DBNull.Value);
        command.Parameters.AddWithValue("@UserAgent", (object?)entry.UserAgent ?? DBNull.Value);
        command.Parameters.AddWithValue("@SourceLayer", entry.SourceLayer);
        command.Parameters.AddWithValue("@CorrelationId", (object?)entry.CorrelationId ?? DBNull.Value);
        command.Parameters.AddWithValue("@RequestPath", (object?)entry.RequestPath ?? DBNull.Value);
        command.Parameters.AddWithValue("@StatusCode", (object?)entry.StatusCode ?? DBNull.Value);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}

public sealed class ErrorLogService : IErrorLogService
{
    private readonly PortalAuthOptions _options;
    private readonly ILogger<ErrorLogService> _logger;

    public ErrorLogService(IOptions<PortalAuthOptions> options, ILogger<ErrorLogService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task WriteAsync(ErrorLogEntry entry, CancellationToken cancellationToken = default)
    {
        _logger.LogError(
            "{SourceLayer} error {ErrorCode} on {RequestPath} ({StatusCode}) corr={CorrelationId}: {ErrorMessage}",
            entry.SourceLayer,
            entry.ErrorCode,
            entry.RequestPath,
            entry.StatusCode,
            entry.CorrelationId,
            entry.ErrorMessage);

        if (string.IsNullOrWhiteSpace(_options.ConnectionString))
        {
            return;
        }

        const string sql = @"
INSERT INTO portal.ErrorLogs (
    ErrorCode,
    ErrorMessage,
    StackTrace,
    SourceLayer,
    CorrelationId,
    UserName,
    RequestPath,
    IpAddress,
    UserAgent,
    StatusCode)
VALUES (
    @ErrorCode,
    @ErrorMessage,
    @StackTrace,
    @SourceLayer,
    @CorrelationId,
    @UserName,
    @RequestPath,
    @IpAddress,
    @UserAgent,
    @StatusCode);";

        await using var connection = new SqlConnection(_options.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection)
        {
            CommandType = CommandType.Text
        };
        command.Parameters.AddWithValue("@ErrorCode", (object?)entry.ErrorCode ?? DBNull.Value);
        command.Parameters.AddWithValue("@ErrorMessage", entry.ErrorMessage);
        command.Parameters.AddWithValue("@StackTrace", (object?)entry.StackTrace ?? DBNull.Value);
        command.Parameters.AddWithValue("@SourceLayer", entry.SourceLayer);
        command.Parameters.AddWithValue("@CorrelationId", (object?)entry.CorrelationId ?? DBNull.Value);
        command.Parameters.AddWithValue("@UserName", (object?)entry.UserName ?? DBNull.Value);
        command.Parameters.AddWithValue("@RequestPath", (object?)entry.RequestPath ?? DBNull.Value);
        command.Parameters.AddWithValue("@IpAddress", (object?)entry.IpAddress ?? DBNull.Value);
        command.Parameters.AddWithValue("@UserAgent", (object?)entry.UserAgent ?? DBNull.Value);
        command.Parameters.AddWithValue("@StatusCode", (object?)entry.StatusCode ?? DBNull.Value);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
