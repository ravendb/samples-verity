import { describe, it, expect, beforeEach } from 'vitest';
import { getUserId, getUserIdWithInfo, getUserAvatarUrl } from './userId';

describe('userId utils', () => {
	beforeEach(() => {
		// Clear localStorage before each test
		localStorage.clear();
	});

	describe('getUserId', () => {
		it('should generate a new userId when localStorage is empty', () => {
			const userId = getUserId();
			expect(userId).toBeTruthy();
			expect(userId.length).toBeGreaterThan(0);
		});

		it('should return the same userId on subsequent calls', () => {
			const userId1 = getUserId();
			const userId2 = getUserId();
			expect(userId1).toBe(userId2);
		});

		it('should persist userId in localStorage', () => {
			const userId = getUserId();
			const storedUserId = localStorage.getItem('library_user_id');
			expect(storedUserId).toBe(userId);
		});
	});

	describe('getUserIdWithInfo', () => {
		it('should return isNewUser: true when localStorage is empty', () => {
			const info = getUserIdWithInfo();
			expect(info.isNewUser).toBe(true);
			expect(info.userId).toBeTruthy();
		});

		it('should return isNewUser: false when userId exists in localStorage', () => {
			// First call to create the user
			getUserIdWithInfo();

			// Second call should return isNewUser: false
			const info = getUserIdWithInfo();
			expect(info.isNewUser).toBe(false);
			expect(info.userId).toBeTruthy();
		});

		it('should return the same userId on subsequent calls', () => {
			const info1 = getUserIdWithInfo();
			const info2 = getUserIdWithInfo();
			expect(info1.userId).toBe(info2.userId);
		});

		it('should persist userId in localStorage', () => {
			const info = getUserIdWithInfo();
			const storedUserId = localStorage.getItem('library_user_id');
			expect(storedUserId).toBe(info.userId);
		});
	});

	describe('getUserAvatarUrl', () => {
		it('should return a data URI with the userId', () => {
			const userId = 'test-user-123';
			const avatarUrl = getUserAvatarUrl(userId);
			expect(avatarUrl).toContain('data:image/svg+xml');
		});

		it('should handle special characters in userId', () => {
			const userId = 'user@test.com';
			const avatarUrl = getUserAvatarUrl(userId);
			expect(avatarUrl).toContain('data:image/svg+xml');
		});
	});
});
