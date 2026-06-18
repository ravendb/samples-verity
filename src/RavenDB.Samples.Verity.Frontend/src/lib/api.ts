/**
 * Minimal API helper
 */

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalPages: number;
}

// API calls use relative paths — BFF proxies them to Azure Functions.
export const API_BASE_URL: string = "";

export function apiUrl(path: string): string {
  const base = API_BASE_URL.endsWith("/")
    ? API_BASE_URL.slice(0, -1)
    : API_BASE_URL;

  const route = path.startsWith("/") ? path : `/${path}`;

  return `${base}${route}`;
}

export async function callApi<T>(
  path: string,
  options?: RequestInit,
): Promise<T> {
  // Use Headers() so merging works correctly when options.headers is a Headers instance.
  const headers = new Headers(options?.headers);
  headers.set("X-CSRF", "1");
  const res = await fetch(apiUrl(path), { ...options, headers });

  if (res.status === 401) {
    if (typeof window === "undefined") {
      throw new Error("HTTP 401 Unauthorized");
    }
    window.location.href = `/bff/login?returnUrl=${encodeURIComponent(window.location.pathname + window.location.search)}`;
    return new Promise(() => {}); // never resolves — navigation takes over
  }

  if (!res.ok) {
    const txt = await res.text().catch(() => "");
    throw new Error(`HTTP ${res.status} ${res.statusText} ${txt}`);
  }

  const ct = res.headers.get("content-type") || "";
  if (ct.includes("application/json")) {
    return (await res.json()) as T;
  }

  // fallback to plain text for non-JSON responses
  return (await res.text()) as unknown as T;
}
