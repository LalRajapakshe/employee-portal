import { NextRequest, NextResponse } from 'next/server';
import { callCoreApi } from '@/lib/core-api';
import { buildCoreSessionHeaders, getPortalSession } from '@/lib/session';
import { CORRELATION_ID_HEADER, getOrCreateCorrelationId } from '@/lib/request-correlation';
import type { CurrentUser } from '@/types/current-user';
import type { EmployeeProfile } from '@/types/employee-profile';

type CurrentUserPayload = { success: boolean; data?: CurrentUser; message?: string; correlationId?: string };
type ProfilePayload = { success: boolean; data?: EmployeeProfile; message?: string; correlationId?: string };

export async function GET(request: NextRequest) {
  const correlationId = getOrCreateCorrelationId(request);
  const session = await getPortalSession();

  if (!session?.userName) {
    const response = NextResponse.json({ success: false, message: 'Not authenticated.', correlationId }, { status: 401 });
    response.headers.set(CORRELATION_ID_HEADER, correlationId);
    return response;
  }

  const headers = buildCoreSessionHeaders(session, correlationId);
  const [currentUserResult, profileResult] = await Promise.all([
    callCoreApi<CurrentUserPayload>('/api/auth/me', { method: 'GET', headers }),
    callCoreApi<ProfilePayload>('/api/employees/me', { method: 'GET', headers }),
  ]);

  const response = NextResponse.json({
    success: currentUserResult.ok && profileResult.ok,
    correlationId,
    checks: {
      currentUser: currentUserResult.ok ? 'PASS' : currentUserResult.message,
      profile: profileResult.ok ? 'PASS' : profileResult.message,
      employeeCodeMatch:
        currentUserResult.ok && profileResult.ok && currentUserResult.payload.data?.employeeCode === profileResult.payload.data?.employeeCode
          ? 'PASS'
          : 'Employee code mismatch',
    },
  }, { status: currentUserResult.ok && profileResult.ok ? 200 : 502 });
  response.headers.set(CORRELATION_ID_HEADER, correlationId);
  return response;
}
