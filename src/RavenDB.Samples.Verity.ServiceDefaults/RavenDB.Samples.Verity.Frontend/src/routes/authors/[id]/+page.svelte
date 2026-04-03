<script lang="ts">
	import { page } from '$app/state';
	import { resolve } from '$app/paths';
	import { onMount } from 'svelte';
	import { getAuthorById, type Author } from '$lib/services/author';
	import TipBox from '$lib/components/TipBox.svelte';
	import { generateAvataaarsAvatar, generateShapesAvatar } from '$lib/utils/avatar';

	let author = $state<Author | null>(null);
	let loading = $state(true);
	let notFound = $state(false);
	let error = $state<string | null>(null);

	// Generate avatars locally
	const authorAvatarUrl = $derived(author ? generateAvataaarsAvatar(author.id) : '');
	const getBookCoverUrl = (bookId: string) => generateShapesAvatar(bookId);

	onMount(async () => {
		const id = page.params.id;

		if (!id) {
			notFound = true;
			loading = false;
			return;
		}

		try {
			author = await getAuthorById(id);
		} catch (e) {
			if (e instanceof Error && e.message.includes('404')) {
				notFound = true;
			} else {
				error = e instanceof Error ? e.message : 'Failed to load author';
			}
		} finally {
			loading = false;
		}
	});
</script>

<svelte:head>
	<title
		>{author
			? `${author.firstName} ${author.lastName}`
			: notFound
				? 'Author Not Found'
				: 'Loading...'} | Library of Ravens</title
	>
</svelte:head>

<div class="page-container">
	{#if loading}
		<div class="card card-centered loading-state">
			<p>Loading...</p>
		</div>
	{:else if notFound}
		<div class="card card-centered not-found-state">
			<h1>Author Not Found</h1>
			<p>The author you're looking for doesn't exist or has been removed.</p>
			<a href={resolve('/')} class="link-primary">← Back to Home</a>
		</div>
	{:else if error}
		<div class="card card-centered error-state">
			<h1>Error</h1>
			<p>{error}</p>
			<a href={resolve('/')} class="link-primary">← Back to Home</a>
		</div>
	{:else if author}
		<div class="author-content">
			<div class="author-left">
				<div class="card author-card">
					<div class="author-avatar avatar-round">
						<img src={authorAvatarUrl} alt="Author avatar" class="image-cover" />
					</div>
					<div class="author-info">
						<h1 class="heading-primary">{author.firstName} {author.lastName}</h1>
						<p class="meta-row">
							<span class="meta-label">ID:</span>
							<span class="meta-value">{author.id}</span>
						</p>
					</div>
				</div>
			</div>

			<div class="author-right">
				<TipBox
					contextText="The author details page. Provides information about the author, including the list of their books. Here, you can easily navigate to a book of your interest if you only know the author."
					ravendbText="As books and authors change very infrequently, we cache them heavily, by leveraging `http` caching and `ETags`. On the server side, `.LazilyAsync` (see: [docs](https://docs.ravendb.net/7.1/client-api/session/querying/how-to-perform-queries-lazily)), to minimize the number of calls to the `RavenDB` server."
				/>
			</div>
		</div>

		{#if author.books && author.books.length > 0}
			<div class="card books-section">
				<h2 class="heading-section">Books</h2>
				<div class="books-list">
					{#each author.books as book (book.id)}
						<a href={resolve(`/books/${book.id.replace('Books/', '')}`)} class="book-item">
							<div class="book-cover-small">
								<img
									src={getBookCoverUrl(book.id)}
									alt="Book cover for {book.title}"
									class="image-cover"
								/>
							</div>
							<div class="book-item-info">
								<h3 class="book-title">{book.title}</h3>
							</div>
						</a>
					{/each}
				</div>
			</div>
		{/if}
	{/if}
</div>

<style>
	.author-content {
		display: flex;
		gap: var(--spacing-6);
	}

	.author-left {
		flex: 1;
	}

	.author-right {
		flex: 1;
		display: flex;
		flex-direction: column;
	}

	.author-card {
		display: flex;
		gap: var(--spacing-6);
		align-items: center;
		height: 100%;
	}

	.author-avatar {
		flex-shrink: 0;
		width: 120px;
		height: 120px;
	}

	.author-info {
		flex: 1;
	}

	.books-section {
		margin-top: var(--spacing-6);
	}

	.books-list {
		display: grid;
		grid-template-columns: repeat(4, 1fr);
		gap: var(--spacing-3);
	}

	.book-item {
		display: flex;
		flex-direction: column;
		gap: var(--spacing-2);
		padding: var(--spacing-3);
		border: 1px solid var(--color-gray-200);
		border-radius: var(--radius-md);
		text-decoration: none;
		color: inherit;
		transition:
			background-color 0.2s,
			border-color 0.2s;
	}

	.book-item:hover {
		background-color: var(--color-gray-50);
		border-color: var(--color-blue-600);
	}

	.book-cover-small {
		width: 100%;
		aspect-ratio: 3 / 4;
		background: var(--color-gray-100);
		border-radius: var(--radius-sm);
		overflow: hidden;
	}

	.book-item-info {
		flex: 1;
	}

	.book-title {
		font-size: var(--font-size-sm);
		font-weight: 500;
		color: var(--color-gray-900);
		margin: 0;
		line-height: 1.4;
	}

	@media (max-width: 799px) {
		.author-content {
			flex-direction: column;
		}

		.books-list {
			grid-template-columns: repeat(2, 1fr);
		}
	}

	@media (max-width: 479px) {
		.books-list {
			grid-template-columns: 1fr;
		}
	}
</style>
