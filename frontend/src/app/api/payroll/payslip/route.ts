import { NextRequest, NextResponse } from 'next/server';

import { callCoreApi } from '@/lib/core-api';
import {
  buildCoreSessionHeaders,
  clearPortalSessionCookie,
  getPortalSession,
} from '@/lib/session';
import {
  CORRELATION_ID_HEADER,
  getOrCreateCorrelationId,
} from '@/lib/request-correlation';

export async function GET(request: NextRequest) {
  const correlationId = getOrCreateCorrelationId(request);
  const session = await getPortalSession();

  if (!session?.userName) {
    const response = NextResponse.json(
      {
        success: false,
        message: 'Not authenticated.',
        correlationId,
      },
      { status: 401 }
    );

    response.headers.set(
      CORRELATION_ID_HEADER,
      correlationId
    );

    return response;
  }

  const monthId = request.nextUrl.searchParams.get('monthId');

  if (!monthId) {
    return NextResponse.json(
      {
        success: false,
        message: 'monthId is required.',
        correlationId,
      },
      { status: 400 }
    );
  }

  const result = await callCoreApi(
    `/api/payroll/payslip?monthId=${encodeURIComponent(monthId)}`,
    {
      method: 'GET',
      headers: buildCoreSessionHeaders(
        session,
        correlationId
      ),
    }
  );

  if (!result.ok) {
    const response = NextResponse.json(
      {
        success: false,
        message: result.message,
        correlationId:
          result.correlationId ?? correlationId,
      },
      {
        status: result.status,
      }
    );

    response.headers.set(
      CORRELATION_ID_HEADER,
      result.correlationId ?? correlationId
    );

    if (
      result.status === 401 ||
      result.status === 403
    ) {
      clearPortalSessionCookie(response);
    }

    return response;
  }

  const response = NextResponse.json(
    {
      ...result.payload,
      correlationId:
        result.correlationId ?? correlationId,
    },
    {
      status: result.status,
    }
  );

  response.headers.set(
    CORRELATION_ID_HEADER,
    result.correlationId ?? correlationId
  );

  return response;
}