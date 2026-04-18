import { NextRequest, NextResponse } from 'next/server';
import { callCoreApi } from '@/lib/core-api';
import { clearPortalSessionCookie } from '@/lib/session';
import { CORRELATION_ID_HEADER, getOrCreateCorrelationId } from '@/lib/request-correlation';

type LogoutPayload = {
  success: boolean;
  message?: string;
  correlationId?: string;
};

export async function POST(request: NextRequest) {
  const correlationId = getOrCreateCorrelationId(request);

  const result = await callCoreApi<LogoutPayload>('/api/auth/logout', {
    method: 'POST',
    headers: {
      [CORRELATION_ID_HEADER]: correlationId,
    },
  });

  const response = NextResponse.json({
    success: true,
    message: result.ok ? result.payload.message ?? 'Logged out.' : 'Local session cleared.',
    correlationId: result.correlationId ?? correlationId,
  });

  response.headers.set(CORRELATION_ID_HEADER, result.correlationId ?? correlationId);
  clearPortalSessionCookie(response);
  return response;
}
