<script lang="ts">
  import { fetch10Q } from '$lib/services/test';

  let cik = $state('');
  let max = $state(5);
  let status = $state<'idle' | 'loading' | 'ok' | 'error'>('idle');

  async function handleSubmit() {
    if (!cik.trim()) return;

    status = 'loading';
    try {
      await fetch10Q(cik.trim(), max);
      status = 'ok';
    } catch {
      status = 'error';
    }
  }
</script>

<main>
  <div class="card">
    <div class="card-header">
      <h1>SEC EDGAR — 10-Q Reports</h1>
      <p class="subtitle">Fetch quarterly reports for a company and save them to the database.</p>
    </div>

    <form onsubmit={(e) => { e.preventDefault(); handleSubmit(); }}>
      <label for="cik">Company CIK</label>
      <div class="input-row">
        <input
          id="cik"
          bind:value={cik}
          placeholder="e.g. 320193 (Apple)"
          disabled={status === 'loading'}
          autocomplete="off"
          spellcheck="false"
        />
        <input
          id="max"
          type="number"
          bind:value={max}
          min="1"
          max="50"
          disabled={status === 'loading'}
          class="input-max"
          title="Number of reports to fetch"
        />
        <button type="submit" disabled={!cik.trim() || status === 'loading'}>
          {status === 'loading' ? 'Fetching…' : 'Fetch 10-Q'}
        </button>
      </div>
      <p class="hint">Fetching <strong>{max}</strong> most recent report{max === 1 ? '' : 's'}.</p>
    </form>

    {#if status === 'ok'}
      <p class="feedback ok">✓ 10-Q reports have been saved successfully.</p>
    {:else if status === 'error'}
      <p class="feedback error">✗ Fetch failed. Please check the CIK and try again.</p>
    {/if}

    <div class="divider"></div>

    <a href="/reports" class="reports-btn">
      <span class="icon">📊</span>
      View Reports
    </a>
  </div>
</main>

<style>
  main {
    display: flex;
    align-items: center;
    justify-content: center;
    min-height: 100vh;
    padding: 2rem;
    background: linear-gradient(135deg, #0f2a5a 0%, #1a4a8a 50%, #1560bd 100%);
  }

  .card {
    background: #fff;
    border-radius: 14px;
    box-shadow: 0 8px 40px rgba(0,0,0,.25);
    padding: 2.5rem;
    width: 100%;
    max-width: 460px;
    display: flex;
    flex-direction: column;
    gap: 1.5rem;
  }

  .card-header {
    display: flex;
    flex-direction: column;
    gap: 0.4rem;
  }

  h1 {
    margin: 0;
    font-size: 1.35rem;
    font-weight: 700;
    color: #1a2a3a;
  }

  .subtitle {
    margin: 0;
    font-size: 0.875rem;
    color: #667;
    line-height: 1.5;
  }

  form {
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
  }

  label {
    font-size: 0.82rem;
    font-weight: 600;
    color: #445;
    text-transform: uppercase;
    letter-spacing: 0.04em;
  }

  .input-row {
    display: flex;
    gap: 0.5rem;
  }

  input {
    flex: 1;
    padding: 0.6rem 0.85rem;
    font-size: 1rem;
    border: 1.5px solid #ccd;
    border-radius: 7px;
    outline: none;
    color: #1a2a3a;
    transition: border-color 0.15s;
  }
  input:focus { border-color: #1a4a8a; }
  input:disabled { opacity: 0.5; background: #f5f7fa; }

  .input-max {
    flex: 0 0 72px;
    text-align: center;
  }

  .hint {
    margin: 0;
    font-size: 0.8rem;
    color: #889;
  }

  button {
    padding: 0.6rem 1.25rem;
    font-size: 0.95rem;
    font-weight: 600;
    background: #1a4a8a;
    color: #fff;
    border: none;
    border-radius: 7px;
    cursor: pointer;
    white-space: nowrap;
    transition: background 0.15s;
  }
  button:hover:not(:disabled) { background: #153c70; }
  button:disabled { opacity: 0.4; cursor: default; }

  .feedback {
    margin: 0;
    padding: 0.6rem 0.9rem;
    border-radius: 7px;
    font-size: 0.875rem;
  }
  .feedback.ok    { background: #e6f7eb; color: #1a6a30; }
  .feedback.error { background: #fde8e8; color: #991111; }

  .divider {
    border: none;
    border-top: 1.5px solid #e8eef5;
    margin: 0;
  }

  .reports-btn {
    display: flex;
    align-items: center;
    justify-content: center;
    gap: 0.6rem;
    padding: 0.8rem 1.5rem;
    background: #f0f5fb;
    border: 2px solid #1a4a8a;
    border-radius: 8px;
    color: #1a4a8a;
    font-size: 1rem;
    font-weight: 700;
    text-decoration: none;
    transition: background 0.15s, color 0.15s;
  }
  .reports-btn:hover {
    background: #1a4a8a;
    color: #fff;
  }

  .icon { font-size: 1.2rem; }
</style>
