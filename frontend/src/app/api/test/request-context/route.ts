import { NextRequest, NextResponse } from 'next/server';
import { callCoreApi } from '@/lib/core-api';
import { buildCoreSessionHeaders, getPortalSession } from '@/lib/session';
import { CORRELATION_ID_HEADER, getOrCreateCorrelationId } from '@/lib/request-correlation';

type DiagnosticPayload = {
  success: boolean;
  data?: unknown;
  message?: string;
};

export async function GET(request: NextRequest) {
  const correlationId = getOrCreateCorrelationId(request);
  const session = await getPortalSession();

  if (!session?.userName) {
    const response = NextResponse.json({ success: false, message: 'Not authenticated.', correlationId }, { status: 401 });
    response.headers.set(CORRELATION_ID_HEADER, correlationId);
    return response;
  }

  const result = await callCoreApi<DiagnosticPayload>('/api/diagnostics/request-context', {
    method: 'GET',
    headers: buildCoreSessionHeaders(session, correlationId),
  });

  const response = NextResponse.json(result.ok ? result.payload : { success: false, message: result.message, correlationId }, { status: result.status });
  response.headers.set(CORRELATION_ID_HEADER, result.correlationId ?? correlationId);
  return response;
}
