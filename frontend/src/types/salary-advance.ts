export type WorkflowActionLog = {
  actionId: string;
  requestId: string;
  moduleCode: string;
  stageNumber: number;
  actionCode: string;
  performedBy: string;
  performedRole?: string | null;
  comments?: string | null;
  actionAtUtc: string;
  resultingStatus: string;
};

export type SalaryAdvancePolicy = {
  maximumAmount: number;
  requirePermanentEmployee: boolean;
  firstApproverRole: string;
  secondApproverRole: string;
  currencyCode: string;
};

export type SalaryAdvanceRequest = {
  requestId: string;
  requestNumber: string;
  employeeCode: string;
  employeeName: string;
  requestedAmount: number;
  currencyCode: string;
  reason?: string | null;
  status: string;
  pendingStageNumber: number;
  pendingWithRole?: string | null;
  payrollHandoffStatus: string;
  createdAtUtc: string;
  updatedAtUtc: string;
  submittedAtUtc?: string | null;
  workflowActions: WorkflowActionLog[];
  validationMessages: string[];
};

export type SalaryAdvanceSummary = {
  requestId: string;
  requestNumber: string;
  requestedAmount: number;
  status: string;
  payrollHandoffStatus: string;
  createdAtUtc: string;
  submittedAtUtc?: string | null;
};

export type SalaryAdvancePrintData = {
  requestNumber: string;
  employeeCode: string;
  employeeName: string;
  department: string;
  designation: string;
  requestedAmount: number;
  currencyCode: string;
  reason?: string | null;
  status: string;
  createdAtUtc: string;
  workflowActions: WorkflowActionLog[];
};

export type ApprovalInboxItem = {
  requestId: string;
  moduleCode: string;
  requestNumber: string;
  employeeCode: string;
  employeeName: string;
  requestedAmount: number;
  status: string;
  pendingStageNumber: number;
  pendingWithRole: string;
  submittedAtUtc: string;
  summary: string;
};

export type NotificationItem = {
  notificationId: string;
  recipientUserName: string;
  title: string;
  message: string;
  severity: string;
  isRead: boolean;
  createdAtUtc: string;
  linkUrl?: string | null;
};
