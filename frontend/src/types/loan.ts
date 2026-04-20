export type LoanPolicy = {
  maximumAmount: number;
  maximumMonths: number;
  interestRate: number;
  requirePermanentEmployee: boolean;
  minimumServiceMonths: number;
  waitingMonthsAfterCompletion: number;
  firstApproverRole: string;
  secondApproverRole: string;
  currencyCode: string;
};

export type LoanEligibility = {
  isEligible: boolean;
  reasons: string[];
  maximumAmount: number;
  maximumMonths: number;
  interestRate: number;
  hasActiveLoan: boolean;
  eligibleOnUtc?: string | null;
};

export type LoanRepaymentScheduleItem = {
  scheduleItemId: string;
  installmentNumber: number;
  dueDateUtc: string;
  openingBalance: number;
  principalComponent: number;
  interestComponent: number;
  installmentAmount: number;
  closingBalance: number;
  status: string;
};

export type LoanRequest = {
  requestId: string;
  requestNumber: string;
  employeeCode: string;
  employeeName: string;
  requestedAmount: number;
  interestRate: number;
  installmentMonths: number;
  monthlyInstallment: number;
  totalRepayableAmount: number;
  outstandingBalance: number;
  currencyCode: string;
  reason?: string | null;
  status: string;
  pendingStageNumber: number;
  pendingWithRole?: string | null;
  payrollHandoffStatus: string;
  createdAtUtc: string;
  updatedAtUtc: string;
  submittedAtUtc?: string | null;
  approvedAtUtc?: string | null;
  completedAtUtc?: string | null;
  repaymentSchedule: LoanRepaymentScheduleItem[];
  workflowActions: Array<{
    workflowActionId?: string;
    moduleCode?: string;
    requestId?: string;
    stageNumber: number;
    actionCode: string;
    performedBy: string;
    performedRole?: string | null;
    comments?: string | null;
    actionAtUtc: string;
    resultingStatus: string;
  }>;
  validationMessages: string[];
};

export type LoanSummary = {
  requestId: string;
  requestNumber: string;
  requestedAmount: number;
  installmentMonths: number;
  monthlyInstallment: number;
  outstandingBalance: number;
  status: string;
  payrollHandoffStatus: string;
  createdAtUtc: string;
  submittedAtUtc?: string | null;
};

export type LoanDashboardSummary = {
  hasActiveLoan: boolean;
  outstandingBalance: number;
  activeLoanCount: number;
  currencyCode: string;
};

export type LoanPrint = {
  requestNumber: string;
  employeeCode: string;
  employeeName: string;
  department: string;
  designation: string;
  requestedAmount: number;
  interestRate: number;
  installmentMonths: number;
  monthlyInstallment: number;
  totalRepayableAmount: number;
  outstandingBalance: number;
  currencyCode: string;
  reason?: string | null;
  status: string;
  createdAtUtc: string;
  repaymentSchedule: LoanRepaymentScheduleItem[];
  workflowActions: LoanRequest['workflowActions'];
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
