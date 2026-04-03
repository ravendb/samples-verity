<script lang="ts">
	import ravendbLogo from '$lib/assets/ravendb-logo.svg';

	interface Props {
		sourceLink?: string;
	}

	let { sourceLink = 'https://github.com/ravendb/samples-library' }: Props = $props();
</script>

<header class="samples-header">
	<!-- eslint-disable-next-line svelte/no-navigation-without-resolve -->
	<a href="https://ravendb.net" target="_blank" rel="noopener noreferrer" class="logo-link">
		<img src={ravendbLogo} alt="RavenDB" class="ravendb-logo" />
	</a>
	<!-- eslint-disable-next-line svelte/no-navigation-without-resolve -->
	<a class="badge-link source-link" href={sourceLink} target="_blank" rel="noopener noreferrer">
		<svg
			xmlns="http://www.w3.org/2000/svg"
			width="12"
			height="12"
			viewBox="0 0 12 12"
			fill="currentColor"
		>
			<path
				d="M6.00007 1.60547C3.51507 1.60547 1.5 3.61999 1.5 6.10524C1.5 8.09335 2.78939 9.78005 4.57737 10.3751C4.80224 10.4167 4.88491 10.2774 4.88491 10.1586C4.88491 10.0513 4.8807 9.69683 4.8788 9.32083C3.62681 9.59307 3.36265 8.78991 3.36265 8.78991C3.15802 8.26983 2.86301 8.1315 2.86301 8.1315C2.45474 7.85221 2.89386 7.85796 2.89386 7.85796C3.34562 7.88969 3.58359 8.3217 3.58359 8.3217C3.985 9.00959 4.63631 8.8107 4.89319 8.69573C4.93352 8.40495 5.0501 8.20638 5.17889 8.09402C4.17931 7.98028 3.12861 7.59439 3.12861 5.87021C3.12861 5.37895 3.30443 4.97751 3.59232 4.66241C3.5456 4.54903 3.39146 4.09139 3.63585 3.47154C3.63585 3.47154 4.01383 3.35068 4.8737 3.93286C5.23261 3.83313 5.61757 3.78314 6.00005 3.7814C6.38239 3.78314 6.76765 3.83313 7.1273 3.93286C7.98629 3.35067 8.36354 3.47154 8.36354 3.47154C8.60851 4.09139 8.4545 4.54903 8.4078 4.66241C8.69626 4.97753 8.87093 5.37896 8.87093 5.87021C8.87093 7.59854 7.81819 7.97905 6.81613 8.09044C6.97754 8.23009 7.12133 8.50391 7.12133 8.92371C7.12133 9.5258 7.11609 10.0104 7.11609 10.1586C7.11609 10.2784 7.19714 10.4187 7.42522 10.3745C9.21223 9.77882 10.5 8.09272 10.5 6.10524C10.5 3.61999 8.48525 1.60547 6.00007 1.60547Z"
			/>
		</svg>
		View the source
	</a>
</header>

<style>
	.samples-header {
		display: flex;
		justify-content: space-between;
		align-items: center;
		background: var(--samples-bg);
		color: var(--samples-text);
		padding: 6px 12px;
		border-bottom: 1px solid var(--samples-border);
		font-size: var(--font-size-xs);
		font-family: Arial, Helvetica, sans-serif;
	}

	.logo-link {
		display: flex;
		align-items: center;
		text-decoration: none;
	}

	.ravendb-logo {
		width: 80px;
		height: 16px;
	}

	.badge-link {
		display: flex;
		align-items: center;
		background: var(--samples-badge-bg);
		color: var(--samples-text);
		gap: 6px;
		padding: 4px 8px;
		border-radius: 6px;
		border: 1px solid var(--samples-badge-border);
		text-decoration: none;
		transition: filter 0.1s ease-in-out;
	}

	.badge-link:hover {
		filter: brightness(var(--samples-hover-brightness));
	}

	.badge-link svg {
		color: var(--samples-icon-color);
	}
</style>
