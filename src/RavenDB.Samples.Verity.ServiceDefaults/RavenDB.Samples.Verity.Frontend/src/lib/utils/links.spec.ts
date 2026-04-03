import { describe, expect, it } from 'vitest';
import { idToLink } from './links';

describe('idToLink', () => {
	it('should convert Books ID to lowercase link', () => {
		expect(idToLink('Books/123')).toBe('/books/123');
	});

	it('should convert Authors ID to lowercase link', () => {
		expect(idToLink('Authors/456')).toBe('/authors/456');
	});

	it('should handle null input', () => {
		expect(idToLink(null)).toBe(null);
	});

	it('should handle undefined input', () => {
		expect(idToLink(undefined)).toBe(null);
	});

	it('should handle empty string', () => {
		expect(idToLink('')).toBe(null);
	});

	it('should preserve the structure of other IDs', () => {
		expect(idToLink('SomeCollection/789')).toBe('/somecollection/789');
	});
});
