import { NextRequest, NextResponse } from 'next/server';
import { callCoreApi } from '@/lib/core-api';
import { buildCoreSessionHeaders, clearPortalSessionCookie, getPortalSession } from '@/lib/session';
import { CORRELATION_ID_HEADER, getOrCreateCorrelationId } from '@/lib/request-correlation';
import type { LoanRequest } from '@/types/loan';

type Payload = { success: boolean; data?: LoanRequest; message?: string; correlationId?: string };

async function requireSession(request: NextRequest) {
  const correlationId = getOrCreateCorrelationId(request);
  const session = await getPortalSession();
  if (!session?.userName) {
    const response = NextResponse.json({ success: false, message: 'Not authenticated.', correlationId }, { status: 401 });
    response.headers.set(CORRELATION_ID_HEADER, correlationId);
    return { correlationId, session: null, early: response };
  }
  return { correlationId, session, early: null as NextResponse | null };
}

export async function GET(request: NextRequest, { params }: { params: Promise<{ requestId: string }> }) {
  const { correlationId, session, early } = await requireSession(request);
  if (!session) return early!;
  const { requestId } = await params;
  const result = await callCoreApi<Payload>(`/api/loans/${requestId}`, { headers: buildCoreSessionHeaders(session, correlationId) });
  const response = NextResponse.json(result.ok ? { ...result.payload, correlationId } : { success: false, message: result.message, correlationId }, { status: result.status });
  response.headers.set(CORRELATION_ID_HEADER, correlationId);
  if (!result.ok && (result.status === 401 || result.status === 403)) clearPortalSessionCookie(response);
  return response;
}

export async function PUT(request: NextRequest, { params }: { params: Promise<{ requestId: string }> }) {
  const { correlationId, session, early } = await requireSession(request);
  if (!session) return early!;
  const { requestId } = await params;
  const body = await request.json();
  const result = await callCoreApi<Payload>(`/api/loans/${requestId}`, {
    method: 'PUT',
    headers: buildCoreSessionHeaders(session, correlationId),
    body: JSON.stringify(body),
  });
  const response = NextResponse.json(result.ok ? { ...result.payload, correlationId } : { success: false, message: result.message, correlationId }, { status: result.status });
  response.headers.set(CORRELATION_ID_HEADER, correlationId);
  if (!result.ok && (result.status === 401 || result.status === 403)) clearPortalSessionCookie(response);
  return response;
}
