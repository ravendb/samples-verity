<script lang="ts">
  import { onMount } from 'svelte';
  import { getReports, type Report } from '$lib/services/test';

  let reports = $state<Report[]>([]);
  let status  = $state<'loading' | 'ok' | 'empty' | 'error'>('loading');
  let errorMsg = $state('');

  onMount(async () => {
    try {
      const data = await getReports();
      reports = data;
      status  = data.length > 0 ? 'ok' : 'empty';
    } catch (e: unknown) {
      errorMsg = e instanceof Error ? e.message : 'Unknown error';
      status   = 'error';
    }
  });

  function formatNumber(n: number | null | undefined): string {
    if (n == null) return '—';
    const abs = Math.abs(n);
    const formatted = abs >= 1_000_000
      ? `$${(abs / 1_000_000).toFixed(3)}M`
      : `$${abs.toLocaleString('en-US')}`;
    return n < 0 ? `−${formatted}` : formatted;
  }
</script>

<main>
  <header>
    <a href="/" class="back-btn">← Back</a>
    <h1>10-Q Reports</h1>
    <span class="badge">{status === 'ok' ? `${reports.length} records` : ''}</span>
  </header>

  {#if status === 'loading'}
    <div class="state-msg">
      <div class="spinner"></div>
      <p>Loading reports…</p>
    </div>

  {:else if status === 'error'}
    <div class="state-msg error">
      <p>✗ Error: {errorMsg}</p>
    </div>

  {:else if status === 'empty'}
    <div class="state-msg">
      <p>No reports in the database. Fetch reports from the home page.</p>
    </div>

  {:else}
    <div class="table-wrap">
      <table>
        <thead>
          <tr>
            <th>Company</th>
            <th>CIK</th>
            <th>Type</th>
            <th>Filing Date</th>
            <th>Report Date</th>
            <th>Days Between</th>
            <th>Profit / Loss</th>
            <th>Status</th>
            <th>AI Summary</th>
            <th>Source</th>
          </tr>
        </thead>
        <tbody>
          {#each reports as r}
            <tr>
              <td class="company">{r.company}</td>
              <td class="mono">{r.cik}</td>
              <td><span class="tag">{r.formType}</span></td>
              <td class="mono">{r.filingDate}</td>
              <td class="mono">{r.reportDate}</td>
              <td class="center">{r.daysBetween}</td>
              <td class="number" class:profit={r.profitable === true} class:loss={r.profitable === false}>
                {formatNumber(r.profitLoss)}
              </td>
              <td class="center">
                {#if r.profitable === true}
                  <span class="pill profit">Profit</span>
                {:else if r.profitable === false}
                  <span class="pill loss">Loss</span>
                {:else}
                  <span class="pill neutral">—</span>
                {/if}
              </td>
              <td class="summary">{r.profitabilitySummary ?? '—'}</td>
              <td>
                {#if r.sourceUrl}
                  <a href={r.sourceUrl} target="_blank" rel="noopener" class="src-link">SEC ↗</a>
                {:else}
                  —
                {/if}
              </td>
            </tr>
          {/each}
        </tbody>
      </table>
    </div>
  {/if}
</main>

<style>
  main {
    min-height: 100vh;
    padding: 0 0 3rem;
    background: #f0f4f9;
    color: #1a2a3a;
  }

  /* ── Header ── */
  header {
    display: flex;
    align-items: center;
    gap: 1rem;
    background: #1a4a8a;
    color: #fff;
    padding: 1rem 2rem;
    box-shadow: 0 2px 8px rgba(0,0,0,.2);
  }

  h1 {
    margin: 0;
    font-size: 1.3rem;
    font-weight: 600;
    flex: 1;
  }

  .badge {
    font-size: 0.8rem;
    background: rgba(255,255,255,.2);
    padding: 0.2rem 0.6rem;
    border-radius: 999px;
  }

  .back-btn {
    color: #fff;
    text-decoration: none;
    font-size: 0.9rem;
    opacity: 0.85;
    white-space: nowrap;
  }
  .back-btn:hover { opacity: 1; }

  /* ── States ── */
  .state-msg {
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    gap: 1rem;
    padding: 5rem 2rem;
    color: #445;
  }
  .state-msg.error { color: #b00; }

  .spinner {
    width: 36px; height: 36px;
    border: 3px solid #ccd;
    border-top-color: #1a4a8a;
    border-radius: 50%;
    animation: spin 0.8s linear infinite;
  }
  @keyframes spin { to { transform: rotate(360deg); } }

  /* ── Table ── */
  .table-wrap {
    padding: 1.5rem 2rem;
    overflow-x: auto;
  }

  table {
    width: 100%;
    border-collapse: collapse;
    background: #fff;
    border-radius: 10px;
    overflow: hidden;
    box-shadow: 0 2px 12px rgba(0,0,0,.08);
    font-size: 0.875rem;
  }

  thead {
    background: #1a4a8a;
    color: #fff;
  }

  thead th {
    padding: 0.8rem 1rem;
    text-align: left;
    font-weight: 600;
    white-space: nowrap;
    letter-spacing: 0.02em;
    font-size: 0.78rem;
    text-transform: uppercase;
  }

  tbody tr {
    border-bottom: 1px solid #e8eef5;
    transition: background 0.12s;
  }
  tbody tr:last-child { border-bottom: none; }
  tbody tr:hover { background: #f0f5fb; }

  td {
    padding: 0.7rem 1rem;
    vertical-align: middle;
  }

  .company  { font-weight: 600; color: #1a4a8a; }
  .mono     { font-family: 'Courier New', monospace; font-size: 0.82rem; color: #445; }
  .center   { text-align: center; }
  .number   { text-align: right; font-weight: 600; font-family: monospace; }
  .number.profit { color: #1a7a3a; }
  .number.loss   { color: #aa2222; }

  .summary {
    font-size: 0.8rem;
    color: #556;
    max-width: 260px;
    line-height: 1.4;
  }

  /* Tags */
  .tag {
    display: inline-block;
    background: #e8eef8;
    color: #1a4a8a;
    padding: 0.1rem 0.5rem;
    border-radius: 4px;
    font-size: 0.78rem;
    font-weight: 600;
  }

  /* Pills */
  .pill {
    display: inline-block;
    padding: 0.15rem 0.6rem;
    border-radius: 999px;
    font-size: 0.75rem;
    font-weight: 600;
  }
  .pill.profit  { background: #d4f0dc; color: #1a6a30; }
  .pill.loss    { background: #fde0e0; color: #991111; }
  .pill.neutral { background: #e8e8e8; color: #777; }

  /* Source link */
  .src-link {
    color: #1a4a8a;
    font-weight: 600;
    font-size: 0.82rem;
    text-decoration: none;
  }
  .src-link:hover { text-decoration: underline; }
</style>
