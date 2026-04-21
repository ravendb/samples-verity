<script lang="ts">
	import favicon from '$lib/assets/favicon.svg';

	let { children, data } = $props();
</script>

<svelte:head>
	<link rel="icon" href={favicon} />
</svelte:head>

<samples-ui-wrapper sourceLink="https://github.com/ravendb/samples-verity" theme="dark">
	<div slot="header-end" style="display:flex;align-items:center;gap:1rem;padding-inline:1rem;">
		{#if data.user?.name}
			<span style="font-size:0.875rem;opacity:0.8">{data.user.name}</span>
			<a
				href={`/bff/logout?sid=${data.user.sid ?? ''}`}
				style="font-size:0.875rem;color:inherit;text-decoration:underline"
			>Sign out</a>
		{:else}
			<a
				href={`/bff/login?returnUrl=${encodeURIComponent(typeof window !== 'undefined' ? window.location.pathname : '/')}`}
				style="font-size:0.875rem;color:inherit;text-decoration:underline"
			>Sign in</a>
		{/if}
	</div>

	{@render children()}
</samples-ui-wrapper>

<style>
	:global(*, *::before, *::after) {
		box-sizing: border-box;
	}

	:global(body) {
		margin: 0;
		font-family: system-ui, -apple-system, sans-serif;
		background: #192d47;
	}

	:global(main > header) {
		box-sizing: border-box;
		min-height: 60px;
		height: 60px;
	}

	:global(.verity-brand) {
		color: inherit;
		text-decoration: none;
	}
	:global(.verity-brand:hover) { opacity: 0.8; }

	:global(input, button, select, textarea) {
		font: inherit;
	}
</style>
