<script lang="ts">
	import { onMount } from 'svelte';
	import { resolve } from '$app/paths';
	import { getUserId, getUserAvatarUrl } from '$lib/utils/userId';
	import { getUserProfile, returnBook, type UserProfile } from '$lib/services/user';
	import TipBox from '$lib/components/TipBox.svelte';
	import { updateNotificationCount } from '$lib/stores/notifications';
	import { idToLink } from '$lib/utils/links';

	let userId = $state('');
	let avatarUrl = $state('');
	let userProfile = $state<UserProfile | null>(null);
	let loading = $state(true);
	let error = $state<string | null>(null);
	let showReturnModal = $state(false);
	let returningBookId = $state<string | null>(null);
	let isReturning = $state(false);

	let overdueBooks = $derived(userProfile?.borrowed.filter((book) => book.overdue) || []);
	let activeBooks = $derived(userProfile?.borrowed.filter((book) => !book.overdue) || []);

	async function loadProfile() {
		loading = true;
		error = null;
		try {
			userProfile = await getUserProfile();
		} catch (e) {
			error = e instanceof Error ? e.message : 'Failed to load profile';
		} finally {
			loading = false;
		}
	}

	onMount(async () => {
		userId = getUserId();
		avatarUrl = getUserAvatarUrl(userId);
		await loadProfile();
	});

	function handleReturnClick(bookId: string) {
		returningBookId = bookId;
		showReturnModal = true;
	}

	async function confirmReturn() {
		if (!returningBookId || isReturning) return;

		isReturning = true;
		try {
			await returnBook(returningBookId);
			showReturnModal = false;
			returningBookId = null;
			// Reload profile data
			await loadProfile();
			// Update notification count after returning a book
			await updateNotificationCount();
		} catch (e) {
			error = e instanceof Error ? e.message : 'Failed to return book';
		} finally {
			isReturning = false;
		}
	}

	function closeModal() {
		if (!isReturning) {
			showReturnModal = false;
			returningBookId = null;
		}
	}
</script>

<svelte:head>
	<title>Profile | Library of Ravens</title>
</svelte:head>

<div class="page-container">
	<h1 class="heading-page">Profile</h1>

	<div class="profile-content">
		<div class="profile-left">
			<div class="card profile-card">
				{#if avatarUrl}
					<img src={avatarUrl} alt="User avatar" class="profile-avatar avatar-round" />
				{/if}
				<div class="profile-info">
					<p class="profile-label text-muted">User ID</p>
					<p class="profile-value text-mono">{userId}</p>
				</div>
			</div>
		</div>

		<div class="profile-right">
			<TipBox
				contextText="Your user profile created automatically for the convenience of using the app. You can see your avatar, id and the list of borrowed books."
				ravendbText="Data for this page is retrieved in an efficient way by using `.Include` (see: [docs](https://docs.ravendb.net/7.1/client-api/how-to/handle-document-relationships#includes)) when querying for books. This means that both, the borrowed copies and the books they link to, are fetched in one request."
			/>
		</div>
	</div>

	{#if loading}
		<section class="borrowed-section">
			<h2 class="heading-section">Borrowed Books</h2>
			<p class="card card-centered loading-state">Loading...</p>
		</section>
	{:else if error}
		<section class="borrowed-section">
			<h2 class="heading-section">Borrowed Books</h2>
			<p class="card card-centered error-state">{error}</p>
		</section>
	{:else if userProfile}
		{#if overdueBooks.length > 0}
			<section class="borrowed-section">
				<h2 class="heading-section overdue-heading">Overdue Books</h2>
				<ul class="card borrowed-list">
					{#each overdueBooks as book (book.id)}
						{@const link = idToLink(book.bookId)}
						<li class="borrowed-item overdue-item">
							{#if link}
								<a href={resolve(link)} class="book-title book-title-link">
									{book.title}
								</a>
							{:else}
								<span class="book-title">{book.title}</span>
							{/if}
							<button class="return-button" onclick={() => handleReturnClick(book.id)}>
								Return
							</button>
						</li>
					{/each}
				</ul>
			</section>
		{/if}

		{#if activeBooks.length > 0}
			<section class="borrowed-section">
				<h2 class="heading-section">Active Books</h2>
				<ul class="card borrowed-list">
					{#each activeBooks as book (book.id)}
						{@const link = idToLink(book.bookId)}
						<li class="borrowed-item">
							{#if link}
								<a href={resolve(link)} class="book-title book-title-link">
									{book.title}
								</a>
							{:else}
								<span class="book-title">{book.title}</span>
							{/if}
							<button class="return-button" onclick={() => handleReturnClick(book.id)}>
								Return
							</button>
						</li>
					{/each}
				</ul>
			</section>
		{/if}

		{#if overdueBooks.length === 0 && activeBooks.length === 0}
			<section class="borrowed-section">
				<h2 class="heading-section">Borrowed Books</h2>
				<p class="card card-centered text-muted">No books currently borrowed.</p>
			</section>
		{/if}
	{/if}
</div>

{#if showReturnModal}
	<div class="modal-backdrop" onclick={closeModal}>
		<div class="modal-content" onclick={(e) => e.stopPropagation()}>
			<p class="modal-text">Returned</p>
			<button class="modal-button" onclick={confirmReturn} disabled={isReturning}>
				{isReturning ? 'Returning...' : 'OK'}
			</button>
		</div>
	</div>
{/if}

<style>
	.profile-content {
		display: flex;
		gap: var(--spacing-6);
	}

	.profile-left {
		flex: 1;
	}

	.profile-right {
		flex: 1;
		display: flex;
		flex-direction: column;
	}

	.profile-card {
		display: flex;
		align-items: center;
		gap: var(--spacing-6);
		height: 100%;
	}

	.profile-avatar {
		width: 80px;
		height: 80px;
	}

	.profile-info {
		display: flex;
		flex-direction: column;
		gap: 4px;
	}

	.profile-label {
		font-size: var(--font-size-sm);
	}

	.profile-value {
		font-size: var(--font-size-sm);
		color: var(--color-gray-900);
	}

	.borrowed-section {
		margin-top: var(--spacing-8);
	}

	.overdue-heading {
		color: var(--color-red-600, #dc2626);
	}

	.borrowed-list {
		list-style: none;
		padding: 0;
		overflow: hidden;
	}

	.borrowed-item {
		display: flex;
		justify-content: space-between;
		align-items: center;
		padding: var(--spacing-4) var(--spacing-6);
		border-bottom: 1px solid var(--color-gray-200);
	}

	.borrowed-item:last-child {
		border-bottom: none;
	}

	.overdue-item {
		background-color: #fef2f2;
	}

	.book-title {
		font-weight: 500;
		color: var(--color-gray-900);
		flex: 1;
	}

	.book-title-link {
		text-decoration: none;
		transition: color 0.2s;
	}

	.book-title-link:hover {
		color: var(--color-blue-600, #2563eb);
		text-decoration: underline;
	}

	.return-button {
		padding: var(--spacing-2) var(--spacing-4);
		background-color: var(--color-blue-600, #2563eb);
		color: white;
		border: none;
		border-radius: 4px;
		font-size: var(--font-size-sm);
		cursor: pointer;
		transition: background-color 0.2s;
	}

	.return-button:hover {
		background-color: var(--color-blue-700, #1d4ed8);
	}

	.return-button:active {
		background-color: var(--color-blue-800, #1e40af);
	}

	.modal-backdrop {
		position: fixed;
		top: 0;
		left: 0;
		right: 0;
		bottom: 0;
		background-color: rgba(0, 0, 0, 0.5);
		display: flex;
		align-items: center;
		justify-content: center;
		z-index: 1000;
	}

	.modal-content {
		background-color: white;
		padding: var(--spacing-8);
		border-radius: 8px;
		box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);
		text-align: center;
		min-width: 300px;
	}

	.modal-text {
		font-size: var(--font-size-lg);
		font-weight: 500;
		margin-bottom: var(--spacing-6);
		color: var(--color-gray-900);
	}

	.modal-button {
		padding: var(--spacing-3) var(--spacing-8);
		background-color: var(--color-blue-600, #2563eb);
		color: white;
		border: none;
		border-radius: 4px;
		font-size: var(--font-size-base);
		cursor: pointer;
		transition: background-color 0.2s;
		min-width: 100px;
	}

	.modal-button:hover {
		background-color: var(--color-blue-700, #1d4ed8);
	}

	.modal-button:disabled {
		background-color: var(--color-gray-400, #9ca3af);
		cursor: not-allowed;
	}

	@media (max-width: 799px) {
		.profile-content {
			flex-direction: column;
		}
	}
</style>
