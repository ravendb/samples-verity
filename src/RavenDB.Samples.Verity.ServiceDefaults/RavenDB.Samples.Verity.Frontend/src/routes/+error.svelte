<script lang="ts">
	import { page } from '$app/state';
	import { resolve } from '$app/paths';
	import image from '$lib/assets/image.webp';
	import TipBox from '$lib/components/TipBox.svelte';

	const status = $derived(page.status);
	const is404 = $derived(status === 404);
</script>

<svelte:head>
	<title>{is404 ? 'Page Not Found' : 'Error'} | Library of Ravens</title>
</svelte:head>

<div class="page-container">
	<div class="card error-page">
		<div class="error-content">
			<div class="error-left">
				<div class="error-image-container">
					<img src={image} alt="A Raven in a library" class="image-cover" />
				</div>
			</div>
			<div class="error-right">
				{#if is404}
					<TipBox
						contextText="Whoops. It looks like this page does not exist. You've been playing with url, haven't you?"
						ravendbText="There's nothing that RavenDB can help you with if your page does not require any data. And clearly, 404 is one of these pages."
					/>
				{:else}
					<TipBox
						contextText="An error occurred while processing your request. Try refreshing the page or going back to the home page."
						ravendbText="RavenDB provides robust error handling and logging capabilities to help diagnose issues quickly."
					/>
				{/if}
			</div>
		</div>
		<div class="error-text">
			<h1 class="error-title">
				{#if is404}
					404 - Page Not Found
				{:else}
					{status} - Error
				{/if}
			</h1>
			<p class="error-message">
				{#if is404}
					The page you're looking for doesn't exist.
				{:else}
					Something went wrong. Please try again later.
				{/if}
			</p>
			<a href={resolve('/')} class="link-primary error-link">← Back to Home</a>
		</div>
	</div>
</div>

<style>
	.error-page {
		padding: var(--spacing-6);
	}

	.error-content {
		display: flex;
		gap: var(--spacing-6);
		margin-bottom: var(--spacing-6);
	}

	.error-left {
		flex: 1;
		display: flex;
		flex-direction: column;
		align-items: center;
		justify-content: center;
	}

	.error-right {
		flex: 1;
		display: flex;
		flex-direction: column;
	}

	.error-image-container {
		width: 100%;
		max-width: 256px;
		aspect-ratio: 1;
		border-radius: var(--radius-lg);
		overflow: hidden;
		background: var(--color-gray-100);
	}

	.error-text {
		display: flex;
		flex-direction: column;
		align-items: center;
		text-align: center;
	}

	.error-title {
		margin-bottom: var(--spacing-4);
		font-size: var(--font-size-2xl);
		font-weight: 600;
		color: var(--color-gray-900);
	}

	.error-message {
		margin-bottom: var(--spacing-6);
		font-size: var(--font-size-base);
		color: var(--color-gray-500);
		line-height: 1.6;
	}

	.error-link {
		font-size: var(--font-size-base);
	}

	@media (max-width: 799px) {
		.error-content {
			flex-direction: column;
		}

		.error-right {
			margin-top: var(--spacing-4);
		}
	}
</style>
