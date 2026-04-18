import { NextRequest, NextResponse } from 'next/server';
import { callCoreApi } from '@/lib/core-api';
import { buildCoreSessionHeaders, clearPortalSessionCookie, getPortalSession } from '@/lib/session';
import { CORRELATION_ID_HEADER, getOrCreateCorrelationId } from '@/lib/request-correlation';
import type { SalaryAdvancePolicy, SalaryAdvanceRequest, SalaryAdvanceSummary } from '@/types/salary-advance';

type ListPayload = {
  success: boolean;
  data?: {
    policy?: SalaryAdvancePolicy | null;
    items?: SalaryAdvanceSummary[];
  };
  message?: string;
  correlationId?: string;
};

type CreatePayload = {
  success: boolean;
  data?: SalaryAdvanceRequest;
  message?: string;
  correlationId?: string;
};

async function requireSession(request: NextRequest) {
  const correlationId = getOrCreateCorrelationId(request);
  const session = await getPortalSession();
  if (!session?.userName) {
    const response = NextResponse.json(
      { success: false, message: 'Not authenticated.', correlationId },
      { status: 401 },
    );
    response.headers.set(CORRELATION_ID_HEADER, correlationId);
    return { correlationId, session: null, early: response };
  }
  return { correlationId, session, early: null as NextResponse | null };
}

export async function GET(request: NextRequest) {
  const { correlationId, session, early } = await requireSession(request);
  if (!session) return early!;

  const result = await callCoreApi<ListPayload>('/api/salary-advance', {
    headers: buildCoreSessionHeaders(session, correlationId),
  });

  const response = NextResponse.json(
    result.ok
      ? { ...result.payload, correlationId }
      : {
          success: false,
          data: { policy: null, items: [] },
          message: result.message,
          correlationId,
        },
    { status: result.status },
  );

  response.headers.set(CORRELATION_ID_HEADER, correlationId);
  if (!result.ok && (result.status === 401 || result.status === 403)) {
    clearPortalSessionCookie(response);
  }
  return response;
}

export async function POST(request: NextRequest) {
  const { correlationId, session, early } = await requireSession(request);
  if (!session) return early!;

  const body = await request.json();
  const result = await callCoreApi<CreatePayload>('/api/salary-advance', {
    method: 'POST',
    headers: buildCoreSessionHeaders(session, correlationId),
    body: JSON.stringify(body),
  });

  const response = NextResponse.json(
    result.ok
      ? { ...result.payload, correlationId }
      : { success: false, message: result.message, correlationId },
    { status: result.status },
  );

  response.headers.set(CORRELATION_ID_HEADER, correlationId);
  if (!result.ok && (result.status === 401 || result.status === 403)) {
    clearPortalSessionCookie(response);
  }
  return response;
}
