<script lang="ts">
  import { browser } from '$app/environment';
  import { goto } from '$app/navigation';
  import { page } from '$app/state';
  import { clampFutureInputDateTime, formatDisplayDateTime, nowForInput } from '$lib/dates';
  import '../app.css';
  import { onMount } from 'svelte';

  let { children, data } = $props();

  const traceModeStorageKey = 'foliotrace.traceMode';
  const auditDateTimeStorageKey = 'foliotrace.auditDateTime';

  let traceMode = $state(false);
  let auditDateTime = $state('');
  let hydrated = $state(false);
  let systemMenuOpen = $state(false);
  let referenceDataOpen = $state(false);
  let valueDataOpen = $state(false);
  let systemMenuContainer: HTMLDivElement;

  onMount(() => {
    const urlAuditDateTime = clampFutureInputDateTime(page.url.searchParams.get('auditDateTime') ?? '');
    const storedTraceMode = sessionStorage.getItem(traceModeStorageKey) === 'true';
    const storedAuditDateTime = clampFutureInputDateTime(sessionStorage.getItem(auditDateTimeStorageKey) ?? '');

    traceMode = urlAuditDateTime ? true : storedTraceMode;
    auditDateTime = urlAuditDateTime || (traceMode ? storedAuditDateTime : '');
    hydrated = true;

    syncTraceStateToUrl(true);
  });

  function updateTraceSession() {
    if (!browser || !hydrated)
      return;

    sessionStorage.setItem(traceModeStorageKey, String(traceMode));

    if (traceMode && auditDateTime)
      sessionStorage.setItem(auditDateTimeStorageKey, auditDateTime);
    else
      sessionStorage.removeItem(auditDateTimeStorageKey);
  }

  function syncTraceStateToUrl(replaceState = false) {
    if (!browser || !hydrated)
      return;

    auditDateTime = clampFutureInputDateTime(auditDateTime);

    updateTraceSession();

    const url = new URL(page.url);
    const currentAuditDateTime = url.searchParams.get('auditDateTime') ?? '';
    const nextAuditDateTime = traceMode && auditDateTime ? auditDateTime : '';

    if (currentAuditDateTime === nextAuditDateTime)
      return;

    if (nextAuditDateTime)
      url.searchParams.set('auditDateTime', nextAuditDateTime);
    else
      url.searchParams.delete('auditDateTime');

    goto(`${url.pathname}${url.search}${url.hash}`, {
      invalidateAll: true,
      keepFocus: true,
      noScroll: true,
      replaceState
    });
  }

  function handleTraceModeChange() {
    if (!traceMode)
      auditDateTime = '';

    syncTraceStateToUrl();
  }

  function handleAuditDateTimeChange() {
    auditDateTime = clampFutureInputDateTime(auditDateTime);
    syncTraceStateToUrl();
  }

  function toggleSystemMenu() {
    systemMenuOpen = !systemMenuOpen;

    if (!systemMenuOpen)
    {
      referenceDataOpen = false;
      valueDataOpen = false;
    }
  }

  function toggleReferenceDataMenu() {
    referenceDataOpen = !referenceDataOpen;
  }

  function toggleValueDataMenu() {
    valueDataOpen = !valueDataOpen;
  }

  function closeSystemMenu() {
    systemMenuOpen = false;
    referenceDataOpen = false;
    valueDataOpen = false;
  }

  function handleDocumentClick(event: MouseEvent) {
    if (!systemMenuOpen || !systemMenuContainer)
      return;

    if (event.target instanceof Node && !systemMenuContainer.contains(event.target))
      closeSystemMenu();
  }

  function handleDocumentKeydown(event: KeyboardEvent) {
    if (event.key === 'Escape')
      closeSystemMenu();
  }

  function pathWithTrace(path: string) {
    const url = new URL(path, page.url);

    if (traceMode && auditDateTime)
      url.searchParams.set('auditDateTime', auditDateTime);

    return `${url.pathname}${url.search}`;
  }

  const formatRecordedBy = formatDisplayDateTime;
</script>

<svelte:document onclick={handleDocumentClick} onkeydown={handleDocumentKeydown} />

<svelte:head>
  <title>FolioTrace</title>
  <meta
    name="description"
    content="FolioTrace reference data and event-sourced aggregate views"
  />
</svelte:head>

<div class="app-shell">
  {#if traceMode && auditDateTime}
    <div class="trace-warning" role="alert">
      <div class="page-container trace-warning-inner">
        <strong>Trace Mode is on</strong>
        <span>This view only includes events recorded on or before {formatRecordedBy(auditDateTime)}.</span>
      </div>
    </div>
  {/if}

  <header class="app-header">
    <div class="app-header-inner">
      <a class="app-brand" href={pathWithTrace('/')} onclick={closeSystemMenu}>
        <span class="app-brand-mark">FT</span>
        <span>FolioTrace</span>
      </a>

      <div class="system-search" bind:this={systemMenuContainer}>
        <button
          aria-expanded={systemMenuOpen}
          aria-label="System menu"
          class="system-menu-button"
          onclick={toggleSystemMenu}
          type="button"
        >
          <span></span>
          <span></span>
          <span></span>
        </button>

        <input
          aria-label="Search"
          class="system-search-input"
          placeholder="start typing..."
          type="search"
        />

        <a
          aria-label="User preferences"
          class="system-user-link"
          href={pathWithTrace('/User/Preferences')}
          onclick={closeSystemMenu}
          title="User preferences"
        >
          me
        </a>

        {#if systemMenuOpen}
          <nav class="system-menu" aria-label="System menu">
            <button type="button">Blotter</button>
            <a href={pathWithTrace('/')} onclick={closeSystemMenu}>Dashboard</a>
            <button
              aria-expanded={valueDataOpen}
              class="system-menu-parent"
              onclick={toggleValueDataMenu}
              type="button"
            >
              <span>Value Data</span>
              <span aria-hidden="true">&gt;</span>
            </button>
            {#if valueDataOpen}
              <a
                class="system-submenu-item"
                href={pathWithTrace('/Value/FXs')}
                onclick={closeSystemMenu}
              >
                FX Data
              </a>
              <a
                class="system-submenu-item"
                href={pathWithTrace('/Value/FXRates')}
                onclick={closeSystemMenu}
              >
                FX Rate Data
              </a>
            {/if}
            <button
              aria-expanded={referenceDataOpen}
              class="system-menu-parent"
              onclick={toggleReferenceDataMenu}
              type="button"
            >
              <span>Reference Data</span>
              <span aria-hidden="true">&gt;</span>
            </button>
            {#if referenceDataOpen}
              <a
                class="system-submenu-item"
                href={pathWithTrace('/Data/Reference/Countries')}
                onclick={closeSystemMenu}
              >
                Country Data
              </a>
              <a
                class="system-submenu-item"
                href={pathWithTrace('/Data/Reference/Currencies')}
                onclick={closeSystemMenu}
              >
                Currency Data
              </a>
            {/if}
            <a href={pathWithTrace('/Diagnostics/RequestTrace')} onclick={closeSystemMenu}>Request Trace</a>
            <button type="button">Help</button>
          </nav>
        {/if}
      </div>
    </div>
  </header>

  <div class="app-content">
    {@render children()}
  </div>

  <footer class={`trace-footer ${traceMode && auditDateTime ? 'trace-footer-active' : ''}`}>
    <div class="trace-footer-inner">
      <div class="trace-footer-controls">
        <div class="trace-mode-control">
          <span>Trace Mode</span>
          <label class="trace-toggle">
            <input
              aria-label="Trace Mode"
              bind:checked={traceMode}
              onchange={handleTraceModeChange}
              type="checkbox"
            />
            <span></span>
          </label>
        </div>

        {#if traceMode}
          <label class="trace-date-control">
            <span>Trace Date</span>
            <input
              bind:value={auditDateTime}
              max={nowForInput()}
              name="traceAuditDateTime"
              onchange={handleAuditDateTimeChange}
              type="datetime-local"
            />
          </label>
        {/if}
      </div>

      <div class="app-version-strip" aria-label="Application versions">
        <span>UI {data.uiVersion}</span>
        <span>API {data.apiVersion}</span>
      </div>
    </div>
  </footer>
</div>
