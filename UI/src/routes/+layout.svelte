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
  const menuTones = {
    home: {
      border: '#5eead4',
      strong: '#0f766e',
      tint: '#ccfbf1',
      tintText: '#115e59'
    },
    value: {
      border: '#93c5fd',
      strong: '#2563eb',
      tint: '#dbeafe',
      tintText: '#1e40af'
    },
    reference: {
      border: '#86efac',
      strong: '#059669',
      tint: '#dcfce7',
      tintText: '#166534'
    },
    logs: {
      border: '#fcd34d',
      strong: '#b45309',
      tint: '#fef3c7',
      tintText: '#92400e'
    },
    danger: {
      border: '#fca5a5',
      strong: '#dc2626',
      tint: '#fee2e2',
      tintText: '#991b1b'
    },
    disabled: {
      border: '#94a3b8',
      strong: '#475569',
      tint: '#e2e8f0',
      tintText: '#334155'
    }
  };
  type MenuTone = typeof menuTones.home;
  type MenuItem = {
    disabled?: boolean;
    hash?: string;
    id: string;
    label: string;
    path?: string;
    tone: MenuTone;
  };
  type TopMenuID = 'data' | 'system' | '';
  type DataBranchID = 'value' | 'reference' | '';
  const topMenuItems: MenuItem[] = [
    { id: 'home', label: 'Home', path: '/', tone: menuTones.home },
    { id: 'blotter', label: 'Blotter', path: '/Blotter', tone: menuTones.disabled },
    { id: 'data', label: 'Data', tone: menuTones.value },
    { id: 'system', label: 'System', tone: menuTones.logs }
  ];
  const dataBranchItems: MenuItem[] = [
    { id: 'value', label: 'Value', tone: menuTones.value },
    { id: 'reference', label: 'Reference', tone: menuTones.reference }
  ];
  const valueItems: MenuItem[] = [
    { id: 'value-fxs', label: 'FXs', path: '/Value/FXRates', tone: menuTones.value },
    { id: 'value-instruments', label: 'Instruments', path: '/Value/InstrumentValues', tone: menuTones.value }
  ];
  const referenceItems: MenuItem[] = [
    { id: 'reference-account', label: 'Account', path: '/Data/Reference/Accounts', tone: menuTones.reference },
    { id: 'reference-country', label: 'Country', path: '/Data/Reference/Countries', tone: menuTones.reference },
    { id: 'reference-currency', label: 'Currency', path: '/Data/Reference/Currencies', tone: menuTones.reference },
    { id: 'reference-fx', label: 'FX', path: '/Value/FXs', tone: menuTones.reference },
    { id: 'reference-holding', label: 'Holding', path: '/Data/Reference/Holdings', tone: menuTones.reference },
    { id: 'reference-instrument', label: 'Instrument', path: '/Data/Reference/Instruments', tone: menuTones.reference }
  ];
  const systemItems: MenuItem[] = [
    { id: 'system-logs', label: 'Logs', path: '/Diagnostics/RequestTrace', tone: menuTones.logs },
    { hash: '#stats-heading', id: 'system-stats', label: 'Stats for Nerds', path: '/', tone: menuTones.logs },
    { hash: '#system-heading', id: 'system-clear-cache', label: 'Clear Cache', path: '/', tone: menuTones.danger },
    { hash: '#system-heading', id: 'system-rebuild-database', label: 'Rebuild Database', path: '/', tone: menuTones.danger }
  ];
  const leafMenuItems = [...valueItems, ...referenceItems, ...systemItems, topMenuItems[0], topMenuItems[1]];

  let traceMode = $state(false);
  let auditDateTime = $state('');
  let hydrated = $state(false);
  let openTopMenu = $state<TopMenuID>(routeTopMenu());
  let openDataBranch = $state<DataBranchID>(routeDataBranch());
  let activeRouteKey = $state('');
  let selectedMenuItemID = $state(routeActiveItem()?.id ?? '');

  onMount(() => {
    const urlAuditDateTime = clampFutureInputDateTime(page.url.searchParams.get('auditDateTime') ?? '');
    const storedTraceMode = sessionStorage.getItem(traceModeStorageKey) === 'true';
    const storedAuditDateTime = clampFutureInputDateTime(sessionStorage.getItem(auditDateTimeStorageKey) ?? '');

    traceMode = urlAuditDateTime ? true : storedTraceMode;
    auditDateTime = urlAuditDateTime || (traceMode ? storedAuditDateTime : '');
    hydrated = true;
    activeRouteKey = routeKey();
    openTopMenu = routeTopMenu();
    openDataBranch = routeDataBranch();
    selectedMenuItemID = routeActiveItem()?.id ?? '';

    syncTraceStateToUrl(true);
  });

  $effect(() => {
    const nextRouteKey = routeKey();
    if (!hydrated || nextRouteKey === activeRouteKey)
      return;

    activeRouteKey = nextRouteKey;
    openTopMenu = routeTopMenu();
    openDataBranch = routeDataBranch();
    selectedMenuItemID = routeActiveItem()?.id ?? '';
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

  function pathWithTrace(path: string) {
    const url = new URL(path, page.url);

    if (traceMode && auditDateTime)
      url.searchParams.set('auditDateTime', auditDateTime);

    return `${url.pathname}${url.search}${url.hash}`;
  }

  function menuHref(item: MenuItem) {
    return pathWithTrace(`${item.path ?? '/'}${item.hash ?? ''}`);
  }

  function isActiveMenuItem(item: MenuItem) {
    if (item.disabled || !item.path)
      return false;

    if (selectedMenuItemID)
      return item.id === selectedMenuItemID && matchesCurrentRoute(item);

    return matchesCurrentRoute(item);
  }

  function matchesCurrentRoute(item: MenuItem) {
    if (page.url.pathname !== item.path)
      return false;

    if (item.hash)
      return page.url.hash === item.hash;

    return !page.url.hash;
  }

  function isOpenTopMenu(id: string) {
    return openTopMenu === id;
  }

  function isOpenDataBranch(id: string) {
    return openDataBranch === id;
  }

  function toggleTopMenu(id: TopMenuID) {
    if (openTopMenu === id) {
      openTopMenu = '';
      openDataBranch = '';
      return;
    }

    openTopMenu = id;
    openDataBranch = '';
  }

  function toggleDataBranch(id: DataBranchID) {
    openDataBranch = openDataBranch === id ? '' : id;
  }

  function handleLeafClick(item: MenuItem) {
    selectedMenuItemID = item.id;

    if (valueItems.includes(item)) {
      openTopMenu = 'data';
      openDataBranch = 'value';
      return;
    }

    if (referenceItems.includes(item)) {
      openTopMenu = 'data';
      openDataBranch = 'reference';
      return;
    }

    if (systemItems.includes(item)) {
      openTopMenu = 'system';
      openDataBranch = '';
      return;
    }

    openTopMenu = '';
    openDataBranch = '';
  }

  function routeKey() {
    return `${page.url.pathname}${page.url.hash}`;
  }

  function routeTopMenu(): TopMenuID {
    const activeItem = routeActiveItem();

    if (!activeItem)
      return '';

    if (valueItems.includes(activeItem) || referenceItems.includes(activeItem))
      return 'data';

    if (systemItems.includes(activeItem))
      return 'system';

    return '';
  }

  function routeDataBranch(): DataBranchID {
    const activeItem = routeActiveItem();

    if (!activeItem)
      return '';

    if (valueItems.includes(activeItem))
      return 'value';

    if (referenceItems.includes(activeItem))
      return 'reference';

    return '';
  }

  function routeActiveItem() {
    return leafMenuItems.find((item) => matchesCurrentRoute(item)) ?? null;
  }

  function menuStyle(tone: MenuTone, stack = 10) {
    return `--menu-strong: ${tone.strong}; --menu-tint: ${tone.tint}; --menu-tint-text: ${tone.tintText}; --menu-border: ${tone.border}; --menu-stack: ${stack};`;
  }

  const formatRecordedBy = formatDisplayDateTime;
</script>

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
      <a class="app-brand" href={pathWithTrace('/')}>
        <span class="app-brand-mark">FT</span>
        <span>FolioTrace</span>
      </a>

      <div class="system-search">
        <nav class="system-menu" aria-label="Primary menu">
          {#each topMenuItems as item, topIndex}
            {#if item.disabled}
              <button aria-disabled="true" class="system-menu-pill system-menu-pill-top system-menu-pill-disabled" style={menuStyle(item.tone, 40 - topIndex)} type="button">
                {item.label}
              </button>
              {:else if item.path}
                <a
                  aria-label={item.id === 'home' ? 'Home' : undefined}
                  aria-current={isActiveMenuItem(item) ? 'page' : undefined}
                  class={`system-menu-pill system-menu-pill-top ${isActiveMenuItem(item) ? 'system-menu-pill-active' : ''}`}
                  href={menuHref(item)}
                onclick={() => handleLeafClick(item)}
                style={menuStyle(item.tone, 40 - topIndex)}
              >
                  {#if item.id === 'home'}
                    <span aria-hidden="true" class="system-menu-home-icon">
                      <svg viewBox="0 0 24 24"><path d="m3 11 9-8 9 8" /><path d="M5 10v10h14V10" /><path d="M10 20v-6h4v6" /></svg>
                    </span>
                  {:else}
                    <span>{item.label}</span>
                  {/if}
                </a>
            {:else}
              <button
                aria-expanded={item.id === 'data' ? openTopMenu === 'data' : openTopMenu === 'system'}
                class={`system-menu-pill system-menu-pill-top ${isOpenTopMenu(item.id) ? 'system-menu-pill-active' : ''}`}
                onclick={() => toggleTopMenu(item.id as TopMenuID)}
                style={menuStyle(item.tone, 40 - topIndex)}
                type="button"
              >
                {item.label}
              </button>
            {/if}

            {#if item.id === 'data' && openTopMenu === 'data'}
              {#if openDataBranch === 'value'}
                {@const valueItem = dataBranchItems[0]}
                <button
                  aria-expanded="true"
                  class="system-menu-pill system-menu-pill-secondary system-menu-pill-open system-menu-pill-overlap"
                  onclick={() => toggleDataBranch('value')}
                  style={menuStyle(valueItem.tone, 39 - topIndex)}
                  type="button"
                >
                  {valueItem.label}
                </button>
                {#each valueItems as valueLeaf, valueIndex}
                  <a
                    aria-current={isActiveMenuItem(valueLeaf) ? 'page' : undefined}
                    class={`system-menu-pill system-menu-pill-overlap ${isActiveMenuItem(valueLeaf) ? 'system-menu-pill-active' : ''}`}
                    href={menuHref(valueLeaf)}
                    onclick={() => handleLeafClick(valueLeaf)}
                    style={menuStyle(valueLeaf.tone, 38 - topIndex - valueIndex)}
                  >
                    {valueLeaf.label}
                  </a>
                {/each}
              {:else if openDataBranch === 'reference'}
                {@const referenceItem = dataBranchItems[1]}
                <button
                  aria-expanded="true"
                  class="system-menu-pill system-menu-pill-secondary system-menu-pill-open system-menu-pill-overlap"
                  onclick={() => toggleDataBranch('reference')}
                  style={menuStyle(referenceItem.tone, 39 - topIndex)}
                  type="button"
                >
                  {referenceItem.label}
                </button>
                {#each referenceItems as referenceLeaf, referenceIndex}
                  <a
                    aria-current={isActiveMenuItem(referenceLeaf) ? 'page' : undefined}
                    class={`system-menu-pill system-menu-pill-overlap ${isActiveMenuItem(referenceLeaf) ? 'system-menu-pill-active' : ''}`}
                    href={menuHref(referenceLeaf)}
                    onclick={() => handleLeafClick(referenceLeaf)}
                    style={menuStyle(referenceLeaf.tone, 38 - topIndex - referenceIndex)}
                  >
                    {referenceLeaf.label}
                  </a>
                {/each}
              {:else}
                {#each dataBranchItems as branchItem, branchIndex}
                  <button
                    aria-expanded="false"
                    class="system-menu-pill system-menu-pill-secondary system-menu-pill-overlap"
                    onclick={() => toggleDataBranch(branchItem.id as DataBranchID)}
                    style={menuStyle(branchItem.tone, 39 - topIndex - branchIndex)}
                    type="button"
                  >
                    {branchItem.label}
                  </button>
                {/each}
            {/if}
          {:else if item.id === 'system' && openTopMenu === 'system'}
              {#each systemItems as systemItem, systemIndex}
                <a
                  aria-current={isActiveMenuItem(systemItem) ? 'page' : undefined}
                  class={`system-menu-pill system-menu-pill-overlap ${isActiveMenuItem(systemItem) ? 'system-menu-pill-active' : ''}`}
                  href={menuHref(systemItem)}
                  onclick={() => handleLeafClick(systemItem)}
                  style={menuStyle(systemItem.tone, 39 - topIndex - systemIndex)}
                >
                  {systemItem.label}
                </a>
              {/each}
            {/if}
          {/each}
        </nav>

        <a aria-label="User preferences" class="system-user-link" href={pathWithTrace('/User/Preferences')} title="User preferences">
          me
        </a>
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
              step="1" type="datetime-local"
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
