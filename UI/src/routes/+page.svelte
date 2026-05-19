<script lang="ts">
  let { data } = $props();
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

    <div class="dashboard-grid">
      <article class="metric-card">
        <span class="metric-label">Reference Data</span>
        <strong>Countries</strong>
        <a href="/Data/Reference/Countries">Open country data</a>
      </article>

      <article class="metric-card">
        <span class="metric-label">Trace Mode</span>
        <strong>Available</strong>
        <span>Use the footer to view data as of a specific audit date.</span>
      </article>

      <article class="metric-card">
        <span class="metric-label">Write-Through Cache</span>
        <strong>{data.memoryDiagnostics?.eventCache.eventCount ?? '-'}</strong>
        <span>
          {data.memoryDiagnostics?.eventCache.streamCount ?? '-'} streams
          {data.memoryDiagnostics?.eventCache.isLoaded ? 'loaded' : 'not loaded'}
        </span>
      </article>

      <article class="metric-card">
        <span class="metric-label">API Diagnostics</span>
        <strong>HTTP</strong>
        <a href="/Diagnostics/HttpExchanges">Search request/response captures</a>
      </article>

      <article class="metric-card">
        <span class="metric-label">Country Service</span>
        <strong>{data.memoryDiagnostics?.countryService.countryCount ?? '-'}</strong>
        <span>{data.memoryDiagnostics?.countryService.cacheEntryCount ?? '-'} cached views</span>
      </article>

      <article class="metric-card">
        <span class="metric-label">Workspace</span>
        <strong>Ready</strong>
        <span>Blotter, dashboard, value data, and help areas are staged in the menu.</span>
      </article>
    </div>
  </section>
</main>
