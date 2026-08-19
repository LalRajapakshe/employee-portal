'use client';

import { useEffect, useState } from 'react';

import PayslipPreview from './PayslipPreview';
// @ts-ignore: Allow side-effect CSS import where no type declarations are present
import './payslip.css';

type CurrentUser = {
  userName: string;
  employeeId: number;
  branchId: number;
};

type PayrollPeriod = {
  id: number;
  description: string;
};

type PayslipLine = {
  plineNo: number;
  narration: string;
  qty: number;
  earn: string;
  paid: number;
};

type CompanyInfo = {
  logo: string | null;
};

export default function PayslipPage() {
  const [currentUser, setCurrentUser] = useState<CurrentUser | null>(null);
  const [periods, setPeriods] = useState<PayrollPeriod[]>([]);
  const [selectedMonthId, setSelectedMonthId] = useState('');

  const [payslip, setPayslip] = useState<PayslipLine[]>([]);
  const [company, setCompany] = useState<CompanyInfo | null>(null);

  const [loadingUser, setLoadingUser] = useState(true);
  const [loadingPeriods, setLoadingPeriods] = useState(false);
  const [loadingPayslip, setLoadingPayslip] = useState(false);

  const [message, setMessage] = useState('');

  // --------------------------------------------------
  // 1. Get logged-in user
  // --------------------------------------------------

  useEffect(() => {
    async function loadCurrentUser() {
      try {
        const response = await fetch('/api/auth/me');

        if (!response.ok) {
          throw new Error('Unable to get current user.');
        }

        const result = await response.json();

        if (!result?.success || !result?.data) {
          throw new Error('Current user was not found.');
        }

        setCurrentUser(result.data);
      } catch (error) {
        setMessage(
          error instanceof Error
            ? error.message
            : 'Unable to load current user.'
        );
      } finally {
        setLoadingUser(false);
      }
    }

    loadCurrentUser();
  }, []);

  // --------------------------------------------------
  // 2. Load payroll periods
  // --------------------------------------------------

  useEffect(() => {
    if (!currentUser) return;

    async function loadPeriods() {
      try {
        setLoadingPeriods(true);
        setMessage('');

        const response = await fetch('/api/payroll/periods');

        if (!response.ok) {
          throw new Error('Unable to load payroll periods.');
        }

        const result = await response.json();

        if (!result?.success) {
          throw new Error('Unable to load payroll periods.');
        }

        setPeriods(result.data ?? []);
      } catch (error) {
        setMessage(
          error instanceof Error
            ? error.message
            : 'Unable to load payroll periods.'
        );
      } finally {
        setLoadingPeriods(false);
      }
    }

    loadPeriods();
  }, [currentUser]);

  // --------------------------------------------------
  // 3. Get payslip
  // --------------------------------------------------

  async function handleViewPayslip() {
    if (!selectedMonthId) {
      setMessage('Please select a pay period.');
      return;
    }

    try {
      setLoadingPayslip(true);
      setMessage('');
      setPayslip([]);

      const response = await fetch(
        `/api/payroll/payslip?monthId=${selectedMonthId}`
      );

      if (!response.ok) {
        throw new Error('Unable to load payslip.');
      }

      const result = await response.json();

      if (!result?.success) {
        throw new Error('Unable to load payslip.');
      }

      setPayslip(result.data ?? []);
      setCompany(result.company ?? null);
    } catch (error) {
      setMessage(
        error instanceof Error
          ? error.message
          : 'Unable to load payslip.'
      );
    } finally {
      setLoadingPayslip(false);
    }
  }

  // --------------------------------------------------
  // UI
  // --------------------------------------------------

  if (loadingUser) {
    return (
      <main className="container">
        <p>Loading...</p>
      </main>
    );
  }

  return (
    <main className="container">
      <div
        className="card"
        style={{
          maxWidth: 900,
          margin: '32px auto',
        }}
      >
        <h1>My Payslip</h1>

        {currentUser && (
          <p className="small">
            Employee: {currentUser.userName}
          </p>
        )}

        <div
          style={{
            display: 'grid',
            gap: 16,
            marginTop: 24,
          }}
        >
          <label>
            <strong>Pay Period</strong>

            <select
              value={selectedMonthId}
              onChange={(e) => setSelectedMonthId(e.target.value)}
              disabled={loadingPeriods}
              style={{
                width: '100%',
                padding: 10,
                marginTop: 6,
              }}
            >
              <option value="">
                {loadingPeriods
                  ? 'Loading periods...'
                  : 'Select pay period'}
              </option>

              {periods.map((period) => (
                <option
                  key={period.id}
                  value={period.id}
                >
                  {period.description}
                </option>
              ))}
            </select>
          </label>

          <button
            type="button"
            onClick={handleViewPayslip}
            disabled={!selectedMonthId || loadingPayslip}
          >
            {loadingPayslip
              ? 'Loading Payslip...'
              : 'View Payslip'}
          </button>

          {message && (
            <p className="small">
              {message}
            </p>
          )}
        </div>

              {payslip.length > 0 && (
          <PayslipPreview
            rows={payslip}
             company={company}
          />
        )}

      </div>
    </main>
  );
}