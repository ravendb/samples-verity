<script lang="ts">
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import { page } from '$app/stores';
	import { loginUrl, register } from '$lib/auth';
	import { getCompanies, type Company } from '$lib/services/companies';

	const returnUrl  = $page.url.searchParams.get('returnUrl') ?? '/';
	const isRegister = $page.url.searchParams.get('tab') === 'register';

	// If not explicitly on the register tab, redirect straight to BFF login
	if (!isRegister) {
		goto(loginUrl(returnUrl), { replaceState: true });
	}

	// ── Company list ─────────────────────────────────────────────
	let companies: Company[] = $state([]);

	onMount(async () => {
		try {
			const data = await getCompanies(1, 100);
			companies = data.items;
		} catch { /* fallback to text input */ }
	});

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
				username,
				password,
				displayName,
				email,
				role,
				companyId: role === 'Employee' ? companyId : null,
			});

			if (res.success) {
				// After registration send user to login
				await goto(loginUrl(returnUrl));
			} else {
				error = res.error ?? 'Registration failed. Please try again.';
			}
		} catch {
			error = 'Network error. Please try again.';
		} finally {
			submitting = false;
		}
	}
</script>

<svelte:head>
	<title>Verity — Create account</title>
</svelte:head>

<main class="auth-page">
	<div class="card">
		<div class="logo">
			<span class="logo-text">Verity</span>
			<span class="logo-tag">SEC Filing Analysis</span>
		</div>

		<h1>Create account</h1>

		<form onsubmit={handleRegister}>
			{#if error}
				<div class="error">{error}</div>
			{/if}

			<label for="username">Username</label>
			<input
				id="username"
				type="text"
				bind:value={username}
				autocomplete="username"
				minlength="2"
				maxlength="50"
				required
			/>

			<label for="displayName">Full name</label>
			<input
				id="displayName"
				type="text"
				bind:value={displayName}
				autocomplete="name"
				minlength="2"
				maxlength="100"
				required
			/>

			<label for="email">Email</label>
			<input
				id="email"
				type="email"
				bind:value={email}
				autocomplete="email"
				required
			/>

			<label for="password">Password</label>
			<input
				id="password"
				type="password"
				bind:value={password}
				autocomplete="new-password"
				minlength="8"
				maxlength="100"
				required
			/>
			<p class="hint">At least 8 characters.</p>

			<label for="confirm">Confirm password</label>
			<input
				id="confirm"
				type="password"
				bind:value={confirm}
				autocomplete="new-password"
				required
			/>

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
				<label for="companyId">Company</label>
				{#if companies.length > 0}
					<select id="companyId" bind:value={companyId} required>
						<option value="" disabled selected>Select a company…</option>
						{#each companies as c}
							<option value={c.id}>{c.name}</option>
						{/each}
					</select>
				{:else}
					<input
						id="companyId"
						type="text"
						bind:value={companyId}
						placeholder="e.g. companies/1-A"
						required
					/>
				{/if}
			{/if}

			<button type="submit" class="btn-primary" disabled={submitting}>
				{submitting ? 'Creating account…' : 'Create account'}
			</button>
		</form>

		<p class="footer-text">
			Already have an account? <a href={loginUrl(returnUrl)}>Sign in</a>
		</p>
	</div>
</main>

<style>
	.auth-page {
		min-height: 100vh;
		display: flex;
		align-items: center;
		justify-content: center;
		background: #192d47;
		padding: 1rem;
	}

	.card {
		background: #1e3a5f;
		border: 1px solid #2e5c9a;
		border-radius: 12px;
		padding: 2.5rem 2rem;
		width: 100%;
		max-width: 400px;
	}

	/* Logo */
	.logo {
		display: flex;
		flex-direction: column;
		align-items: center;
		margin-bottom: 1.5rem;
	}

	.logo-text {
		font-size: 2rem;
		font-weight: 700;
		color: #e8f0fe;
		letter-spacing: -0.02em;
	}

	.logo-tag {
		font-size: 0.75rem;
		color: #5b8fd4;
		text-transform: uppercase;
		letter-spacing: 0.1em;
	}

	h1 {
		margin: 0 0 1.25rem;
		font-size: 1.4rem;
		color: #e8f0fe;
		text-align: center;
	}

	/* Form */
	form {
		display: flex;
		flex-direction: column;
		gap: 0;
	}

	label {
		display: block;
		font-size: 0.85rem;
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
		font-size: 1rem;
		margin-bottom: 1rem;
		outline: none;
		box-sizing: border-box;
		transition: border-color 0.15s;
	}

	input[type='text']:focus,
	input[type='email']:focus,
	input[type='password']:focus {
		border-color: #5b8fd4;
	}

	select {
		width: 100%;
		padding: 0.6rem 0.75rem;
		border: 1px solid #2e5c9a;
		border-radius: 6px;
		background: #122238;
		color: #e8f0fe;
		font-size: 1rem;
		margin-bottom: 1rem;
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
		margin: -0.75rem 0 1rem;
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
		margin-bottom: 1rem;
	}

	.role-option {
		flex: 1;
		display: flex;
		align-items: center;
		justify-content: center;
		padding: 0.6rem 0.5rem;
		border: 1px solid #2e5c9a;
		border-radius: 6px;
		cursor: pointer;
		font-size: 0.875rem;
		color: #93b4d8;
		margin-bottom: 0;
		transition: background 0.15s, color 0.15s, border-color 0.15s;
		text-align: center;
	}

	.role-option input[type='radio'] {
		display: none;
	}

	.role-option.selected {
		background: #2e5c9a;
		border-color: #5b8fd4;
		color: #e8f0fe;
	}

	.role-option:hover:not(.selected) {
		background: rgba(46, 92, 154, 0.3);
	}

	.btn-primary {
		display: block;
		width: 100%;
		padding: 0.75rem;
		background: #2e5c9a;
		border: none;
		border-radius: 8px;
		color: #e8f0fe;
		font-size: 1rem;
		font-weight: 500;
		cursor: pointer;
		margin-top: 0.5rem;
		transition: background 0.15s;
		box-sizing: border-box;
	}

	.btn-primary:hover:not(:disabled) {
		background: #3a70b8;
	}

	.btn-primary:disabled {
		opacity: 0.6;
		cursor: not-allowed;
	}

	.error {
		background: rgba(248, 113, 113, 0.1);
		border: 1px solid rgba(248, 113, 113, 0.4);
		border-radius: 6px;
		padding: 0.6rem 0.75rem;
		color: #f87171;
		font-size: 0.875rem;
		margin-bottom: 1rem;
	}

	.footer-text {
		margin: 1.25rem 0 0;
		font-size: 0.875rem;
		color: #93b4d8;
		text-align: center;
	}

	.footer-text a {
		color: #5b8fd4;
		text-decoration: none;
	}

	.footer-text a:hover {
		text-decoration: underline;
	}
</style>
