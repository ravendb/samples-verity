<script lang="ts">
	import { onMount } from 'svelte';
	import { getAllUsers, setUserRole, setUserCompanies, type User } from '$lib/services/users';
	import { getCompanies, type Company } from '$lib/services/companies';

	let users      = $state<User[]>([]);
	let companies  = $state<Company[]>([]);
	let loading    = $state(true);
	let pageError  = $state('');

	let pendingRole      = $state<Record<string, string>>({});
	let pendingCompanyIds = $state<Record<string, string[]>>({});
	let saving  = $state<Record<string, boolean>>({});
	let saved   = $state<Record<string, boolean>>({});

	onMount(async () => {
		try {
			const [usersData, companiesData] = await Promise.all([
				getAllUsers(),
				getCompanies(1, 200),
			]);
			users     = usersData;
			companies = companiesData.items;
			for (const u of users) {
				pendingRole[u.subjectId]       = u.role;
				pendingCompanyIds[u.subjectId] = [...u.companyIds];
			}
		} catch (e) {
			pageError = e instanceof Error ? e.message : 'Failed to load data. Make sure you are logged in as Admin.';
		} finally {
			loading = false;
		}
	});

	async function saveUser(u: User) {
		saving[u.subjectId] = true;
		pageError = '';
		try {
			await Promise.all([
				setUserRole(u.subjectId, pendingRole[u.subjectId]),
				setUserCompanies(u.subjectId, pendingCompanyIds[u.subjectId] ?? []),
			]);
			users = users.map(x =>
				x.subjectId === u.subjectId
					? { ...x, role: pendingRole[u.subjectId], companyIds: pendingCompanyIds[u.subjectId] ?? [] }
					: x
			);
			saved[u.subjectId] = true;
			setTimeout(() => { saved[u.subjectId] = false; }, 2000);
		} catch {
			pageError = `Failed to save changes for ${u.name}.`;
		} finally {
			saving[u.subjectId] = false;
		}
	}

	function toggleCompany(subjectId: string, companyId: string) {
		const current = pendingCompanyIds[subjectId] ?? [];
		pendingCompanyIds[subjectId] = current.includes(companyId)
			? current.filter(id => id !== companyId)
			: [...current, companyId];
	}
</script>

<svelte:head>
	<title>Verity — Admin Panel</title>
</svelte:head>

<main>
	<header class="page-header">
		<div class="page-header__left">
			<a href="/" class="back-link">← Back</a>
			<h1>Admin Panel</h1>
		</div>
	</header>

	{#if loading}
		<p class="status">Loading…</p>
	{:else if pageError}
		<p class="status status--error">{pageError}</p>
	{:else}
		<section class="users-section">
			<p class="section-info">{users.length} user{users.length !== 1 ? 's' : ''}</p>
			<div class="table-wrap">
				<table>
					<thead>
						<tr>
							<th>User</th>
							<th>Role</th>
							<th>Companies</th>
							<th></th>
						</tr>
					</thead>
					<tbody>
						{#each users as u (u.id)}
							<tr>
								<td class="cell-user">
									<span class="user-name">{u.name} {u.surname}</span>
									<span class="user-email">{u.email}</span>
								</td>
								<td class="cell-role">
									<select bind:value={pendingRole[u.subjectId]}>
										<option value="Viewer">Viewer</option>
										<option value="Analyst">Analyst</option>
										<option value="Admin">Admin</option>
									</select>
								</td>
								<td class="cell-companies">
									{#if pendingRole[u.subjectId] === 'Analyst'}
										<div class="company-list">
											{#each companies as c}
												<label class="company-check">
													<input
														type="checkbox"
														checked={pendingCompanyIds[u.subjectId]?.includes(c.id)}
														onchange={() => toggleCompany(u.subjectId, c.id)}
													/>
													{c.name}
												</label>
											{/each}
										</div>
									{:else}
										<span class="na">—</span>
									{/if}
								</td>
								<td class="cell-action">
									<button
										class="save-btn"
										class:save-btn--saved={saved[u.subjectId]}
										disabled={saving[u.subjectId]}
										onclick={() => saveUser(u)}
									>
										{saving[u.subjectId] ? 'Saving…' : saved[u.subjectId] ? 'Saved ✓' : 'Save'}
									</button>
								</td>
							</tr>
						{/each}
					</tbody>
				</table>
			</div>
		</section>
	{/if}
</main>

<style>
	main {
		max-width: 1100px;
		margin: 0 auto;
		padding: 2rem 1.5rem;
		color: #e8f0fe;
	}

	.page-header {
		display: flex;
		align-items: center;
		justify-content: space-between;
		margin-bottom: 2rem;
	}
	.page-header__left {
		display: flex;
		align-items: center;
		gap: 1rem;
	}

	h1 {
		margin: 0;
		font-size: 1.6rem;
		font-weight: 700;
		color: #e8f0fe;
	}

	.back-link {
		font-size: 0.875rem;
		color: #5b8fd4;
		text-decoration: none;
	}
	.back-link:hover { color: #93b4d8; }

	.status {
		text-align: center;
		color: #93b4d8;
		padding: 3rem 0;
	}
	.status--error { color: #f87171; }

	.section-info {
		font-size: 0.82rem;
		color: #6a8fb5;
		margin: 0 0 0.75rem;
	}

	.table-wrap {
		overflow-x: auto;
	}

	table {
		width: 100%;
		border-collapse: collapse;
		font-size: 0.9rem;
	}

	thead th {
		text-align: left;
		padding: 0.6rem 0.75rem;
		font-size: 0.75rem;
		font-weight: 600;
		color: #6a8fb5;
		text-transform: uppercase;
		letter-spacing: 0.05em;
		border-bottom: 1px solid #2e5c9a;
	}

	tbody tr {
		border-bottom: 1px solid rgba(46, 92, 154, 0.3);
	}
	tbody tr:last-child { border-bottom: none; }
	tbody tr:hover { background: rgba(46, 92, 154, 0.08); }

	td {
		padding: 0.75rem;
		vertical-align: top;
	}

	.cell-user {
		display: flex;
		flex-direction: column;
		gap: 0.2rem;
	}
	.user-name  { font-weight: 500; color: #e8f0fe; }
	.user-email { font-size: 0.78rem; color: #6a8fb5; }

	.cell-role select {
		padding: 0.4rem 0.6rem;
		border: 1px solid #2e5c9a;
		border-radius: 5px;
		background: #122238;
		color: #e8f0fe;
		font-size: 0.875rem;
		cursor: pointer;
		outline: none;
	}
	.cell-role select:focus { border-color: #5b8fd4; }

	.cell-companies { min-width: 200px; }

	.company-list {
		display: flex;
		flex-direction: column;
		gap: 0.3rem;
		max-height: 180px;
		overflow-y: auto;
	}

	.company-check {
		display: flex;
		align-items: center;
		gap: 0.5rem;
		font-size: 0.82rem;
		color: #93b4d8;
		cursor: pointer;
		user-select: none;
	}
	.company-check input[type='checkbox'] { cursor: pointer; accent-color: #5b8fd4; }

	.na { color: #4a6080; font-size: 0.875rem; }

	.cell-action { white-space: nowrap; }

	.save-btn {
		padding: 0.4rem 1rem;
		border: 1px solid #2e5c9a;
		border-radius: 5px;
		background: #2e5c9a;
		color: #e8f0fe;
		font-size: 0.82rem;
		cursor: pointer;
		transition: background 0.15s;
	}
	.save-btn:hover:not(:disabled) { background: #3a70b8; }
	.save-btn:disabled { opacity: 0.6; cursor: not-allowed; }
	.save-btn--saved {
		background: rgba(74, 222, 128, 0.15);
		border-color: rgba(74, 222, 128, 0.4);
		color: #4ade80;
	}
</style>
