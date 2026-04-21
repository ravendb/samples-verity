/**
 * Minimal API helper — all requests go through the BFF (same origin).
 * Credentials (session cookie) and anti-forgery header are included automatically.
 */

export interface PagedResult<T> {
	items:      T[];
	page:       number;
	pageSize:   number;
	totalPages: number;
}

// With BFF as the public entry, all API calls are same-origin.
export const API_BASE_URL = '';

export function apiUrl(path: string): string {
	const route = path.startsWith('/') ? path : `/${path}`;
	return route;
}

export async function callApi<T>(path: string, options?: RequestInit): Promise<T> {
	const method = (options?.method ?? 'GET').toUpperCase();

	const headers = new Headers(options?.headers);
	// BFF anti-forgery token required for state-changing requests
	if (method !== 'GET' && method !== 'HEAD') {
		headers.set('X-CSRF', '1');
	}

	const res = await fetch(apiUrl(path), {
		...options,
		credentials: 'include',
		headers
	});

	if (res.status === 401) {
		window.location.href = `/bff/login?returnUrl=${encodeURIComponent(window.location.pathname)}`;
		return new Promise(() => {}); // never resolves; page navigates away
	}

	if (!res.ok) {
		const txt = await res.text().catch(() => '');
		throw new Error(`HTTP ${res.status} ${res.statusText} ${txt}`);
	}

	const ct = res.headers.get('content-type') || '';
	if (ct.includes('application/json')) {
		return (await res.json()) as T;
	}

	return (await res.text()) as unknown as T;
}
