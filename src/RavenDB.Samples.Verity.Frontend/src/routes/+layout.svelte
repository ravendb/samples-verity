<script lang="ts">
	import favicon from '$lib/assets/favicon.svg';

	let { children } = $props();
</script>

<svelte:head>
	<link rel="icon" href={favicon} />
</svelte:head>

<samples-ui-wrapper sourceLink="https://github.com/ravendb/samples-verity" theme="dark">
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
