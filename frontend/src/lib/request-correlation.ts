import { randomUUID } from 'crypto';
import type { NextRequest } from 'next/server';

export const CORRELATION_ID_HEADER = 'X-Correlation-Id';

export function getOrCreateCorrelationId(request: NextRequest | Request): string {
  const existing = request.headers.get(CORRELATION_ID_HEADER);
  return existing?.trim() || `web-${randomUUID()}`;
}
