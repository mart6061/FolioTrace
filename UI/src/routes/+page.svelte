<script lang="ts">
  let { data, form } = $props();

  function formatCount(value: number | null | undefined) {
    return typeof value === 'number' ? value.toLocaleString() : '-';
  }
</script>

<main class="min-h-screen">
  <section class="page-header">
    <div class="page-container">
      <p class="page-kicker">FolioTrace</p>
      <h1 class="page-title">Dashboard</h1>
    </div>
  </section>

  <section class="page-container page-section">
    {#if data.error}
      <div class="mb-4 rounded-md border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-800">
        {data.error}
      </div>
    {/if}

    <div class="dashboard-sections">
      <section class="dashboard-section" aria-labelledby="value-data-heading">
        <div class="dashboard-section-header">
          <h2 id="value-data-heading">Value Data</h2>
        </div>

        <div class="dashboard-grid">
          <article class="metric-card">
            <span class="metric-label">FX</span>
            <strong>FXs</strong>
            <a href="/Value/FXs">Open FX data</a>
          </article>

          <article class="metric-card">
            <span class="metric-label">FX Rate</span>
            <strong>FX Rates</strong>
            <a href="/Value/FXRates">Open FX rate data</a>
          </article>
        </div>
      </section>

      <section class="dashboard-section" aria-labelledby="reference-data-heading">
        <div class="dashboard-section-header">
          <h2 id="reference-data-heading">Reference Data</h2>
        </div>

        <div class="dashboard-grid">
          <article class="metric-card">
            <span class="metric-label">Countries</span>
            <strong>Countries</strong>
            <a href="/Data/Reference/Countries">Open country data</a>
          </article>

          <article class="metric-card">
            <span class="metric-label">Currencies</span>
            <strong>Currencies</strong>
            <a href="/Data/Reference/Currencies">Open currency data</a>
          </article>
        </div>
      </section>

      <section class="dashboard-section" aria-labelledby="system-heading">
        <div class="dashboard-section-header">
          <h2 id="system-heading">System</h2>
        </div>

        {#if form?.intent === 'build'}
          <div class={`dashboard-alert ${form.status === 'success' ? 'dashboard-alert-success' : 'dashboard-alert-danger'}`}>
            {form.message}
          </div>
        {/if}

        <div class="dashboard-grid">
          <article class="metric-card">
            <span class="metric-label">Diagnostics</span>
            <strong>Request Trace</strong>
            <a href="/Diagnostics/RequestTrace">Search request and response captures</a>
          </article>

          <article class="metric-card metric-card-danger">
            <span class="metric-label">Danger Zone</span>
            <strong>Build()</strong>
            <span>This will clear the database of all manually created events and state. The database will be seeded with sample data.</span>
            <form
              action="?/build"
              method="POST"
              onsubmit={(event) => {
                if (!confirm('Build() will clear and rebuild the event database. Continue?'))
                  event.preventDefault();
              }}
            >
              <button type="submit">Rebuild database</button>
            </form>
          </article>
        </div>
      </section>

      <section class="dashboard-section" aria-labelledby="stats-heading">
        <div class="dashboard-section-header">
          <h2 id="stats-heading">Stats for nerds!</h2>
        </div>

        <div class="dashboard-grid">
          <article class="metric-card">
            <span class="metric-label">Write-Through Cache</span>
            <strong>{formatCount(data.memoryDiagnostics?.eventCache.eventCount)}</strong>
            <span>
              {formatCount(data.memoryDiagnostics?.eventCache.streamCount)} streams
              {data.memoryDiagnostics?.eventCache.isLoaded ? 'loaded' : 'not loaded'}
            </span>
          </article>

          <article class="metric-card">
            <span class="metric-label">Country Service</span>
            <strong>{formatCount(data.memoryDiagnostics?.countryService.countryCount)}</strong>
            <span>{formatCount(data.memoryDiagnostics?.countryService.cacheEntryCount)} cached views</span>
          </article>

          <article class="metric-card">
            <span class="metric-label">Currency Service</span>
            <strong>{formatCount(data.memoryDiagnostics?.currencyService.currencyCount)}</strong>
            <span>{formatCount(data.memoryDiagnostics?.currencyService.cacheEntryCount)} cached views</span>
          </article>

          <article class="metric-card">
            <span class="metric-label">FX Service</span>
            <strong>{formatCount(data.memoryDiagnostics?.fxService?.fxCount)}</strong>
            <span>{formatCount(data.memoryDiagnostics?.fxService?.cacheEntryCount)} cached views</span>
          </article>

          <article class="metric-card">
            <span class="metric-label">FX Rate Service</span>
            <strong>{formatCount(data.memoryDiagnostics?.fxRateService?.fxRateCount)}</strong>
            <span>{formatCount(data.memoryDiagnostics?.fxRateService?.cacheEntryCount)} cached views</span>
          </article>
        </div>
      </section>
    </div>
  </section>
</main>
