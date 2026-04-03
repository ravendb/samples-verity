import { describe, it, expect } from 'vitest';
import { generateFunEmojiAvatar, generateShapesAvatar, generateAvataaarsAvatar } from './avatar';

describe('avatar utils', () => {
	describe('generateFunEmojiAvatar', () => {
		it('should generate a data URI for fun-emoji style', () => {
			const seed = 'test-seed-123';
			const avatarUrl = generateFunEmojiAvatar(seed);
			expect(avatarUrl).toContain('data:image/svg+xml');
		});

		it('should generate consistent avatars for the same seed', () => {
			const seed = 'test-seed-456';
			const avatar1 = generateFunEmojiAvatar(seed);
			const avatar2 = generateFunEmojiAvatar(seed);
			expect(avatar1).toBe(avatar2);
		});
	});

	describe('generateShapesAvatar', () => {
		it('should generate a data URI for shapes style', () => {
			const seed = 'book-id-789';
			const avatarUrl = generateShapesAvatar(seed);
			expect(avatarUrl).toContain('data:image/svg+xml');
		});

		it('should generate different avatars for different seeds', () => {
			const avatar1 = generateShapesAvatar('seed1');
			const avatar2 = generateShapesAvatar('seed2');
			expect(avatar1).not.toBe(avatar2);
		});
	});

	describe('generateAvataaarsAvatar', () => {
		it('should generate a data URI for avataaars style', () => {
			const seed = 'author-id-101';
			const avatarUrl = generateAvataaarsAvatar(seed);
			expect(avatarUrl).toContain('data:image/svg+xml');
		});

		it('should generate consistent avatars for the same seed', () => {
			const seed = 'author-id-202';
			const avatar1 = generateAvataaarsAvatar(seed);
			const avatar2 = generateAvataaarsAvatar(seed);
			expect(avatar1).toBe(avatar2);
		});
	});
});
