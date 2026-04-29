<script lang="ts">
	import { loginUrl, register } from '$lib/auth';
	import { getCompanies, type Company } from '$lib/services/companies';
	import { authModal } from '$lib/stores/authModal';
	import { page } from '$app/stores';

	// Build the login URL with the current page as returnUrl so BFF redirects
	// back here after a successful OIDC flow.
	let currentLoginUrl = $derived(loginUrl($page.url.pathname + $page.url.search));

	// ── Company list (loaded once, reused) ───────────────────────
	let companies: Company[] = $state([]);
	let companiesLoaded      = $state(false);

	// ── Register form state ──────────────────────────────────────
	let username    = $state('');
	let displayName = $state('');
	let email       = $state('');
	let password    = $state('');
	let confirm     = $state('');
	let role        = $state<'User' | 'Employee'>('User');
	let companyId   = $state('');
	let error       = $state('');
	let submitting  = $state(false);
	let registered  = $state(false);

	$effect(() => {
		if ($authModal.open) {
			if (!companiesLoaded) loadCompanies();
		} else {
			resetForm();
		}
	});

	async function loadCompanies() {
		try {
			const data = await getCompanies(1, 100);
			companies       = data.items;
			companiesLoaded = true;
		} catch { /* silently ignore — fallback to text input */ }
	}

	function resetForm() {
		username = displayName = email = password = confirm = companyId = error = '';
		role       = 'User';
		submitting = false;
		registered = false;
	}

	async function handleRegister(e: Event) {
		e.preventDefault();
		error = '';

		if (password !== confirm) {
			error = 'Passwords do not match.';
			return;
		}

		submitting = true;
		try {
			const res = await register({
				username, password, displayName, email, role,
				companyId: role === 'Employee' ? companyId : null,
			});
			if (res.success) {
				registered = true;
				authModal.switchTab('login');
			} else {
				error = res.error ?? 'Registration failed. Please try again.';
			}
		} catch {
			error = 'Network error. Please try again.';
		} finally {
			submitting = false;
		}
	}

	function onOverlayKeydown(e: KeyboardEvent) {
		if (e.key === 'Escape') authModal.close();
	}
</script>

<svelte:window onkeydown={onOverlayKeydown} />

{#if $authModal.open}
	<!-- svelte-ignore a11y_no_static_element_interactions a11y_click_events_have_key_events -->
	<div class="overlay" onclick={authModal.close}>
		<div
			class="modal"
			onclick={(e) => e.stopPropagation()}
			role="dialog"
			aria-modal="true"
			aria-label="Authentication"
			tabindex="-1"
		>
			<button class="close-btn" onclick={authModal.close} aria-label="Close">✕</button>

			<div class="logo">
				<span class="logo-text">Verity</span>
				<span class="logo-tag">SEC Filing Analysis</span>
			</div>

			<!-- Tabs -->
			<div class="tabs" role="tablist">
				<button
					role="tab"
					class="tab"
					class:active={$authModal.tab === 'login'}
					aria-selected={$authModal.tab === 'login'}
					onclick={() => authModal.switchTab('login')}
				>Sign in</button>
				<button
					role="tab"
					class="tab"
					class:active={$authModal.tab === 'register'}
					aria-selected={$authModal.tab === 'register'}
					onclick={() => authModal.switchTab('register')}
				>Create account</button>
			</div>

			<!-- ── Sign-in panel ───────────────────────────────── -->
			{#if $authModal.tab === 'login'}
				<div class="section" role="tabpanel">
					{#if registered}
						<div class="success">Account created — you can sign in now.</div>
					{/if}
					{#if $authModal.hint}
						<div class="hint-banner">{$authModal.hint}</div>
					{/if}
					<p class="subtitle">Access financial reports and AI-powered audit analysis.</p>
					<a href={currentLoginUrl} class="btn-primary">Sign in</a>
				</div>

			<!-- ── Register panel ─────────────────────────────── -->
			{:else}
				<div class="section" role="tabpanel">
					<form onsubmit={handleRegister}>
						{#if error}
							<div class="error">{error}</div>
						{/if}

						<label for="am-username">Username</label>
						<input id="am-username" type="text" bind:value={username}
							autocomplete="username" minlength="2" maxlength="50" required />

						<label for="am-name">Full name</label>
						<input id="am-name" type="text" bind:value={displayName}
							autocomplete="name" minlength="2" maxlength="100" required />

						<label for="am-email">Email</label>
						<input id="am-email" type="email" bind:value={email}
							autocomplete="email" required />

						<label for="am-password">Password</label>
						<input id="am-password" type="password" bind:value={password}
							autocomplete="new-password" minlength="8" maxlength="100" required />
						<p class="hint">At least 8 characters.</p>

						<label for="am-confirm">Confirm password</label>
						<input id="am-confirm" type="password" bind:value={confirm}
							autocomplete="new-password" required />

						<p class="section-label">Account type</p>
						<div class="role-group">
							<label class="role-option" class:selected={role === 'User'}>
								<input type="radio" bind:group={role} value="User" />
								Regular user
							</label>
							<label class="role-option" class:selected={role === 'Employee'}>
								<input type="radio" bind:group={role} value="Employee" />
								Company employee
							</label>
						</div>

						{#if role === 'Employee'}
							<label for="am-company">Company</label>
							{#if companies.length > 0}
								<select id="am-company" bind:value={companyId} required>
									<option value="" disabled selected>Select a company…</option>
									{#each companies as c}
										<option value={c.id}>{c.name}</option>
									{/each}
								</select>
							{:else}
								<input id="am-company" type="text" bind:value={companyId}
									placeholder="e.g. Companies/Apple Inc." required />
							{/if}
						{/if}

						<button type="submit" class="btn-primary btn-submit" disabled={submitting}>
							{submitting ? 'Creating account…' : 'Create account'}
						</button>
					</form>
				</div>
			{/if}
		</div>
	</div>
{/if}

<style>
	.overlay {
		position: fixed;
		inset: 0;
		background: rgba(0, 0, 0, 0.55);
		display: flex;
		align-items: center;
		justify-content: center;
		z-index: 1000;
		padding: 1rem;
		backdrop-filter: blur(2px);
	}

	.modal {
		background: #1e3a5f;
		border: 1px solid #2e5c9a;
		border-radius: 14px;
		padding: 2rem;
		width: 100%;
		max-width: 420px;
		max-height: 90vh;
		overflow-y: auto;
		position: relative;
		box-shadow: 0 20px 60px rgba(0, 0, 0, 0.5);
		animation: pop-in 0.18s ease-out;
	}

	@keyframes pop-in {
		from { opacity: 0; transform: scale(0.95) translateY(8px); }
		to   { opacity: 1; transform: scale(1)    translateY(0);   }
	}

	.close-btn {
		position: absolute;
		top: 0.75rem;
		right: 0.75rem;
		background: none;
		border: none;
		color: #5b8fd4;
		font-size: 1.1rem;
		cursor: pointer;
		padding: 0.25rem 0.5rem;
		border-radius: 4px;
		line-height: 1;
		transition: color 0.15s, background 0.15s;
	}
	.close-btn:hover { color: #e8f0fe; background: rgba(255,255,255,0.08); }

	.logo {
		display: flex;
		flex-direction: column;
		align-items: center;
		margin-bottom: 1.5rem;
	}
	.logo-text {
		font-size: 1.8rem;
		font-weight: 700;
		color: #e8f0fe;
		letter-spacing: -0.02em;
	}
	.logo-tag {
		font-size: 0.7rem;
		color: #5b8fd4;
		text-transform: uppercase;
		letter-spacing: 0.1em;
	}

	/* Tabs */
	.tabs {
		display: flex;
		border-bottom: 1px solid #2e5c9a;
		margin-bottom: 1.5rem;
	}
	.tab {
		flex: 1;
		padding: 0.55rem 0;
		background: none;
		border: none;
		border-bottom: 2px solid transparent;
		color: #5b8fd4;
		font-size: 0.9rem;
		font-weight: 500;
		cursor: pointer;
		margin-bottom: -1px;
		transition: color 0.15s, border-color 0.15s;
	}
	.tab:hover { color: #93b4d8; }
	.tab.active { color: #e8f0fe; border-bottom-color: #5b8fd4; }

	/* Panels */
	.section { display: flex; flex-direction: column; }

	.subtitle {
		margin: 0 0 1.25rem;
		font-size: 0.875rem;
		color: #93b4d8;
		text-align: center;
	}

	.hint-banner {
		background: rgba(91, 143, 212, 0.12);
		border: 1px solid rgba(91, 143, 212, 0.35);
		border-radius: 6px;
		padding: 0.55rem 0.75rem;
		color: #93b4d8;
		font-size: 0.875rem;
		margin-bottom: 1rem;
		text-align: center;
	}

	.success {
		background: rgba(74, 222, 128, 0.08);
		border: 1px solid rgba(74, 222, 128, 0.3);
		border-radius: 6px;
		padding: 0.55rem 0.75rem;
		color: #4ade80;
		font-size: 0.875rem;
		margin-bottom: 1rem;
	}

	/* Form */
	form { display: flex; flex-direction: column; }

	label {
		display: block;
		font-size: 0.82rem;
		color: #93b4d8;
		margin-bottom: 0.25rem;
	}

	input[type='text'],
	input[type='email'],
	input[type='password'] {
		width: 100%;
		padding: 0.6rem 0.75rem;
		border: 1px solid #2e5c9a;
		border-radius: 6px;
		background: #122238;
		color: #e8f0fe;
		font-size: 0.95rem;
		margin-bottom: 0.875rem;
		outline: none;
		box-sizing: border-box;
		transition: border-color 0.15s;
	}
	input[type='text']:focus,
	input[type='email']:focus,
	input[type='password']:focus { border-color: #5b8fd4; }

	select {
		width: 100%;
		padding: 0.6rem 0.75rem;
		border: 1px solid #2e5c9a;
		border-radius: 6px;
		background: #122238;
		color: #e8f0fe;
		font-size: 0.95rem;
		margin-bottom: 0.875rem;
		outline: none;
		box-sizing: border-box;
		cursor: pointer;
		transition: border-color 0.15s;
		appearance: none;
		background-image: url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='12' height='12' viewBox='0 0 12 12'%3E%3Cpath fill='%235b8fd4' d='M6 8L1 3h10z'/%3E%3C/svg%3E");
		background-repeat: no-repeat;
		background-position: right 0.75rem center;
		padding-right: 2rem;
	}
	select:focus { border-color: #5b8fd4; }

	.hint {
		font-size: 0.75rem;
		color: #6a8fb5;
		margin: -0.6rem 0 0.875rem;
	}

	.section-label {
		font-size: 0.75rem;
		color: #6a8fb5;
		text-transform: uppercase;
		letter-spacing: 0.05em;
		margin: 0 0 0.5rem;
	}

	.role-group {
		display: flex;
		gap: 0.75rem;
		margin-bottom: 0.875rem;
	}
	.role-option {
		flex: 1;
		display: flex;
		align-items: center;
		justify-content: center;
		padding: 0.55rem 0.5rem;
		border: 1px solid #2e5c9a;
		border-radius: 6px;
		cursor: pointer;
		font-size: 0.875rem;
		color: #93b4d8;
		transition: background 0.15s, color 0.15s, border-color 0.15s;
	}
	.role-option input[type='radio'] { display: none; }
	.role-option.selected { background: #2e5c9a; border-color: #5b8fd4; color: #e8f0fe; }
	.role-option:hover:not(.selected) { background: rgba(46, 92, 154, 0.3); }

	.btn-primary {
		display: block;
		width: 100%;
		padding: 0.72rem;
		background: #2e5c9a;
		border: none;
		border-radius: 8px;
		color: #e8f0fe;
		font-size: 0.95rem;
		font-weight: 500;
		text-align: center;
		text-decoration: none;
		cursor: pointer;
		transition: background 0.15s;
		box-sizing: border-box;
	}
	.btn-primary:hover:not(:disabled) { background: #3a70b8; }
	.btn-primary:disabled { opacity: 0.6; cursor: not-allowed; }
	.btn-submit { margin-top: 0.5rem; }

	.error {
		background: rgba(248, 113, 113, 0.1);
		border: 1px solid rgba(248, 113, 113, 0.4);
		border-radius: 6px;
		padding: 0.55rem 0.75rem;
		color: #f87171;
		font-size: 0.875rem;
		margin-bottom: 0.875rem;
	}
</style>
