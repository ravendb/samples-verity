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

	:global(input, button, select, textarea) {
		font: inherit;
	}
</style>
