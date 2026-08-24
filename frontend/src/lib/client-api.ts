const APP_BASE_PATH = process.env.NEXT_PUBLIC_APP_BASE_PATH ?? '';

export function apiUrl(path: string): string {
  const normalizedPath = path.startsWith('/') ? path : `/${path}`;

  return `${APP_BASE_PATH}${normalizedPath}`;
}