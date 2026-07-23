<svelte:head>
	<title>Verity — Security Events</title>
</svelte:head>

<script lang="ts">
	import { onMount } from 'svelte';
	import { getSecurityEvents, type SecurityEvent } from '$lib/services/security';

	let events      = $state<SecurityEvent[]>([]);
	let status      = $state<'loading' | 'ok' | 'empty' | 'error'>('loading');
	let errorMsg    = $state('');
	let currentPage = $state(1);
	let totalPages  = $state(1);
	const pageSize  = 20;

	onMount(() => loadEvents());

	async function loadEvents(page = currentPage) {
		status = 'loading';
		try {
			const data  = await getSecurityEvents(page, pageSize);
			events      = data.items;
			currentPage = data.page;
			totalPages  = data.totalPages;
			status      = data.items.length > 0 ? 'ok' : 'empty';
		} catch (e: unknown) {
			errorMsg = e instanceof Error ? e.message : 'Unknown error';
			status   = 'error';
		}
	}

	function goToPage(page: number) {
		if (page < 1 || page > totalPages) return;
		loadEvents(page);
	}

	function formatDate(iso: string): string {
		return new Date(iso).toLocaleString(undefined, {
			dateStyle: 'medium',
			timeStyle: 'medium',
		});
	}

	function eventLabel(type: string): string {
		return type.replace(/([A-Z])/g, ' $1').trim();
	}
</script>

<main>
	<header>
		<a href="/" class="back-link">← Companies</a>
		<h1>Security Events</h1>
		<div></div>
	</header>

	<div class="description">
		Authentication events recorded by Duende IdentityServer and stored in RavenDB.
		Each row represents a login, logout, token issuance, or auth failure.
	</div>

	{#if status === 'loading'}
		<div class="state-msg">
			<div class="spinner"></div>
			<p>Loading events…</p>
		</div>

	{:else if status === 'error'}
		<div class="state-msg error">
			<p>✗ {errorMsg}</p>
		</div>

	{:else if status === 'empty'}
		<div class="state-msg">
			<p>No security events yet. Log in or out to generate the first ones.</p>
		</div>

	{:else}
		<div class="table-wrap">
			<table>
				<thead>
					<tr>
						<th>Event</th>
						<th>User</th>
						<th>IP</th>
						<th>Details</th>
						<th>Time</th>
					</tr>
				</thead>
				<tbody>
					{#each events as ev (ev.id)}
						<tr class:failure={!ev.success}>
							<td>
								<span class="badge" class:badge--ok={ev.success} class:badge--fail={!ev.success}>
									{ev.success ? '✓' : '✗'}
								</span>
								{eventLabel(ev.eventType)}
							</td>
							<td class="muted">{ev.userName ?? ev.userId ?? '—'}</td>
							<td class="muted mono">{ev.ipAddress ?? '—'}</td>
							<td class="muted">{ev.details ?? '—'}</td>
							<td class="muted mono">{formatDate(ev.at)}</td>
						</tr>
					{/each}
				</tbody>
			</table>
		</div>

		{#if totalPages > 1}
			<div class="pagination">
				<button class="page-btn" disabled={currentPage <= 1} onclick={() => goToPage(currentPage - 1)}>← Prev</button>
				<span class="page-info">Page {currentPage} of {totalPages}</span>
				<button class="page-btn" disabled={currentPage >= totalPages} onclick={() => goToPage(currentPage + 1)}>Next →</button>
			</div>
		{/if}
	{/if}
</main>

<style>
	main {
		width: 100%;
		background: #192d47;
		color: #d8e4f0;
		min-height: 100vh;
	}

	header {
		display: grid;
		grid-template-columns: 1fr auto 1fr;
		align-items: center;
		background: #0b2e5c;
		padding: 1rem 2rem;
		box-shadow: 0 2px 8px rgba(0,0,0,.5);
	}

	h1 {
		margin: 0;
		font-size: 1.2rem;
		font-weight: 600;
		text-align: center;
	}

	.back-link {
		color: #5b9bd5;
		text-decoration: none;
		font-size: 0.9rem;
		font-weight: 500;
	}
	.back-link:hover { text-decoration: underline; }

	.description {
		padding: 1rem 2rem;
		font-size: 0.875rem;
		color: #7a96b2;
		border-bottom: 1px solid #1e3a5f;
	}

	.state-msg {
		display: flex;
		flex-direction: column;
		align-items: center;
		gap: 1rem;
		padding: 5rem 2rem;
		color: #8aa4be;
	}
	.state-msg.error { color: #e74c3c; }

	.spinner {
		width: 32px; height: 32px;
		border: 3px solid #243550;
		border-top-color: #5b9bd5;
		border-radius: 50%;
		animation: spin 0.8s linear infinite;
	}
	@keyframes spin { to { transform: rotate(360deg); } }

	.table-wrap {
		padding: 1.5rem 2rem;
		overflow-x: auto;
	}

	table {
		width: 100%;
		border-collapse: collapse;
		font-size: 0.875rem;
	}

	thead tr {
		border-bottom: 1px solid #2e4a6a;
	}

	th {
		text-align: left;
		padding: 0.6rem 1rem;
		font-size: 0.75rem;
		font-weight: 600;
		color: #5e7a96;
		text-transform: uppercase;
		letter-spacing: 0.05em;
	}

	td {
		padding: 0.7rem 1rem;
		border-bottom: 1px solid #1a2e47;
		vertical-align: middle;
	}

	tr.failure td { background: rgba(231, 76, 60, 0.04); }

	.muted { color: #7a96b2; }
	.mono  { font-family: monospace; font-size: 0.82rem; }

	.badge {
		display: inline-flex;
		align-items: center;
		justify-content: center;
		width: 1.1rem;
		height: 1.1rem;
		border-radius: 50%;
		font-size: 0.65rem;
		font-weight: 700;
		margin-right: 0.4rem;
		vertical-align: middle;
	}
	.badge--ok   { background: rgba(39,174,96,.2);  color: #27ae60; }
	.badge--fail { background: rgba(231,76,60,.2);  color: #e74c3c; }

	.pagination {
		display: flex;
		align-items: center;
		justify-content: center;
		gap: 1rem;
		padding: 1.5rem 2rem 2rem;
	}

	.page-btn {
		padding: 0.45rem 1rem;
		background: #19253a;
		border: 1.5px solid #2a3f58;
		border-radius: 7px;
		font-size: 0.875rem;
		font-weight: 600;
		color: #5b9bd5;
		cursor: pointer;
		transition: background 0.15s;
	}
	.page-btn:hover:not(:disabled) { background: #1e3a5f; }
	.page-btn:disabled { opacity: 0.4; cursor: default; }

	.page-info {
		font-size: 0.875rem;
		color: #8aa4be;
	}
</style>
