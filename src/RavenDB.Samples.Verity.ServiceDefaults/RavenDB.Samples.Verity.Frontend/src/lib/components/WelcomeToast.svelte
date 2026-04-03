<script lang="ts">
	import { onMount } from 'svelte';

	let visible = $state(false);
	let dismissed = $state(false);

	onMount(() => {
		// Check if toast was previously dismissed
		const wasDismissed = localStorage.getItem('welcomeToastDismissed');
		if (wasDismissed) {
			dismissed = true;
			return;
		}

		// Show toast after a delay
		setTimeout(() => {
			visible = true;
		}, 1000);
	});

	function closeToast() {
		visible = false;
		dismissed = true;
		localStorage.setItem('welcomeToastDismissed', 'true');
	}
</script>

{#if !dismissed}
	<aside class="welcome-toast" class:visible aria-live="polite">
		<div class="welcome-toast-content">
			<button
				class="welcome-toast-close"
				onclick={closeToast}
				aria-label="Close welcome message"
				type="button"
			>
				<svg
					xmlns="http://www.w3.org/2000/svg"
					width="9"
					height="9"
					viewBox="0 0 9 9"
					fill="currentColor"
				>
					<path
						d="M5.39739 4.50001L7.24739 2.66251L6.24739 1.67501L4.40989 3.51251L2.57239 1.66251L1.57239 2.66251L3.42239 4.50001L1.58489 6.33751L2.57239 7.32501L4.40989 5.48751L6.24739 7.33751L7.24739 6.33751L5.39739 4.50001Z"
					/>
				</svg>
			</button>
			<div class="welcome-toast-title">Welcome to RavenDB Sample App</div>
			<p class="welcome-toast-description">
				This project demonstrates how to build apps with RavenDB features like document storage,
				indexing, and real-time subscriptions.
			</p>
			<!-- eslint-disable-next-line svelte/no-navigation-without-resolve -->
			<a
				class="welcome-toast-link"
				href="https://ravendb.net/cloud"
				target="_blank"
				rel="noopener noreferrer"
			>
				Try it yourself on RavenDB Cloud
				<svg
					width="8"
					height="9"
					viewBox="0 0 8 9"
					xmlns="http://www.w3.org/2000/svg"
					fill="currentColor"
				>
					<path
						d="M0.569336 7.26527L5.73282 2.10178H2.96975L2.96959 0.889648H7.79053L7.79037 5.70978L6.57824 5.70963V2.94655L1.41444 8.11035L0.569336 7.26527Z"
					/>
				</svg>
			</a>
		</div>
	</aside>
{/if}

<style>
	.welcome-toast {
		position: fixed;
		right: 16px;
		bottom: 60px;
		width: 350px;
		z-index: 100;
		transform: translateX(120%);
		transition: transform 0.5s ease-out;
		font-family: Arial, Helvetica, sans-serif;
		font-size: var(--font-size-xs);
	}

	.welcome-toast.visible {
		transform: translateX(0);
	}

	.welcome-toast-content {
		position: relative;
		display: flex;
		flex-direction: column;
		padding: 16px;
		border-radius: 8px;
		border: 1px solid var(--samples-border);
		background: var(--samples-bg);
		color: var(--samples-toast-text);
		box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
	}

	.welcome-toast-close {
		position: absolute;
		display: flex;
		top: -8px;
		right: -8px;
		cursor: pointer;
		border-radius: 50%;
		background: var(--samples-bg);
		border: 1px solid var(--samples-border);
		padding: 4px 4px 4px 5px;
		width: 24px;
		height: 24px;
		align-items: center;
		justify-content: center;
		transition: filter 0.1s ease-in-out;
	}

	.welcome-toast-close:hover {
		filter: brightness(var(--samples-hover-brightness));
	}

	.welcome-toast-close svg {
		color: var(--samples-toast-text);
	}

	.welcome-toast-title {
		font-weight: 600;
		margin-bottom: 8px;
		font-size: var(--font-size-sm);
	}

	.welcome-toast-description {
		margin-bottom: 12px;
		line-height: 1.5;
	}

	.welcome-toast-link {
		width: fit-content;
		display: flex;
		gap: 4px;
		align-items: center;
		text-decoration: none;
		border-bottom: 1px dotted var(--samples-toast-link);
		color: var(--samples-toast-link);
		transition: filter 0.1s ease-in-out;
	}

	.welcome-toast-link:hover {
		filter: brightness(var(--samples-hover-brightness));
	}

	@media screen and (max-width: 769px) {
		.welcome-toast {
			bottom: 84px;
		}
	}

	@media (max-width: 370px) {
		.welcome-toast {
			width: 260px;
			right: 8px;
		}
	}
</style>
