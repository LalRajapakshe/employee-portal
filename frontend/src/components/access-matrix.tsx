'use client';

import type { CurrentUser } from '@/types/current-user';
import { getAccessMatrix } from '@/lib/access-control';

type AccessMatrixProps = {
  user: CurrentUser;
};

export function AccessMatrix({ user }: AccessMatrixProps) {
  const rows = getAccessMatrix(user);

  return (
    <div className="card">
      <h3>Access Matrix</h3>
      <p className="small">This page shows how the current roles and permissions resolve into route access.</p>

      <div style={{ overflowX: 'auto', marginTop: 16 }}>
        <table style={{ width: '100%', borderCollapse: 'collapse' }}>
          <thead>
            <tr>
              <th style={{ textAlign: 'left', padding: '10px 8px' }}>Route</th>
              <th style={{ textAlign: 'left', padding: '10px 8px' }}>Required Permissions</th>
              <th style={{ textAlign: 'left', padding: '10px 8px' }}>Required Roles</th>
              <th style={{ textAlign: 'left', padding: '10px 8px' }}>Access</th>
            </tr>
          </thead>
          <tbody>
            {rows.map((row) => (
              <tr key={row.href} style={{ borderTop: '1px solid rgba(255,255,255,0.08)' }}>
                <td style={{ padding: '10px 8px' }}>
                  <strong>{row.label}</strong>
                  <div className="small">{row.href}</div>
                </td>
                <td style={{ padding: '10px 8px' }}>{row.requiredPermissions.join(', ') || '-'}</td>
                <td style={{ padding: '10px 8px' }}>{row.requiredRoles.join(', ') || '-'}</td>
                <td style={{ padding: '10px 8px' }}>
                  <span className={row.allowed ? 'badge badge-success' : 'badge badge-muted'}>
                    {row.allowed ? 'ALLOWED' : 'BLOCKED'}
                  </span>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}
