<script lang="ts">
  import { onMount } from 'svelte';
  import { page } from '$app/stores';
  import { goto } from '$app/navigation';
  import { getReportsByCik, fetch10Q, type Report } from '$lib/services/reports';
  import { getCompany, type Company } from '$lib/services/companies';
  import { lastUpdatedReportId } from '$lib/stores/liveUpdates';

  const cik = decodeURIComponent($page.params.cik);

  let company   = $state<Company | null>(null);
  let reports   = $state<Report[]>([]);
  let status    = $state<'loading' | 'ok' | 'error'>('loading');
  let errorMsg  = $state('');

  let fetchStatus   = $state<'idle' | 'loading' | 'ok' | 'error'>('idle');
  let fetchErrorMsg = $state('');
  let maxReports    = $state(5);

  onMount(loadData);

  $effect(() => {
    document.title = company ? `Verity - ${company.name}` : 'Verity';
  });

  $effect(() => {
    const updatedId = $lastUpdatedReportId;
    if (updatedId && reports.some(r => r.id === updatedId)) {
      getReportsByCik(cik).then(r => { reports = r; });
    }
  });

  async function loadData() {
    status = 'loading';
    try {
      const [fetchedCompany, fetchedReports] = await Promise.all([
        getCompany(cik),
        getReportsByCik(cik),
      ]);
      company = fetchedCompany;
      reports = fetchedReports;
      status  = 'ok';
    } catch (e: unknown) {
      errorMsg = e instanceof Error ? e.message : 'Unknown error';
      status   = 'error';
    }
  }

  async function handleFetch10Q() {
    if (!company) return;
    fetchStatus   = 'loading';
    fetchErrorMsg = '';
    try {
      await fetch10Q(company.cik, maxReports);
      fetchStatus = 'ok';
      await loadData();
      setTimeout(() => { fetchStatus = 'idle'; }, 2000);
    } catch (e: unknown) {
      fetchErrorMsg = e instanceof Error ? e.message : 'Unknown error';
      fetchStatus   = 'error';
    }
  }

  function formatNumber(n: number | null | undefined): string {
    if (n == null) return '—';
    const abs = Math.abs(n);
    const formatted = abs >= 1_000_000
      ? `$${(abs / 1_000_000).toFixed(2)}M`
      : `$${abs.toLocaleString('en-US')}`;
    return n < 0 ? `−${formatted}` : formatted;
  }

  // ── Chart ────────────────────────────────────────────────────
  const ML = 56; const MT = 6; const MR = 8; const MB = 36;
  const cW = 500;
  const cH = 300;
  const SVG_W = cW + ML + MR;
  const SVG_H = cH + MT + MB;

  // For 10-K annual reports subtract the 10-Q quarterly values so the bar
  // represents Q4 standalone. Fiscal year ≠ calendar year, so we match
  // quarters by date range: between the previous 10-K and this 10-K.
  const adjustedReports = $derived.by(() => {
    const annuals = reports
      .filter(r => r.formType === '10-K')
      .sort((a, b) => a.reportDate.localeCompare(b.reportDate));

    return reports.map(r => {
      if (r.formType !== '10-K') return r;

      const idx      = annuals.findIndex(a => a.id === r.id);
      const prevAnnual = annuals[idx - 1];

      // 10-Q reports that belong to this fiscal year:
      // after the previous 10-K (exclusive) up to this 10-K (inclusive)
      const quarters = reports.filter(q =>
        q.formType === '10-Q' &&
        q.reportDate <= r.reportDate &&
        (prevAnnual == null || q.reportDate > prevAnnual.reportDate)
      );

      const sumRevenues = quarters.reduce((s, q) => s + (q.revenues ?? 0), 0);
      const sumExpenses = quarters.reduce((s, q) => s + (q.expenses ?? 0), 0);

      return {
        ...r,
        revenues: r.revenues != null ? r.revenues - sumRevenues : r.revenues,
        expenses: r.expenses != null ? r.expenses - sumExpenses : r.expenses,
      };
    });
  });

  const chartReports = $derived(
    adjustedReports
      .filter(r => r.revenues != null || r.expenses != null)
      .sort((a, b) => a.reportDate.localeCompare(b.reportDate))
  );

  const chartMaxVal = $derived(
    Math.max(1, ...chartReports.flatMap(r => [r.revenues ?? 0, r.expenses ?? 0]))
  );

  function niceStep(maxVal: number): number {
    if (maxVal <= 0) return 1;
    const raw = maxVal / 5;
    const mag = Math.pow(10, Math.floor(Math.log10(raw)));
    const norm = raw / mag;
    const nice = norm <= 1 ? 1 : norm <= 2 ? 2 : norm <= 5 ? 5 : 10;
    return nice * mag;
  }

  const yTicks = $derived.by(() => {
    const step  = niceStep(chartMaxVal);
    const count = Math.ceil(chartMaxVal / step);
    return Array.from({ length: count + 1 }, (_, i) => i * step);
  });

  const chartNiceMax = $derived(yTicks.length > 1 ? yTicks[yTicks.length - 1] : chartMaxVal);

  // X position for report i out of n (evenly spaced, pinned to edges)
  function xPos(i: number, n: number): number {
    return n <= 1 ? cW / 2 : (i / (n - 1)) * cW;
  }

  function yPos(val: number): number {
    return cH - (val / chartNiceMax) * cH;
  }

  function linePoints(key: 'revenues' | 'expenses'): string {
    return chartReports
      .map((r, i) => {
        const v = r[key];
        return v != null ? `${xPos(i, chartReports.length)},${yPos(v)}` : null;
      })
      .filter(Boolean)
      .join(' ');
  }

  function xLabel(r: Report): string {
    const yearSuffix = r.year ? `'${r.year.toString().slice(2)}` : '';
    return yearSuffix ? `Q${r.quarter} ${yearSuffix}` : `Q${r.quarter}`;
  }

  function abbrevMultiplier(abbrev: string | null | undefined): number {
    switch (abbrev?.toLowerCase().trim()) {
      case 'bil': case 'billions': case 'b': return 1_000_000_000;
      case 'mil': case 'millions': case 'm': return 1_000_000;
      case 'k':   case 'thousands':          return 1_000;
      default: return 1;
    }
  }

  const chartScale = $derived(
    abbrevMultiplier(chartReports.find(r => r.abbreviation)?.abbreviation)
  );

  function fmtAxis(n: number): string {
    const actual = n * chartScale;
    if (actual >= 1_000_000_000) return `$${(actual / 1_000_000_000).toFixed(1)}B`;
    if (actual >= 1_000_000)     return `$${(actual / 1_000_000).toFixed(0)}M`;
    if (actual >= 1_000)         return `$${(actual / 1_000).toFixed(0)}K`;
    return `$${actual}`;
  }
</script>

<main>
  <header>
    {#if company}
      <h1><a href="/" class="verity-brand">Verity:</a> {company.name}</h1>
    {:else}
      <h1><a href="/" class="verity-brand">Verity:</a> Company</h1>
    {/if}
    <a href="/" class="back-btn">← Back</a>
    <span class="badge">{reports.length} report{reports.length !== 1 ? 's' : ''}</span>
  </header>

  {#if status === 'loading'}
    <div class="state-msg">
      <div class="spinner"></div>
      <p>Loading…</p>
    </div>

  {:else if status === 'error'}
    <div class="state-msg error">
      <p>✗ Error: {errorMsg}</p>
    </div>

  {:else if !company}
    <div class="state-msg">
      <p>Company not found. <a href="/">Go back</a> and add it first.</p>
    </div>

  {:else}
    <!-- Company info -->
    <section class="info-section">
      <div class="info-grid">
        <div class="info-item">
          <span class="info-label">CIK</span>
          <span class="info-value mono">{company.cik}</span>
        </div>
        {#if company.sic}
          <div class="info-item">
            <span class="info-label">SIC</span>
            <span class="info-value mono">{company.sic}</span>
          </div>
        {/if}
        {#if company.sicDescription}
          <div class="info-item">
            <span class="info-label">Industry</span>
            <span class="info-value">{company.sicDescription}</span>
          </div>
        {/if}
        {#if company.fiscalYearEnd}
          <div class="info-item">
            <span class="info-label">Fiscal Year End</span>
            <span class="info-value">{company.fiscalYearEnd}</span>
          </div>
        {/if}
      </div>

      <!-- Fetch 10-Q/K -->
      <div class="fetch-bar">
        <button
          class="fetch-btn"
          onclick={handleFetch10Q}
          disabled={fetchStatus === 'loading'}>
          {fetchStatus === 'loading' ? 'Fetching…' : 'Fetch'}
        </button>
        <label for="max-input">last</label>
        <input
          id="max-input"
          type="number"
          bind:value={maxReports}
          min="1"
          max="50"
          class="input-max"
          disabled={fetchStatus === 'loading'}
        />
        <span class="fetch-label">10-Q/K reports</span>
      </div>

      {#if fetchStatus === 'ok'}
        <p class="feedback ok">✓ Reports fetched and saved successfully.</p>
      {:else if fetchStatus === 'error'}
        <p class="feedback error">✗ {fetchErrorMsg || 'Fetch failed.'}</p>
      {/if}
    </section>

	<div class="tab-chart-wrap">
		<!-- Reports table -->
        {#if reports.length === 0}
          <div class="state-msg">
            <p>No reports yet. Use the button above to fetch quarterly reports.</p>
          </div>
        {:else}
          <div class="table-wrap">
            <table>
              <thead>
                <tr>
                  <th id="Type" style="border-left:none">Type</th>
                  <th id="Quarter">Quarter</th>
                  <th id="ReportDate">Report Date</th>
                  <th id="Revenues">Revenues</th>
                  <th id="Expenses">Expenses</th>
                  <th id="ProfitLoss">Income</th>
                  <th id="Status">Status</th>
                  <th id="Source">Source</th>
                </tr>
              </thead>
              <tbody>
                {#each reports as r}
                  <tr class="clickable-row" onclick={() => goto(`/companies/${encodeURIComponent(cik)}/reports/${encodeURIComponent(r.accessionNumber)}`)}>
                    <td><span class="tag" headers="Type">{r.formType}</span></td>
                    <td class="center" headers="Quarter">{r.year} - {r.quarter != null ? `Q${r.quarter}` : '—'}</td>
                    <td class="center" headers="ReportDate">{r.reportDate}</td>
                    <td class="number revenue" headers="Revenues">{formatNumber(r.revenues)} {r.abbreviation}</td>
                    <td class="number expenses" headers="Expenses">{formatNumber(r.expenses)} {r.abbreviation}</td>
                    <td class="number" class:profit={r.profitable === true} class:loss={r.profitable === false} headers="ProfitLoss">
                      {formatNumber(r.profitLoss)} {r.abbreviation}
                    </td>
                    <td class="center" headers="Status">
                      {#if r.profitable === true}
                        <span class="pill profit">Profit</span>
                      {:else if r.profitable === false}
                        <span class="pill loss">Loss</span>
                      {:else}
                        <span class="pill neutral">—</span>
                      {/if}
                    </td>
                    <td onclick={(e) => e.stopPropagation()}>
                      {#if r.sourceUrl}
                        <a href={r.sourceUrl} target="_blank" rel="noopener" class="src-link">SEC ↗</a>
                      {:else}—{/if}
                    </td>
                  </tr>
                {/each}
              </tbody>
            </table>
          </div>

        {/if}
		<!-- Revenue vs Expenses chart -->
        {#if chartReports.length > 0}
        <div class="chart-section">
          <div class="chart-legend">
            <span class="legend-tag revenue-tag">Revenue</span>
            <span class="legend-tag expenses-tag">Expenses</span>
          </div>
          <div class="chart-wrap">
            <svg viewBox="0 0 {SVG_W} {SVG_H}" width="100%" height="100%" preserveAspectRatio="none" role="img" aria-label="Revenue vs Expenses chart">
              <g transform="translate({ML}, {MT})">
                <!-- Gridlines + Y-axis labels -->
                {#each yTicks as tick}
                  {@const y = yPos(tick)}
                  <line
                    x1={0} y1={y} x2={cW} y2={y}
                    stroke={tick === 0 ? '#aab' : '#e8eef5'}
                    stroke-width={tick === 0 ? 1.5 : 1}
                    stroke-dasharray={tick === 0 ? '' : '3,3'}
                  />
                  <text x={-8} y={y + 4} text-anchor="end" font-size="11" fill="#7a96b2">{fmtAxis(tick)}</text>
                {/each}

                <!-- Vertical quarter lines + X labels -->
                {#each chartReports as r, i}
                  {@const x = xPos(i, chartReports.length)}
                  <line
                    x1={x} y1={0} x2={x} y2={cH}
                    stroke="#243550" stroke-width="1" stroke-dasharray="3,3"
                  />
                  <text
                    x={x} y={cH + 16}
                    text-anchor="middle" font-size="10" fill="#7a96b2"
                    transform={chartReports.length > 7 ? `rotate(-40 ${x} ${cH + 16})` : ''}
                  >{xLabel(r)}</text>
                {/each}

                <!-- Revenue line -->
                <polyline
                  points={linePoints('revenues')}
                  fill="none" stroke="#2ecc71" stroke-width="2" stroke-linejoin="round" stroke-linecap="round"
                />
                <!-- Expenses line -->
                <polyline
                  points={linePoints('expenses')}
                  fill="none" stroke="#e74c3c" stroke-width="2" stroke-linejoin="round" stroke-linecap="round"
                />

                <!-- Y axis line -->
                <line x1={0} y1={0} x2={0} y2={cH} stroke="#4a6070" stroke-width="1.5" />
                <!-- X axis line -->
                <line x1={0} y1={cH} x2={cW} y2={cH} stroke="#4a6070" stroke-width="1.5" />
              </g>
            </svg>
          </div>
        </div>
        {/if}
	</div>
  {/if}
</main>

<style>
	main {
	width: 100%;
	flex: 1;
	display: flex;
	flex-direction: column;
	min-height: 0;
	overflow: hidden;
	background: #192d47;
	color: #d8e4f0;
	}

	/* Header */
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

	.badge {
	margin-left: auto;
	font-size: 0.8rem;
	background: rgba(255,255,255,.12);
	padding: 0.2rem 0.6rem;
	border-radius: 999px;
	white-space: nowrap;
	}

	/* Info section */
	.info-section {
	background: #121d30;
	margin: 1.5rem 2rem 0;
	border-radius: 12px;
	box-shadow: 0 2px 10px rgba(0,0,0,.5);
	padding: 1.5rem;
	display: flex;
	flex-direction: column;
	gap: 1.25rem;
	}

	.info-grid {
	display: flex;
	flex-wrap: wrap;
	gap: 1.5rem;
	}

	.info-item {
	display: flex;
	flex-direction: column;
	gap: 0.2rem;
	}

	.info-label {
	font-size: 0.7rem;
	font-weight: 700;
	text-transform: uppercase;
	letter-spacing: 0.06em;
	color: #5e7a96;
	}

	.info-value {
	font-size: 0.95rem;
	color: #d8e4f0;
	font-weight: 500;
	}

	.info-value.mono { }

	/* Fetch bar */
	.fetch-bar {
	display: flex;
	align-items: center;
	gap: 0.6rem;
	padding-top: 0.25rem;
	border-top: 1.5px solid #243550;
	font-size: 0.88rem;
	color: #8aa4be;
	}

	.fetch-label {
	color: #8aa4be;
	}

	.input-max {
	width: 64px;
	background: #121d30;
	padding: 0.4rem 0.5rem;
	font-size: 0.9rem;
	border: 1.5px solid #243550;
	border-radius: 6px;
	text-align: center;
	outline: none;
	color: #9ebddb;
	}
	.input-max:focus { border-color: #5b9bd5; }
	.input-max:disabled { opacity: 0.5; background: #111e30; }

	.fetch-btn {
	padding: 0.4rem 1rem;
	background: #0b2e5c;
	color: #fff;
	border: none;
	border-radius: 7px;
	font-size: 0.88rem;
	font-weight: 600;
	cursor: pointer;
	transition: background 0.15s;
	}
	.fetch-btn:hover:not(:disabled) { background: #153c70; }
	.fetch-btn:disabled { opacity: 0.4; cursor: default; }

	.feedback {
	margin: 0;
	padding: 0.55rem 0.9rem;
	border-radius: 7px;
	font-size: 0.875rem;
	}
	.feedback.ok    { background: #0d2a1a; color: #27ae60; }
	.feedback.error { background: #2a0d0d; color: #e74c3c; }

	/* Tab + Chart wrapper */
	.tab-chart-wrap {
	margin: 1.5rem 2rem;
	flex: 1;
	min-height: 0;
	display: flex;
	gap: 1.25rem;
	overflow: hidden;
	}


	/* Chart section */
	.chart-section {
	float: right;
	flex: 40;
	min-height: 0;
	background: #121d30;
	border-radius: 12px;
	box-shadow: 0 2px 10px rgba(0,0,0,.5);
	padding: 1.25rem 1.5rem 1.5rem;
	display: flex;
	flex-direction: column;
	}

	.chart-legend {
	display: flex;
	justify-content: center;
	align-items: center;
	gap: 0.75rem;
	margin-bottom: 1rem;
	}

	.legend-tag {
	display: inline-block;
	padding: 0.25rem 0.75rem;
	border-radius: 4px;
	font-size: 0.9rem;
	font-weight: 600;
	}

	.revenue-tag  { background: #0d2a1a; color: #2ecc71; }
	.expenses-tag { background: #2a0d0d; color: #e74c3c; }

	.chart-wrap {
	flex: 1;
	min-height: 0;
	}

	.chart-wrap svg {
	display: block;
	width: 100%;
	height: 100%;
	overflow: visible;
	}

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

	/* Table */
	.table-wrap {
	flex: 55;
	min-height: 0;
	overflow: visible;
	}

	table {
	width: 100%;
	float: left;
	border-collapse: collapse;
	background: #121d30;
	border-radius: 10px;
	overflow: hidden;
	box-shadow: 0 2px 12px rgba(0,0,0,.5);
	font-size: 0.825rem;
	table-layout: fixed;
	}

	thead {
	display: table-header-group;
	background: #0b2e5c;
	color: #fff;
	}

	thead th {
	border-left: 1px solid #537aad;
	padding: 0.6rem 0.8rem;
	text-align: center;
	font-weight: 600;
	white-space: nowrap;
	overflow: hidden;
	text-overflow: ellipsis;
	max-width: 0;
	letter-spacing: 0.02em;
	text-transform: uppercase;
	}

	tbody tr {
	border-bottom: 1px solid #243550;
	transition: background 0.12s;
	}
	tbody tr:last-child { border-bottom: none; }
	tbody tr:hover { background: #16243a; }
	tbody tr.clickable-row { cursor: pointer; }
	tbody tr.clickable-row:hover { background: #1c2e4a; }

	td {
	border-left: 1px solid #243550;
	padding: 0.6rem 0.8rem;
	vertical-align: middle;
	text-align: center;
	white-space: nowrap;
	overflow: hidden;
	text-overflow: ellipsis;
	max-width: 0;
	}

	.number  { text-align: right; font-weight: 600; }
	.number.profit   { color: #2ecc71; }
	.number.loss     { color: #e74c3c; }
	.number.revenue  { color: #2ecc71; }
	.number.expenses { color: #e74c3c; }

	.summary {
	font-size: 0.8rem;
	color: #7a96b2;
	max-width: 260px;
	line-height: 1.4;
	}

	.tag {
	display: inline-block;
	background: #1a2d4a;
	color: #5b9bd5;
	padding: 0.1rem 0.5rem;
	border-radius: 4px;
	font-weight: 600;
	}

	.pill {
	display: inline-block;
	padding: 0.15rem 0.6rem;
	border-radius: 999px;
	font-weight: 600;
	}
	.pill.profit  { background: #0d2a1a; color: #27ae60; }
	.pill.loss    { background: #2a0d0d; color: #e74c3c; }
	.pill.neutral { background: #1e2b3c; color: #6a84a0; }

	.src-link {
	color: #5b9bd5;
	font-weight: 600;
	text-decoration: none;
	}
	.src-link:hover { text-decoration: underline; }

	@media (max-width: 1400px) {
		.tab-chart-wrap {
			flex-direction: column;
			overflow: auto;
		}
		.chart-section {
			flex: none;
			min-height: 260px;
		}
		.table-wrap {
			flex: none;
			overflow: visible;
		}
	}

</style>
