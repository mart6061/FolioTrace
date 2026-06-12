<script lang="ts">
  import { browser } from '$app/environment';
  import { goto } from '$app/navigation';
  import { page } from '$app/state';
  import DateTimeInput from '$lib/components/DateTimeInput.svelte';
  import { formatBookmarkMenuUrl, formatBookmarkType } from '$lib/bookmarks';
  import { clampFutureInputDateTime, formatDisplayDateTime, nowForInput } from '$lib/dates';
  import { normalizeMenuPreferenceItems } from '$lib/menuPreferences';
  import { applyDarkModePreference, readInitialDarkMode } from '$lib/themeMode';
  import '../app.css';
  import { onMount } from 'svelte';

  let { children, data } = $props();

  const traceModeStorageKey = 'foliotrace.traceMode';
  const auditDateTimeStorageKey = 'foliotrace.auditDateTime';
  const menuTones = {
    home: {
      border: '#8fd8c4',
      strong: '#146c5c',
      tint: '#dceee9',
      tintText: '#0f574b'
    },
    value: {
      border: '#9bbcff',
      strong: '#2f6fed',
      tint: '#dce8ff',
      tintText: '#1f4f9f'
    },
    reference: {
      border: '#8bd9a8',
      strong: '#177245',
      tint: '#ddf3e6',
      tintText: '#115b36'
    },
    tickets: {
      border: '#e0a0b3',
      strong: '#a83b5b',
      tint: '#f4dfe7',
      tintText: '#7d2d49'
    },
    logs: {
      border: '#e6c17b',
      strong: '#b9822f',
      tint: '#f3e3c7',
      tintText: '#865817'
    },
    administration: {
      border: '#b8b7dc',
      strong: '#5e5a9d',
      tint: '#e7e7f3',
      tintText: '#454178'
    },
    todo: {
      border: '#dda0b4',
      strong: '#a33a5e',
      tint: '#f4dfe7',
      tintText: '#7d2d49'
    },
    danger: {
      border: '#ee9a92',
      strong: '#b42318',
      tint: '#fde3df',
      tintText: '#8f1c13'
    },
    disabled: {
      border: '#aeb9b0',
      strong: '#66736a',
      tint: '#e8eee8',
      tintText: '#47534c'
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
  type TopMenuID = 'bookmarks' | 'system' | '';
  type DataBranchID = 'data' | 'value' | 'reference' | 'configuration' | 'internals' | '';
  const topMenuItems: MenuItem[] = [
    { id: 'home', label: 'Home', path: '/', tone: menuTones.home },
    { id: 'bookmarks', label: 'Bookmarks', tone: menuTones.home },
    { id: 'blotter', label: 'Blotter', path: '/Blotter', tone: menuTones.tickets },
    { id: 'asset', label: 'Asset', path: '/Asset', tone: menuTones.value },
    { id: 'report', label: 'Report', path: '/Report', tone: menuTones.value },
    { id: 'account', label: 'Account', path: '/Data/Reference/Accounts', tone: menuTones.reference },
    { id: 'administration', label: 'Administration', path: '/Administration', tone: menuTones.administration },
    { id: 'system', label: 'System', tone: menuTones.logs },
    { id: 'todo', label: 'To Do', path: '/ToDo', tone: menuTones.todo }
  ];
  const dataMenuItem: MenuItem = { id: 'data', label: 'Data', tone: menuTones.value };
  const dataBranchItems: MenuItem[] = [
    { id: 'value', label: 'Value', tone: menuTones.value },
    { id: 'reference', label: 'Reference', tone: menuTones.reference },
    { id: 'configuration', label: 'Configuration', tone: menuTones.administration }
  ];
  const valueItems: MenuItem[] = [
    { id: 'value-fxs', label: 'FXs', path: '/Value/FXRates', tone: menuTones.value },
    { id: 'value-instruments', label: 'Instruments', path: '/Value/InstrumentValues', tone: menuTones.value }
  ];
  const referenceItems: MenuItem[] = [
    { id: 'reference-broker', label: 'Broker', path: '/Data/Reference/Brokers', tone: menuTones.reference },
    { id: 'reference-country', label: 'Country', path: '/Data/Reference/Countries', tone: menuTones.reference },
    { id: 'reference-currency', label: 'Currency', path: '/Data/Reference/Currencies', tone: menuTones.reference },
    { id: 'reference-fx', label: 'FX', path: '/Value/FXs', tone: menuTones.reference },
    { id: 'reference-holding', label: 'Holding', path: '/Data/Reference/Holdings', tone: menuTones.reference },
    { id: 'reference-instrument', label: 'Instrument', path: '/Data/Reference/Instruments', tone: menuTones.reference }
  ];
  const configurationItems: MenuItem[] = [
    { id: 'configuration-asset-allocation-tools', label: 'Asset Allocation Tools', path: '/Data/Configuration/AssetAllocationTools', tone: menuTones.administration }
  ];
  const internalsMenuItem: MenuItem = { id: 'internals', label: 'Internals', tone: menuTones.logs };
  const internalsItems: MenuItem[] = [
    { id: 'system-logs', label: 'Request Trace', path: '/Diagnostics/RequestTrace', tone: menuTones.logs },
    { id: 'system-fix-trace', label: 'FIX Trace', path: '/Diagnostics/FIXTrace', tone: menuTones.logs },
    { id: 'system-stats', label: 'Stats for Nerds', path: '/StatsForNerds', tone: menuTones.logs }
  ];
  const topLeafItems = topMenuItems.filter((item) => item.path);
  const leafMenuItems = [...valueItems, ...referenceItems, ...configurationItems, ...internalsItems, ...topLeafItems];
  const menuVisibility = $derived(new Map(normalizeMenuPreferenceItems(data.menuPreferences?.items).map((item) => [item.menuItemID, item.visible])));
  const visibleTopMenuItems = $derived(topMenuItems.filter((item) => isMenuItemVisible(item)));
  const visibleDataBranchItems = $derived(dataBranchItems.filter((item) => isMenuItemVisible(item)));
  const visibleValueItems = $derived(valueItems.filter((item) => isMenuItemVisible(item)));
  const visibleReferenceItems = $derived(referenceItems.filter((item) => isMenuItemVisible(item)));
  const visibleConfigurationItems = $derived(configurationItems.filter((item) => isMenuItemVisible(item)));
  const visibleInternalsItems = $derived(internalsItems.filter((item) => isMenuItemVisible(item)));
  const bookmarkMenuItems: MenuItem[] = $derived((data.userBookmarks?.items ?? []).map((bookmark) => ({
    id: `bookmark-${bookmark.bookmarkID}`,
    label: `${formatBookmarkType(bookmark.bookmarkType)}: ${formatBookmarkMenuUrl(bookmark.url)}`,
    path: bookmark.url,
    tone: menuTones.home
  })));

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

    applyDarkModePreference(readInitialDarkMode());
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
      if (openDataBranch || selectedMenuItemID) {
        selectedMenuItemID = '';
        openDataBranch = '';
        return;
      }

      openTopMenu = '';
      return;
    }

    selectedMenuItemID = '';
    openTopMenu = id;
    openDataBranch = '';
  }

  function toggleDataBranch(id: DataBranchID) {
    if (openDataBranch === id && selectedMenuItemID) {
      selectedMenuItemID = '';
      return;
    }

    openDataBranch = openDataBranch === id ? '' : id;
  }

  function handleLeafClick(item: MenuItem) {
    selectedMenuItemID = item.id;

    if (valueItems.includes(item)) {
      openTopMenu = 'system';
      openDataBranch = 'value';
      return;
    }

    if (referenceItems.includes(item)) {
      openTopMenu = 'system';
      openDataBranch = 'reference';
      return;
    }

    if (configurationItems.includes(item)) {
      openTopMenu = 'system';
      openDataBranch = 'configuration';
      return;
    }

    if (internalsItems.includes(item)) {
      openTopMenu = 'system';
      openDataBranch = 'internals';
      return;
    }

    if (bookmarkMenuItems.includes(item)) {
      openTopMenu = 'bookmarks';
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

    if (valueItems.includes(activeItem) || referenceItems.includes(activeItem) || configurationItems.includes(activeItem) || internalsItems.includes(activeItem))
      return 'system';

    if (bookmarkMenuItems.includes(activeItem))
      return 'bookmarks';

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

    if (configurationItems.includes(activeItem))
      return 'configuration';

    if (internalsItems.includes(activeItem))
      return 'internals';

    return '';
  }

  function routeActiveItem() {
    return [...leafMenuItems, ...bookmarkMenuItems].find((item) => matchesCurrentRoute(item)) ?? null;
  }

  function visibleSubmenuItems(items: MenuItem[]) {
    const activeItem = items.find((item) => item.id === selectedMenuItemID && matchesCurrentRoute(item));
    return activeItem ? [activeItem] : items;
  }

  function isConfiguredMenuVisible(id: string) {
    return id === 'home' || menuVisibility.get(id) !== false;
  }

  function isMenuItemVisible(item: MenuItem) {
    if (item.id === 'home')
      return true;

    if (valueItems.includes(item))
      return isConfiguredMenuVisible('system') && isConfiguredMenuVisible('data') && isConfiguredMenuVisible('value') && isConfiguredMenuVisible(item.id);

    if (referenceItems.includes(item))
      return isConfiguredMenuVisible('system') && isConfiguredMenuVisible('data') && isConfiguredMenuVisible('reference');

    if (configurationItems.includes(item))
      return isConfiguredMenuVisible('system') && isConfiguredMenuVisible('data') && isConfiguredMenuVisible('configuration') && isConfiguredMenuVisible(item.id);

    if (internalsItems.includes(item))
      return isConfiguredMenuVisible('system') && isConfiguredMenuVisible('internals') && isConfiguredMenuVisible(item.id);

    if (item.id === 'value' || item.id === 'reference' || item.id === 'configuration')
      return isConfiguredMenuVisible('system') && isConfiguredMenuVisible('data') && isConfiguredMenuVisible(item.id);

    if (item.id === 'internals')
      return isConfiguredMenuVisible('system') && isConfiguredMenuVisible('internals');

    if (item.id === 'data')
      return isConfiguredMenuVisible('system') && isConfiguredMenuVisible('data');

    return isConfiguredMenuVisible(item.id);
  }

  function menuStyle(tone: MenuTone, stack = 10) {
    return `--menu-strong: ${tone.strong}; --menu-tint: ${tone.tint}; --menu-tint-text: ${tone.tintText}; --menu-border: ${tone.border}; --menu-stack: ${stack};`;
  }

  const formatRecordedBy = formatDisplayDateTime;
</script>

<svelte:head>
  <title>Foleo</title>
  <meta
    name="description"
    content="Foleo reference data and event-sourced aggregate views"
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
        <span class="app-brand-mark">F<span class="app-brand-accent">e</span></span>
        <span class="app-brand-name brand-wordmark">Fol<span class="brand-wordmark-accent">e</span>o</span>
      </a>

      <div class="system-search">
        <nav class="system-menu" aria-label="Primary menu">
          {#each visibleTopMenuItems as item, topIndex (item.id)}
            {#if item.id === 'bookmarks'}
              <div class="system-menu-bookmark-cluster">
                <button aria-expanded={openTopMenu === 'bookmarks'} aria-label="Bookmarks" class={`system-menu-pill system-menu-pill-top system-menu-pill-icon-only ${isOpenTopMenu(item.id) ? 'system-menu-pill-active' : ''}`} onclick={() => toggleTopMenu('bookmarks')} style={menuStyle(item.tone, 40 - topIndex)} title="Bookmarks" type="button">
                  <span aria-hidden="true" class="system-menu-bookmark-icon">
                    <svg viewBox="0 0 24 24"><path d="M6 4h12v17l-6-4-6 4z" /></svg>
                  </span>
                </button>
                {#if openTopMenu === 'bookmarks'}
                  <div aria-label="Bookmarks" class="system-menu-bookmark-dropdown" role="menu">
                    {#each bookmarkMenuItems as bookmarkItem, bookmarkIndex (bookmarkItem.id)}
                      <a
                        aria-current={isActiveMenuItem(bookmarkItem) ? 'page' : undefined}
                        class={`system-menu-pill system-menu-pill-secondary system-menu-pill-bookmark-subitem ${isActiveMenuItem(bookmarkItem) ? 'system-menu-pill-active' : ''}`}
                        href={menuHref(bookmarkItem)}
                        onclick={() => handleLeafClick(bookmarkItem)}
                        role="menuitem"
                        style={menuStyle(bookmarkItem.tone, 80 - bookmarkIndex)}
                      >
                        {bookmarkItem.label}
                      </a>
                    {/each}
                  </div>
                {/if}
              </div>
            {:else if item.disabled}
              <button aria-disabled="true" class="system-menu-pill system-menu-pill-top system-menu-pill-disabled" style={menuStyle(item.tone, 40 - topIndex)} type="button">
                {item.label}
              </button>
              {:else if item.path}
                <a
                  aria-label={item.id === 'home' || item.id === 'todo' ? item.label : undefined}
                  aria-current={isActiveMenuItem(item) ? 'page' : undefined}
                  class={`system-menu-pill system-menu-pill-top ${item.id === 'home' || item.id === 'todo' ? 'system-menu-pill-icon-only' : ''} ${item.id === 'todo' ? 'system-menu-pill-todo' : ''} ${isActiveMenuItem(item) ? 'system-menu-pill-active' : ''}`}
                  href={menuHref(item)}
                onclick={() => handleLeafClick(item)}
                style={menuStyle(item.tone, 40 - topIndex)}
                title={item.id === 'home' || item.id === 'todo' ? item.label : undefined}
              >
                  {#if item.id === 'home'}
                    <span aria-hidden="true" class="system-menu-home-icon">
                      <svg viewBox="0 0 24 24"><path d="m3 11 9-8 9 8" /><path d="M5 10v10h14V10" /><path d="M10 20v-6h4v6" /></svg>
                    </span>
                  {:else if item.id === 'todo'}
                    <span aria-hidden="true" class="system-menu-todo-icon">
                      <svg viewBox="0 0 24 24"><path d="M8 7h10" /><path d="M8 12h10" /><path d="M8 17h10" /><path d="m3.5 7 1 1 2-2" /><path d="m3.5 12 1 1 2-2" /><path d="m3.5 17 1 1 2-2" /></svg>
                    </span>
                  {:else}
                    <span>{item.label}</span>
                  {/if}
                </a>
            {:else}
              <button
                aria-expanded={openTopMenu === 'system'}
                class={`system-menu-pill system-menu-pill-top ${isOpenTopMenu(item.id) ? 'system-menu-pill-active' : ''}`}
                onclick={() => toggleTopMenu(item.id as TopMenuID)}
                style={menuStyle(item.tone, 40 - topIndex)}
                type="button"
              >
                {item.label}
              </button>
            {/if}

          {#if item.id === 'system' && openTopMenu === 'system'}
              {#if openDataBranch !== 'internals' && openDataBranch && isMenuItemVisible(dataMenuItem)}
                <button
                  aria-expanded="true"
                  class="system-menu-pill system-menu-pill-secondary system-menu-pill-open system-menu-pill-overlap"
                  onclick={() => toggleDataBranch('data')}
                  style={menuStyle(dataMenuItem.tone, 39 - topIndex)}
                  type="button"
                >
                  {dataMenuItem.label}
                </button>

                {#if openDataBranch === 'value' && isMenuItemVisible(dataBranchItems[0])}
                  {#each visibleSubmenuItems(visibleValueItems) as valueLeaf, valueIndex (valueLeaf.id)}
                    <a
                      aria-current={isActiveMenuItem(valueLeaf) ? 'page' : undefined}
                      class={`system-menu-pill system-menu-pill-tertiary system-menu-pill-overlap ${isActiveMenuItem(valueLeaf) ? 'system-menu-pill-active' : ''}`}
                      href={menuHref(valueLeaf)}
                      onclick={() => handleLeafClick(valueLeaf)}
                      style={menuStyle(valueLeaf.tone, 38 - topIndex - valueIndex)}
                    >
                      {valueLeaf.label}
                    </a>
                  {/each}
                {:else if openDataBranch === 'reference' && isMenuItemVisible(dataBranchItems[1])}
                  {#each visibleSubmenuItems(visibleReferenceItems) as referenceLeaf, referenceIndex (referenceLeaf.id)}
                    <a
                      aria-current={isActiveMenuItem(referenceLeaf) ? 'page' : undefined}
                      class={`system-menu-pill system-menu-pill-tertiary system-menu-pill-overlap ${isActiveMenuItem(referenceLeaf) ? 'system-menu-pill-active' : ''}`}
                      href={menuHref(referenceLeaf)}
                      onclick={() => handleLeafClick(referenceLeaf)}
                      style={menuStyle(referenceLeaf.tone, 38 - topIndex - referenceIndex)}
                    >
                      {referenceLeaf.label}
                    </a>
                  {/each}
                {:else if openDataBranch === 'configuration' && isMenuItemVisible(dataBranchItems[2])}
                  {#each visibleSubmenuItems(visibleConfigurationItems) as configurationLeaf, configurationIndex (configurationLeaf.id)}
                    <a
                      aria-current={isActiveMenuItem(configurationLeaf) ? 'page' : undefined}
                      class={`system-menu-pill system-menu-pill-tertiary system-menu-pill-overlap ${isActiveMenuItem(configurationLeaf) ? 'system-menu-pill-active' : ''}`}
                      href={menuHref(configurationLeaf)}
                      onclick={() => handleLeafClick(configurationLeaf)}
                      style={menuStyle(configurationLeaf.tone, 38 - topIndex - configurationIndex)}
                    >
                      {configurationLeaf.label}
                    </a>
                  {/each}
                {:else}
                  {#each visibleDataBranchItems as branchItem, branchIndex (branchItem.id)}
                    <button
                      aria-expanded="false"
                      class="system-menu-pill system-menu-pill-tertiary system-menu-pill-overlap"
                      onclick={() => toggleDataBranch(branchItem.id as DataBranchID)}
                      style={menuStyle(branchItem.tone, 38 - topIndex - branchIndex)}
                      type="button"
                    >
                      {branchItem.label}
                    </button>
                  {/each}
                {/if}
              {:else if openDataBranch === 'internals' && isMenuItemVisible(internalsMenuItem)}
                <button
                  aria-expanded="true"
                  class="system-menu-pill system-menu-pill-secondary system-menu-pill-open system-menu-pill-overlap"
                  onclick={() => toggleDataBranch('internals')}
                  style={menuStyle(internalsMenuItem.tone, 39 - topIndex)}
                  type="button"
                >
                  {internalsMenuItem.label}
                </button>

                {#each visibleSubmenuItems(visibleInternalsItems) as internalsLeaf, internalsIndex (internalsLeaf.id)}
                  <a
                    aria-current={isActiveMenuItem(internalsLeaf) ? 'page' : undefined}
                    class={`system-menu-pill system-menu-pill-tertiary system-menu-pill-overlap ${isActiveMenuItem(internalsLeaf) ? 'system-menu-pill-active' : ''}`}
                    href={menuHref(internalsLeaf)}
                    onclick={() => handleLeafClick(internalsLeaf)}
                    style={menuStyle(internalsLeaf.tone, 38 - topIndex - internalsIndex)}
                  >
                    {internalsLeaf.label}
                  </a>
                {/each}
              {:else}
                {#if isMenuItemVisible(dataMenuItem)}
                  <button
                    aria-expanded="false"
                    class="system-menu-pill system-menu-pill-secondary system-menu-pill-overlap"
                    onclick={() => toggleDataBranch('data')}
                    style={menuStyle(dataMenuItem.tone, 39 - topIndex)}
                    type="button"
                  >
                    {dataMenuItem.label}
                  </button>
                {/if}

                {#if isMenuItemVisible(internalsMenuItem)}
                  <button
                    aria-expanded="false"
                    class="system-menu-pill system-menu-pill-secondary system-menu-pill-overlap"
                    onclick={() => toggleDataBranch('internals')}
                    style={menuStyle(internalsMenuItem.tone, 38 - topIndex)}
                    type="button"
                  >
                    {internalsMenuItem.label}
                  </button>
                {/if}
              {/if}
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
          <span class="trace-info-control">
            <button aria-describedby="trace-mode-help" aria-label="Trace Mode information" class="trace-info-button" type="button">i</button>
            <span class="trace-info-message" id="trace-mode-help" role="tooltip">
              Trace Mode shows each page as it looked using data entered on or before the selected date.
            </span>
          </span>
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
            <span>Date</span>
            <DateTimeInput
              bind:value={auditDateTime}
              futureLimited
              max={nowForInput()}
              name="traceAuditDateTime"
              onchange={handleAuditDateTimeChange}
              step="1"
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
