import { createAvatar } from '@dicebear/core';
import { funEmoji, shapes, avataaars } from '@dicebear/collection';

/**
 * Generate a fun-emoji style avatar as a data URI
 * @param seed - The seed string for generating the avatar
 * @returns A data URI string containing the SVG avatar
 */
export function generateFunEmojiAvatar(seed: string): string {
	const avatar = createAvatar(funEmoji, {
		seed
	});
	return avatar.toDataUri();
}

/**
 * Generate a shapes style avatar as a data URI
 * @param seed - The seed string for generating the avatar
 * @returns A data URI string containing the SVG avatar
 */
export function generateShapesAvatar(seed: string): string {
	const avatar = createAvatar(shapes, {
		seed
	});
	return avatar.toDataUri();
}

/**
 * Generate an avataaars style avatar as a data URI
 * @param seed - The seed string for generating the avatar
 * @returns A data URI string containing the SVG avatar
 */
export function generateAvataaarsAvatar(seed: string): string {
	const avatar = createAvatar(avataaars, {
		seed
	});
	return avatar.toDataUri();
}
