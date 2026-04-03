<script lang="ts">
	import { onMount, onDestroy } from 'svelte';
	import { resolve } from '$app/paths';
	import { getUserIdWithInfo, getUserAvatarUrl } from '$lib/utils/userId';
	import SearchModal from './SearchModal.svelte';
	import {
		notificationCount,
		startNotificationPolling,
		stopNotificationPolling,
		updateNotificationCount
	} from '$lib/stores/notifications';
	import { getUserProfile } from '$lib/services/user';
	import { page } from '$app/stores';

	let userId = $state('');
	let avatarUrl = $state('');
	let searchOpen = $state(false);
	let isMac = $state(false);

	// Subscribe to notification count store
	let count = $state(0);
	const unsubscribe = notificationCount.subscribe((value) => {
		count = value;
	});

	onMount(async () => {
		const userIdInfo = getUserIdWithInfo();
		userId = userIdInfo.userId;
		avatarUrl = getUserAvatarUrl(userId);
		isMac = navigator.platform.toLowerCase().includes('mac');

		// If this is a new user, fetch the profile first to ensure user is created
		if (userIdInfo.isNewUser) {
			try {
				await getUserProfile();
			} catch (error) {
				console.error('Failed to fetch user profile for new user:', error);
			}
		}

		// Start polling for notification count
		startNotificationPolling();
	});

	onDestroy(() => {
		// Clean up polling when component is destroyed
		stopNotificationPolling();
		unsubscribe();
	});

	// Update notification count on page navigation
	$effect(() => {
		// Access the page store to trigger on navigation
		void $page;
		// Update notification count when navigating between pages
		void updateNotificationCount();
	});

	function handleGlobalKeydown(e: KeyboardEvent) {
		// Check for Ctrl+K (Windows/Linux) or Cmd+K (Mac)
		if ((e.ctrlKey || e.metaKey) && e.key === 'k') {
			e.preventDefault();
			searchOpen = true;
		}
	}
</script>

<svelte:window onkeydown={handleGlobalKeydown} />

<header class="topbar">
	<div class="topbar-content">
		<div class="topbar-left">
			<a href={resolve('/')} class="logo">
				<span class="logo-text">📚 Library of Ravens</span>
			</a>

			<button
				class="search-trigger"
				onclick={() => (searchOpen = true)}
				aria-label="Open search dialog"
			>
				<svg class="search-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor">
					<circle cx="11" cy="11" r="8" />
					<path d="M21 21l-4.35-4.35" />
				</svg>
				<span class="search-placeholder">Search...</span>
				<kbd class="kbd">{isMac ? '⌘' : 'Ctrl'}+K</kbd>
			</button>
		</div>

		<nav class="topbar-right">
			<a href={resolve('/notifications')} class="bell-link" aria-label="Notifications">
				<svg class="bell-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor">
					<path d="M18 8A6 6 0 0 0 6 8c0 7-3 9-3 9h18s-3-2-3-9" />
					<path d="M13.73 21a2 2 0 0 1-3.46 0" />
				</svg>
				{#if count > 0}
					<span class="notification-badge" aria-label="{count} unread notifications"></span>
				{/if}
			</a>
			<a href={resolve('/profile')} class="user-link">
				{#if avatarUrl}
					<img src={avatarUrl} alt="User avatar" class="user-avatar" />
				{:else}
					<div class="user-avatar-placeholder"></div>
				{/if}
			</a>
		</nav>
	</div>
</header>

<SearchModal bind:isOpen={searchOpen} />

<style>
	.topbar {
		position: sticky;
		top: 0;
		background: var(--color-white);
		border-bottom: 1px solid var(--color-gray-200);
		z-index: 50;
	}

	.topbar-content {
		max-width: 1200px;
		margin: 0 auto;
		padding: var(--spacing-3) var(--spacing-6);
		display: flex;
		align-items: center;
		justify-content: space-between;
	}

	.topbar-left {
		display: flex;
		align-items: center;
		gap: var(--spacing-6);
	}

	.topbar-right {
		display: flex;
		align-items: center;
		gap: var(--spacing-4);
	}

	.logo {
		text-decoration: none;
		color: inherit;
	}

	.logo-text {
		font-size: var(--font-size-lg);
		font-weight: 600;
	}

	.search-trigger {
		width: 320px;
		display: flex;
		align-items: center;
		gap: var(--spacing-2);
		padding: var(--spacing-2) var(--spacing-3);
		background: var(--color-gray-100);
		border: 1px solid var(--color-gray-200);
		border-radius: var(--radius-md);
		cursor: pointer;
		transition: all 0.15s;
	}

	.search-trigger:hover {
		border-color: var(--color-gray-300);
		background: var(--color-gray-50);
	}

	.search-icon {
		width: 16px;
		height: 16px;
		stroke-width: 2;
		color: var(--color-gray-400);
	}

	.search-placeholder {
		flex: 1;
		text-align: left;
		color: var(--color-gray-400);
		font-size: var(--font-size-sm);
	}

	.kbd {
		background: var(--color-white);
		border: 1px solid var(--color-gray-200);
		border-radius: var(--radius-sm);
		padding: 2px 6px;
		font-size: var(--font-size-xs);
		color: var(--color-gray-500);
		font-family: monospace;
	}

	.bell-link {
		position: relative;
		display: flex;
		align-items: center;
		justify-content: center;
		width: 36px;
		height: 36px;
		text-decoration: none;
		color: var(--color-gray-600);
		transition: all 0.15s;
		border-radius: var(--radius-md);
	}

	.bell-link:hover {
		background: var(--color-gray-100);
		color: var(--color-gray-900);
	}

	.bell-icon {
		width: 20px;
		height: 20px;
		stroke-width: 2;
	}

	.notification-badge {
		position: absolute;
		top: 6px;
		right: 6px;
		width: 12px;
		height: 12px;
		background: var(--color-red-600);
		border: 2px solid var(--color-white);
		border-radius: var(--radius-full);
		pointer-events: none;
	}

	.user-link {
		display: block;
		transition: opacity 0.15s;
	}

	.user-link:hover {
		opacity: 0.8;
	}

	.user-avatar {
		width: 36px;
		height: 36px;
		border-radius: var(--radius-full);
		background: var(--color-gray-100);
	}

	.user-avatar-placeholder {
		width: 36px;
		height: 36px;
		border-radius: var(--radius-full);
		background: var(--color-gray-200);
	}
</style>
