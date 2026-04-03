import { callApi } from '$lib/api';

export interface AuthorBook {
	id: string;
	title: string;
}

export interface Author {
	id: string;
	firstName: string;
	lastName: string;
	books?: AuthorBook[];
}

/**
 * Fetches an author by their ID from the API.
 * @param id - The author ID (e.g., "123" for "Authors/123")
 * @returns The author data
 * @throws Error if the author is not found or API fails
 */
export async function getAuthorById(id: string): Promise<Author> {
	return callApi<Author>(`/authors/${id}`);
}
