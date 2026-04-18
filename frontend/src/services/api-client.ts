export type ApiResult<T> = {
  success: boolean;
  data?: T;
  message?: string;
};

export async function getJson<T>(url: string): Promise<ApiResult<T>> {
  try {
    const response = await fetch(url, {
      method: 'GET',
      headers: { 'Content-Type': 'application/json' },
      cache: 'no-store',
    });

    let payload: ApiResult<T> | null = null;
    try {
      payload = (await response.json()) as ApiResult<T>;
    } catch {
      payload = null;
    }

    if (!response.ok) {
      return {
        success: false,
        message: payload?.message ?? `Request failed with ${response.status}`,
      };
    }

    return payload ?? { success: false, message: 'Invalid JSON response.' };
  } catch {
    return { success: false, message: 'Request could not be completed.' };
  }
}
