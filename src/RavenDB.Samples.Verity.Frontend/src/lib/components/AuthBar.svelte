<script lang="ts">
	import { onMount } from 'svelte';
	import { getUser, loginUrl, registerUrl, type UserInfo } from '$lib/auth';
	import { page } from '$app/stores';

	let user = $state<UserInfo | null | 'loading'>('loading');

	onMount(async () => {
		user = await getUser();
	});

	let returnUrl = $derived($page.url.pathname + $page.url.search);
</script>

{#if user !== 'loading'}
	<div class="auth-bar">
		{#if user}
			<span class="auth-name" title={user.email}>{user.name}</span>
			<a href={user.logoutUrl} class="auth-btn auth-btn--ghost">Sign out</a>
		{:else}
			<a href={registerUrl(returnUrl)} class="auth-btn auth-btn--ghost">Register</a>
			<a href={loginUrl(returnUrl)} class="auth-btn">Sign in</a>
		{/if}
	</div>
{/if}

<style>
	.auth-bar {
		display: flex;
		align-items: center;
		gap: 0.5rem;
	}

	.auth-name {
		font-size: 0.875rem;
		color: #93b4d8;
		max-width: 180px;
		overflow: hidden;
		text-overflow: ellipsis;
		white-space: nowrap;
	}

	.auth-btn {
		font-size: 0.8rem;
		padding: 0.35rem 0.8rem;
		border-radius: 5px;
		background: #2e5c9a;
		color: #e8f0fe;
		text-decoration: none;
		border: 1px solid transparent;
		white-space: nowrap;
		cursor: pointer;
		transition: background 0.15s;
	}
	.auth-btn:hover { background: #3a70b8; }

	.auth-btn--ghost {
		background: transparent;
		border-color: #2e5c9a;
		color: #93b4d8;
	}
	.auth-btn--ghost:hover {
		background: rgba(46, 92, 154, 0.2);
		color: #e8f0fe;
	}
</style>
