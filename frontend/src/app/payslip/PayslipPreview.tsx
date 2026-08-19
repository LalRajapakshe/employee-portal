'use client';

type PayslipRow = {
  plineNo: number;
  narration: string;
  qty: number;
  earn: string;
  paid: number;
};

type CompanyInfo = {
  logo: string | null;
};

type PayslipPreviewProps = {
  rows: PayslipRow[];
  company: CompanyInfo | null;
};

const clean = (value: string | number | null | undefined) => {
  if (value === null || value === undefined) return '';

  const text = String(value).trim();

  if (
    text === '' ||
    text === '0' ||
    text === '0.00'
  ) {
    return '';
  }

  return text;
};

const isSection = (narration: string) => {
  const value = narration.trim().toUpperCase();

  return (
    value === 'EARNINGS' ||
    value === 'DEDUCTIONS' ||
    value === 'COMPANY CONTRIBUTION'
  );
};

const isSummary = (narration: string) => {
  const value = narration.trim().toUpperCase();

  return (
    value === 'TOTAL BASIC SALARY' ||
    value === 'GROSS SALARY' ||
    value === 'DEDUCTION FOR GROSS' ||
    value === 'NET GROSS' ||
    value === 'TOTAL DEDUCTION' ||
    value === 'NET SALARY' ||
    value === 'LOAN BALANCE DUE'
  );
};

const isEmployeeInfo = (narration: string) => {
  const value = narration.trim().toUpperCase();

  return (
    value === 'PAYROLL PERIOD' ||
    value === 'EMP NO' ||
    value === 'EMP NAME' ||
    value === 'DEPARTMENT' ||
    value === 'DESIGNATION'
  );
};

export default function PayslipPreview({
  rows,
  company,
}: PayslipPreviewProps) {
  const payrollPeriod =
    rows.find(
      (r) =>
        r.narration.trim().toUpperCase() ===
        'PAYROLL PERIOD'
    )?.earn ?? '';

  const employeeNo =
    rows.find(
      (r) =>
        r.narration.trim().toUpperCase() ===
        'EMP NO'
    )?.earn ?? '';

  const employeeName =
    rows.find(
      (r) =>
        r.narration.trim().toUpperCase() ===
        'EMP NAME'
    )?.earn ?? '';

  const department =
    rows.find(
      (r) =>
        r.narration.trim().toUpperCase() ===
        'DEPARTMENT'
    )?.earn ?? '';

  const designation =
    rows.find(
      (r) =>
        r.narration.trim().toUpperCase() ===
        'DESIGNATION'
    )?.earn ?? '';

  const bodyRows = rows.filter(
    (row) => !isEmployeeInfo(row.narration)
  );

  return (
    <>
      <div className="payslip-toolbar">
        <button
          type="button"
          onClick={() => window.print()}
        >
          Print Payslip
        </button>
      </div>

      <div className="payslip-page">

        {/* HEADER / LOGO AREA */}
        <header className="payslip-header">

          <div className="payslip-logo">
            {/* Company logo will be placed here */}
                {company?.logo ? (
                  <img
                    src={company.logo}
                    alt="Company Logo"
                    className="company-logo"
                  />
                ) : (
                  <div className="logo-placeholder">
                    COMPANY LOGO
                  </div>
                )}
          </div>

          <div className="payslip-title">
            <h1>PAYSLIP</h1>
            <div className="title-period">
              {clean(payrollPeriod)}
            </div>
          </div>

        </header>

        {/* EMPLOYEE INFORMATION */}
        <section className="employee-info">

          <div className="info-row">
            <span className="info-label">
              Pay Period
            </span>

            <span className="info-value">
              {clean(payrollPeriod)}
            </span>
          </div>

          <div className="info-row">
            <span className="info-label">
              Employee No
            </span>

            <span className="info-value">
              {clean(employeeNo)}
            </span>
          </div>

          <div className="info-row">
            <span className="info-label">
              Employee Name
            </span>

            <span className="info-value">
              {clean(employeeName)}
            </span>
          </div>

          <div className="info-row">
            <span className="info-label">
              Department
            </span>

            <span className="info-value">
              {clean(department)}
            </span>
          </div>

          <div className="info-row">
            <span className="info-label">
              Designation
            </span>

            <span className="info-value">
              {clean(designation)}
            </span>
          </div>

        </section>

        {/* PAYROLL TABLE */}
        <table className="payslip-table">

          <thead>
            <tr>
              <th>Description</th>
              <th className="number-column">
                Qty
              </th>
              <th className="number-column">
                Earn
              </th>
              <th className="number-column">
                Paid
              </th>
            </tr>
          </thead>

          <tbody>

            {bodyRows.map((row) => {

              const narration =
                row.narration.trim();

              if (isSection(narration)) {
                return (
                  <tr
                    key={row.plineNo}
                    className="section-row"
                  >
                    <td colSpan={4}>
                      {narration}
                    </td>
                  </tr>
                );
              }

              if (isSummary(narration)) {
                return (
                  <tr
                    key={row.plineNo}
                    className="summary-row"
                  >
                    <td>
                      {narration}
                    </td>

                    <td className="number-column">
                      {clean(row.qty)}
                    </td>

                    <td className="number-column">
                      {clean(row.earn)}
                    </td>

                    <td className="number-column">
                      {clean(row.paid)}
                    </td>
                  </tr>
                );
              }

              return (
                <tr key={row.plineNo}>
                  <td>
                    {narration}
                  </td>

                  <td className="number-column">
                    {clean(row.qty)}
                  </td>

                  <td className="number-column">
                    {clean(row.earn)}
                  </td>

                  <td className="number-column">
                    {clean(row.paid)}
                  </td>
                </tr>
              );
            })}

          </tbody>

        </table>

        <footer className="payslip-footer">
            <div className="payslip-print-notice">
            <p>
              This is an electronically generated pay slip.
            </p>
            <p>
              Confidentiality notice: This document contains private personal data.
              If received in error, please notify HR immediately.
            </p>
            </div>
        </footer>

      </div>
    </>
  );
}