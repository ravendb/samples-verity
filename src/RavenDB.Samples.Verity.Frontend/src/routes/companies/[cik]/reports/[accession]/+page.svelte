<script lang="ts">
  import { onMount } from 'svelte';
  import { page } from '$app/stores';
  import { getReport, type Report } from '$lib/services/reports';
  import { generateAuditDraft, getAuditRevisions, restoreAuditRevision, saveAudit, type Audit, type AuditRevision } from '$lib/services/audit';
  import { getUsersByCompany, type User } from '$lib/services/users';
  import AuditDiff from '$lib/components/AuditDiff.svelte';

  const accession = decodeURIComponent($page.params.accession);

  function autoResize(node: HTMLTextAreaElement) {
    function resize() {
      // Preserve scroll position — setting height='auto' triggers a reflow that causes the page to jump to top
      const scrollY = window.scrollY;
      node.style.height = 'auto';
      node.style.height = node.scrollHeight + 'px';
      window.scrollTo({ top: scrollY, behavior: 'instant' });
    }
    resize();
    node.addEventListener('input', resize);
    // Re-run when formNotes is set programmatically (e.g. after AI generation)
    $effect(() => { formNotes; resize(); });
    return { destroy() { node.removeEventListener('input', resize); } };
  }

  let report       = $state<Report | null>(null);
  let revisions     = $state<AuditRevision[]>([]);
  let restoreIdx    = $state<number | null>(null);
  let restoreStatus = $state<'idle' | 'loading' | 'ok' | 'error'>('idle');
  let status        = $state<'loading' | 'ok' | 'error'>('loading');
  let errorMsg      = $state('');

  // Inline edit form
  let editing      = $state(false);
  let users        = $state<User[]>([]);
  let selectedUser = $state<string>(''); // user id
  let formNotes    = $state('');
  let saveStatus   = $state<'idle' | 'saving' | 'error'>('idle');
  let agentStatus  = $state<'idle' | 'loading' | 'error'>('idle');
  let notesFromAi  = $state(false);

  async function openForm() {
    const current = revisions[0]?.data;
    users         = await getUsersByCompany(report!.companyId);
    // Pre-select the user matching the current auditor
    const match   = users.find(u =>
      u.name === current?.auditorName && u.surname === current?.auditorSurname
    );
    selectedUser  = match?.id ?? (users[0]?.id ?? '');
    formNotes     = current?.auditString ?? '';
    notesFromAi   = current?.generatedByAi ?? false;
    saveStatus    = 'idle';
    editing       = true;
  }

  async function handleSave() {
    const user = users.find(u => u.id === selectedUser);
    if (!user) return;
    saveStatus = 'saving';
    try {
      await saveAudit({
        reportId:       report!.id,
        auditorName:    user.name,
        auditorSurname: user.surname,
        auditorEmail:   user.email,
        auditString:    formNotes,
        generatedByAi:  notesFromAi,
      });
      revisions = (await getAuditRevisions(report!.id)) ?? [];
      editing   = false;
    } catch {
      saveStatus = 'error';
    }
  }

  async function handleGenerate() {
    if (!selectedUser || !report) return;
    agentStatus = 'loading';
    try {
      formNotes   = await generateAuditDraft(report.id, selectedUser);
      notesFromAi = true;
      agentStatus = 'idle';
    } catch {
      agentStatus = 'error';
    }
  }

  async function handleRestore(idx: number) {
    const rev = revisions[idx];
    if (!rev) return;
    restoreIdx    = idx;
    restoreStatus = 'loading';
    try {
      await restoreAuditRevision(rev.data.id, rev.changeVector);
      revisions     = (await getAuditRevisions(report!.id)) ?? [];
      restoreIdx    = null;
      restoreStatus = 'ok';
      setTimeout(() => { restoreStatus = 'idle'; }, 2500);
    } catch {
      restoreStatus = 'error';
    }
  }

  function fmtDate(iso: string): string {
    if (!iso) return '—';
    return new Date(iso).toLocaleString('en-US', {
      year: 'numeric', month: 'short', day: 'numeric',
      hour: '2-digit', minute: '2-digit',
    });
  }

  onMount(async () => {
    try {
      report = await getReport(accession);
      revisions = (await getAuditRevisions(report.id)) ?? [];
      status = 'ok';
    } catch (e: unknown) {
      errorMsg = e instanceof Error ? e.message : 'Unknown error';
      status = 'error';
    }
  });

  function fmt(n: number | null | undefined): string {
    if (n == null) return '—';
    const abs = Math.abs(n);
    const s = abs >= 1_000_000
      ? `${(abs / 1_000_000).toFixed(2)}M`
      : abs >= 1_000
      ? `${(abs / 1_000).toFixed(1)}K`
      : `${abs}`;
    return (n < 0 ? '−' : '') + '$' + s;
  }

  function fmtAbbr(n: number | null | undefined, abbr: string | null | undefined): string {
    if (n == null) return '—';
    return `$${n.toLocaleString('en-US')} ${abbr ?? ''}`.trim();
  }

  $effect(() => {
    if (report) {
      document.title = `Verity - ${report.companyId.split('/')[1]} ${report.year} Q${report.quarter}`;
    }
  });
</script>

<main>
  <header>
    {#if report}
      <h1><a href="/" class="verity-brand">Verity:</a> {report.companyId.split('/')[1]} - {report.year} · Q{report.quarter}</h1>
    {:else}
      <h1><a href="/" class="verity-brand">Verity:</a> Report</h1>
    {/if}
    <a href="/companies/{encodeURIComponent($page.params.cik)}" class="back-btn">← Back</a>
  </header>

  {#if status === 'loading'}
    <div class="state-msg"><div class="spinner"></div><p>Loading…</p></div>

  {:else if status === 'error'}
    <div class="state-msg error"><p>✗ {errorMsg}</p></div>

  {:else if report}
    <div class="content">

      <!-- Filing info -->
      <section class="card">
        <h2 class="section-title">Filing Info</h2>
        <div class="info-grid">
          <div class="info-item">
            <span class="label">Form Type</span>
            <span class="value"><span class="tag">{report.formType}</span></span>
          </div>
          <div class="info-item">
            <span class="label">Quarter</span>
            <span class="value mono">Q{report.quarter} {report.year}</span>
          </div>
          <div class="info-item">
            <span class="label">Report Date</span>
            <span class="value mono">{report.reportDate}</span>
          </div>
          <div class="info-item">
            <span class="label">Filing Date</span>
            <span class="value mono">{report.filingDate}</span>
          </div>
          <div class="info-item">
            <span class="label">Days to File</span>
            <span class="value mono">{report.daysBetween}</span>
          </div>
          <div class="info-item">
            <span class="label">Accession No.</span>
            <span class="value mono small">{report.accessionNumber}</span>
          </div>
          {#if report.sourceUrl}
            <div class="info-item">
              <span class="label">SEC Source</span>
              <a href={report.sourceUrl} target="_blank" rel="noopener" class="src-link">View on SEC EDGAR ↗</a>
            </div>
          {/if}
        </div>
      </section>

      <!-- Financials -->
      <section class="card">
        <h2 class="section-title">Financials {report.abbreviation ? `(${report.abbreviation})` : ''}</h2>
        <div class="financials-grid">
          <div class="fin-item">
            <span class="fin-label">Revenue</span>
            <span class="fin-value revenue">{fmtAbbr(report.revenues, report.abbreviation)}</span>
          </div>
          <div class="fin-item">
            <span class="fin-label">Expenses</span>
            <span class="fin-value expenses">{fmtAbbr(report.expenses, report.abbreviation)}</span>
          </div>
          <div class="fin-item">
            <span class="fin-label">Assets</span>
            <span class="fin-value">{fmtAbbr(report.assetsVal, report.abbreviation)}</span>
          </div>
          <div class="fin-item">
            <span class="fin-label">Income / Loss</span>
            <span class="fin-value" class:profit={report.profitable === true} class:loss={report.profitable === false}>
              {fmtAbbr(report.profitLoss, report.abbreviation)}
            </span>
          </div>
          <div class="fin-item">
            <span class="fin-label">Status</span>
            {#if report.profitable === true}
              <span class="pill profit">Profit</span>
            {:else if report.profitable === false}
              <span class="pill loss">Loss</span>
            {:else}
              <span class="pill neutral">—</span>
            {/if}
          </div>
        </div>
      </section>

      <!-- AI Summary -->
      {#if report.summary}
        <section class="card">
          <h2 class="section-title">AI Summary</h2>
          <p class="summary-text">{report.summary}</p>
        </section>
      {/if}

      <!-- Audit -->
      <section class="card">
        <div class="audit-section-header">
          <h2 class="section-title" style="margin:0">Audit</h2>
          {#if !editing}
            <button class="edit-btn" onclick={openForm}>
              {revisions.length > 0 ? '✎ Edit' : '+ Add Audit'}
            </button>
          {/if}
        </div>

        {#if editing}
          <!-- Inline form -->
          <form class="audit-form" onsubmit={(e) => { e.preventDefault(); handleSave(); }}>
            <label class="full">
              <span>Auditor</span>
              {#if users.length > 0}
                <select bind:value={selectedUser} required>
                  {#each users as u}
                    <option value={u.id}>{u.name} {u.surname} — {u.email}</option>
                  {/each}
                </select>
              {:else}
                <p class="no-users">No users found for this company.</p>
              {/if}
            </label>
            <div class="notes-label-row">
              <button
                type="button"
                class="generate-btn"
                disabled={agentStatus === 'loading' || !selectedUser}
                onclick={handleGenerate}>
                {#if agentStatus === 'loading'}
                  <span class="gen-spinner"></span> Generating…
                {:else}
                  ✨ Generate with AI
                {/if}
              </button>
              <span class="notes-label">Notes</span>
            </div>
            {#if agentStatus === 'error'}
              <p class="form-error">✗ Failed to generate audit notes.</p>
            {/if}
            <textarea
              class="notes-textarea"
              bind:value={formNotes}
              placeholder="Enter audit findings…"
              rows="6"
              use:autoResize
              oninput={() => { notesFromAi = false; }}
            ></textarea>
            {#if saveStatus === 'error'}
              <p class="form-error">✗ Failed to save audit.</p>
            {/if}
            <div class="form-actions">
              <button type="button" class="cancel-btn" onclick={() => editing = false}>Cancel</button>
              <button type="submit" class="save-btn" disabled={saveStatus === 'saving'}>
                {saveStatus === 'saving' ? 'Saving…' : 'Save'}
              </button>
            </div>
          </form>

        {:else if revisions.length > 0}
          <!-- Current version -->
          {#if revisions[0]}
            {@const current = revisions[0].data}
            <div class="audit-current" class:ai-generated={current.generatedByAi}>
              {#if current.generatedByAi}
                <div class="ai-banner">✨ Generated by AI</div>
              {/if}
              <div class="audit-grid">
                <div class="audit-item">
                  <span class="label">Auditor</span>
                  <span class="value">{current.auditorName} {current.auditorSurname}</span>
                </div>
                <div class="audit-item">
                  <span class="label">Email</span>
                  <a href="mailto:{current.auditorEmail}" class="audit-email">{current.auditorEmail}</a>
                </div>
                <div class="audit-item full">
                  <span class="label">Notes</span>
                  <p class="audit-notes">{current.auditString}</p>
                </div>
              </div>
            </div>
          {/if}

          <!-- Revision history -->
          {#if revisions.length > 1}
            <div class="history">
              <span class="rev-label">Revision history</span>
              {#each revisions.slice(1) as rev, i}
                {@const idx = i + 1}
                <div class="history-entry" class:ai-generated={rev.data.generatedByAi}>
                  {#if rev.data.generatedByAi}
                    <div class="ai-banner ai-banner--sm">✨ Generated by AI</div>
                  {/if}
                  <div class="history-header">
                    <div class="history-meta">
                      <span class="history-auditor">{rev.data.auditorName} {rev.data.auditorSurname}</span>
                      <span class="history-date">{fmtDate(rev.lastModified)}</span>
                    </div>
                    <button
                      class="restore-btn"
                      disabled={restoreStatus === 'loading' && restoreIdx === idx}
                      onclick={() => handleRestore(idx)}>
                      {restoreStatus === 'loading' && restoreIdx === idx ? 'Restoring…' : '↩ Restore'}
                    </button>
                  </div>
                  {#if restoreStatus === 'ok' && restoreIdx === null}
                    <span class="restore-feedback ok">✓ Restored.</span>
                  {:else if restoreStatus === 'error' && restoreIdx === idx}
                    <span class="restore-feedback error">✗ Restore failed.</span>
                  {/if}
                  <AuditDiff from={revisions[idx]} to={revisions[idx - 1]} />
                </div>
              {/each}
            </div>
          {/if}

        {:else}
          <p class="no-audit">No audit yet.</p>
        {/if}
      </section>
    </div>
  {/if}
</main>

<style>
	main {
	width: 100%;
	background: #192d47;
	color: #d8e4f0;
	}

	header {
	display: flex;
	align-items: center;
	gap: 1rem;
	background: #0b2e5c;
	color: #fff;
	padding: 1rem 2rem;
	box-shadow: 0 2px 8px rgba(0,0,0,.5);
	}

	h1 {
	margin: 0;
	font-size: 1.3rem;
	font-weight: 600;
	}

	.back-btn {
	color: #fff;
	text-decoration: none;
	font-size: 0.9rem;
	opacity: 0.85;
	white-space: nowrap;
	}
	.back-btn:hover { opacity: 1; }

	.audit-section-header {
	display: flex;
	align-items: center;
	justify-content: space-between;
	margin-bottom: 1.25rem;
	}

	.edit-btn {
	padding: 0.35rem 0.9rem;
	background: #16243a;
	border: 1.5px solid #2a3f58;
	border-radius: 7px;
	font-size: 0.825rem;
	font-weight: 600;
	color: #5b9bd5;
	cursor: pointer;
	transition: background 0.15s;
	}
	.edit-btn:hover { background: #1c2e4a; border-color: #5b9bd5; }

	.audit-form {
	display: flex;
	flex-direction: column;
	gap: 0.9rem;
	}

	.audit-form .form-row {
	display: flex;
	gap: 1rem;
	}
	.audit-form .form-row label { flex: 1; }

	.audit-form label {
	display: flex;
	flex-direction: column;
	gap: 0.3rem;
	}
	.audit-form label.full { width: 100%; }

	.audit-form label span {
	font-size: 0.7rem;
	font-weight: 700;
	text-transform: uppercase;
	letter-spacing: 0.05em;
	color: #5e7a96;
	}

	.audit-form select,
	.audit-form input,
	.audit-form textarea {
	padding: 0.5rem 0.75rem;
	border: 1.5px solid #243550;
	border-radius: 7px;
	font-size: 0.9rem;
	color: #d8e4f0;
	background: #111e30;
	font-family: inherit;
	resize: vertical;
	transition: border-color 0.15s;
	}
	.audit-form select:focus,
	.audit-form input:focus,
	.audit-form textarea:focus { outline: none; border-color: #5b9bd5; background: #19253a; }

	.no-users {
	margin: 0;
	font-size: 0.85rem;
	color: #5e7a96;
	font-style: italic;
	}

	.form-actions {
	display: flex;
	justify-content: flex-end;
	gap: 0.6rem;
	}

	.save-btn {
	padding: 0.45rem 1.2rem;
	background: #0b2e5c;
	color: #fff;
	border: none;
	border-radius: 7px;
	font-size: 0.875rem;
	font-weight: 600;
	cursor: pointer;
	transition: background 0.15s;
	}
	.save-btn:hover:not(:disabled) { background: #163d72; }
	.save-btn:disabled { opacity: 0.5; cursor: not-allowed; }

	.cancel-btn {
	padding: 0.45rem 1rem;
	background: #0e1621;
	color: #8aa4be;
	border: 1.5px solid #243550;
	border-radius: 7px;
	font-size: 0.875rem;
	font-weight: 600;
	cursor: pointer;
	transition: background 0.15s;
	}
	.cancel-btn:hover { background: #1c2e4a; }

	.notes-label-row {
	display: flex;
	align-items: center;
	gap: 0.6rem;
	margin-bottom: 0.3rem;
	}

	.notes-label {
	font-size: 0.7rem;
	font-weight: 700;
	text-transform: uppercase;
	letter-spacing: 0.05em;
	color: #5e7a96;
	}

	.generate-btn {
	display: flex;
	align-items: center;
	gap: 0.4rem;
	padding: 0.3rem 0.85rem;
	background: #1e1235;
	color: #b07ef0;
	border: 1.5px solid #5a3090;
	border-radius: 7px;
	font-size: 0.8rem;
	font-weight: 600;
	cursor: pointer;
	transition: background 0.15s;
	white-space: nowrap;
	}
	.generate-btn:hover:not(:disabled) { background: #2a1850; border-color: #9060d0; }
	.generate-btn:disabled { opacity: 0.5; cursor: not-allowed; }

	.gen-spinner {
	display: inline-block;
	width: 12px;
	height: 12px;
	border: 2px solid #c4a6f0;
	border-top-color: #5a2d9a;
	border-radius: 50%;
	animation: spin 0.7s linear infinite;
	}

	.notes-textarea {
	width: 100%;
	box-sizing: border-box;
	padding: 0.5rem 0.75rem;
	border: 1.5px solid #243550;
	border-radius: 7px;
	font-size: 0.9rem;
	color: #d8e4f0;
	background: #111e30;
	font-family: inherit;
	resize: none;
	overflow: hidden;
	transition: border-color 0.15s;
	}
	.notes-textarea:focus { outline: none; border-color: #5b9bd5; background: #19253a; }

	.form-error {
	margin: 0;
	font-size: 0.825rem;
	color: #e74c3c;
	}

	.no-audit {
	margin: 0;
	font-size: 0.9rem;
	color: #5e7a96;
	font-style: italic;
	}

	.badge {
	font-size: 0.75rem;
	background: rgba(255,255,255,.1);
	padding: 0.2rem 0.7rem;
	border-radius: 999px;
	white-space: nowrap;
	}

	.content {
	padding: 1.5rem 2rem;
	display: flex;
	flex-direction: column;
	gap: 1.25rem;
	}

	.card {
	background: #121d30;
	border-radius: 12px;
	box-shadow: 0 2px 10px rgba(0,0,0,.5);
	padding: 1.5rem;
	}

	.section-title {
	margin: 0 0 1.25rem;
	font-size: 0.95rem;
	font-weight: 700;
	text-transform: uppercase;
	letter-spacing: 0.06em;
	color: #5e7a96;
	}

	/* Filing info grid */
	.info-grid {
	display: flex;
	flex-wrap: wrap;
	gap: 1.5rem 2.5rem;
	}

	.info-item {
	display: flex;
	flex-direction: column;
	gap: 0.25rem;
	}

	.label {
	font-size: 0.7rem;
	font-weight: 700;
	text-transform: uppercase;
	letter-spacing: 0.06em;
	color: #5e7a96;
	}

	.value {
	font-size: 1rem;
	font-weight: 500;
	color: #d8e4f0;
	}
	.value.mono { }
	.value.small { font-size: 0.85rem; }

	.src-link {
	color: #5b9bd5;
	font-weight: 600;
	text-decoration: none;
	font-size: 0.95rem;
	}
	.src-link:hover { text-decoration: underline; }

	/* Financials */
	.financials-grid {
	display: flex;
	flex-wrap: wrap;
	gap: 1rem;
	}

	.fin-item {
	flex: 1;
	min-width: 140px;
	background: #111e30;
	border-radius: 10px;
	padding: 1rem 1.25rem;
	display: flex;
	flex-direction: column;
	gap: 0.4rem;
	}

	.fin-label {
	font-size: 0.7rem;
	font-weight: 700;
	text-transform: uppercase;
	letter-spacing: 0.06em;
	color: #5e7a96;
	}

	.fin-value {
	font-size: 1.4rem;
	font-weight: 700;
	color: #d8e4f0;
	}
	.fin-value.revenue  { color: #2ecc71; }
	.fin-value.expenses { color: #e74c3c; }
	.fin-value.profit   { color: #2ecc71; }
	.fin-value.loss     { color: #e74c3c; }

	/* Summary */
	.summary-text {
	margin: 0;
	line-height: 1.7;
	color: #8aa4be;
	font-size: 0.95rem;
	}

	/* Audit */
	.audit-grid {
	display: flex;
	flex-wrap: wrap;
	gap: 1.25rem 2.5rem;
	}

	.audit-item {
	display: flex;
	flex-direction: column;
	gap: 0.25rem;
	}
	.audit-item.full { width: 100%; }

	.audit-email {
	font-size: 1rem;
	font-weight: 500;
	color: #5b9bd5;
	text-decoration: none;
	}
	.audit-email:hover { text-decoration: underline; }

	.audit-notes {
	margin: 0;
	line-height: 1.7;
	color: #8aa4be;
	font-size: 0.95rem;
	white-space: pre-wrap;
	}

	.rev-label {
	display: block;
	font-size: 0.7rem;
	font-weight: 700;
	text-transform: uppercase;
	letter-spacing: 0.06em;
	color: #5e7a96;
	margin-bottom: 0.75rem;
	}

	.history {
	margin-top: 1.5rem;
	padding-top: 1.25rem;
	border-top: 1.5px solid #243550;
	display: flex;
	flex-direction: column;
	gap: 1rem;
	}

	.history-entry {
	background: #111e30;
	border: 1.5px solid #243550;
	border-radius: 10px;
	padding: 1rem 1.25rem;
	display: flex;
	flex-direction: column;
	gap: 0.75rem;
	}

	.history-header {
	display: flex;
	align-items: center;
	justify-content: space-between;
	gap: 1rem;
	}

	.history-meta {
	display: flex;
	flex-direction: column;
	gap: 0.15rem;
	}

	.history-auditor {
	font-size: 0.9rem;
	font-weight: 700;
	color: #d8e4f0;
	}

	.history-date {
	font-size: 0.75rem;
	color: #5e7a96;
	}

	/* AI-generated audit styling */
	.audit-current {
	display: flex;
	flex-direction: column;
	gap: 0.75rem;
	}

	.audit-current.ai-generated {
	border-left: 3px solid #8855cc;
	padding-left: 1rem;
	border-radius: 4px;
	}

	.history-entry.ai-generated {
	border-left: 3px solid #8855cc;
	background: #201140;
	}

	.ai-banner {
	display: flex;
	align-items: center;
	gap: 0.4rem;
	padding: 0.35rem 0.75rem;
	background: #201140;
	color: #8f67c7;
	border: 1px solid #5a3090;
	border-radius: 6px;
	font-size: 0.78rem;
	font-weight: 700;
	letter-spacing: 0.03em;
	width: fit-content;
	}

	.ai-banner--sm {
	padding: 0.2rem 0.6rem;
	font-size: 0.72rem;
	border-radius: 4px;
	}

	.restore-btn {
	padding: 0.35rem 0.85rem;
	background: #2a1f00;
	color: #f0b429;
	border: 1.5px solid #b89a00;
	border-radius: 7px;
	font-size: 0.8rem;
	font-weight: 600;
	cursor: pointer;
	white-space: nowrap;
	transition: background 0.15s;
	}
	.restore-btn:hover:not(:disabled) { background: #3a2e00; }
	.restore-btn:disabled { opacity: 0.5; cursor: not-allowed; }

	.restore-feedback {
	font-size: 0.8rem;
	font-weight: 600;
	}
	.restore-feedback.ok    { color: #27ae60; }
	.restore-feedback.error { color: #e74c3c; }

	/* Tags & pills */
  .tag {
    display: inline-block;
    background: rgba(255,255,255,.12);
    padding: 0.15rem 0.55rem;
    border-radius: 4px;
    font-weight: 700;
    font-size: 1rem;
  }

  .pill {
    display: inline-block;
    padding: 0.25rem 0.75rem;
    border-radius: 999px;
    font-weight: 600;
    font-size: 0.9rem;
  }
  .pill.profit  { background: #0d2a1a; color: #27ae60; }
  .pill.loss    { background: #2a0d0d; color: #e74c3c; }
  .pill.neutral { background: #1e2b3c; color: #6a84a0; }

  /* States */
  .state-msg {
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    gap: 1rem;
    padding: 4rem 2rem;
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
</style>