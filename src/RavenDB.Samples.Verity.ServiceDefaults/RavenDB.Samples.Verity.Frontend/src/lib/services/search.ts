import { callApi } from '$lib/api';
import { idToLink } from '$lib/utils/links';
import { generateShapesAvatar, generateAvataaarsAvatar } from '$lib/utils/avatar';

export interface SearchResult {
	id: string;
	type: string;
	name: string;
	imageUrl: string;
	link: string;
}

interface ApiSearchResult {
	id: string;
	query: string;
}

function transformApiResult(result: ApiSearchResult): SearchResult {
	const id = result.id;
	const seed = id; // Use the id directly as seed, no need to encode
	const prefix = id.slice(0, id.indexOf('/'));
	let type: string = '';
	let imageUrl: string = '';

	switch (prefix) {
		case 'Books':
			type = 'book';
			imageUrl = generateShapesAvatar(seed);
			break;
		case 'Authors':
			type = 'author';
			imageUrl = generateAvataaarsAvatar(seed);
			break;
	}

	// Convert "Books/123" to "/books/123" or "Authors/123" to "/authors/123"
	const link = idToLink(id) ?? '';

	return {
		id,
		type,
		name: result.query,
		imageUrl,
		link
	};
}

export async function searchBooksAndAuthors(
	query: string,
	useVector = false
): Promise<SearchResult[]> {
	if (!query.trim()) {
		return [];
	}

	const params = new URLSearchParams({
		query: query
	});

	if (useVector) {
		params.append('vector', 'true');
	}

	const apiResults = await callApi<ApiSearchResult[]>(`/search?${params.toString()}`);

	return apiResults.map(transformApiResult);
}
