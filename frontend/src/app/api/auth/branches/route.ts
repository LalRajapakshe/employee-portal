import { NextRequest, NextResponse } from 'next/server';
import { callCoreApi } from '@/lib/core-api';
import { CORRELATION_ID_HEADER, getOrCreateCorrelationId } from '@/lib/request-correlation';

type BranchOption = {
  value: number;
  text: string;
};

type BranchPayload = {
  success: boolean;
  data?: BranchOption[];
  message?: string;
  correlationId?: string;
};

export async function GET(request: NextRequest) {
  const correlationId = getOrCreateCorrelationId(request);

  const result = await callCoreApi<BranchPayload>('/api/auth/branches', {
    method: 'GET',
    headers: {
      [CORRELATION_ID_HEADER]: correlationId,
    },
  });

  const response = NextResponse.json(
    result.ok
      ? {
          ...result.payload,
          correlationId: result.correlationId ?? correlationId,
        }
      : {
          success: false,
          data: [],
          message: result.message,
          correlationId: result.correlationId ?? correlationId,
        },
    { status: result.status },
  );

  response.headers.set(
    CORRELATION_ID_HEADER,
    result.correlationId ?? correlationId,
  );

  return response;
}