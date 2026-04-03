/**
 * API configuration module for managing API endpoint URLs and calls.
 * Uses the BASE_API_HTTP environment variable to determine the base URL.
 */

import { getUserId } from '$lib/utils/userId';

/**
 * The base URL for API calls.
 * This is the APP_HTTP environment variable value.
 * Falls back to empty string if not set (for relative URLs in same-origin scenarios).
 */
export const API_BASE_URL: string = _BASE_API_HTTP_ ?? '';

/**
 * Builds a full API URL from the given route.
 * @param route - The API route (e.g., '/books' or 'books')
 * @param baseUrl - Optional base URL override (defaults to API_BASE_URL)
 * @returns The full API URL (e.g., 'http://localhost:5000/api/books')
 */
export function apiUrl(route: string, baseUrl: string = API_BASE_URL): string {
	const normalizedBase = baseUrl.endsWith('/') ? baseUrl.slice(0, -1) : baseUrl;
	const normalizedRoute = route.startsWith('/') ? route : `/${route}`;
	return `${normalizedBase}/api${normalizedRoute}`;
}

/**
 * Makes an API call with automatic user header injection, error handling, and JSON decoding.
 * @param route - The API route (e.g., '/user/profile')
 * @param options - Optional fetch options (method, body, etc.)
 * @returns The decoded JSON response
 * @throws Error if the response is not ok
 */
export async function callApi<T>(route: string, options?: RequestInit): Promise<T> {
	const userId = getUserId();

	const response = await fetch(apiUrl(route), {
		...options,
		headers: {
			'X-User-Id': userId,
			...options?.headers
		}
	});

	if (!response.ok) {
		throw new Error(`API error: ${response.status}`);
	}

	if (response.status === 200) return response.json();

	return {} as T;
}
