'use client';

import { useEffect, useState } from 'react';
import { getJson } from '@/services/api-client';
import type { EmployeeProfile } from '@/types/employee-profile';

type ViewState = {
  loading: boolean;
  message?: string;
  profile?: EmployeeProfile;
};

export function ProfileContent() {
  const [state, setState] = useState<ViewState>({ loading: true });

  useEffect(() => {
    let active = true;

    async function load() {
      const result = await getJson<EmployeeProfile>('/api/profile/me');
      if (!active) return;

      if (!result.success || !result.data) {
        setState({ loading: false, message: result.message ?? 'Employee profile could not be loaded.' });
        return;
      }

      setState({ loading: false, profile: result.data });
    }

    load();
    return () => {
      active = false;
    };
  }, []);

  if (state.loading) {
    return (
      <div className="card">
        <p className="small">Loading profile...</p>
      </div>
    );
  }

  if (state.message || !state.profile) {
    return (
      <div className="card">
        <p className="small">{state.message ?? 'Employee profile could not be loaded.'}</p>
      </div>
    );
  }

  return (
    <div className="card">
      <div className="grid grid-2">
        <div>
          <strong>Employee Code</strong>
          <p>{state.profile.employeeCode}</p>
        </div>
        <div>
          <strong>Full Name</strong>
          <p>{state.profile.fullName}</p>
        </div>
        <div>
          <strong>Department</strong>
          <p>{state.profile.department ?? '-'}</p>
        </div>
        <div>
          <strong>Designation</strong>
          <p>{state.profile.designation ?? '-'}</p>
        </div>
        <div>
          <strong>Join Date</strong>
          <p>{state.profile.joinDate ?? '-'}</p>
        </div>
        <div>
          <strong>Employment Status</strong>
          <p>{state.profile.employmentStatus ?? '-'}</p>
        </div>
        <div>
          <strong>Permanent</strong>
          <p>{state.profile.isPermanent ? 'Yes' : 'No'}</p>
        </div>
        <div>
          <strong>Official Email</strong>
          <p>{state.profile.officialEmail ?? '-'}</p>
        </div>
      </div>
    </div>
  );
}
