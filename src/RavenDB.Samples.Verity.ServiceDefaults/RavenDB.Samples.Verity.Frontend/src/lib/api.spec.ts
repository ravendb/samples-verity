import { describe, expect, it, vi, beforeEach } from 'vitest';
import { apiUrl, API_BASE_URL, callApi } from './api';

// Mock fetch globally
const mockFetch = vi.fn();
global.fetch = mockFetch;

// Mock localStorage
const mockStorage: Record<string, string> = {};
vi.stubGlobal('localStorage', {
	getItem: (key: string) => mockStorage[key] ?? null,
	setItem: (key: string, value: string) => {
		mockStorage[key] = value;
	}
});

// Mock crypto.randomUUID
vi.stubGlobal('crypto', {
	randomUUID: () => 'test-user-id'
});

describe('api.ts', () => {
	beforeEach(() => {
		vi.clearAllMocks();
		Object.keys(mockStorage).forEach((key) => delete mockStorage[key]);
	});

	describe('apiUrl', () => {
		it('should build URL with base URL and route starting with slash', () => {
			expect(apiUrl('/books', 'http://localhost:5000')).toBe('http://localhost:5000/api/books');
		});

		it('should build URL with base URL and route without leading slash', () => {
			expect(apiUrl('books', 'http://localhost:5000')).toBe('http://localhost:5000/api/books');
		});

		it('should build URL with nested route', () => {
			expect(apiUrl('/books/123', 'http://localhost:5000')).toBe(
				'http://localhost:5000/api/books/123'
			);
		});

		it('should build relative URL when base URL is empty', () => {
			expect(apiUrl('/books', '')).toBe('/api/books');
		});

		it('should handle trailing slash in base URL', () => {
			expect(apiUrl('/books', 'http://localhost:5000/')).toBe('http://localhost:5000/api/books');
		});

		it('should use API_BASE_URL by default', () => {
			// This tests the default behavior using the actual env variable
			// In test environment, BASE_API_HTTP is empty string
			expect(apiUrl('/books')).toBe(`${API_BASE_URL}/api/books`);
		});
	});

	describe('API_BASE_URL', () => {
		it('should be a string', () => {
			expect(typeof API_BASE_URL).toBe('string');
		});
	});

	describe('callApi', () => {
		it('should call fetch with correct URL and user header', async () => {
			const mockResponse = { data: 'test' };
			mockFetch.mockResolvedValueOnce({
				ok: true,
				json: async () => mockResponse
			});

			const result = await callApi('/test/route');

			expect(mockFetch).toHaveBeenCalledWith(
				expect.stringContaining('/api/test/route'),
				expect.objectContaining({
					headers: expect.objectContaining({
						'X-User-Id': 'test-user-id'
					})
				})
			);
			expect(result).toEqual(mockResponse);
		});

		it('should merge custom headers with user header', async () => {
			mockFetch.mockResolvedValueOnce({
				ok: true,
				json: async () => ({})
			});

			await callApi('/test', {
				headers: {
					'Content-Type': 'application/json'
				}
			});

			expect(mockFetch).toHaveBeenCalledWith(
				expect.any(String),
				expect.objectContaining({
					headers: expect.objectContaining({
						'X-User-Id': 'test-user-id',
						'Content-Type': 'application/json'
					})
				})
			);
		});

		it('should throw error on non-ok response', async () => {
			mockFetch.mockResolvedValueOnce({
				ok: false,
				status: 404
			});

			await expect(callApi('/test')).rejects.toThrow('API error: 404');
		});

		it('should pass through fetch options', async () => {
			mockFetch.mockResolvedValueOnce({
				ok: true,
				json: async () => ({})
			});

			await callApi('/test', {
				method: 'POST',
				body: JSON.stringify({ data: 'test' })
			});

			expect(mockFetch).toHaveBeenCalledWith(
				expect.any(String),
				expect.objectContaining({
					method: 'POST',
					body: JSON.stringify({ data: 'test' })
				})
			);
		});
	});
});
