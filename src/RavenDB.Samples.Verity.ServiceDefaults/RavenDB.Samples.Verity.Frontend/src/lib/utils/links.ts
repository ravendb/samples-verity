/**
 * Converts a RavenDB document ID to a client-side URL path.
 * Handles IDs in the format "Books/123" -> "/books/123" or "Authors/456" -> "/authors/456"
 * @param id - The RavenDB document ID (e.g., "Books/123" or "Authors/456")
 * @returns The client-side URL path (e.g., "/books/123" or "/authors/456")
 */
export function idToLink(id: string | null | undefined): string | null {
	if (!id) {
		return null;
	}

	// Convert "Books/123" to "/books/123" or "Authors/123" to "/authors/123"
	return '/' + id.toLowerCase();
}
