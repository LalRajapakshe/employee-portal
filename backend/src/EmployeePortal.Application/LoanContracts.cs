namespace EmployeePortal.Application;

public sealed record LoanPolicyDto(
    decimal MaximumAmount,
    int MaximumMonths,
    decimal InterestRate,
    bool RequirePermanentEmployee,
    int MinimumServiceMonths,
    int WaitingMonthsAfterCompletion,
    string FirstApproverRole,
    string SecondApproverRole,
    string CurrencyCode
);

public sealed record LoanEligibilityDto(
    bool IsEligible,
    IReadOnlyList<string> Reasons,
    decimal MaximumAmount,
    int MaximumMonths,
    decimal InterestRate,
    bool HasActiveLoan,
    DateTimeOffset? EligibleOnUtc
);

public sealed record LoanCreateRequestDto(decimal RequestedAmount, int InstallmentMonths, string? Reason);
public sealed record LoanUpdateDraftRequestDto(decimal RequestedAmount, int InstallmentMonths, string? Reason);
public sealed record LoanApprovalActionRequestDto(string ActionCode, string? Comments);

public sealed record LoanRepaymentScheduleItemDto(
    Guid ScheduleItemId,
    int InstallmentNumber,
    DateTimeOffset DueDateUtc,
    decimal OpeningBalance,
    decimal PrincipalComponent,
    decimal InterestComponent,
    decimal InstallmentAmount,
    decimal ClosingBalance,
    string Status
);

public sealed record LoanRequestDto(
    Guid RequestId,
    string RequestNumber,
    string EmployeeCode,
    string EmployeeName,
    decimal RequestedAmount,
    decimal InterestRate,
    int InstallmentMonths,
    decimal MonthlyInstallment,
    decimal TotalRepayableAmount,
    decimal OutstandingBalance,
    string CurrencyCode,
    string? Reason,
    string Status,
    int PendingStageNumber,
    string? PendingWithRole,
    string PayrollHandoffStatus,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    DateTimeOffset? SubmittedAtUtc,
    DateTimeOffset? ApprovedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    IReadOnlyList<LoanRepaymentScheduleItemDto> RepaymentSchedule,
    IReadOnlyList<WorkflowActionLogDto> WorkflowActions,
    IReadOnlyList<string> ValidationMessages
);

public sealed record LoanSummaryDto(
    Guid RequestId,
    string RequestNumber,
    decimal RequestedAmount,
    int InstallmentMonths,
    decimal MonthlyInstallment,
    decimal OutstandingBalance,
    string Status,
    string PayrollHandoffStatus,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? SubmittedAtUtc
);

public sealed record LoanDashboardSummaryDto(
    bool HasActiveLoan,
    decimal OutstandingBalance,
    int ActiveLoanCount,
    string CurrencyCode
);

public sealed record LoanPrintDto(
    string RequestNumber,
    string EmployeeCode,
    string EmployeeName,
    string Department,
    string Designation,
    decimal RequestedAmount,
    decimal InterestRate,
    int InstallmentMonths,
    decimal MonthlyInstallment,
    decimal TotalRepayableAmount,
    decimal OutstandingBalance,
    string CurrencyCode,
    string? Reason,
    string Status,
    DateTimeOffset CreatedAtUtc,
    IReadOnlyList<LoanRepaymentScheduleItemDto> RepaymentSchedule,
    IReadOnlyList<WorkflowActionLogDto> WorkflowActions
);

public sealed record LoanCompletionInfoDto(DateTimeOffset? CompletedAtUtc);

public interface ILoanRepository
{
    Task<LoanPolicyDto> GetPolicyAsync(CancellationToken cancellationToken = default);
    Task<LoanRequestDto?> GetByIdAsync(Guid requestId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LoanSummaryDto>> ListForEmployeeAsync(string employeeCode, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ApprovalInboxItemDto>> ListPendingApprovalsAsync(IReadOnlyList<string> roles, CancellationToken cancellationToken = default);
    Task<LoanRequestDto> SaveDraftAsync(LoanRequestDto request, CancellationToken cancellationToken = default);
    Task<LoanRequestDto> UpdateAsync(LoanRequestDto request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LoanRepaymentScheduleItemDto>> GetRepaymentScheduleAsync(Guid requestId, CancellationToken cancellationToken = default);
    Task<bool> HasActiveLoanAsync(string employeeCode, CancellationToken cancellationToken = default);
    Task<LoanCompletionInfoDto?> GetMostRecentCompletedLoanAsync(string employeeCode, CancellationToken cancellationToken = default);
    Task<decimal> GetOutstandingBalanceAsync(string employeeCode, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<NotificationItemDto>> ListNotificationsAsync(IReadOnlyList<string> recipientKeys, CancellationToken cancellationToken = default);
    Task AddNotificationsAsync(IEnumerable<NotificationItemDto> notifications, CancellationToken cancellationToken = default);
}

public interface ILoanService
{
    Task<LoanPolicyDto> GetPolicyAsync(CancellationToken cancellationToken = default);
    Task<LoanEligibilityDto> GetEligibilityAsync(CancellationToken cancellationToken = default);
    Task<LoanRequestDto> CreateDraftAsync(LoanCreateRequestDto request, CancellationToken cancellationToken = default);
    Task<LoanRequestDto?> UpdateDraftAsync(Guid requestId, LoanUpdateDraftRequestDto request, CancellationToken cancellationToken = default);
    Task<LoanRequestDto?> SubmitAsync(Guid requestId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LoanSummaryDto>> ListMyLoansAsync(CancellationToken cancellationToken = default);
    Task<LoanDashboardSummaryDto> GetDashboardSummaryAsync(CancellationToken cancellationToken = default);
    Task<LoanRequestDto?> GetRequestAsync(Guid requestId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LoanRepaymentScheduleItemDto>> GetRepaymentScheduleAsync(Guid requestId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ApprovalInboxItemDto>> GetApprovalInboxAsync(CancellationToken cancellationToken = default);
    Task<LoanRequestDto?> ApplyApprovalActionAsync(Guid requestId, LoanApprovalActionRequestDto request, CancellationToken cancellationToken = default);
    Task<LoanPrintDto?> GetPrintDataAsync(Guid requestId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<NotificationItemDto>> GetNotificationsAsync(CancellationToken cancellationToken = default);
}

public sealed class LoanService : ILoanService
{
    private const string ModuleCode = "LOAN";
    private readonly ILoanRepository _repository;
    private readonly ICurrentUserAccessor _currentUserAccessor;
    private readonly IEmployeeReadRepository _employeeReadRepository;
    private readonly IWorkflowEngineService _workflowEngineService;
    private readonly IAuditLogService _auditLogService;

    public LoanService(
        ILoanRepository repository,
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

    public Task<LoanPolicyDto> GetPolicyAsync(CancellationToken cancellationToken = default)
        => _repository.GetPolicyAsync(cancellationToken);

    public async Task<LoanEligibilityDto> GetEligibilityAsync(CancellationToken cancellationToken = default)
    {
        var userName = RequireUserName();
        var profile = await RequireProfileAsync(userName, cancellationToken);
        var policy = await _repository.GetPolicyAsync(cancellationToken);
        var reasons = new List<string>();

        var serviceMonths = GetCompletedMonths(GetJoinDateDateOnly(profile.JoinDate), DateOnly.FromDateTime(DateTime.UtcNow));
        if (serviceMonths < policy.MinimumServiceMonths)
        {
            reasons.Add($"Minimum service period is {policy.MinimumServiceMonths} months.");
        }

        if (policy.RequirePermanentEmployee && !profile.IsPermanent)
        {
            reasons.Add("Only permanent employees are eligible for employee loans.");
        }

        if (!string.Equals(profile.EmploymentStatus, "Active", StringComparison.OrdinalIgnoreCase))
        {
            reasons.Add("Only active employees are eligible for employee loans.");
        }

        var hasActiveLoan = await _repository.HasActiveLoanAsync(profile.EmployeeCode, cancellationToken);
        if (hasActiveLoan)
        {
            reasons.Add("Employee already has an active loan. Top-up is not allowed.");
        }

        DateTimeOffset? eligibleOnUtc = null;
        var lastCompletedLoan = await _repository.GetMostRecentCompletedLoanAsync(profile.EmployeeCode, cancellationToken);
        if (lastCompletedLoan?.CompletedAtUtc is { } completedAtUtc)
        {
            var waitingUntil = completedAtUtc.AddMonths(policy.WaitingMonthsAfterCompletion);
            if (waitingUntil > DateTimeOffset.UtcNow)
            {
                eligibleOnUtc = waitingUntil;
                reasons.Add($"Employee becomes eligible again on {waitingUntil:yyyy-MM-dd} after the waiting period.");
            }
        }

        return new LoanEligibilityDto(
            IsEligible: reasons.Count == 0,
            Reasons: reasons,
            MaximumAmount: policy.MaximumAmount,
            MaximumMonths: policy.MaximumMonths,
            InterestRate: policy.InterestRate,
            HasActiveLoan: hasActiveLoan,
            EligibleOnUtc: eligibleOnUtc);
    }

    public async Task<LoanRequestDto> CreateDraftAsync(LoanCreateRequestDto request, CancellationToken cancellationToken = default)
    {
        var userName = RequireUserName();
        var profile = await RequireProfileAsync(userName, cancellationToken);
        var policy = await _repository.GetPolicyAsync(cancellationToken);
        var eligibility = await GetEligibilityAsync(cancellationToken);
        var validations = ValidateRequest(request.RequestedAmount, request.InstallmentMonths, profile, policy, eligibility).ToList();

        var schedule = GenerateRepaymentSchedule(request.RequestedAmount, policy.InterestRate, request.InstallmentMonths);
        var monthlyInstallment = schedule.Count == 0 ? 0 : schedule[0].InstallmentAmount;
        var totalRepayable = schedule.Sum(x => x.InstallmentAmount);

        var entity = new LoanRequestDto(
            RequestId: Guid.NewGuid(),
            RequestNumber: $"LN-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}",
            EmployeeCode: profile.EmployeeCode,
            EmployeeName: profile.FullName,
            RequestedAmount: request.RequestedAmount,
            InterestRate: policy.InterestRate,
            InstallmentMonths: request.InstallmentMonths,
            MonthlyInstallment: monthlyInstallment,
            TotalRepayableAmount: totalRepayable,
            OutstandingBalance: totalRepayable,
            CurrencyCode: policy.CurrencyCode,
            Reason: CleanText(request.Reason),
            Status: WorkflowStatuses.Draft,
            PendingStageNumber: 0,
            PendingWithRole: null,
            PayrollHandoffStatus: "NOT_READY",
            CreatedAtUtc: DateTimeOffset.UtcNow,
            UpdatedAtUtc: DateTimeOffset.UtcNow,
            SubmittedAtUtc: null,
            ApprovedAtUtc: null,
            CompletedAtUtc: null,
            RepaymentSchedule: schedule,
            WorkflowActions: [],
            ValidationMessages: validations);

        var saved = await _repository.SaveDraftAsync(entity, cancellationToken);
        await WriteAuditAsync("LOAN.DRAFT_CREATED", saved.RequestId.ToString(), userName, $"Loan draft created for {saved.RequestNumber}.", 200, cancellationToken);
        return saved;
    }

    public async Task<LoanRequestDto?> UpdateDraftAsync(Guid requestId, LoanUpdateDraftRequestDto request, CancellationToken cancellationToken = default)
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
        var eligibility = await GetEligibilityAsync(cancellationToken);
        var validations = ValidateRequest(request.RequestedAmount, request.InstallmentMonths, profile, policy, eligibility).ToList();
        var schedule = GenerateRepaymentSchedule(request.RequestedAmount, policy.InterestRate, request.InstallmentMonths);
        var monthlyInstallment = schedule.Count == 0 ? 0 : schedule[0].InstallmentAmount;
        var totalRepayable = schedule.Sum(x => x.InstallmentAmount);

        var updated = existing with
        {
            RequestedAmount = request.RequestedAmount,
            InstallmentMonths = request.InstallmentMonths,
            InterestRate = policy.InterestRate,
            MonthlyInstallment = monthlyInstallment,
            TotalRepayableAmount = totalRepayable,
            OutstandingBalance = totalRepayable,
            Reason = CleanText(request.Reason),
            UpdatedAtUtc = DateTimeOffset.UtcNow,
            ValidationMessages = validations,
            RepaymentSchedule = schedule,
            Status = WorkflowStatuses.Draft,
            PendingStageNumber = 0,
            PendingWithRole = null,
            PayrollHandoffStatus = "NOT_READY"
        };

        var saved = await _repository.UpdateAsync(updated, cancellationToken);
        await WriteAuditAsync("LOAN.DRAFT_UPDATED", saved.RequestId.ToString(), userName, $"Loan draft updated for {saved.RequestNumber}.", 200, cancellationToken);
        return saved;
    }

    public async Task<LoanRequestDto?> SubmitAsync(Guid requestId, CancellationToken cancellationToken = default)
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
        var eligibility = await GetEligibilityAsync(cancellationToken);
        var validations = ValidateRequest(existing.RequestedAmount, existing.InstallmentMonths, profile, policy, eligibility).ToList();
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
                CreateNotification(policy.FirstApproverRole.ToLowerInvariant() + ".queue", $"Loan approval pending: {saved.RequestNumber}", $"{saved.EmployeeName} submitted {saved.RequestNumber} for {saved.RequestedAmount:0.00}.", $"/loans/approvals?requestId={saved.RequestId}"),
                CreateNotification(userName, $"Loan submitted: {saved.RequestNumber}", "Your loan request is now pending first approval.", $"/loans/{saved.RequestId}")
            },
            cancellationToken);
        await WriteAuditAsync("LOAN.SUBMITTED", saved.RequestId.ToString(), userName, $"Loan request {saved.RequestNumber} submitted.", 200, cancellationToken);
        return saved;
    }

    public async Task<IReadOnlyList<LoanSummaryDto>> ListMyLoansAsync(CancellationToken cancellationToken = default)
    {
        var userName = RequireUserName();
        var profile = await RequireProfileAsync(userName, cancellationToken);
        return await _repository.ListForEmployeeAsync(profile.EmployeeCode, cancellationToken);
    }

    public async Task<LoanDashboardSummaryDto> GetDashboardSummaryAsync(CancellationToken cancellationToken = default)
    {
        var userName = RequireUserName();
        var profile = await RequireProfileAsync(userName, cancellationToken);
        var list = await _repository.ListForEmployeeAsync(profile.EmployeeCode, cancellationToken);
        var outstanding = await _repository.GetOutstandingBalanceAsync(profile.EmployeeCode, cancellationToken);
        var activeCount = list.Count(x => x.Status is WorkflowStatuses.PendingFirstApproval or WorkflowStatuses.PendingSecondApproval or WorkflowStatuses.Approved);
        var policy = await _repository.GetPolicyAsync(cancellationToken);
        return new LoanDashboardSummaryDto(activeCount > 0, outstanding, activeCount, policy.CurrencyCode);
    }

    public async Task<LoanRequestDto?> GetRequestAsync(Guid requestId, CancellationToken cancellationToken = default)
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

    public async Task<IReadOnlyList<LoanRepaymentScheduleItemDto>> GetRepaymentScheduleAsync(Guid requestId, CancellationToken cancellationToken = default)
    {
        var request = await GetRequestAsync(requestId, cancellationToken);
        if (request is null)
        {
            return Array.Empty<LoanRepaymentScheduleItemDto>();
        }

        return request.RepaymentSchedule;
    }

    public async Task<IReadOnlyList<ApprovalInboxItemDto>> GetApprovalInboxAsync(CancellationToken cancellationToken = default)
    {
        var roles = ParseCurrentRoles();
        return await _repository.ListPendingApprovalsAsync(roles.ToList(), cancellationToken);
    }

    public async Task<LoanRequestDto?> ApplyApprovalActionAsync(Guid requestId, LoanApprovalActionRequestDto request, CancellationToken cancellationToken = default)
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

        LoanRequestDto updated;
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
        await WriteAuditAsync($"LOAN.{action}", saved.RequestId.ToString(), userName, $"Action {action} applied to {saved.RequestNumber}.", 200, cancellationToken);
        return saved;
    }

    public async Task<LoanPrintDto?> GetPrintDataAsync(Guid requestId, CancellationToken cancellationToken = default)
    {
        var request = await _repository.GetByIdAsync(requestId, cancellationToken);
        if (request is null)
        {
            return null;
        }

        var profile = await _employeeReadRepository.GetEmployeeProfileByEmployeeCodeAsync(request.EmployeeCode, cancellationToken);
        return new LoanPrintDto(
            RequestNumber: request.RequestNumber,
            EmployeeCode: request.EmployeeCode,
            EmployeeName: request.EmployeeName,
            Department: profile?.Department ?? "-",
            Designation: profile?.Designation ?? "-",
            RequestedAmount: request.RequestedAmount,
            InterestRate: request.InterestRate,
            InstallmentMonths: request.InstallmentMonths,
            MonthlyInstallment: request.MonthlyInstallment,
            TotalRepayableAmount: request.TotalRepayableAmount,
            OutstandingBalance: request.OutstandingBalance,
            CurrencyCode: request.CurrencyCode,
            Reason: request.Reason,
            Status: request.Status,
            CreatedAtUtc: request.CreatedAtUtc,
            RepaymentSchedule: request.RepaymentSchedule,
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

    private (LoanRequestDto Updated, WorkflowActionLogDto WorkflowLog, IEnumerable<NotificationItemDto> Notifications) ApplyStageOneAction(
        LoanRequestDto existing,
        string? action,
        string performedBy,
        string? comments,
        LoanPolicyDto policy)
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
                    CreateNotification(policy.SecondApproverRole.ToLowerInvariant() + ".queue", $"Loan second approval pending: {existing.RequestNumber}", $"{existing.EmployeeName} request is ready for second approval.", $"/loans/approvals?requestId={existing.RequestId}"),
                    CreateNotification(existing.EmployeeCode, $"Loan advanced: {existing.RequestNumber}", "Your request passed first approval and is pending second approval.", $"/loans/{existing.RequestId}")
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
                new[] { CreateNotification(existing.EmployeeCode, $"Loan rejected: {existing.RequestNumber}", "Your loan request was rejected at first approval.", $"/loans/{existing.RequestId}") }),
            WorkflowActions.SendBack => (
                existing with
                {
                    Status = WorkflowStatuses.SentBack,
                    PendingStageNumber = 0,
                    PendingWithRole = null,
                    PayrollHandoffStatus = "NOT_READY"
                },
                _workflowEngineService.CreateApprovalLog(existing.RequestId, ModuleCode, 1, WorkflowActions.SendBack, performedBy, policy.FirstApproverRole, comments, WorkflowStatuses.SentBack),
                new[] { CreateNotification(existing.EmployeeCode, $"Loan sent back: {existing.RequestNumber}", "Your loan request was sent back for updates.", $"/loans/{existing.RequestId}") }),
            _ => (
                existing with { ValidationMessages = new[] { "Unsupported workflow action." } },
                _workflowEngineService.CreateApprovalLog(existing.RequestId, ModuleCode, 1, action ?? "UNKNOWN", performedBy, policy.FirstApproverRole, comments, existing.Status),
                Array.Empty<NotificationItemDto>())
        };
    }

    private (LoanRequestDto Updated, WorkflowActionLogDto WorkflowLog, IEnumerable<NotificationItemDto> Notifications) ApplyStageTwoAction(
        LoanRequestDto existing,
        string? action,
        string performedBy,
        string? comments,
        LoanPolicyDto policy)
    {
        return action switch
        {
            WorkflowActions.Approve => (
                existing with
                {
                    Status = WorkflowStatuses.Approved,
                    PendingStageNumber = 0,
                    PendingWithRole = null,
                    PayrollHandoffStatus = "READY",
                    ApprovedAtUtc = DateTimeOffset.UtcNow,
                    OutstandingBalance = existing.TotalRepayableAmount
                },
                _workflowEngineService.CreateApprovalLog(existing.RequestId, ModuleCode, 2, WorkflowActions.Approve, performedBy, policy.SecondApproverRole, comments, WorkflowStatuses.Approved),
                new[]
                {
                    CreateNotification(existing.EmployeeCode, $"Loan approved: {existing.RequestNumber}", "Your employee loan request is fully approved.", $"/loans/{existing.RequestId}"),
                    CreateNotification("payroll.queue", $"Loan ready for payroll: {existing.RequestNumber}", "Approved loan is ready for payroll handoff.", $"/loans/{existing.RequestId}")
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
                new[] { CreateNotification(existing.EmployeeCode, $"Loan rejected: {existing.RequestNumber}", "Your loan request was rejected at second approval.", $"/loans/{existing.RequestId}") }),
            WorkflowActions.SendBack => (
                existing with
                {
                    Status = WorkflowStatuses.SentBack,
                    PendingStageNumber = 0,
                    PendingWithRole = null,
                    PayrollHandoffStatus = "NOT_READY"
                },
                _workflowEngineService.CreateApprovalLog(existing.RequestId, ModuleCode, 2, WorkflowActions.SendBack, performedBy, policy.SecondApproverRole, comments, WorkflowStatuses.SentBack),
                new[] { CreateNotification(existing.EmployeeCode, $"Loan sent back: {existing.RequestNumber}", "Your loan request was sent back after second-level review.", $"/loans/{existing.RequestId}") }),
            _ => (
                existing with { ValidationMessages = new[] { "Unsupported workflow action." } },
                _workflowEngineService.CreateApprovalLog(existing.RequestId, ModuleCode, 2, action ?? "UNKNOWN", performedBy, policy.SecondApproverRole, comments, existing.Status),
                Array.Empty<NotificationItemDto>())
        };
    }

    private IEnumerable<string> ValidateRequest(decimal requestedAmount, int installmentMonths, EmployeeProfileDto profile, LoanPolicyDto policy, LoanEligibilityDto eligibility)
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

        if (installmentMonths <= 0 || installmentMonths > policy.MaximumMonths)
        {
            messages.Add($"Installment months must be between 1 and {policy.MaximumMonths}.");
        }

        messages.AddRange(eligibility.Reasons);

        return messages.Distinct(StringComparer.OrdinalIgnoreCase);
    }

    private IReadOnlyList<LoanRepaymentScheduleItemDto> GenerateRepaymentSchedule(decimal principal, decimal annualInterestRate, int months)
    {
        if (principal <= 0 || months <= 0)
        {
            return Array.Empty<LoanRepaymentScheduleItemDto>();
        }

        var monthlyRate = annualInterestRate / 100m / 12m;
        var installmentAmount = monthlyRate == 0
            ? decimal.Round(principal / months, 2, MidpointRounding.AwayFromZero)
            : decimal.Round(principal * (monthlyRate * (decimal)Math.Pow((double)(1 + monthlyRate), months)) /
                ((decimal)Math.Pow((double)(1 + monthlyRate), months) - 1), 2, MidpointRounding.AwayFromZero);

        var balance = principal;
        var dueDate = new DateTimeOffset(DateTime.SpecifyKind(DateTime.UtcNow.Date.AddMonths(1), DateTimeKind.Utc));
        var items = new List<LoanRepaymentScheduleItemDto>();

        for (var i = 1; i <= months; i++)
        {
            var opening = balance;
            var interest = decimal.Round(opening * monthlyRate, 2, MidpointRounding.AwayFromZero);
            var principalComponent = decimal.Round(installmentAmount - interest, 2, MidpointRounding.AwayFromZero);
            var closing = decimal.Round(opening - principalComponent, 2, MidpointRounding.AwayFromZero);
            if (i == months)
            {
                closing = 0;
                principalComponent = opening;
                interest = decimal.Round(installmentAmount - principalComponent, 2, MidpointRounding.AwayFromZero);
            }

            items.Add(new LoanRepaymentScheduleItemDto(
                ScheduleItemId: Guid.NewGuid(),
                InstallmentNumber: i,
                DueDateUtc: dueDate,
                OpeningBalance: opening,
                PrincipalComponent: principalComponent,
                InterestComponent: interest,
                InstallmentAmount: installmentAmount,
                ClosingBalance: closing,
                Status: "PENDING"));

            balance = closing;
            dueDate = dueDate.AddMonths(1);
        }

        return items;
    }


    private static DateOnly GetJoinDateDateOnly(object? value)
    {
        return value switch
        {
            DateOnly dateOnly => dateOnly,
            DateTime dateTime => DateOnly.FromDateTime(dateTime),
            DateTimeOffset dateTimeOffset => DateOnly.FromDateTime(dateTimeOffset.UtcDateTime),
            _ => DateOnly.FromDateTime(DateTime.UtcNow)
        };
    }

    private static int GetCompletedMonths(DateOnly fromDate, DateOnly toDate)
    {
        var months = ((toDate.Year - fromDate.Year) * 12) + toDate.Month - fromDate.Month;
        if (toDate.Day < fromDate.Day)
        {
            months -= 1;
        }
        return Math.Max(months, 0);
    }

    private async Task<EmployeeProfileDto> RequireProfileAsync(string userName, CancellationToken cancellationToken)
    {
        var profile = await _employeeReadRepository.GetEmployeeProfileByUserNameAsync(userName, cancellationToken);
        return profile ?? throw new InvalidOperationException("Current employee profile could not be resolved.");
    }

    private string RequireUserName()
        => _currentUserAccessor.GetUserName() ?? throw new InvalidOperationException("Current user could not be resolved.");

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
        => await _repository.AddNotificationsAsync(notifications, cancellationToken);

    private async Task WriteAuditAsync(string eventType, string entityId, string performedBy, string details, int statusCode, CancellationToken cancellationToken)
    {
        await _auditLogService.WriteAsync(
            new AuditLogEntry(
                EventType: eventType,
                EntityName: "LoanRequest",
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
