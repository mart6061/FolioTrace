<script lang="ts">
  import { enhance } from '$app/forms';
  import { invalidateAll } from '$app/navigation';
  import BookmarkButton from '$lib/components/BookmarkButton.svelte';
  import { formatTableDateTime } from '$lib/dates';
  import type { AggregateMaintenanceNotification, BuildProgressNotification } from '$lib/types';
  import { onMount } from 'svelte';
  import type { SubmitFunction } from './$types';

  let { data, form } = $props();
  let buildRunning = $state(false);
  let buildProgress = $state<BuildProgressNotification | null>(null);
  let buildConfirmationInput = $state('');
  let clearConfirmationInput = $state('');

  function formatCount(value: number | null | undefined) {
    return typeof value === 'number' ? value.toLocaleString() : '-';
  }

  function formatMaybeDateTime(value: string | null | undefined) {
    return value ? formatTableDateTime(value) : '-';
  }

  function formatBytes(value: number | null | undefined) {
    if (typeof value !== 'number')
      return '-';

    if (value < 1024)
      return `${value.toLocaleString()} B`;

    const units = ['KB', 'MB', 'GB'];
    let size = value / 1024;
    let unitIndex = 0;

    while (size >= 1024 && unitIndex < units.length - 1) {
      size /= 1024;
      unitIndex++;
    }

    return `${size.toLocaleString(undefined, { maximumFractionDigits: size >= 10 ? 1 : 2 })} ${units[unitIndex]}`;
  }

  const buildPercent = $derived(calculateBuildPercent(buildProgress));
  const canConfirmBuild = $derived(buildConfirmationInput === 'Build');
  const canConfirmClear = $derived(clearConfirmationInput === 'Clear');

  onMount(() => {
    const source = new EventSource('/API/Notifications/Aggregates');
    const onBuildProgress = (event: MessageEvent<string>) => {
      try {
        buildProgress = JSON.parse(event.data) as BuildProgressNotification;
        buildRunning = buildProgress.status === 'Running';
      } catch {
        return;
      }
    };
    const onAggregateMaintenance = (event: MessageEvent<string>) => {
      try {
        const notification = JSON.parse(event.data) as AggregateMaintenanceNotification;
        if (notification.changed)
          void invalidateAll();
      } catch {
        return;
      }
    };

    source.addEventListener('build-progress', onBuildProgress);
    source.addEventListener('aggregate-maintenance', onAggregateMaintenance);

    return () => {
      source.removeEventListener('build-progress', onBuildProgress);
      source.removeEventListener('aggregate-maintenance', onAggregateMaintenance);
      source.close();
    };
  });

  const enhanceBuild: SubmitFunction = ({ cancel }) => {
    if (!canConfirmBuild) {
      cancel();
      return;
    }

    buildRunning = true;
    buildConfirmationInput = '';
    buildProgress = {
      notificationType: 'BuildProgress',
      buildID: '',
      status: 'Running',
      stage: 'Starting',
      message: 'Starting database rebuild.',
      completedSteps: 0,
      totalSteps: 12,
      completedEvents: 0,
      totalEvents: 0,
      startedAtUtc: new Date().toISOString(),
      updatedAtUtc: new Date().toISOString(),
      error: null
    };

    return async ({ update }) => {
      await update({ reset: false });
      buildRunning = false;
    };
  };

  const enhanceClearCache: SubmitFunction = ({ cancel }) => {
    if (!canConfirmClear) {
      cancel();
      return;
    }

    clearConfirmationInput = '';

    return async ({ update }) => {
      await update({ reset: false });
    };
  };

  function calculateBuildPercent(progress: BuildProgressNotification | null) {
    if (!progress)
      return 0;

    if (progress.totalEvents > 0)
      return Math.min(100, Math.round((progress.completedEvents / progress.totalEvents) * 100));

    if (progress.totalSteps > 0)
      return Math.min(100, Math.round((progress.completedSteps / progress.totalSteps) * 100));

    return 0;
  }
</script>

<main class="min-h-screen">
  <section class="page-header">
    <div class="page-container">
      <p class="page-kicker">System</p>
      <div class="page-title-row">
        <h1 class="page-title">Stats for Nerds</h1>
        <BookmarkButton />
      </div>
    </div>
  </section>

  <section class="page-container page-section">
    {#if data.error}
      <div class="mb-4 rounded-md border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-800">
        {data.error}
      </div>
    {/if}

    {#if data.memoryDiagnostics?.eventCache.unprocessedEventCount}
      <div class="mb-4 rounded-md border border-red-300 bg-red-50 px-4 py-3 text-sm text-red-900">
        <strong>{formatCount(data.memoryDiagnostics.eventCache.unprocessedEventCount)} unprocessed events</strong>
        <span class="ml-2">Some stored events could not be loaded and were skipped. Review the diagnostics below.</span>
      </div>
    {/if}

    <div class="dashboard-sections">
      <section class="dashboard-section" aria-labelledby="reference-data-heading">
        <div class="dashboard-section-header">
          <h2 id="reference-data-heading">Reference Data</h2>
        </div>

        <div class="dashboard-grid">
          <article class="metric-card">
            <span class="metric-label">Accounts</span>
            <strong>Accounts</strong>
            <a href="/Data/Reference/Accounts">Open account data</a>
          </article>

          <article class="metric-card">
            <span class="metric-label">Brokers</span>
            <strong>Brokers</strong>
            <a href="/Data/Reference/Brokers">Open broker data</a>
          </article>

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

          <article class="metric-card">
            <span class="metric-label">FX</span>
            <strong>FXs</strong>
            <a href="/Value/FXs">Open FX data</a>
          </article>

          <article class="metric-card">
            <span class="metric-label">Holdings</span>
            <strong>Holdings</strong>
            <a href="/Data/Reference/Holdings">Open holding data</a>
          </article>

          <article class="metric-card">
            <span class="metric-label">Instruments</span>
            <strong>Instruments</strong>
            <a href="/Data/Reference/Instruments">Open instrument data</a>
          </article>
        </div>
      </section>

      <section class="dashboard-section" aria-labelledby="value-data-heading">
        <div class="dashboard-section-header">
          <h2 id="value-data-heading">Value Data</h2>
        </div>

        <div class="dashboard-grid">
          <article class="metric-card">
            <span class="metric-label">FX Rate</span>
            <strong>FX Rates</strong>
            <a href="/Value/FXRates">Open FX rate data</a>
          </article>

          <article class="metric-card">
            <span class="metric-label">Instrument Value</span>
            <strong>Instrument Values</strong>
            <a href="/Value/InstrumentValues">Open instrument value data</a>
          </article>
        </div>
      </section>

      <section class="dashboard-section" aria-labelledby="system-heading">
        <div class="dashboard-section-header">
          <h2 id="system-heading">System</h2>
        </div>

        {#if form?.intent === 'build' || form?.intent === 'clearCacheAndProjections'}
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

          <article class="metric-card">
            <span class="metric-label">Diagnostics</span>
            <strong>FIX Trace</strong>
            <a href="/Diagnostics/FIXTrace">Search sent and received FIX operations</a>
          </article>

          <article class="metric-card metric-card-danger">
            <span class="metric-label">Danger Zone</span>
            <strong>Build</strong>
            <span>This will clear the database of all manually created events and state. The database will be seeded with sample data.</span>
            <form
              class="danger-confirmation"
              action="?/build"
              method="POST"
              use:enhance={enhanceBuild}
            >
              <label>
                <span>Type Build to confirm</span>
                <input
                  autocomplete="off"
                  bind:value={buildConfirmationInput}
                  disabled={buildRunning}
                  spellcheck="false"
                />
              </label>
              <button disabled={!canConfirmBuild || buildRunning} type="submit">
                {buildRunning ? 'Build running' : 'Rebuild database'}
              </button>
            </form>

            {#if buildProgress}
              <div class="mt-3 grid gap-2 rounded-md border border-red-200 bg-white/70 p-3 text-sm text-slate-800">
                <div class="flex items-center justify-between gap-3">
                  <span class="font-semibold">{buildProgress.stage}</span>
                  <span>{buildPercent}%</span>
                </div>
                <div class="h-2 overflow-hidden rounded-full bg-red-100">
                  <div class="h-full bg-red-700 transition-all" style={`width: ${buildPercent}%`}></div>
                </div>
                <span>{buildProgress.error ?? buildProgress.message}</span>
                <span>
                  Step {formatCount(buildProgress.completedSteps)} of {formatCount(buildProgress.totalSteps)}
                  · Events {formatCount(buildProgress.completedEvents)} of {formatCount(buildProgress.totalEvents)}
                </span>
                <span>Status {buildProgress.status} · Updated {formatMaybeDateTime(buildProgress.updatedAtUtc)}</span>
              </div>
            {/if}
          </article>

          <article class="metric-card metric-card-danger">
            <span class="metric-label">Danger Zone</span>
            <strong>Clear caches and projections</strong>
            <span>This will clear all in-memory aggregate caches and stored projection data. Events will not be deleted.</span>
            <form
              class="danger-confirmation"
              action="?/clearCacheAndProjections"
              method="POST"
              use:enhance={enhanceClearCache}
            >
              <label>
                <span>Type Clear to confirm</span>
                <input
                  autocomplete="off"
                  bind:value={clearConfirmationInput}
                  spellcheck="false"
                />
              </label>
              <button disabled={!canConfirmClear} type="submit">Clear caches</button>
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
            <span>{formatBytes(data.memoryDiagnostics?.eventCache.estimatedMemoryBytes)} estimated memory</span>
          </article>

          <article class={`metric-card ${data.memoryDiagnostics?.eventCache.unprocessedEventCount ? 'metric-card-danger' : ''}`}>
            <span class="metric-label">Unprocessed Events</span>
            <strong>{formatCount(data.memoryDiagnostics?.eventCache.unprocessedEventCount)}</strong>
            {#if data.memoryDiagnostics?.eventCache.recentUnprocessedEvents?.length}
              {#each data.memoryDiagnostics.eventCache.recentUnprocessedEvents.slice(0, 3) as event (event.eventId ?? `${event.eventType}-${event.recordedAtUtc}`)}
                <span>{event.eventType}: {event.reason}</span>
              {/each}
            {:else}
              <span>No event load failures recorded</span>
            {/if}
          </article>

          <article class="metric-card">
            <span class="metric-label">Country Service</span>
            <strong>{formatCount(data.memoryDiagnostics?.countryService.countryCount)}</strong>
            <span>{formatCount(data.memoryDiagnostics?.countryService.cacheEntryCount)} cached views</span>
            <span>{formatBytes(data.memoryDiagnostics?.countryService.estimatedMemoryBytes)} estimated memory</span>
          </article>

          <article class="metric-card">
            <span class="metric-label">Broker Service</span>
            <strong>{formatCount(data.memoryDiagnostics?.brokerService?.brokerCount)}</strong>
            <span>{formatCount(data.memoryDiagnostics?.brokerService?.cacheEntryCount)} cached views</span>
            <span>{formatBytes(data.memoryDiagnostics?.brokerService?.estimatedMemoryBytes)} estimated memory</span>
          </article>

          <article class="metric-card">
            <span class="metric-label">Currency Service</span>
            <strong>{formatCount(data.memoryDiagnostics?.currencyService.currencyCount)}</strong>
            <span>{formatCount(data.memoryDiagnostics?.currencyService.cacheEntryCount)} cached views</span>
            <span>{formatBytes(data.memoryDiagnostics?.currencyService.estimatedMemoryBytes)} estimated memory</span>
          </article>

          <article class="metric-card">
            <span class="metric-label">FX Service</span>
            <strong>{formatCount(data.memoryDiagnostics?.fxService?.fxCount)}</strong>
            <span>{formatCount(data.memoryDiagnostics?.fxService?.cacheEntryCount)} cached views</span>
            <span>{formatBytes(data.memoryDiagnostics?.fxService?.estimatedMemoryBytes)} estimated memory</span>
          </article>

          <article class="metric-card">
            <span class="metric-label">FX Rate Service</span>
            <strong>{formatCount(data.memoryDiagnostics?.fxRateService?.fxRateCount)}</strong>
            <span>{formatCount(data.memoryDiagnostics?.fxRateService?.cacheEntryCount)} cached views</span>
            <span>{formatBytes(data.memoryDiagnostics?.fxRateService?.estimatedMemoryBytes)} estimated memory</span>
          </article>

          <article class="metric-card">
            <span class="metric-label">Instrument Service</span>
            <strong>{formatCount(data.memoryDiagnostics?.instrumentService?.instrumentCount)}</strong>
            <span>{formatCount(data.memoryDiagnostics?.instrumentService?.cacheEntryCount)} cached views</span>
            <span>{formatBytes(data.memoryDiagnostics?.instrumentService?.estimatedMemoryBytes)} estimated memory</span>
          </article>

          <article class="metric-card">
            <span class="metric-label">Instrument Value Service</span>
            <strong>{formatCount(data.memoryDiagnostics?.instrumentValueService?.instrumentValueCount)}</strong>
            <span>{formatCount(data.memoryDiagnostics?.instrumentValueService?.cacheEntryCount)} cached views</span>
            <span>{formatBytes(data.memoryDiagnostics?.instrumentValueService?.estimatedMemoryBytes)} estimated memory</span>
          </article>

          <article class="metric-card">
            <span class="metric-label">User Service</span>
            <strong>{formatCount(data.memoryDiagnostics?.userService?.userCount)}</strong>
            <span>{formatCount(data.memoryDiagnostics?.userService?.cacheEntryCount)} cached views</span>
            <span>{formatBytes(data.memoryDiagnostics?.userService?.estimatedMemoryBytes)} estimated memory</span>
          </article>

          <article class="metric-card">
            <span class="metric-label">Holding Service</span>
            <strong>{formatCount(data.memoryDiagnostics?.holdingService?.holdingCount)}</strong>
            <span>{formatCount(data.memoryDiagnostics?.holdingService?.cacheEntryCount)} cached views</span>
            <span>{formatBytes(data.memoryDiagnostics?.holdingService?.estimatedMemoryBytes)} estimated memory</span>
          </article>

          <article class="metric-card">
            <span class="metric-label">Holding Position Service</span>
            <strong>{formatCount(data.memoryDiagnostics?.holdingPositionService?.positionCount)}</strong>
            <span>{formatCount(data.memoryDiagnostics?.holdingPositionService?.cacheEntryCount)} cached views</span>
            <span>{formatBytes(data.memoryDiagnostics?.holdingPositionService?.estimatedMemoryBytes)} estimated memory</span>
          </article>

          <article class="metric-card">
            <span class="metric-label">SSE</span>
            <strong>{formatCount(data.memoryDiagnostics?.sse?.activeSubscriberCount)}</strong>
            <span>{formatCount(data.memoryDiagnostics?.sse?.publishedNotificationCount)} notifications published</span>
          </article>

          <article class="metric-card">
            <span class="metric-label">SSE Last Notification</span>
            <strong>{data.memoryDiagnostics?.sse?.lastKind ?? '-'}</strong>
            <span>{data.memoryDiagnostics?.sse?.lastNotificationType ?? 'No notifications published'}</span>
            <span>{formatMaybeDateTime(data.memoryDiagnostics?.sse?.lastEventDateTime)} event</span>
            <span>{formatMaybeDateTime(data.memoryDiagnostics?.sse?.lastAuditDateTime)} audit</span>
          </article>

          <article class="metric-card">
            <span class="metric-label">SSE Build Progress</span>
            <strong>{data.memoryDiagnostics?.sse?.lastBuildStatus ?? '-'}</strong>
            <span>{data.memoryDiagnostics?.sse?.lastBuildStage ?? 'No build progress published'}</span>
            <span>{formatMaybeDateTime(data.memoryDiagnostics?.sse?.lastBuildUpdatedAtUtc)} updated</span>
          </article>

          <article class="metric-card">
            <span class="metric-label">Aggregate Maintenance</span>
            <strong>{data.memoryDiagnostics?.aggregateMaintenance?.status ?? '-'}</strong>
            <span>
              {formatCount(data.memoryDiagnostics?.aggregateMaintenance?.lastFixedAggregates)} fixed
              from {formatCount(data.memoryDiagnostics?.aggregateMaintenance?.lastMissingAggregates)} missing
            </span>
            <span>
              {formatCount(data.memoryDiagnostics?.aggregateMaintenance?.pendingEventCount)} pending events
              {data.memoryDiagnostics?.aggregateMaintenance?.lastTrigger ? `after ${data.memoryDiagnostics.aggregateMaintenance.lastTrigger}` : ''}
            </span>
            <span>{formatMaybeDateTime(data.memoryDiagnostics?.aggregateMaintenance?.lastCompletedAtUtc)} completed</span>
          </article>
        </div>
      </section>
    </div>
  </section>
</main>
