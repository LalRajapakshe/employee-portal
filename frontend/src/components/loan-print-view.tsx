'use client';

import { useEffect, useState } from 'react';
import type { LoanPrint } from '@/types/loan';

type Props = { requestId: string };
type PrintResponse = { success: boolean; data?: LoanPrint; message?: string };

export function LoanPrintView({ requestId }: Props) {
  const [state, setState] = useState<{ loading: boolean; data?: LoanPrint; message?: string }>({ loading: true });

  useEffect(() => {
    void load();
  }, [requestId]);

  async function load() {
    try {
      const response = await fetch(`/api/loans/${requestId}/print`, { cache: 'no-store' });
      const payload = (await response.json()) as PrintResponse;
      if (!response.ok || !payload.success || !payload.data) {
        setState({ loading: false, message: payload.message ?? 'Unable to load print view.' });
        return;
      }
      setState({ loading: false, data: payload.data });
    } catch {
      setState({ loading: false, message: 'Unable to load print view.' });
    }
  }

  if (state.loading) return <div className="card"><p className="small">Loading print view...</p></div>;
  if (!state.data) return <div className="card"><p className="small">{state.message ?? 'Print data not found.'}</p></div>;

  const item = state.data;
  return (
    <div className="card" style={{ maxWidth: 960, margin: '0 auto' }}>
      <h1>Employee Loan Form</h1>
      <p className="small">Request Number: {item.requestNumber}</p>
      <div className="grid grid-3" style={{ marginTop: 16 }}>
        <div><strong>Employee</strong><p className="small">{item.employeeName}</p></div>
        <div><strong>Employee Code</strong><p className="small">{item.employeeCode}</p></div>
        <div><strong>Department</strong><p className="small">{item.department}</p></div>
        <div><strong>Designation</strong><p className="small">{item.designation}</p></div>
        <div><strong>Amount</strong><p className="small">{item.requestedAmount.toFixed(2)} {item.currencyCode}</p></div>
        <div><strong>Interest</strong><p className="small">{item.interestRate.toFixed(2)}%</p></div>
        <div><strong>Months</strong><p className="small">{item.installmentMonths}</p></div>
        <div><strong>Monthly Installment</strong><p className="small">{item.monthlyInstallment.toFixed(2)}</p></div>
        <div><strong>Total Repayable</strong><p className="small">{item.totalRepayableAmount.toFixed(2)}</p></div>
        <div><strong>Outstanding</strong><p className="small">{item.outstandingBalance.toFixed(2)}</p></div>
        <div><strong>Status</strong><p className="small">{item.status}</p></div>
        <div><strong>Created</strong><p className="small">{new Date(item.createdAtUtc).toLocaleString()}</p></div>
      </div>
      <div style={{ marginTop: 16 }}>
        <strong>Reason</strong>
        <p className="small">{item.reason ?? '-'}</p>
      </div>
      <div style={{ marginTop: 16 }}>
        <h3>Repayment Schedule</h3>
        <table style={{ width: '100%', borderCollapse: 'collapse' }}>
          <thead>
            <tr>
              <th align="left">#</th>
              <th align="left">Due Date</th>
              <th align="left">Principal</th>
              <th align="left">Interest</th>
              <th align="left">Installment</th>
              <th align="left">Closing</th>
            </tr>
          </thead>
          <tbody>
            {item.repaymentSchedule.map((scheduleItem) => (
              <tr key={scheduleItem.scheduleItemId} style={{ borderTop: '1px solid #e2e8f0' }}>
                <td>{scheduleItem.installmentNumber}</td>
                <td>{new Date(scheduleItem.dueDateUtc).toLocaleDateString()}</td>
                <td>{scheduleItem.principalComponent.toFixed(2)}</td>
                <td>{scheduleItem.interestComponent.toFixed(2)}</td>
                <td>{scheduleItem.installmentAmount.toFixed(2)}</td>
                <td>{scheduleItem.closingBalance.toFixed(2)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}
