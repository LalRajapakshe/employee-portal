import { NextRequest, NextResponse } from 'next/server';
import { callCoreApi } from '@/lib/core-api';
import { applyPortalSessionCookie } from '@/lib/session';
import { CORRELATION_ID_HEADER, getOrCreateCorrelationId } from '@/lib/request-correlation';
import type { CurrentUser } from '@/types/current-user';

type LoginPayload = {
  success: boolean;
  data?: CurrentUser;
  message?: string;
  correlationId?: string;
};

export async function POST(request: NextRequest) {
  const correlationId = getOrCreateCorrelationId(request);

  let body: { userName?: string; password?: string };
  try {
    body = await request.json();
  } catch {
    const response = NextResponse.json({ success: false, message: 'Invalid login request.', correlationId }, { status: 400 });
    response.headers.set(CORRELATION_ID_HEADER, correlationId);
    return response;
  }

  const result = await callCoreApi<LoginPayload>('/api/auth/login', {
    method: 'POST',
    body: JSON.stringify({ userName: body.userName, password: body.password }),
    headers: {
      [CORRELATION_ID_HEADER]: correlationId,
    },
  });

  if (!result.ok) {
    const response = NextResponse.json(
      { success: false, message: result.message, correlationId: result.correlationId ?? correlationId },
      { status: result.status },
    );
    response.headers.set(CORRELATION_ID_HEADER, result.correlationId ?? correlationId);
    return response;
  }

  if (!result.payload.success || !result.payload.data) {
    const response = NextResponse.json(
      { success: false, message: result.payload.message ?? 'Login failed.', correlationId: result.correlationId ?? correlationId },
      { status: 400 },
    );
    response.headers.set(CORRELATION_ID_HEADER, result.correlationId ?? correlationId);
    return response;
  }

  const response = NextResponse.json({ success: true, data: result.payload.data, correlationId: result.correlationId ?? correlationId });
  response.headers.set(CORRELATION_ID_HEADER, result.correlationId ?? correlationId);
  applyPortalSessionCookie(response, result.payload.data);
  return response;
}
