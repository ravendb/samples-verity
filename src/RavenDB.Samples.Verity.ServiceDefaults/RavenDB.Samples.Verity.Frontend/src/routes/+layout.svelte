<script lang="ts">
	import '../app.css';
	import fav16 from '$lib/assets/favicon-16x16.png';
	import fav32 from '$lib/assets/favicon-32x32.png';
	import favApple from '$lib/assets/apple-touch-icon.png';
	import manifest from '$lib/assets/site.webmanifest';
	import SamplesHeader from '$lib/components/SamplesHeader.svelte';
	import TopBar from '$lib/components/TopBar.svelte';
	import SamplesFooter from '$lib/components/SamplesFooter.svelte';
	import WelcomeToast from '$lib/components/WelcomeToast.svelte';

	let { children } = $props();
</script>

<svelte:head>
	<link rel="apple-touch-icon" sizes="180x180" href={favApple} />
	<link rel="icon" type="image/png" sizes="32x32" href={fav32} />
	<link rel="icon" type="image/png" sizes="16x16" href={fav16} />
	<link rel="manifest" href={manifest} />
</svelte:head>

<div class="app-wrapper">
	<SamplesHeader sourceLink="https://github.com/ravendb/samples-library" />
	<TopBar />
	<main>
		{@render children()}
	</main>
	<SamplesFooter />
	<WelcomeToast />
</div>

<style>
	:global(*) {
		margin: 0;
		padding: 0;
		box-sizing: border-box;
	}

	:global(body) {
		font-family:
			-apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Oxygen, Ubuntu, Cantarell, 'Open Sans',
			'Helvetica Neue', sans-serif;
		background: var(--color-gray-50);
		color: var(--color-gray-900);
		line-height: 1.5;
	}

	.app-wrapper {
		display: flex;
		flex-direction: column;
		min-height: 100vh;
	}

	main {
		flex: 1;
		display: flex;
		flex-direction: column;
	}
</style>
