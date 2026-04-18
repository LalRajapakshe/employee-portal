export type CoreApiSuccess<T> = {
  ok: true;
  status: number;
  payload: T;
  correlationId?: string;
};

export type CoreApiFailure = {
  ok: false;
  status: number;
  message: string;
  correlationId?: string;
};

export type CoreApiResult<T> = CoreApiSuccess<T> | CoreApiFailure;

const DEFAULT_CORE_API_TIMEOUT_MS = 8000;

export async function callCoreApi<T>(
  path: string,
  init?: RequestInit & { timeoutMs?: number },
): Promise<CoreApiResult<T>> {
  const baseUrl = process.env.CORE_API_BASE_URL ?? 'http://localhost:5000';
  const controller = new AbortController();
  const timeout = setTimeout(() => controller.abort(), init?.timeoutMs ?? DEFAULT_CORE_API_TIMEOUT_MS);

  try {
    const response = await fetch(`${baseUrl}${path}`, {
      ...init,
      cache: 'no-store',
      signal: controller.signal,
      headers: {
        'Content-Type': 'application/json',
        ...(init?.headers ?? {}),
      },
    });

    const correlationId = response.headers.get('X-Correlation-Id') ?? undefined;

    let payload: T | null = null;
    try {
      payload = (await response.json()) as T;
    } catch {
      payload = null;
    }

    if (!response.ok) {
      const message = extractMessage(payload) ?? `Core API request failed with ${response.status}.`;
      return { ok: false, status: response.status, message, correlationId };
    }

    if (payload === null) {
      return {
        ok: false,
        status: 502,
        message: 'Core API returned an empty or invalid JSON payload.',
        correlationId,
      };
    }

    return { ok: true, status: response.status, payload, correlationId };
  } catch (error) {
    const isAbort = error instanceof Error && error.name === 'AbortError';
    return {
      ok: false,
      status: isAbort ? 504 : 502,
      message: isAbort ? 'Core Backend Layer timed out.' : 'Unable to reach Core Backend Layer.',
    };
  } finally {
    clearTimeout(timeout);
  }
}

function extractMessage(value: unknown): string | undefined {
  if (!value || typeof value !== 'object') {
    return undefined;
  }

  const candidate = (value as { message?: unknown }).message;
  return typeof candidate === 'string' && candidate.trim().length > 0 ? candidate : undefined;
}
