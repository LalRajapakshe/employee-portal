namespace EmployeePortal.Application;

public static class WorkflowStatuses
{
    public const string Draft = "DRAFT";
    public const string PendingFirstApproval = "PENDING_FIRST_APPROVAL";
    public const string PendingSecondApproval = "PENDING_SECOND_APPROVAL";
    public const string Approved = "APPROVED";
    public const string Rejected = "REJECTED";
    public const string SentBack = "SENT_BACK";
    public const string Cancelled = "CANCELLED";
}

public static class WorkflowActions
{
    public const string Submit = "SUBMIT";
    public const string Approve = "APPROVE";
    public const string Reject = "REJECT";
    public const string SendBack = "SEND_BACK";
}

public sealed record WorkflowActionLogDto(
    Guid ActionId,
    Guid RequestId,
    string ModuleCode,
    int StageNumber,
    string ActionCode,
    string PerformedBy,
    string? PerformedRole,
    string? Comments,
    DateTimeOffset ActionAtUtc,
    string ResultingStatus
);

public sealed record ApprovalInboxItemDto(
    Guid RequestId,
    string ModuleCode,
    string RequestNumber,
    string EmployeeCode,
    string EmployeeName,
    decimal RequestedAmount,
    string Status,
    int PendingStageNumber,
    string PendingWithRole,
    DateTimeOffset SubmittedAtUtc,
    string Summary
);

public interface IWorkflowEngineService
{
    WorkflowActionLogDto CreateSubmissionLog(Guid requestId, string moduleCode, string performedBy, string resultingStatus);
    WorkflowActionLogDto CreateApprovalLog(Guid requestId, string moduleCode, int stageNumber, string actionCode, string performedBy, string? performedRole, string? comments, string resultingStatus);
}

public sealed class WorkflowEngineService : IWorkflowEngineService
{
    public WorkflowActionLogDto CreateSubmissionLog(Guid requestId, string moduleCode, string performedBy, string resultingStatus)
        => new(
            ActionId: Guid.NewGuid(),
            RequestId: requestId,
            ModuleCode: moduleCode,
            StageNumber: 0,
            ActionCode: WorkflowActions.Submit,
            PerformedBy: performedBy,
            PerformedRole: null,
            Comments: null,
            ActionAtUtc: DateTimeOffset.UtcNow,
            ResultingStatus: resultingStatus);

    public WorkflowActionLogDto CreateApprovalLog(Guid requestId, string moduleCode, int stageNumber, string actionCode, string performedBy, string? performedRole, string? comments, string resultingStatus)
        => new(
            ActionId: Guid.NewGuid(),
            RequestId: requestId,
            ModuleCode: moduleCode,
            StageNumber: stageNumber,
            ActionCode: actionCode,
            PerformedBy: performedBy,
            PerformedRole: performedRole,
            Comments: comments,
            ActionAtUtc: DateTimeOffset.UtcNow,
            ResultingStatus: resultingStatus);
}
