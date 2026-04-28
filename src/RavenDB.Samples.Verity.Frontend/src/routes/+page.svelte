<svelte:head>
  <title>Verity</title>
</svelte:head>

<script lang="ts">
  import { onMount } from 'svelte';
  import { getCompanies, saveCompany, type Company } from '$lib/services/companies';

  let companies   = $state<Company[]>([]);
  let status      = $state<'loading' | 'ok' | 'empty' | 'error'>('loading');
  let errorMsg    = $state('');
  let currentPage = $state(1);
  let totalPages  = $state(1);
  const pageSize  = 20;

  // Add company modal
  let showModal   = $state(false);
  let cikInput    = $state('');
  let addStatus   = $state<'idle' | 'loading' | 'ok' | 'error'>('idle');
  let addErrorMsg = $state('');

  onMount(loadCompanies);

  async function loadCompanies(page = currentPage) {
    status = 'loading';
    try {
      const data  = await getCompanies(page, pageSize);
      companies   = data.items;
      currentPage = data.page;
      totalPages  = data.totalPages;
      status      = data.items.length > 0 ? 'ok' : 'empty';
    } catch (e: unknown) {
      errorMsg = e instanceof Error ? e.message : 'Unknown error';
      status   = 'error';
    }
  }

  async function goToPage(page: number) {
    if (page < 1 || page > totalPages) return;
    await loadCompanies(page);
  }

  async function handleAddCompany() {
    if (!cikInput.trim()) return;
    addStatus = 'loading';
    addErrorMsg = '';
    try {
      await saveCompany(cikInput.trim());
      addStatus = 'ok';
      cikInput  = '';
      await loadCompanies();
      setTimeout(() => { showModal = false; addStatus = 'idle'; }, 1200);
    } catch (e: unknown) {
      addErrorMsg = e instanceof Error ? e.message : 'Unknown error';
      addStatus = 'error';
    }
  }

  function openModal() {
    cikInput  = '';
    addStatus = 'idle';
    addErrorMsg = '';
    showModal = true;
  }
</script>

<main>
  <header>
    <h1><a href="/" class="verity-brand">Verity:</a> The Fiscal Truth Engine</h1>
    <button class="add-btn" onclick={openModal}>+ Add Company</button>
  </header>

  {#if status === 'loading'}
    <div class="state-msg">
      <div class="spinner"></div>
      <p>Loading companies…</p>
    </div>

  {:else if status === 'error'}
    <div class="state-msg error">
      <p>✗ Error: {errorMsg}</p>
    </div>

  {:else if status === 'empty'}
    <div class="state-msg">
      <p>No companies in the database. Add a company using the button above.</p>
    </div>

  {:else}
    <div class="grid">
      {#each companies as c}
        <a href="/companies/{encodeURIComponent(c.cik)}" class="card">
          <div class="card-name">{c.name}</div>
          <div class="card-meta">
            <span class="meta-item"><span class="meta-label">CIK</span>{c.cik}</span>
            {#if c.sic}
              <span class="meta-item"><span class="meta-label">SIC</span>{c.sic}</span>
            {/if}
          </div>
          {#if c.sicDescription}
            <div class="card-desc">{c.sicDescription}</div>
          {/if}
          {#if c.fiscalYearEnd}
            <div class="card-fy">Fiscal year end: {c.fiscalYearEnd}</div>
          {/if}
          <div class="card-arrow">View reports →</div>
        </a>
      {/each}
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

{#if showModal}
  <!-- svelte-ignore a11y_click_events_have_key_events a11y_no_static_element_interactions -->
  <div class="overlay" onclick={() => { if (addStatus !== 'loading') showModal = false; }}>
    <div class="modal" onclick={(e) => e.stopPropagation()}>
      <h2>Add Company</h2>
      <p class="modal-hint">Enter the <a class="modal-link" href="https://www.sec.gov/search-filings/cik-lookup" target="_blank" rel="noopener noreferrer">SEC EDGAR CIK</a> number to fetch and save company data.</p>
		<p class="modal-disclouse">(For best results enter companies from USA)</p>

      <form onsubmit={(e) => { e.preventDefault(); handleAddCompany(); }}>
        <label for="cik-input">Company CIK</label>
        <input
          id="cik-input"
          bind:value={cikInput}
          placeholder="e.g. 320193 (Apple)"
          disabled={addStatus === 'loading'}
          autocomplete="off"
          spellcheck="false"
        />
        <div class="modal-actions">
          <button type="button" class="btn-secondary"
            onclick={() => showModal = false}
            disabled={addStatus === 'loading'}>
            Cancel
          </button>
          <button type="submit" class="btn-primary"
            disabled={!cikInput.trim() || addStatus === 'loading'}>
            {addStatus === 'loading' ? 'Fetching…' : 'Add Company'}
          </button>
        </div>
      </form>

      {#if addStatus === 'ok'}
        <p class="feedback ok">✓ Company saved successfully.</p>
      {:else if addStatus === 'error'}
        <p class="feedback error">✗ {addErrorMsg || 'Failed to add company.'}</p>
      {/if}
    </div>
  </div>
{/if}

<style>
	main {
	width: 100%;
	background: #192d47;
	color: #d8e4f0;
	}

	/* Header */
	header {
	display: flex;
	align-items: center;
	justify-content: space-between;
	background: #0b2e5c;
	color: #fff;
	padding: 1rem 2rem;
	box-shadow: 0 2px 8px rgba(0,0,0,.5);
	}

	.header-left {
	display: flex;
	align-items: center;
	gap: 0.75rem;
	}

	h1 {
	margin: 0;
	font-size: 1.3rem;
	font-weight: 600;
	flex: 1;
	}

	.badge {
	font-size: 0.8rem;
	background: rgba(255,255,255,.12);
	padding: 0.2rem 0.6rem;
	border-radius: 999px;
	}

	.add-btn {
	padding: 0.5rem 1.1rem;
	background: #121d30;
	color: #5b9bd5;
	border: none;
	border-radius: 7px;
	font-size: 0.9rem;
	font-weight: 700;
	cursor: pointer;
	transition: background 0.15s, color 0.15s;
	}
	.add-btn:hover { background: #e0eaf8; }

	/* States */
	.state-msg {
	display: flex;
	flex-direction: column;
	align-items: center;
	justify-content: center;
	gap: 1rem;
	padding: 5rem 2rem;
	color: #8aa4be;
	}
	.state-msg.error { color: #b00; }

	.spinner {
	width: 36px; height: 36px;
	border: 3px solid #243550;
	border-top-color: #5b9bd5;
	border-radius: 50%;
	animation: spin 0.8s linear infinite;
	}
	@keyframes spin { to { transform: rotate(360deg); } }

	/* Grid */
	.grid {
	display: grid;
	grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
	gap: 1.25rem;
	padding: 2rem;
	}

	.card {
	background: #121d30;
	border-radius: 12px;
	box-shadow: 0 2px 10px rgba(0,0,0,.5);
	padding: 1.25rem 1.5rem;
	text-decoration: none;
	color: inherit;
	display: flex;
	flex-direction: column;
	gap: 0.5rem;
	transition: box-shadow 0.15s, transform 0.12s;
	border: 2px solid transparent;
	}
	.card:hover {
	box-shadow: 0 6px 20px rgba(26,74,138,.15);
	border-color: #5b9bd5;
	transform: translateY(-2px);
	}

	.card-name {
	font-size: 1.05rem;
	font-weight: 700;
	color: #d8e4f0;
	}

	.card-meta {
	display: flex;
	gap: 1rem;
	flex-wrap: wrap;
	}

	.meta-item {
	font-size: 0.8rem;
	color: #7a96b2;
	}

	.meta-label {
	font-weight: 600;
	color: #5e7a96;
	text-transform: uppercase;
	font-size: 0.7rem;
	margin-right: 0.3rem;
	}

	.card-desc {
	font-size: 0.82rem;
	color: #7a96b2;
	}

	.card-fy {
	font-size: 0.78rem;
	color: #5e7a96;
	}

	.card-arrow {
	margin-top: auto;
	font-size: 0.82rem;
	color: #5b9bd5;
	font-weight: 600;
	padding-top: 0.5rem;
	}

	/* Modal */
	.overlay {
	position: fixed;
	inset: 0;
	background: rgba(0,0,0,.45);
	display: flex;
	align-items: center;
	justify-content: center;
	z-index: 100;
	padding: 1rem;
	}

	.modal {
	background: #19253a;
	border-radius: 14px;
	box-shadow: 0 12px 48px rgba(0,0,0,.3);
	padding: 2rem;
	width: 100%;
	max-width: 420px;
	display: flex;
	flex-direction: column;
	gap: 1rem;
	}

	.modal h2 {
	margin: 0;
	font-size: 1.2rem;
	font-weight: 700;
	color: #d8e4f0;
	}

	.modal-hint {
	margin: 0;
	font-size: 0.85rem;
	color: #7a96b2;
	}

	.modal-disclouse {
	margin: 0;
	font-size: 0.7rem;
	color: #64819e;
	}

	form {
	display: flex;
	flex-direction: column;
	gap: 0.6rem;
	}

	label {
	font-size: 0.8rem;
	font-weight: 600;
	color: #8aa4be;
	text-transform: uppercase;
	letter-spacing: 0.04em;
	}

	.modal-link {
	color: #65d6d1;
	text-decoration: none;
	}

	input {
	padding: 0.65rem 0.9rem;
	font-size: 1rem;
	border: 1.5px solid #243550;
	border-radius: 7px;
	outline: none;
	color: #9ebddb;
	background: #121d30;
	transition: border-color 0.15s;
	}
	input:focus { border-color: #5b9bd5; }
	input:disabled { opacity: 0.5; background: #111e30; }

	.modal-actions {
	display: flex;
	gap: 0.6rem;
	justify-content: flex-end;
	padding-top: 0.4rem;
	}

	.btn-primary {
	padding: 0.55rem 1.2rem;
	background: #1a4a8a;
	color: #fff;
	border: none;
	border-radius: 7px;
	font-size: 0.9rem;
	font-weight: 600;
	cursor: pointer;
	transition: background 0.15s;
	}
	.btn-primary:hover:not(:disabled) { background: #153c70; }
	.btn-primary:disabled { opacity: 0.4; cursor: default; }

	.btn-secondary {
	padding: 0.55rem 1.2rem;
	background: #0e1621;
	color: #8aa4be;
	border: 1.5px solid #243550;
	border-radius: 7px;
	font-size: 0.9rem;
	font-weight: 600;
	cursor: pointer;
	transition: background 0.15s;
	}
	.btn-secondary:hover:not(:disabled) { background: #e0eaf8; }
	.btn-secondary:disabled { opacity: 0.4; cursor: default; }

	.feedback {
	margin: 0;
	padding: 0.6rem 0.9rem;
	border-radius: 7px;
	font-size: 0.875rem;
	}
	.feedback.ok    { background: #0d2a1a; color: #27ae60; }
	.feedback.error { background: #2a0d0d; color: #e74c3c; }

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
	transition: background 0.15s, border-color 0.15s;
	}
	.page-btn:hover:not(:disabled) { background: #e8f0fb; border-color: #5b9bd5; }
	.page-btn:disabled { opacity: 0.4; cursor: default; }

	.page-info {
	font-size: 0.875rem;
	font-weight: 600;
	color: #8aa4be;
	}
</style>
