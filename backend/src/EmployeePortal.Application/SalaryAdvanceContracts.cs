namespace EmployeePortal.Application;

public static class PortalRoles
{
    public const string Employee = "EMPLOYEE";
    public const string Director = "DIRECTOR";
    public const string HrAdmin = "HR_ADMIN";
}

public sealed record SalaryAdvancePolicyDto(
    decimal MaximumAmount,
    bool RequirePermanentEmployee,
    string FirstApproverRole,
    string SecondApproverRole,
    string CurrencyCode
);

public sealed record SalaryAdvanceCreateRequestDto(decimal RequestedAmount, string? Reason);
public sealed record SalaryAdvanceUpdateDraftRequestDto(decimal RequestedAmount, string? Reason);
public sealed record SalaryAdvanceApprovalActionRequestDto(string ActionCode, string? Comments);

public sealed record NotificationItemDto(
    Guid NotificationId,
    string RecipientUserName,
    string Title,
    string Message,
    string Severity,
    bool IsRead,
    DateTimeOffset CreatedAtUtc,
    string? LinkUrl
);

public sealed record SalaryAdvanceRequestDto(
    Guid RequestId,
    string RequestNumber,
    string EmployeeCode,
    string EmployeeName,
    decimal RequestedAmount,
    string CurrencyCode,
    string? Reason,
    string Status,
    int PendingStageNumber,
    string? PendingWithRole,
    string PayrollHandoffStatus,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    DateTimeOffset? SubmittedAtUtc,
    IReadOnlyList<WorkflowActionLogDto> WorkflowActions,
    IReadOnlyList<string> ValidationMessages
);

public sealed record SalaryAdvanceSummaryDto(
    Guid RequestId,
    string RequestNumber,
    decimal RequestedAmount,
    string Status,
    string PayrollHandoffStatus,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? SubmittedAtUtc
);

public sealed record SalaryAdvancePrintDto(
    string RequestNumber,
    string EmployeeCode,
    string EmployeeName,
    string Department,
    string Designation,
    decimal RequestedAmount,
    string CurrencyCode,
    string? Reason,
    string Status,
    DateTimeOffset CreatedAtUtc,
    IReadOnlyList<WorkflowActionLogDto> WorkflowActions
);

public interface ISalaryAdvanceRepository
{
    Task<SalaryAdvancePolicyDto> GetPolicyAsync(CancellationToken cancellationToken = default);
    Task<SalaryAdvanceRequestDto?> GetByIdAsync(Guid requestId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SalaryAdvanceSummaryDto>> ListForEmployeeAsync(string employeeCode, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ApprovalInboxItemDto>> ListPendingApprovalsAsync(IReadOnlyList<string> roles, CancellationToken cancellationToken = default);
    Task<SalaryAdvanceRequestDto> SaveDraftAsync(SalaryAdvanceRequestDto request, CancellationToken cancellationToken = default);
    Task<SalaryAdvanceRequestDto> UpdateAsync(SalaryAdvanceRequestDto request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<NotificationItemDto>> ListNotificationsAsync(IReadOnlyList<string> recipientKeys, CancellationToken cancellationToken = default);
    Task AddNotificationsAsync(IEnumerable<NotificationItemDto> notifications, CancellationToken cancellationToken = default);
}

public interface ISalaryAdvanceService
{
    Task<SalaryAdvancePolicyDto> GetPolicyAsync(CancellationToken cancellationToken = default);
    Task<SalaryAdvanceRequestDto> CreateDraftAsync(SalaryAdvanceCreateRequestDto request, CancellationToken cancellationToken = default);
    Task<SalaryAdvanceRequestDto?> UpdateDraftAsync(Guid requestId, SalaryAdvanceUpdateDraftRequestDto request, CancellationToken cancellationToken = default);
    Task<SalaryAdvanceRequestDto?> SubmitAsync(Guid requestId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SalaryAdvanceSummaryDto>> ListMyRequestsAsync(CancellationToken cancellationToken = default);
    Task<SalaryAdvanceRequestDto?> GetRequestAsync(Guid requestId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ApprovalInboxItemDto>> GetApprovalInboxAsync(CancellationToken cancellationToken = default);
    Task<SalaryAdvanceRequestDto?> ApplyApprovalActionAsync(Guid requestId, SalaryAdvanceApprovalActionRequestDto request, CancellationToken cancellationToken = default);
    Task<SalaryAdvancePrintDto?> GetPrintDataAsync(Guid requestId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<NotificationItemDto>> GetNotificationsAsync(CancellationToken cancellationToken = default);
}

public sealed class SalaryAdvanceService : ISalaryAdvanceService
{
    private const string ModuleCode = "SALARY_ADVANCE";
    private readonly ISalaryAdvanceRepository _repository;
    private readonly ICurrentUserAccessor _currentUserAccessor;
    private readonly IEmployeeReadRepository _employeeReadRepository;
    private readonly IWorkflowEngineService _workflowEngineService;
    private readonly IAuditLogService _auditLogService;

    public SalaryAdvanceService(
        ISalaryAdvanceRepository repository,
        ICurrentUserAccessor currentUserAccessor,
        IEmployeeReadRepository employeeReadRepository,
        IWorkflowEngineService workflowEngineService,
        IAuditLogService auditLogService)
    {
        _repository = repository;
        _currentUserAccessor = currentUserAccessor;
        _employeeReadRepository = employeeReadRepository;
        _workflowEngineService = workflowEngineService;
        _auditLogService = auditLogService;
    }

    public Task<SalaryAdvancePolicyDto> GetPolicyAsync(CancellationToken cancellationToken = default)
        => _repository.GetPolicyAsync(cancellationToken);

    public async Task<SalaryAdvanceRequestDto> CreateDraftAsync(SalaryAdvanceCreateRequestDto request, CancellationToken cancellationToken = default)
    {
        var userName = RequireUserName();
        var profile = await RequireProfileAsync(userName, cancellationToken);
        var policy = await _repository.GetPolicyAsync(cancellationToken);
        var validations = ValidateRequestedAmount(request.RequestedAmount, profile, policy);

        var entity = new SalaryAdvanceRequestDto(
            RequestId: Guid.NewGuid(),
            RequestNumber: $"SA-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}",
            EmployeeCode: profile.EmployeeCode,
            EmployeeName: profile.FullName,
            RequestedAmount: request.RequestedAmount,
            CurrencyCode: policy.CurrencyCode,
            Reason: CleanText(request.Reason),
            Status: WorkflowStatuses.Draft,
            PendingStageNumber: 0,
            PendingWithRole: null,
            PayrollHandoffStatus: "NOT_READY",
            CreatedAtUtc: DateTimeOffset.UtcNow,
            UpdatedAtUtc: DateTimeOffset.UtcNow,
            SubmittedAtUtc: null,
            WorkflowActions: [],
            ValidationMessages: validations);

        var saved = await _repository.SaveDraftAsync(entity, cancellationToken);
        await WriteAuditAsync("SALARY_ADVANCE.DRAFT_CREATED", saved.RequestId.ToString(), userName, $"Draft created for {saved.RequestNumber}.", 200, cancellationToken);
        return saved;
    }

    public async Task<SalaryAdvanceRequestDto?> UpdateDraftAsync(Guid requestId, SalaryAdvanceUpdateDraftRequestDto request, CancellationToken cancellationToken = default)
    {
        var userName = RequireUserName();
        var existing = await _repository.GetByIdAsync(requestId, cancellationToken);
        if (existing is null)
        {
            return null;
        }

        var profile = await RequireProfileAsync(userName, cancellationToken);
        if (!string.Equals(existing.EmployeeCode, profile.EmployeeCode, StringComparison.OrdinalIgnoreCase))
        {
            return existing with { ValidationMessages = new[] { "Current user does not own this request." } };
        }

        if (existing.Status is not WorkflowStatuses.Draft and not WorkflowStatuses.SentBack)
        {
            return existing with { ValidationMessages = new[] { "Only draft or sent back requests can be updated." } };
        }

        var policy = await _repository.GetPolicyAsync(cancellationToken);
        var validations = ValidateRequestedAmount(request.RequestedAmount, profile, policy);

        var updated = existing with
        {
            RequestedAmount = request.RequestedAmount,
            Reason = CleanText(request.Reason),
            UpdatedAtUtc = DateTimeOffset.UtcNow,
            ValidationMessages = validations,
            Status = WorkflowStatuses.Draft,
            PendingStageNumber = 0,
            PendingWithRole = null,
            PayrollHandoffStatus = "NOT_READY"
        };

        var saved = await _repository.UpdateAsync(updated, cancellationToken);
        await WriteAuditAsync("SALARY_ADVANCE.DRAFT_UPDATED", saved.RequestId.ToString(), userName, $"Draft updated for {saved.RequestNumber}.", 200, cancellationToken);
        return saved;
    }

    public async Task<SalaryAdvanceRequestDto?> SubmitAsync(Guid requestId, CancellationToken cancellationToken = default)
    {
        var userName = RequireUserName();
        var existing = await _repository.GetByIdAsync(requestId, cancellationToken);
        if (existing is null)
        {
            return null;
        }

        if (existing.Status is not WorkflowStatuses.Draft and not WorkflowStatuses.SentBack)
        {
            return existing with { ValidationMessages = new[] { "Only draft or sent back requests can be submitted." } };
        }

        var profile = await RequireProfileAsync(userName, cancellationToken);
        if (!string.Equals(existing.EmployeeCode, profile.EmployeeCode, StringComparison.OrdinalIgnoreCase))
        {
            return existing with { ValidationMessages = new[] { "Current user does not own this request." } };
        }

        var policy = await _repository.GetPolicyAsync(cancellationToken);
        var validations = ValidateRequestedAmount(existing.RequestedAmount, profile, policy);
        if (validations.Count > 0)
        {
            return existing with { ValidationMessages = validations };
        }

        var submissionLog = _workflowEngineService.CreateSubmissionLog(existing.RequestId, ModuleCode, userName, WorkflowStatuses.PendingFirstApproval);
        var submitted = existing with
        {
            Status = WorkflowStatuses.PendingFirstApproval,
            PendingStageNumber = 1,
            PendingWithRole = policy.FirstApproverRole,
            SubmittedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow,
            WorkflowActions = existing.WorkflowActions.Append(submissionLog).ToArray(),
            ValidationMessages = []
        };

        var saved = await _repository.UpdateAsync(submitted, cancellationToken);
        await CreateNotificationsAsync(
            new[]
            {
                CreateNotification(policy.FirstApproverRole.ToLowerInvariant() + ".queue", $"Salary advance approval pending: {saved.RequestNumber}", $"{saved.EmployeeName} submitted {saved.RequestNumber} for {saved.RequestedAmount:0.00}.", $"/approvals?requestId={saved.RequestId}"),
                CreateNotification(userName, $"Salary advance submitted: {saved.RequestNumber}", "Your salary advance request is now pending first approval.", $"/salary-advance/{saved.RequestId}")
            },
            cancellationToken);
        await WriteAuditAsync("SALARY_ADVANCE.SUBMITTED", saved.RequestId.ToString(), userName, $"Request {saved.RequestNumber} submitted.", 200, cancellationToken);
        return saved;
    }

    public async Task<IReadOnlyList<SalaryAdvanceSummaryDto>> ListMyRequestsAsync(CancellationToken cancellationToken = default)
    {
        var userName = RequireUserName();
        var profile = await RequireProfileAsync(userName, cancellationToken);
        return await _repository.ListForEmployeeAsync(profile.EmployeeCode, cancellationToken);
    }

    public async Task<SalaryAdvanceRequestDto?> GetRequestAsync(Guid requestId, CancellationToken cancellationToken = default)
    {
        var request = await _repository.GetByIdAsync(requestId, cancellationToken);
        if (request is null)
        {
            return null;
        }

        var userName = RequireUserName();
        var profile = await RequireProfileAsync(userName, cancellationToken);
        var currentRoles = ParseCurrentRoles();
        var canView = string.Equals(request.EmployeeCode, profile.EmployeeCode, StringComparison.OrdinalIgnoreCase)
            || currentRoles.Contains(PortalRoles.Director)
            || currentRoles.Contains(PortalRoles.HrAdmin);

        if (!canView)
        {
            return request with { ValidationMessages = new[] { "You do not have access to this request." } };
        }

        return request;
    }

    public async Task<IReadOnlyList<ApprovalInboxItemDto>> GetApprovalInboxAsync(CancellationToken cancellationToken = default)
    {
        var roles = ParseCurrentRoles();
        return await _repository.ListPendingApprovalsAsync(roles.ToList(), cancellationToken);
    }

    public async Task<SalaryAdvanceRequestDto?> ApplyApprovalActionAsync(Guid requestId, SalaryAdvanceApprovalActionRequestDto request, CancellationToken cancellationToken = default)
    {
        var existing = await _repository.GetByIdAsync(requestId, cancellationToken);
        if (existing is null)
        {
            return null;
        }

        var userName = RequireUserName();
        var roles = ParseCurrentRoles();
        var policy = await _repository.GetPolicyAsync(cancellationToken);
        var action = request.ActionCode?.Trim().ToUpperInvariant();
        var comments = CleanText(request.Comments);

        SalaryAdvanceRequestDto updated;
        WorkflowActionLogDto workflowLog;
        IEnumerable<NotificationItemDto> notifications;

        if (existing.PendingStageNumber == 1 && roles.Contains(policy.FirstApproverRole))
        {
            (updated, workflowLog, notifications) = ApplyStageOneAction(existing, action, userName, comments, policy);
        }
        else if (existing.PendingStageNumber == 2 && roles.Contains(policy.SecondApproverRole))
        {
            (updated, workflowLog, notifications) = ApplyStageTwoAction(existing, action, userName, comments, policy);
        }
        else
        {
            return existing with { ValidationMessages = new[] { "You do not have permission to action this request at the current stage." } };
        }

        updated = updated with
        {
            WorkflowActions = existing.WorkflowActions.Append(workflowLog).ToArray(),
            UpdatedAtUtc = DateTimeOffset.UtcNow,
            ValidationMessages = []
        };

        var saved = await _repository.UpdateAsync(updated, cancellationToken);
        await CreateNotificationsAsync(notifications, cancellationToken);
        await WriteAuditAsync($"SALARY_ADVANCE.{action}", saved.RequestId.ToString(), userName, $"Action {action} applied to {saved.RequestNumber}.", 200, cancellationToken);
        return saved;
    }

    public async Task<SalaryAdvancePrintDto?> GetPrintDataAsync(Guid requestId, CancellationToken cancellationToken = default)
    {
        var request = await _repository.GetByIdAsync(requestId, cancellationToken);
        if (request is null)
        {
            return null;
        }

        var profile = await _employeeReadRepository.GetEmployeeProfileByEmployeeCodeAsync(request.EmployeeCode, cancellationToken);
        return new SalaryAdvancePrintDto(
            RequestNumber: request.RequestNumber,
            EmployeeCode: request.EmployeeCode,
            EmployeeName: request.EmployeeName,
            Department: profile?.Department ?? "-",
            Designation: profile?.Designation ?? "-",
            RequestedAmount: request.RequestedAmount,
            CurrencyCode: request.CurrencyCode,
            Reason: request.Reason,
            Status: request.Status,
            CreatedAtUtc: request.CreatedAtUtc,
            WorkflowActions: request.WorkflowActions);
    }

    public async Task<IReadOnlyList<NotificationItemDto>> GetNotificationsAsync(CancellationToken cancellationToken = default)
    {
        var userName = RequireUserName();
        var profile = await RequireProfileAsync(userName, cancellationToken);
        var recipientKeys = new List<string> { userName, profile.EmployeeCode };
        recipientKeys.AddRange(ParseCurrentRoles().Select(role => role.ToLowerInvariant() + ".queue"));
        return await _repository.ListNotificationsAsync(recipientKeys, cancellationToken);
    }

    private (SalaryAdvanceRequestDto Updated, WorkflowActionLogDto WorkflowLog, IEnumerable<NotificationItemDto> Notifications) ApplyStageOneAction(
        SalaryAdvanceRequestDto existing,
        string? action,
        string performedBy,
        string? comments,
        SalaryAdvancePolicyDto policy)
    {
        return action switch
        {
            WorkflowActions.Approve => (
                existing with
                {
                    Status = WorkflowStatuses.PendingSecondApproval,
                    PendingStageNumber = 2,
                    PendingWithRole = policy.SecondApproverRole,
                    PayrollHandoffStatus = "NOT_READY"
                },
                _workflowEngineService.CreateApprovalLog(existing.RequestId, ModuleCode, 1, WorkflowActions.Approve, performedBy, policy.FirstApproverRole, comments, WorkflowStatuses.PendingSecondApproval),
                new[]
                {
                    CreateNotification(policy.SecondApproverRole.ToLowerInvariant() + ".queue", $"Salary advance second approval pending: {existing.RequestNumber}", $"{existing.EmployeeName} request is ready for second approval.", $"/approvals?requestId={existing.RequestId}"),
                    CreateNotification(existing.EmployeeCode, $"Salary advance advanced: {existing.RequestNumber}", "Your request passed first approval and is pending second approval.", $"/salary-advance/{existing.RequestId}")
                }),
            WorkflowActions.Reject => (
                existing with
                {
                    Status = WorkflowStatuses.Rejected,
                    PendingStageNumber = 0,
                    PendingWithRole = null,
                    PayrollHandoffStatus = "NOT_READY"
                },
                _workflowEngineService.CreateApprovalLog(existing.RequestId, ModuleCode, 1, WorkflowActions.Reject, performedBy, policy.FirstApproverRole, comments, WorkflowStatuses.Rejected),
                new[] { CreateNotification(existing.EmployeeCode, $"Salary advance rejected: {existing.RequestNumber}", "Your salary advance request was rejected at first approval.", $"/salary-advance/{existing.RequestId}") }),
            WorkflowActions.SendBack => (
                existing with
                {
                    Status = WorkflowStatuses.SentBack,
                    PendingStageNumber = 0,
                    PendingWithRole = null,
                    PayrollHandoffStatus = "NOT_READY"
                },
                _workflowEngineService.CreateApprovalLog(existing.RequestId, ModuleCode, 1, WorkflowActions.SendBack, performedBy, policy.FirstApproverRole, comments, WorkflowStatuses.SentBack),
                new[] { CreateNotification(existing.EmployeeCode, $"Salary advance sent back: {existing.RequestNumber}", "Your salary advance request was sent back for updates.", $"/salary-advance/{existing.RequestId}") }),
            _ => (
                existing with { ValidationMessages = new[] { "Unsupported workflow action." } },
                _workflowEngineService.CreateApprovalLog(existing.RequestId, ModuleCode, 1, action ?? "UNKNOWN", performedBy, policy.FirstApproverRole, comments, existing.Status),
                Array.Empty<NotificationItemDto>())
        };
    }

    private (SalaryAdvanceRequestDto Updated, WorkflowActionLogDto WorkflowLog, IEnumerable<NotificationItemDto> Notifications) ApplyStageTwoAction(
        SalaryAdvanceRequestDto existing,
        string? action,
        string performedBy,
        string? comments,
        SalaryAdvancePolicyDto policy)
    {
        return action switch
        {
            WorkflowActions.Approve => (
                existing with
                {
                    Status = WorkflowStatuses.Approved,
                    PendingStageNumber = 0,
                    PendingWithRole = null,
                    PayrollHandoffStatus = "READY"
                },
                _workflowEngineService.CreateApprovalLog(existing.RequestId, ModuleCode, 2, WorkflowActions.Approve, performedBy, policy.SecondApproverRole, comments, WorkflowStatuses.Approved),
                new[]
                {
                    CreateNotification(existing.EmployeeCode, $"Salary advance approved: {existing.RequestNumber}", "Your salary advance request is fully approved.", $"/salary-advance/{existing.RequestId}"),
                    CreateNotification("payroll.queue", $"Salary advance ready for payroll: {existing.RequestNumber}", "Approved salary advance is ready for payroll handoff.", $"/salary-advance/{existing.RequestId}")
                }),
            WorkflowActions.Reject => (
                existing with
                {
                    Status = WorkflowStatuses.Rejected,
                    PendingStageNumber = 0,
                    PendingWithRole = null,
                    PayrollHandoffStatus = "NOT_READY"
                },
                _workflowEngineService.CreateApprovalLog(existing.RequestId, ModuleCode, 2, WorkflowActions.Reject, performedBy, policy.SecondApproverRole, comments, WorkflowStatuses.Rejected),
                new[] { CreateNotification(existing.EmployeeCode, $"Salary advance rejected: {existing.RequestNumber}", "Your salary advance request was rejected at second approval.", $"/salary-advance/{existing.RequestId}") }),
            WorkflowActions.SendBack => (
                existing with
                {
                    Status = WorkflowStatuses.SentBack,
                    PendingStageNumber = 0,
                    PendingWithRole = null,
                    PayrollHandoffStatus = "NOT_READY"
                },
                _workflowEngineService.CreateApprovalLog(existing.RequestId, ModuleCode, 2, WorkflowActions.SendBack, performedBy, policy.SecondApproverRole, comments, WorkflowStatuses.SentBack),
                new[] { CreateNotification(existing.EmployeeCode, $"Salary advance sent back: {existing.RequestNumber}", "Your salary advance request was sent back after second-level review.", $"/salary-advance/{existing.RequestId}") }),
            _ => (
                existing with { ValidationMessages = new[] { "Unsupported workflow action." } },
                _workflowEngineService.CreateApprovalLog(existing.RequestId, ModuleCode, 2, action ?? "UNKNOWN", performedBy, policy.SecondApproverRole, comments, existing.Status),
                Array.Empty<NotificationItemDto>())
        };
    }

    private List<string> ValidateRequestedAmount(decimal requestedAmount, EmployeeProfileDto profile, SalaryAdvancePolicyDto policy)
    {
        var messages = new List<string>();
        if (requestedAmount <= 0)
        {
            messages.Add("Requested amount must be greater than zero.");
        }

        if (requestedAmount > policy.MaximumAmount)
        {
            messages.Add($"Requested amount cannot exceed {policy.MaximumAmount:0.00} {policy.CurrencyCode}.");
        }

        if (policy.RequirePermanentEmployee && !profile.IsPermanent)
        {
            messages.Add("Only permanent employees can submit salary advance requests.");
        }

        if (!string.Equals(profile.EmploymentStatus, "Active", StringComparison.OrdinalIgnoreCase))
        {
            messages.Add("Only active employees can submit salary advance requests.");
        }

        return messages;
    }

    private async Task<EmployeeProfileDto> RequireProfileAsync(string userName, CancellationToken cancellationToken)
    {
        var profile = await _employeeReadRepository.GetEmployeeProfileByUserNameAsync(userName, cancellationToken);
        return profile ?? throw new InvalidOperationException("Current employee profile could not be resolved.");
    }

    private string RequireUserName()
    {
        return _currentUserAccessor.GetUserName() ?? throw new InvalidOperationException("Current user could not be resolved.");
    }

    private HashSet<string> ParseCurrentRoles()
        => new(_currentUserAccessor.GetRoles(), StringComparer.OrdinalIgnoreCase);

    private static string? CleanText(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static NotificationItemDto CreateNotification(string recipientUserName, string title, string message, string? linkUrl)
        => new(
            NotificationId: Guid.NewGuid(),
            RecipientUserName: recipientUserName,
            Title: title,
            Message: message,
            Severity: "INFO",
            IsRead: false,
            CreatedAtUtc: DateTimeOffset.UtcNow,
            LinkUrl: linkUrl);

    private async Task CreateNotificationsAsync(IEnumerable<NotificationItemDto> notifications, CancellationToken cancellationToken)
    {
        await _repository.AddNotificationsAsync(notifications, cancellationToken);
    }

    private async Task WriteAuditAsync(string eventType, string entityId, string performedBy, string details, int statusCode, CancellationToken cancellationToken)
    {
        await _auditLogService.WriteAsync(
            new AuditLogEntry(
                EventType: eventType,
                EntityName: "SalaryAdvanceRequest",
                EntityId: entityId,
                PerformedBy: performedBy,
                Details: details,
                IpAddress: _currentUserAccessor.GetIpAddress(),
                UserAgent: _currentUserAccessor.GetUserAgent(),
                SourceLayer: "Core Backend Layer",
                CorrelationId: _currentUserAccessor.GetCorrelationId(),
                RequestPath: _currentUserAccessor.GetPath(),
                StatusCode: statusCode),
            cancellationToken);
    }

}
