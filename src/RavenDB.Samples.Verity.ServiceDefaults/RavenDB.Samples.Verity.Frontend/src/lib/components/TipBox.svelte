<script lang="ts">
	import { marked } from 'marked';
	import DOMPurify from 'isomorphic-dompurify';

	interface Props {
		contextText?: string;
		ravendbText?: string;
	}

	let { contextText, ravendbText }: Props = $props();

	// Configure marked for inline rendering (no wrapping <p> tags for single lines)
	marked.use({
		breaks: true,
		gfm: true
	});

	function renderMarkdown(text: string | undefined): string {
		if (!text) return '';
		const rawHtml = marked.parse(text, { async: false }) as string;
		return DOMPurify.sanitize(rawHtml);
	}

	const contextHtml = $derived(renderMarkdown(contextText));
	const ravendbHtml = $derived(renderMarkdown(ravendbText));
</script>

{#if contextText || ravendbText}
	<div class="tip-box">
		{#if contextText}
			<div class="tip-section tip-context">
				<h3 class="tip-heading">Library Tip</h3>
				<!-- eslint-disable-next-line svelte/no-at-html-tags -- HTML is sanitized with isomorphic-dompurify -->
				<div class="tip-text">{@html contextHtml}</div>
			</div>
		{/if}
		{#if ravendbText}
			<div class="tip-section tip-ravendb">
				<h3 class="tip-heading">RavenDB Tip</h3>
				<!-- eslint-disable-next-line svelte/no-at-html-tags -- HTML is sanitized with isomorphic-dompurify -->
				<div class="tip-text">{@html ravendbHtml}</div>
			</div>
		{/if}
	</div>
{/if}

<style>
	.tip-box {
		display: flex;
		flex-direction: column;
		gap: var(--spacing-4);
	}

	.tip-section {
		padding: var(--spacing-4);
		border-radius: var(--radius-lg);
		border: 2px solid;
	}

	.tip-context {
		background-color: var(--color-orange-100);
		border-color: var(--color-orange-600);
	}

	.tip-ravendb {
		background-color: var(--color-blue-100);
		border-color: var(--color-blue-600);
	}

	.tip-heading {
		margin: 0 0 var(--spacing-2) 0;
		font-size: var(--font-size-base);
		font-weight: 600;
		color: var(--color-gray-900);
	}

	.tip-context .tip-heading {
		color: var(--color-orange-700);
	}

	.tip-ravendb .tip-heading {
		color: var(--color-blue-700);
	}

	.tip-text {
		margin: 0;
		font-size: var(--font-size-sm);
		color: var(--color-gray-900);
		line-height: 1.6;
	}

	.tip-text :global(p) {
		margin: 0 0 var(--spacing-2) 0;
	}

	.tip-text :global(p:last-child) {
		margin-bottom: 0;
	}

	.tip-text :global(code) {
		background-color: rgba(0, 0, 0, 0.05);
		padding: 0.125rem 0.25rem;
		border-radius: 3px;
		font-family: 'Courier New', Courier, monospace;
		font-size: 0.9em;
	}

	.tip-text :global(a) {
		color: inherit;
		text-decoration: underline;
		font-weight: 500;
	}

	.tip-text :global(a:hover) {
		opacity: 0.8;
	}

	.tip-context .tip-text :global(a) {
		color: var(--color-orange-700);
	}

	.tip-ravendb .tip-text :global(a) {
		color: var(--color-blue-700);
	}

	.tip-text :global(pre) {
		background-color: rgba(0, 0, 0, 0.05);
		padding: var(--spacing-2);
		border-radius: var(--radius-md);
		overflow-x: auto;
		margin: var(--spacing-2) 0;
	}

	.tip-text :global(pre code) {
		background-color: transparent;
		padding: 0;
	}

	.tip-text :global(strong) {
		font-weight: 600;
	}

	.tip-text :global(em) {
		font-style: italic;
	}
</style>
