<script lang="ts">
	import favicon from '$lib/assets/favicon.png';
	import { apiUrl } from '$lib/api';
	import { onMount, onDestroy } from 'svelte';
	import { lastUpdatedReportId } from '$lib/stores/liveUpdates';

	let { children } = $props();

	interface AuditNotification {
		id:            string;
		auditId:       string;
		companyName:   string;
		reportYear:    number;
		reportQuarter: number;
		at:            string;
	}

	interface ToastEntry {
		notification: AuditNotification;
		fading:       boolean;
	}

	const VISIBLE_MS = 5000;
	const FADE_MS    = 400;

	let toasts = $state<ToastEntry[]>([]);
	let auditEs:  EventSource | null = null;
	let reportEs: EventSource | null = null;

	const timers = new Map<string, ReturnType<typeof setTimeout>>();

	function clearTimer(id: string) {
		const t = timers.get(id);
		if (t !== undefined) clearTimeout(t);
	}

	function startFade(id: string) {
		toasts = toasts.map(t =>
			t.notification.id === id ? { ...t, fading: true } : t
		);
		timers.set(id, setTimeout(() => remove(id), FADE_MS));
	}

	function startTimer(id: string) {
		clearTimer(id);
		timers.set(id, setTimeout(() => startFade(id), VISIBLE_MS));
	}

	function remove(id: string) {
		toasts = toasts.filter(t => t.notification.id !== id);
		timers.delete(id);
	}

	function onMouseEnter(id: string) {
		clearTimer(id);
		toasts = toasts.map(t =>
			t.notification.id === id ? { ...t, fading: false } : t
		);
	}

	function onMouseLeave(id: string) {
		startTimer(id);
	}

	let reconnectTimer: ReturnType<typeof setTimeout> | null = null;

	function connectSSE() {
		auditEs?.close();
		auditEs = new EventSource(apiUrl('/api/audit/stream'));

		auditEs.onmessage = (e) => {
			const notification: AuditNotification = JSON.parse(e.data);
			toasts = [...toasts, { notification, fading: false }];
			startTimer(notification.id);
		};

		auditEs.onerror = () => {
			auditEs?.close();
			auditEs = null;
			if (reconnectTimer) clearTimeout(reconnectTimer);
			reconnectTimer = setTimeout(connectSSE, 3000);
		};

		reportEs?.close();
		reportEs = new EventSource(apiUrl('/api/report/stream'));

		reportEs.onmessage = (e) => {
			const reportId: string = JSON.parse(e.data);
			lastUpdatedReportId.set(reportId);
		};

		reportEs.onerror = () => {
			reportEs?.close();
			reportEs = null;
		};
	}

	onMount(() => {
		connectSSE();
	});

	onDestroy(() => {
		if (reconnectTimer) clearTimeout(reconnectTimer);
		auditEs?.close();
		reportEs?.close();
		timers.forEach(clearTimeout);
	});
</script>

<svelte:head>
	<link rel="icon" href={favicon} />
</svelte:head>

<samples-ui-wrapper sourceLink="https://github.com/ravendb/samples-verity" theme="dark">
	{@render children()}
</samples-ui-wrapper>

<div class="audit-toast-stack">
	{#each toasts as t (t.notification.id)}
		<div
			class="audit-toast"
			class:fading={t.fading}
			onmouseenter={() => onMouseEnter(t.notification.id)}
			onmouseleave={() => onMouseLeave(t.notification.id)}
			role="status"
		>
			<div class="audit-toast__body">
				<strong>
					{t.notification.companyName} audit for Q{t.notification.reportQuarter} {t.notification.reportYear} report changed
				</strong>
			</div>
			<button class="audit-toast__close" onclick={() => remove(t.notification.id)}>✕</button>
		</div>
	{/each}
</div>

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

	.audit-toast-stack {
		position: fixed;
		bottom: 1.5rem;
		right: 1.5rem;
		z-index: 9999;
		display: flex;
		flex-direction: column;
		gap: 0.5rem;
		align-items: flex-end;
	}

	.audit-toast {
		display: flex;
		align-items: center;
		gap: 1rem;
		padding: 0.75rem 1rem;
		background: #1e3a5f;
		border: 1px solid #2e5c9a;
		border-radius: 8px;
		color: #e8f0fe;
		font-size: 0.875rem;
		box-shadow: 0 4px 20px rgba(0, 0, 0, 0.4);
		opacity: 1;
		transition: opacity 400ms ease-out;
		animation: slide-in 0.2s ease-out;
	}

	.audit-toast.fading {
		opacity: 0;
	}

	.audit-toast__body {
		display: flex;
		flex-direction: column;
		gap: 0.2rem;
	}

	.audit-toast__close {
		background: none;
		border: none;
		color: #93b4d8;
		cursor: pointer;
		padding: 0.25rem;
		line-height: 1;
		flex-shrink: 0;
	}
	.audit-toast__close:hover { color: #e8f0fe; }

	@keyframes slide-in {
		from { transform: translateY(1rem); opacity: 0; }
		to   { transform: translateY(0);    opacity: 1; }
	}
</style>
