/**
 * Minimal API helper
 */

export const API_BASE_URL: string = _BASE_API_HTTP_ ?? '';

export function apiUrl(path: string): string {
	const base = API_BASE_URL.endsWith('/')
		? API_BASE_URL.slice(0, -1)
		: API_BASE_URL;

	const route = path.startsWith('/') ? path : `/${path}`;

	return `${base}${route}`;
}

export async function callApi<T>(path: string, options?: RequestInit): Promise<T> {
	const res = await fetch(apiUrl(path), options);

	if (!res.ok) {
		const txt = await res.text().catch(() => '');
		throw new Error(`HTTP ${res.status} ${res.statusText} ${txt}`);
	}

	const ct = res.headers.get('content-type') || '';
	if (ct.includes('application/json')) {
		return (await res.json()) as T;
	}

	// fallback to plain text for non-JSON responses
	return (await res.text()) as unknown as T;
}