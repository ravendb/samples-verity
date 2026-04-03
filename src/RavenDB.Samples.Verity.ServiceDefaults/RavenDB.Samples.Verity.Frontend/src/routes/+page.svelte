<script lang="ts">
	import { onMount } from 'svelte';
	import image from '$lib/assets/image.webp';
	import BookCard from '$lib/components/BookCard.svelte';
	import TipBox from '$lib/components/TipBox.svelte';
	import { getHomeBooks, type Book } from '$lib/services/book';

	let books = $state<Book[]>([]);
	let loading = $state(true);
	let error = $state<string | null>(null);

	onMount(async () => {
		try {
			books = await getHomeBooks();
		} catch (e) {
			error = e instanceof Error ? e.message : 'Failed to load books';
		} finally {
			loading = false;
		}
	});
</script>

<svelte:head>
	<title>Home | Library of Ravens</title>
</svelte:head>

<div class="page-container">
	<div class="card hero-section">
		<div class="hero-content">
			<div class="hero-left">
				<div class="hero-image-container">
					<img src={image} alt="A Raven in a library" class="image-cover" />
				</div>
				<h1 class="hero-title">Library of Ravens</h1>
			</div>
			<div class="hero-right">
				<TipBox
					contextText="In Library of Ravens, you can borrow books and bring them back. We already created your virtual profile. Click on books, search them, borrow them. Enjoy!"
					ravendbText="This Library uses `RavenDB` extensively. It integrates with `Azure Storage Queues`, leverages `http` caching, and query capabilities provided by `RavenDB`."
				/>
			</div>
		</div>
	</div>

	{#if loading}
		<div class="card card-centered books-section">
			<p class="text-muted">Loading books...</p>
		</div>
	{:else if error}
		<div class="card card-centered books-section">
			<p class="text-error">{error}</p>
		</div>
	{:else if books.length > 0}
		<div class="card books-section">
			<h2 class="heading-section">Books</h2>
			<div class="books-grid">
				{#each books as book (book.id)}
					<BookCard {book} />
				{/each}
			</div>
		</div>
	{/if}
</div>

<style>
	.hero-section {
		padding: var(--spacing-6);
	}

	.hero-content {
		display: flex;
		gap: var(--spacing-6);
	}

	.hero-left {
		flex: 1;
		display: flex;
		flex-direction: column;
		align-items: center;
		text-align: center;
	}

	.hero-right {
		flex: 1;
		display: flex;
		flex-direction: column;
	}

	.hero-image-container {
		width: 100%;
		max-width: 256px;
		aspect-ratio: 1;
		margin-bottom: var(--spacing-6);
		border-radius: var(--radius-lg);
		overflow: hidden;
		background: var(--color-gray-100);
	}

	.hero-title {
		margin-bottom: var(--spacing-4);
		font-size: var(--font-size-2xl);
		font-weight: 600;
	}

	.hero-description {
		font-size: var(--font-size-base);
		color: var(--color-gray-500);
		line-height: 1.6;
	}

	@media (max-width: 799px) {
		.hero-content {
			flex-direction: column;
		}

		.hero-right {
			margin-top: var(--spacing-4);
		}
	}

	.books-section {
		margin-top: var(--spacing-6);
	}

	.books-grid {
		display: grid;
		grid-template-columns: repeat(4, 1fr);
		gap: var(--spacing-3);
	}

	@media (max-width: 799px) {
		.books-grid {
			grid-template-columns: repeat(2, 1fr);
		}
	}

	@media (max-width: 479px) {
		.books-grid {
			grid-template-columns: 1fr;
		}
	}
</style>
