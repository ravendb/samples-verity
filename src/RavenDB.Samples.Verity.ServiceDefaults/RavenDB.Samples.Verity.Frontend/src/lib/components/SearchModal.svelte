<script lang="ts">
	import { base } from '$app/paths';
	import { searchBooksAndAuthors, type SearchResult } from '$lib/services/search';

	let { isOpen = $bindable(false) }: { isOpen: boolean } = $props();

	let query = $state('');
	let results = $state<SearchResult[]>([]);
	let loading = $state(false);
	let inputRef: HTMLInputElement | undefined = $state();
	let selectedIndex = $state(-1);
	let useVectorSearch = $state(false);

	let debounceTimer: ReturnType<typeof setTimeout>;

	function handleInput() {
		clearTimeout(debounceTimer);
		debounceTimer = setTimeout(async () => {
			if (query.trim()) {
				loading = true;
				results = await searchBooksAndAuthors(query, useVectorSearch);
				loading = false;
				selectedIndex = -1; // Reset selection when results change
			} else {
				results = [];
				selectedIndex = -1;
			}
		}, 200);
	}

	function close() {
		isOpen = false;
		query = '';
		results = [];
		selectedIndex = -1;
	}

	function handleBackdropClick(e: MouseEvent) {
		if (e.target === e.currentTarget) {
			close();
		}
	}

	function handleKeydown(e: KeyboardEvent) {
		if (e.key === 'Escape') {
			close();
		} else if (e.key === 'ArrowDown') {
			e.preventDefault();
			if (results.length > 0) {
				selectedIndex = selectedIndex < results.length - 1 ? selectedIndex + 1 : 0;
			}
		} else if (e.key === 'ArrowUp') {
			e.preventDefault();
			if (results.length > 0) {
				selectedIndex = selectedIndex > 0 ? selectedIndex - 1 : results.length - 1;
			}
		} else if (e.key === 'Enter') {
			e.preventDefault();
			if (selectedIndex >= 0 && selectedIndex < results.length) {
				const selectedResult = results[selectedIndex];
				window.location.href = resolveLink(selectedResult.link);
			}
		}
	}

	function resolveLink(link: string): string {
		return `${base}${link}`;
	}

	$effect(() => {
		if (isOpen && inputRef) {
			inputRef.focus();
		}
	});
</script>

{#if isOpen}
	<div
		class="search-modal-backdrop"
		onclick={handleBackdropClick}
		onkeydown={handleKeydown}
		role="dialog"
		aria-modal="true"
		aria-label="Search"
		tabindex="-1"
	>
		<div class="search-modal">
			<div class="search-input-wrapper">
				<svg class="search-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor">
					<circle cx="11" cy="11" r="8" />
					<path d="M21 21l-4.35-4.35" />
				</svg>
				<input
					bind:this={inputRef}
					type="text"
					placeholder="Search books and authors..."
					aria-label="Search books and authors"
					bind:value={query}
					oninput={handleInput}
					class="search-input"
				/>
				<div class="kbd-hints">
					<kbd class="kbd">↑↓</kbd>
					<kbd class="kbd">↵</kbd>
					<kbd class="kbd">ESC</kbd>
				</div>
			</div>

			<div class="search-options">
				<label class="checkbox-label" for="vector-search-checkbox">
					<input
						type="checkbox"
						id="vector-search-checkbox"
						bind:checked={useVectorSearch}
						oninput={handleInput}
						class="vector-checkbox"
					/>
					<span class="checkbox-text">Use vector search</span>
				</label>
			</div>

			<div class="search-results">
				{#if loading}
					<div class="search-loading">Searching...</div>
				{:else if results.length > 0}
					{#each results as result, index (result.id)}
						<!-- eslint-disable-next-line svelte/no-navigation-without-resolve -->
						<a
							href={resolveLink(result.link)}
							class="search-result-item"
							class:selected={index === selectedIndex}
							onclick={close}
						>
							<img src={result.imageUrl} alt={result.name} class="result-image" />
							<div class="result-info">
								<span class="result-name">{result.name}</span>
								<span class="result-type">{result.type}</span>
							</div>
						</a>
					{/each}
				{:else if query.trim()}
					<div class="search-no-results">No results found for "{query}"</div>
				{:else}
					<div class="search-hint">Start typing to search...</div>
				{/if}
			</div>
		</div>
	</div>
{/if}

<style>
	.search-modal-backdrop {
		position: fixed;
		inset: 0;
		background: rgba(0, 0, 0, 0.5);
		display: flex;
		justify-content: center;
		align-items: flex-start;
		padding-top: 10vh;
		z-index: 100;
	}

	.search-modal {
		background: var(--color-white);
		border-radius: var(--radius-lg);
		width: 100%;
		max-width: 560px;
		box-shadow: 0 25px 50px -12px rgba(0, 0, 0, 0.25);
		overflow: hidden;
	}

	.search-input-wrapper {
		display: flex;
		align-items: center;
		padding: var(--spacing-4);
		border-bottom: 1px solid var(--color-gray-200);
		gap: var(--spacing-3);
	}

	.search-icon {
		width: 20px;
		height: 20px;
		stroke-width: 2;
		color: var(--color-gray-400);
		flex-shrink: 0;
	}

	.search-input {
		flex: 1;
		border: none;
		outline: none;
		font-size: var(--font-size-base);
		background: transparent;
	}

	.search-input::placeholder {
		color: var(--color-gray-400);
	}

	.kbd {
		background: var(--color-gray-100);
		border: 1px solid var(--color-gray-200);
		border-radius: var(--radius-sm);
		padding: 2px 6px;
		font-size: var(--font-size-xs);
		color: var(--color-gray-500);
		font-family: monospace;
	}

	.kbd-hints {
		display: flex;
		gap: var(--spacing-2);
	}

	.search-options {
		display: flex;
		align-items: center;
		padding: var(--spacing-3) var(--spacing-4);
		border-bottom: 1px solid var(--color-gray-200);
		background: var(--color-gray-50);
	}

	.checkbox-label {
		display: flex;
		align-items: center;
		gap: var(--spacing-2);
		cursor: pointer;
		font-size: var(--font-size-sm);
		color: var(--color-gray-700);
	}

	.vector-checkbox {
		cursor: pointer;
	}

	.checkbox-text {
		user-select: none;
	}

	.search-results {
		max-height: 400px;
		overflow-y: auto;
	}

	.search-result-item {
		display: flex;
		align-items: center;
		gap: var(--spacing-3);
		padding: var(--spacing-3) var(--spacing-4);
		text-decoration: none;
		color: inherit;
		transition: background-color 0.15s;
	}

	.search-result-item:hover {
		background: var(--color-gray-100);
	}

	.search-result-item.selected {
		background: var(--color-gray-200);
		border-left: 3px solid var(--color-gray-500);
		padding-left: calc(var(--spacing-4) - 3px);
	}

	.result-image {
		width: 40px;
		height: 40px;
		border-radius: var(--radius-md);
		object-fit: cover;
		background: var(--color-gray-100);
	}

	.result-info {
		display: flex;
		flex-direction: column;
		gap: 2px;
	}

	.result-name {
		font-weight: 500;
		color: var(--color-gray-900);
	}

	.result-type {
		font-size: var(--font-size-xs);
		color: var(--color-gray-500);
		text-transform: capitalize;
	}

	.search-loading,
	.search-no-results,
	.search-hint {
		padding: var(--spacing-6);
		text-align: center;
		color: var(--color-gray-500);
	}
</style>
