export function apiUrl(
  path: string,
  baseUrl: string = import.meta.env.VITE_API_URL ?? '',
): string {
  const base = baseUrl.replace(/\/+$/, '');
  const suffix = path.startsWith('/') ? path : `/${path}`;
  return `${base}${suffix}`;
}
