import { describe, expect, it, vi, beforeEach } from 'vitest';
import { getUserProfile, getNotifications, deleteNotification, returnBook } from './user';

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

describe('user service', () => {
	beforeEach(() => {
		vi.clearAllMocks();
		Object.keys(mockStorage).forEach((key) => delete mockStorage[key]);
	});

	describe('getUserProfile', () => {
		it('should fetch user profile from correct endpoint', async () => {
			const mockResponse = {
				id: 'Users/test-user-id',
				borrowed: [{ id: 'book-1', title: 'Test Book', authorId: 'author-1' }]
			};

			mockFetch.mockResolvedValueOnce({
				ok: true,
				json: async () => mockResponse
			});

			const result = await getUserProfile();

			expect(mockFetch).toHaveBeenCalledWith(
				expect.stringContaining('/api/user/profile'),
				expect.objectContaining({
					headers: expect.objectContaining({
						'X-User-Id': 'test-user-id'
					})
				})
			);
			expect(result).toEqual(mockResponse);
		});

		it('should throw error on non-ok response', async () => {
			mockFetch.mockResolvedValueOnce({
				ok: false,
				status: 401
			});

			await expect(getUserProfile()).rejects.toThrow('API error: 401');
		});
	});

	describe('getNotifications', () => {
		it('should fetch notifications from correct endpoint', async () => {
			const mockNotifications = [
				{ id: 'Notifications/1', text: 'Test notification 1' },
				{ id: 'Notifications/2', text: 'Test notification 2' }
			];

			mockFetch.mockResolvedValueOnce({
				ok: true,
				json: async () => mockNotifications
			});

			const result = await getNotifications();

			expect(mockFetch).toHaveBeenCalledWith(
				expect.stringContaining('/api/user/notifications'),
				expect.objectContaining({
					headers: expect.objectContaining({
						'X-User-Id': 'test-user-id'
					})
				})
			);
			expect(result).toEqual(mockNotifications);
		});

		it('should handle notifications with referencedItemId', async () => {
			const mockNotifications = [
				{ id: 'Notifications/1', text: 'Test notification 1', referencedItemId: 'Books/123' },
				{ id: 'Notifications/2', text: 'Test notification 2' }
			];

			mockFetch.mockResolvedValueOnce({
				ok: true,
				json: async () => mockNotifications
			});

			const result = await getNotifications();

			expect(result).toEqual(mockNotifications);
			expect(result[0].referencedItemId).toBe('Books/123');
			expect(result[1].referencedItemId).toBeUndefined();
		});

		it('should throw error on non-ok response', async () => {
			mockFetch.mockResolvedValueOnce({
				ok: false,
				status: 500
			});

			await expect(getNotifications()).rejects.toThrow('API error: 500');
		});
	});

	describe('deleteNotification', () => {
		it('should delete notification with correct endpoint and method', async () => {
			const notificationId = 'Notifications/123';

			mockFetch.mockResolvedValueOnce({
				ok: true,
				json: async () => ({})
			});

			await deleteNotification(notificationId);

			expect(mockFetch).toHaveBeenCalledWith(
				expect.stringContaining(`/api/user/notifications/${notificationId}`),
				expect.objectContaining({
					method: 'DELETE',
					headers: expect.objectContaining({
						'X-User-Id': 'test-user-id'
					})
				})
			);
		});

		it('should throw error on non-ok response', async () => {
			mockFetch.mockResolvedValueOnce({
				ok: false,
				status: 403
			});

			await expect(deleteNotification('Notifications/123')).rejects.toThrow('API error: 403');
		});
	});

	describe('returnBook', () => {
		it('should return book with correct endpoint and method', async () => {
			const borrowedBookId = 'BorrowedBooks/123';

			mockFetch.mockResolvedValueOnce({
				ok: true,
				json: async () => ({})
			});

			await returnBook(borrowedBookId);

			expect(mockFetch).toHaveBeenCalledWith(
				expect.stringContaining('/api/user/books/123/return'),
				expect.objectContaining({
					method: 'POST',
					headers: expect.objectContaining({
						'X-User-Id': 'test-user-id'
					})
				})
			);
		});

		it('should normalize BorrowedBooks prefix', async () => {
			const borrowedBookId = 'BorrowedBooks/456';

			mockFetch.mockResolvedValueOnce({
				ok: true,
				json: async () => ({})
			});

			await returnBook(borrowedBookId);

			expect(mockFetch).toHaveBeenCalledWith(
				expect.stringContaining('/api/user/books/456/return'),
				expect.any(Object)
			);
		});

		it('should throw error on non-ok response', async () => {
			mockFetch.mockResolvedValueOnce({
				ok: false,
				status: 403
			});

			await expect(returnBook('BorrowedBooks/123')).rejects.toThrow('API error: 403');
		});
	});
});
