<script lang="ts">
	import { page } from '$app/state';
	import { resolve } from '$app/paths';
	import { onMount, onDestroy } from 'svelte';
	import { getBookById, type Book } from '$lib/services/book';
	import { borrowBook } from '$lib/services/user';
	import TipBox from '$lib/components/TipBox.svelte';
	import { updateNotificationCount } from '$lib/stores/notifications';
	import { generateShapesAvatar } from '$lib/utils/avatar';

	let book = $state<Book | null>(null);
	let loading = $state(true);
	let notFound = $state(false);
	let error = $state<string | null>(null);
	let showBorrowedPopup = $state(false);
	let popupTimeoutId: ReturnType<typeof setTimeout> | null = null;
	let isBorrowing = $state(false);

	// Generate book cover locally
	const bookCoverUrl = $derived(book ? generateShapesAvatar(book.id) : '');

	async function loadBook() {
		const id = page.params.id;

		if (id === undefined) {
			notFound = true;
			loading = false;
		} else {
			try {
				book = await getBookById(id);
			} catch (e) {
				if (e instanceof Error && e.message.includes('404')) {
					notFound = true;
				} else {
					error = e instanceof Error ? e.message : 'Failed to load book';
				}
			} finally {
				loading = false;
			}
		}
	}

	onMount(async () => {
		await loadBook();
	});

	onDestroy(() => {
		if (popupTimeoutId !== null) {
			clearTimeout(popupTimeoutId);
		}
	});

	async function handleBorrow() {
		if (!book || isBorrowing) return;

		isBorrowing = true;
		try {
			await borrowBook(book.id.replace('Books/', ''));
			showBorrowedPopup = true;
			popupTimeoutId = setTimeout(() => {
				showBorrowedPopup = false;
				popupTimeoutId = null;
			}, 2000);
			// Update notification count after borrowing a book
			await updateNotificationCount();
			// Refresh book details to show updated availability
			await loadBook();
		} catch (e) {
			error = e instanceof Error ? e.message : 'Failed to borrow book';
		} finally {
			isBorrowing = false;
		}
	}
</script>

<svelte:head>
	<title>{book ? book.title : notFound ? 'Book Not Found' : 'Loading...'} | Library of Ravens</title
	>
</svelte:head>

<div class="page-container">
	{#if loading}
		<div class="card card-centered loading-state">
			<p>Loading...</p>
		</div>
	{:else if notFound}
		<div class="card card-centered not-found-state">
			<h1>Book Not Found</h1>
			<p>The book you're looking for doesn't exist or has been removed.</p>
			<a href={resolve('/')} class="link-primary">← Back to Home</a>
		</div>
	{:else if error}
		<div class="card card-centered error-state">
			<h1>Error</h1>
			<p>{error}</p>
			<a href={resolve('/')} class="link-primary">← Back to Home</a>
		</div>
	{:else if book}
		<div class="book-content">
			<div class="book-left">
				<div class="card book-card">
					<div class="book-cover">
						<img src={bookCoverUrl} alt="Book cover" class="image-cover" />
					</div>
					<div class="book-info">
						<h1 class="heading-primary">{book.title}</h1>
						<p class="meta-row">
							<span class="meta-label">ID:</span>
							<span class="meta-value">{book.id}</span>
						</p>
						{#if book.author}
							<p class="meta-row">
								<span class="meta-label">Author:</span>
								<a
									href={resolve(`/authors/${book.author.id.replace('Authors/', '')}`)}
									class="link-primary">{book.author.firstName} {book.author.lastName}</a
								>
							</p>
						{/if}
						{#if book.availability}
							<p class="meta-row">
								<span class="meta-label">Availability:</span>
								<span class="meta-value"
									>{book.availability.available} of {book.availability.total} available</span
								>
							</p>
							{#if book.availability.available > 0}
								<button class="btn-borrow" onclick={handleBorrow} disabled={isBorrowing}>
									{isBorrowing ? 'Borrowing...' : 'Borrow'}
								</button>
							{/if}
						{/if}
					</div>
				</div>
			</div>

			<div class="book-right">
				<TipBox
					contextText="The book details page. Provides information about the book, including the number of copies. You can borrow a copy of this book here if some are available."
					ravendbText="This page uses extensive http caching to reduce the egress data. Requests to the database are grouped using `.Include` (see: [docs](https://docs.ravendb.net/7.1/client-api/session/loading-entities#load-with-includes))"
				/>
			</div>
		</div>

		{#if book.description}
			<div class="card book-description-section">
				<h2 class="description-heading">Description</h2>
				<p class="description-text">{book.description}</p>
			</div>
		{/if}
	{/if}
</div>

{#if showBorrowedPopup}
	<div class="popup-overlay">
		<div class="popup">
			<p>Borrowed</p>
		</div>
	</div>
{/if}

<style>
	.book-content {
		display: flex;
		gap: var(--spacing-6);
	}

	.book-left {
		flex: 1;
	}

	.book-right {
		flex: 1;
		display: flex;
		flex-direction: column;
	}

	.book-card {
		display: flex;
		gap: var(--spacing-6);
		height: 100%;
	}

	.book-cover {
		flex-shrink: 0;
		width: 120px;
		height: 160px;
		background: var(--color-gray-100);
		border-radius: var(--radius-md);
		overflow: hidden;
	}

	.book-info {
		flex: 1;
	}

	.btn-borrow {
		margin-top: var(--spacing-4);
		padding: var(--spacing-2) var(--spacing-4);
		background: var(--color-blue-600);
		color: var(--color-white);
		border: none;
		border-radius: var(--radius-md);
		font-size: var(--font-size-base);
		font-weight: 500;
		cursor: pointer;
		transition: background 0.15s;
	}

	.btn-borrow:hover {
		background: var(--color-blue-700);
	}

	.btn-borrow:disabled {
		background: var(--color-gray-400);
		cursor: not-allowed;
	}

	.book-description-section {
		margin-top: var(--spacing-6);
	}

	.description-heading {
		font-size: var(--font-size-lg);
		font-weight: 600;
		color: var(--color-gray-900);
		margin: 0 0 var(--spacing-3) 0;
	}

	.description-text {
		font-size: var(--font-size-base);
		line-height: 1.6;
		color: var(--color-gray-700);
		margin: 0;
		white-space: pre-wrap;
		word-wrap: break-word;
		overflow-wrap: break-word;
	}

	.popup-overlay {
		position: fixed;
		top: 0;
		left: 0;
		right: 0;
		bottom: 0;
		display: flex;
		align-items: center;
		justify-content: center;
		background: rgba(0, 0, 0, 0.3);
		z-index: 100;
	}

	.popup {
		background: var(--color-white);
		padding: var(--spacing-6) var(--spacing-8);
		border-radius: var(--radius-lg);
		box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);
	}

	.popup p {
		margin: 0;
		font-size: var(--font-size-lg);
		font-weight: 600;
		color: var(--color-gray-900);
	}

	@media (max-width: 799px) {
		.book-content {
			flex-direction: column;
		}
	}
</style>
