export type ApiResult<T> = {
  success: boolean;
  data?: T;
  message?: string;
};

export async function getJson<T>(url: string): Promise<ApiResult<T>> {
  const response = await fetch(url, {
    method: 'GET',
    headers: { 'Content-Type': 'application/json' },
    cache: 'no-store',
  });

  if (!response.ok) {
    return { success: false, message: `Request failed with ${response.status}` };
  }

  return response.json();
}
