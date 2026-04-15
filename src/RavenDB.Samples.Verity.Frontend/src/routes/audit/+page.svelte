<script lang="ts">
  import { onMount } from 'svelte';
  import { page } from '$app/stores';
  import { getAudit, saveAudit, type Audit } from '$lib/services/audit';

  const reportId = decodeURIComponent($page.url.searchParams.get('reportId') ?? '');

  let existing  = $state<Audit | null>(null);
  let status    = $state<'loading' | 'ready' | 'saving' | 'success' | 'error'>('loading');
  let errorMsg  = $state('');

  // Form fields
  let auditorName    = $state('');
  let auditorSurname = $state('');
  let auditorEmail   = $state('');
  let auditString    = $state('');

  onMount(async () => {
    if (!reportId) {
      errorMsg = 'Missing reportId parameter.';
      status = 'error';
      return;
    }
    try {
      existing = await getAudit(reportId);
      if (existing) {
        auditorName    = existing.auditorName;
        auditorSurname = existing.auditorSurname;
        auditorEmail   = existing.auditorEmail;
        auditString    = existing.auditString;
      }
      status = 'ready';
    } catch (e: unknown) {
      errorMsg = e instanceof Error ? e.message : 'Unknown error';
      status = 'error';
    }
  });

  async function handleSubmit() {
    status = 'saving';
    try {
      existing = await saveAudit({ reportId, auditorName, auditorSurname, auditorEmail, auditString });
      status = 'success';
    } catch (e: unknown) {
      errorMsg = e instanceof Error ? e.message : 'Unknown error';
      status = 'error';
    }
  }
</script>

<main>
  <header>
    <a href="javascript:history.back()" class="back-btn">← Back</a>
    <h1>Audit</h1>
    {#if existing}
      <span class="badge">Editing existing audit</span>
    {:else if status === 'ready'}
      <span class="badge new">New audit</span>
    {/if}
  </header>

  {#if status === 'loading'}
    <div class="state-msg"><div class="spinner"></div><p>Loading…</p></div>

  {:else if status === 'error'}
    <div class="state-msg error"><p>✗ {errorMsg}</p></div>

  {:else if status === 'success'}
    <div class="state-msg success">
      <p>✓ Audit saved successfully.</p>
      <a href="javascript:history.back()" class="btn">← Back to report</a>
    </div>

  {:else}
    <div class="content">
      <section class="card">
        <h2 class="section-title">Auditor Details</h2>
        <form onsubmit={(e) => { e.preventDefault(); handleSubmit(); }}>
          <div class="form-row">
            <label>
              <span>First Name</span>
              <input type="text" bind:value={auditorName} required placeholder="John" />
            </label>
            <label>
              <span>Last Name</span>
              <input type="text" bind:value={auditorSurname} required placeholder="Smith" />
            </label>
          </div>
          <label class="full">
            <span>Email</span>
            <input type="email" bind:value={auditorEmail} required placeholder="john@company.com" />
          </label>
          <label class="full">
            <span>Audit Notes</span>
            <textarea bind:value={auditString} rows="6" required placeholder="Enter audit findings…"></textarea>
          </label>
          <div class="actions">
            <button type="submit" class="btn" disabled={status === 'saving'}>
              {status === 'saving' ? 'Saving…' : existing ? 'Update Audit' : 'Create Audit'}
            </button>
          </div>
        </form>
      </section>
    </div>
  {/if}
</main>

<style>
  main {
    width: 100%;
    background: #0e1621;
    color: #d8e4f0;
  }

  header {
    display: flex;
    align-items: center;
    gap: 1rem;
    background: #1a4a8a;
    color: #fff;
    padding: 1rem 2rem;
    box-shadow: 0 2px 8px rgba(0,0,0,.5);
  }

  h1 {
    margin: 0;
    font-size: 1.3rem;
    font-weight: 600;
    flex: 1;
  }

  .back-btn {
    color: #fff;
    text-decoration: none;
    font-size: 0.9rem;
    opacity: 0.85;
    white-space: nowrap;
  }
  .back-btn:hover { opacity: 1; }

  .badge {
    font-size: 0.75rem;
    background: rgba(255,255,255,.1);
    padding: 0.2rem 0.7rem;
    border-radius: 999px;
    white-space: nowrap;
  }
  .badge.new { background: rgba(100,200,130,.3); }

  .content {
    padding: 1.5rem 2rem;
  }

  .card {
    background: #19253a;
    border-radius: 12px;
    box-shadow: 0 2px 10px rgba(0,0,0,.5);
    padding: 1.5rem;
    max-width: 680px;
  }

  .section-title {
    margin: 0 0 1.25rem;
    font-size: 0.95rem;
    font-weight: 700;
    text-transform: uppercase;
    letter-spacing: 0.06em;
    color: #5e7a96;
  }

  form {
    display: flex;
    flex-direction: column;
    gap: 1rem;
  }

  .form-row {
    display: flex;
    gap: 1rem;
  }
  .form-row label { flex: 1; }

  label {
    display: flex;
    flex-direction: column;
    gap: 0.35rem;
  }
  label.full { width: 100%; }

  label span {
    font-size: 0.75rem;
    font-weight: 700;
    text-transform: uppercase;
    letter-spacing: 0.05em;
    color: #7a96b2;
  }

  input, textarea {
    padding: 0.55rem 0.75rem;
    border: 1.5px solid #243550;
    border-radius: 8px;
    font-size: 0.95rem;
    color: #d8e4f0;
    background: #111e30;
    transition: border-color 0.15s;
    font-family: inherit;
    resize: vertical;
  }
  input:focus, textarea:focus {
    outline: none;
    border-color: #5b9bd5;
    background: #19253a;
  }

  .actions {
    display: flex;
    justify-content: flex-end;
    margin-top: 0.5rem;
  }

  .btn {
    display: inline-block;
    background: #1a4a8a;
    color: #fff;
    border: none;
    padding: 0.6rem 1.4rem;
    border-radius: 8px;
    font-size: 0.95rem;
    font-weight: 600;
    cursor: pointer;
    text-decoration: none;
    transition: background 0.15s;
  }
  .btn:hover:not(:disabled) { background: #163d72; }
  .btn:disabled { opacity: 0.6; cursor: not-allowed; }

  .state-msg {
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    gap: 1rem;
    padding: 4rem 2rem;
    color: #8aa4be;
  }
  .state-msg.error   { color: #b00; }
  .state-msg.success { color: #2ecc71; }

  .spinner {
    width: 36px; height: 36px;
    border: 3px solid #243550;
    border-top-color: #5b9bd5;
    border-radius: 50%;
    animation: spin 0.8s linear infinite;
  }
  @keyframes spin { to { transform: rotate(360deg); } }
</style>
