<script lang="ts">
	import { resolve } from '$app/paths';
	import type { Book } from '$lib/services/book';
	import { generateShapesAvatar } from '$lib/utils/avatar';

	interface Props {
		book: Book;
	}

	let { book }: Props = $props();

	// Generate avatar locally
	const bookCoverUrl = $derived(generateShapesAvatar(book.id));
</script>

<div class="book-card">
	<a href={resolve(`/books/${book.id.replace('Books/', '')}`)} class="book-link">
		<div class="book-cover">
			<img src={bookCoverUrl} alt="Book cover for {book.title}" class="image-cover" />
		</div>
		<div class="book-info">
			<h3 class="book-title">{book.title}</h3>
		</div>
	</a>
	{#if book.author}
		<a
			href={resolve(`/authors/${book.author.id.replace('Authors/', '')}`)}
			class="book-author link-primary"
		>
			{book.author.firstName}
			{book.author.lastName}
		</a>
	{/if}
</div>

<style>
	.book-card {
		display: flex;
		flex-direction: column;
		gap: var(--spacing-2);
		padding: var(--spacing-3);
		border: 1px solid var(--color-gray-200);
		border-radius: var(--radius-md);
		transition:
			background-color 0.2s,
			border-color 0.2s;
	}

	.book-card:hover {
		background-color: var(--color-gray-50);
		border-color: var(--color-blue-600);
	}

	.book-link {
		display: flex;
		flex-direction: column;
		gap: var(--spacing-2);
		text-decoration: none;
		color: inherit;
	}

	.book-cover {
		width: 100%;
		aspect-ratio: 3 / 4;
		background: var(--color-gray-100);
		border-radius: var(--radius-sm);
		overflow: hidden;
	}

	.book-info {
		flex: 1;
	}

	.book-title {
		font-size: var(--font-size-sm);
		font-weight: 500;
		color: var(--color-gray-900);
		margin: 0;
		line-height: 1.4;
	}

	.book-author {
		font-size: var(--font-size-xs);
		line-height: 1.4;
	}
</style>
