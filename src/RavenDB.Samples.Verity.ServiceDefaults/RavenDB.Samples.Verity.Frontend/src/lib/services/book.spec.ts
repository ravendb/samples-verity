import { describe, expect, it, vi, beforeEach } from 'vitest';
import { getBookById } from './book';

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

describe('book service', () => {
	beforeEach(() => {
		vi.clearAllMocks();
		Object.keys(mockStorage).forEach((key) => delete mockStorage[key]);
	});

	describe('getBookById', () => {
		it('should fetch book with availability from correct endpoint', async () => {
			const mockResponse = {
				id: 'Books/1',
				title: 'Test Book',
				author: {
					id: 'Authors/1',
					firstName: 'John',
					lastName: 'Doe'
				},
				availability: {
					available: 2,
					total: 5
				}
			};

			mockFetch.mockResolvedValueOnce({
				ok: true,
				json: async () => mockResponse
			});

			const result = await getBookById('1');

			expect(mockFetch).toHaveBeenCalledWith(
				expect.stringContaining('/api/books/1'),
				expect.objectContaining({
					headers: expect.objectContaining({
						'X-User-Id': 'test-user-id'
					})
				})
			);
			expect(result).toEqual(mockResponse);
			expect(result.availability?.available).toBe(2);
			expect(result.availability?.total).toBe(5);
		});

		it('should handle books with description', async () => {
			const mockResponse = {
				id: 'Books/1',
				title: 'Test Book',
				description: 'This is a test book description with a lot of words.',
				author: {
					id: 'Authors/1',
					firstName: 'John',
					lastName: 'Doe'
				}
			};

			mockFetch.mockResolvedValueOnce({
				ok: true,
				json: async () => mockResponse
			});

			const result = await getBookById('1');

			expect(result.description).toBe('This is a test book description with a lot of words.');
		});

		it('should handle books without availability', async () => {
			const mockResponse = {
				id: 'Books/2',
				title: 'Another Book',
				author: {
					id: 'Authors/2',
					firstName: 'Jane',
					lastName: 'Smith'
				}
			};

			mockFetch.mockResolvedValueOnce({
				ok: true,
				json: async () => mockResponse
			});

			const result = await getBookById('2');

			expect(result.availability).toBeUndefined();
		});

		it('should handle books without description', async () => {
			const mockResponse = {
				id: 'Books/3',
				title: 'Book Without Description',
				author: {
					id: 'Authors/3',
					firstName: 'Bob',
					lastName: 'Brown'
				}
			};

			mockFetch.mockResolvedValueOnce({
				ok: true,
				json: async () => mockResponse
			});

			const result = await getBookById('3');

			expect(result.description).toBeUndefined();
		});

		it('should throw error on non-ok response', async () => {
			mockFetch.mockResolvedValueOnce({
				ok: false,
				status: 404
			});

			await expect(getBookById('999')).rejects.toThrow('API error: 404');
		});
	});
});
