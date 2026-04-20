'use client';

import Link from 'next/link';
import type { LoanRequest } from '@/types/loan';

type Props = {
  request: LoanRequest;
  onSubmitForApproval?: () => Promise<void>;
  submitting?: boolean;
  message?: string;
};

export function LoanDetail({ request, onSubmitForApproval, submitting, message }: Props) {
  return (
    <div className="grid" style={{ gap: 16 }}>
      <div className="card">
        <div style={{ display: 'flex', justifyContent: 'space-between', gap: 12, alignItems: 'center' }}>
          <div>
            <h2>{request.requestNumber}</h2>
            <p className="small">{request.employeeName} ({request.employeeCode})</p>
          </div>
          <div style={{ display: 'flex', gap: 8, flexWrap: 'wrap' }}>
            <Link href={`/loans/${request.requestId}/print`}>Print</Link>
            {(request.status === 'DRAFT' || request.status === 'SENT_BACK') ? <Link href={`/loans/${request.requestId}/edit`}>Edit</Link> : null}
            {(request.status === 'DRAFT' || request.status === 'SENT_BACK') && onSubmitForApproval ? (
              <button onClick={() => void onSubmitForApproval()} disabled={submitting}>{submitting ? 'Submitting...' : 'Submit for Approval'}</button>
            ) : null}
          </div>
        </div>
        {message ? <p className="small">{message}</p> : null}
        {request.validationMessages.length > 0 ? (
          <ul>
            {request.validationMessages.map((item) => <li key={item}>{item}</li>)}
          </ul>
        ) : null}
        <div className="grid grid-3" style={{ marginTop: 12 }}>
          <div><strong>Amount</strong><p className="small">{request.requestedAmount.toFixed(2)} {request.currencyCode}</p></div>
          <div><strong>Interest Rate</strong><p className="small">{request.interestRate.toFixed(2)}%</p></div>
          <div><strong>Months</strong><p className="small">{request.installmentMonths}</p></div>
          <div><strong>Monthly Installment</strong><p className="small">{request.monthlyInstallment.toFixed(2)}</p></div>
          <div><strong>Total Repayable</strong><p className="small">{request.totalRepayableAmount.toFixed(2)}</p></div>
          <div><strong>Outstanding</strong><p className="small">{request.outstandingBalance.toFixed(2)}</p></div>
          <div><strong>Status</strong><p className="small">{request.status}</p></div>
          <div><strong>Pending With</strong><p className="small">{request.pendingWithRole ?? '-'}</p></div>
          <div><strong>Payroll Handoff</strong><p className="small">{request.payrollHandoffStatus}</p></div>
        </div>
      </div>

      <div className="card">
        <h3>Repayment Schedule</h3>
        <table style={{ width: '100%', borderCollapse: 'collapse' }}>
          <thead>
            <tr>
              <th align="left">#</th>
              <th align="left">Due Date</th>
              <th align="left">Opening</th>
              <th align="left">Principal</th>
              <th align="left">Interest</th>
              <th align="left">Installment</th>
              <th align="left">Closing</th>
            </tr>
          </thead>
          <tbody>
            {request.repaymentSchedule.map((item) => (
              <tr key={item.scheduleItemId} style={{ borderTop: '1px solid #e2e8f0' }}>
                <td>{item.installmentNumber}</td>
                <td>{new Date(item.dueDateUtc).toLocaleDateString()}</td>
                <td>{item.openingBalance.toFixed(2)}</td>
                <td>{item.principalComponent.toFixed(2)}</td>
                <td>{item.interestComponent.toFixed(2)}</td>
                <td>{item.installmentAmount.toFixed(2)}</td>
                <td>{item.closingBalance.toFixed(2)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      <div className="card">
        <h3>Workflow History</h3>
        {request.workflowActions.length === 0 ? <p className="small">No workflow actions yet.</p> : null}
        {request.workflowActions.length > 0 ? (
          <table style={{ width: '100%', borderCollapse: 'collapse' }}>
            <thead>
              <tr>
                <th align="left">Action</th>
                <th align="left">By</th>
                <th align="left">Role</th>
                <th align="left">At</th>
                <th align="left">Status</th>
              </tr>
            </thead>
            <tbody>
              {request.workflowActions.map((item, index) => (
                <tr key={`${item.actionCode}-${index}`} style={{ borderTop: '1px solid #e2e8f0' }}>
                  <td>{item.actionCode}</td>
                  <td>{item.performedBy}</td>
                  <td>{item.performedRole ?? '-'}</td>
                  <td>{new Date(item.actionAtUtc).toLocaleString()}</td>
                  <td>{item.resultingStatus}</td>
                </tr>
              ))}
            </tbody>
          </table>
        ) : null}
      </div>
    </div>
  );
}
