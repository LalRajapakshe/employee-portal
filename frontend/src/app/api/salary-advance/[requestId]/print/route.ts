import { NextRequest, NextResponse } from 'next/server';
import { callCoreApi } from '@/lib/core-api';
import { buildCoreSessionHeaders, clearPortalSessionCookie, getPortalSession } from '@/lib/session';
import { CORRELATION_ID_HEADER, getOrCreateCorrelationId } from '@/lib/request-correlation';
import type { SalaryAdvancePrintData } from '@/types/salary-advance';

type Payload = { success: boolean; data?: SalaryAdvancePrintData; message?: string; correlationId?: string };

export async function GET(request: NextRequest, { params }: { params: Promise<{ requestId: string }> }) {
  const correlationId = getOrCreateCorrelationId(request);
  const session = await getPortalSession();
  if (!session?.userName) {
    const response = NextResponse.json({ success: false, message: 'Not authenticated.', correlationId }, { status: 401 });
    response.headers.set(CORRELATION_ID_HEADER, correlationId);
    return response;
  }
  const { requestId } = await params;
  const result = await callCoreApi<Payload>(`/api/salary-advance/${requestId}/print`, {
    headers: buildCoreSessionHeaders(session, correlationId),
  });
  const response = NextResponse.json(result.ok ? { ...result.payload, correlationId } : { success: false, message: result.message, correlationId }, { status: result.status });
  response.headers.set(CORRELATION_ID_HEADER, correlationId);
  if (!result.ok && (result.status === 401 || result.status === 403)) clearPortalSessionCookie(response);
  return response;
}
