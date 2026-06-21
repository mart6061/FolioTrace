<script lang="ts">
  import { browser } from '$app/environment';
  import { goto } from '$app/navigation';
  import { page } from '$app/state';
  import { closeOnOutside } from '$lib/actions/dropdown';
  import DateTimeInput from '$lib/components/DateTimeInput.svelte';
  import { Toggle } from '$lib/components/forms';
  import { formatBookmarkMenuUrl, formatBookmarkType } from '$lib/bookmarks';
  import { clampFutureInputDateTime, formatDisplayDateTime, nowForInput } from '$lib/dates';
  import { normalizeMenuPreferenceItems } from '$lib/menuPreferences';
  import { applyDarkModePreference, readInitialDarkMode } from '$lib/themeMode';
  import '../app.css';
  import { onMount } from 'svelte';

  let { children, data } = $props();

  const publicPage = $derived(Boolean(data.publicPage));
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
    configuration: {
      border: '#b8b7dc',
      strong: '#5e5a9d',
      tint: '#e7e7f3',
      tintText: '#454178'
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
  const childMenuTones: Record<number, MenuTone> = {
    2: {
      border: menuTones.home.border,
      strong: menuTones.home.strong,
      tint: menuTones.home.strong,
      tintText: '#ffffff'
    },
    3: {
      border: '#8fd8c4',
      strong: '#0e745f',
      tint: '#d8efe7',
      tintText: '#075440'
    },
    4: {
      border: '#b6e2d4',
      strong: '#2f927d',
      tint: '#e4f5ef',
      tintText: '#0d5e4c'
    }
  };
  type MenuItem = {
    disabled?: boolean;
    hash?: string;
    id: string;
    label: string;
    path?: string;
    tone: MenuTone;
  };
  type TopMenuID = 'bookmarks' | 'data-list' | 'tools' | 'system' | 'diagnostics' | '';
  type DataBranchID = 'data' | 'reference' | '';
  const topMenuItems: MenuItem[] = [
    { id: 'bookmarks', label: 'Bookmarks', tone: menuTones.home },
    { id: 'blotter', label: 'Blotter', path: '/Blotter', tone: menuTones.tickets },
    { id: 'viewer', label: 'Viewer', path: '/Viewer', tone: menuTones.value },
    { id: 'account', label: 'Account', path: '/Data/Reference/Accounts', tone: menuTones.reference },
    { id: 'data-list', label: 'Data List', tone: menuTones.reference },
    { id: 'tools', label: 'Tools', tone: menuTones.configuration },
    { id: 'system', label: 'System', tone: menuTones.logs },
    { id: 'diagnostics', label: 'Diagnostics', tone: menuTones.logs }
  ];
  const dataMenuItem: MenuItem = { id: 'data', label: 'Data', tone: menuTones.value };
  const dataBranchItems: MenuItem[] = [
    { id: 'reference', label: 'Reference', tone: menuTones.reference }
  ];
  const referenceItems: MenuItem[] = [
    { id: 'reference-broker', label: 'Broker', path: '/Data/Reference/Brokers', tone: menuTones.reference },
    { id: 'reference-holding', label: 'Holding', path: '/Data/Reference/Holdings', tone: menuTones.reference }
  ];
  const toolsItems: MenuItem[] = [
    { id: 'configuration-account-tools', label: 'Account Tools', path: '/Data/Configuration/AccountTools', tone: menuTones.configuration },
    { id: 'configuration-asset-allocation-tools', label: 'Asset Allocation Tools', path: '/Data/Configuration/AssetAllocationTools', tone: menuTones.configuration },
    { id: 'configuration-report-tools', label: 'Report Tools', path: '/Data/Configuration/ReportTools', tone: menuTones.configuration }
  ];
  const dataListItems: MenuItem[] = [
    { id: 'data-list-fx', label: 'FX', path: '/DataList/FX', tone: menuTones.reference },
    { id: 'data-list-instrument', label: 'Instrument', path: '/DataList/Instrument', tone: menuTones.reference },
    { id: 'data-list-iso', label: 'ISO', path: '/DataList/ISO', tone: menuTones.reference },
    { id: 'data-list-holding', label: 'Holding', path: '/Data/Reference/Holdings', tone: menuTones.reference },
    { id: 'data-list-broker', label: 'Broker', path: '/Data/Reference/Brokers', tone: menuTones.reference }
  ];
  const diagnosticsItems: MenuItem[] = [
    { id: 'system-logs', label: 'Request Trace', path: '/Diagnostics/RequestTrace', tone: menuTones.logs },
    { id: 'system-fix-trace', label: 'FIX Trace', path: '/Diagnostics/FIXTrace', tone: menuTones.logs },
    { id: 'system-stats', label: 'Stats for Nerds', path: '/StatsForNerds', tone: menuTones.logs }
  ];
  const topLeafItems = topMenuItems.filter((item) => item.path);
  const systemBranchItems: MenuItem[] = [dataMenuItem];
  const leafMenuItems = [...referenceItems, ...toolsItems, ...dataListItems, ...diagnosticsItems, ...topLeafItems];
  const menuVisibility = $derived(new Map(normalizeMenuPreferenceItems(data.menuPreferences?.items).map((item) => [item.menuItemID, item.visible])));
  const visibleTopMenuItems = $derived(topMenuItems.filter((item) => isMenuItemVisible(item)));
  const visibleDataBranchItems = $derived(dataBranchItems.filter((item) => isMenuItemVisible(item)));
  const visibleReferenceItems = $derived(referenceItems.filter((item) => isMenuItemVisible(item)));
  const visibleToolsItems = $derived(toolsItems.filter((item) => isMenuItemVisible(item)));
  const visibleDataListItems = $derived(dataListItems.filter((item) => isMenuItemVisible(item)));
  const visibleDiagnosticsItems = $derived(diagnosticsItems.filter((item) => isMenuItemVisible(item)));
  const visibleSystemBranchItems = $derived(systemBranchItems.filter((item) => isMenuItemVisible(item)));
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
    applyDarkModePreference(readInitialDarkMode());

    if (publicPage)
      return;

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
    if (publicPage)
      return;

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
    if (publicPage)
      return path;

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

  function closeMenus() {
    openTopMenu = '';
    openDataBranch = '';
    selectedMenuItemID = '';
  }

  function handleLeafClick(item: MenuItem) {
    selectedMenuItemID = item.id;

    if (referenceItems.includes(item)) {
      openTopMenu = 'system';
      openDataBranch = 'reference';
      return;
    }

    if (toolsItems.includes(item)) {
      openTopMenu = 'tools';
      openDataBranch = '';
      return;
    }

    if (dataListItems.includes(item)) {
      openTopMenu = 'data-list';
      openDataBranch = '';
      return;
    }

    if (diagnosticsItems.includes(item)) {
      openTopMenu = 'diagnostics';
      openDataBranch = '';
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

    if (referenceItems.includes(activeItem))
      return 'system';

    if (toolsItems.includes(activeItem))
      return 'tools';

    if (bookmarkMenuItems.includes(activeItem))
      return 'bookmarks';

    if (dataListItems.includes(activeItem))
      return 'data-list';

    if (diagnosticsItems.includes(activeItem))
      return 'diagnostics';

    return '';
  }

  function routeDataBranch(): DataBranchID {
    const activeItem = routeActiveItem();

    if (!activeItem)
      return '';

    if (referenceItems.includes(activeItem))
      return 'reference';

    return '';
  }

  function routeActiveItem() {
    return [...leafMenuItems, ...bookmarkMenuItems].find((item) => matchesCurrentRoute(item)) ?? null;
  }

  function visibleSubmenuItems(items: MenuItem[]) {
    const activeItem = items.find((item) => item.id === selectedMenuItemID && matchesCurrentRoute(item));
    return activeItem ? [activeItem] : items;
  }

  function dataBranchFor(id: DataBranchID) {
    return dataBranchItems.find((item) => item.id === id) ?? null;
  }

  function isConfiguredMenuVisible(id: string) {
    return menuVisibility.get(id) !== false;
  }

  function isMenuItemVisible(item: MenuItem) {
    if (referenceItems.includes(item))
      return isConfiguredMenuVisible('system') && isConfiguredMenuVisible('data') && isConfiguredMenuVisible('reference');

    if (toolsItems.includes(item))
      return isConfiguredMenuVisible('tools') && isConfiguredMenuVisible(item.id);

    if (dataListItems.includes(item))
      return isConfiguredMenuVisible('data-list') && isConfiguredMenuVisible(item.id);

    if (diagnosticsItems.includes(item))
      return isConfiguredMenuVisible('diagnostics') && isConfiguredMenuVisible(item.id);

    if (item.id === 'reference')
      return isConfiguredMenuVisible('system') && isConfiguredMenuVisible('data') && isConfiguredMenuVisible(item.id);

    if (item.id === 'tools')
      return isConfiguredMenuVisible('tools');

    if (item.id === 'data')
      return isConfiguredMenuVisible('system') && isConfiguredMenuVisible('data');

    return isConfiguredMenuVisible(item.id);
  }

  function menuStyle(tone: MenuTone, stack = 10) {
    return `--menu-strong: ${tone.strong}; --menu-tint: ${tone.tint}; --menu-tint-text: ${tone.tintText}; --menu-border: ${tone.border}; --menu-stack: ${stack};`;
  }

  function menuLevelStyle(tone: MenuTone, level: number, stack = 10) {
    return menuStyle(childMenuTones[Math.min(Math.max(level, 2), 4)] ?? tone, stack);
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

{#if publicPage}
  {@render children()}
{:else}
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
          <nav
            aria-label="Primary menu"
            class="system-menu"
            use:closeOnOutside={{ close: closeMenus, enabled: Boolean(openTopMenu || openDataBranch || selectedMenuItemID) }}
          >
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
                        style={menuLevelStyle(bookmarkItem.tone, 2, 80 - bookmarkIndex)}
                      >
                        {bookmarkItem.label}
                      </a>
                    {/each}
                  </div>
                {/if}
              </div>
            {:else if item.id === 'data-list'}
              <div class="system-menu-bookmark-cluster">
                <button aria-expanded={openTopMenu === 'data-list'} aria-label="Data list" class={`system-menu-pill system-menu-pill-top system-menu-pill-icon-only ${isOpenTopMenu(item.id) ? 'system-menu-pill-active' : ''}`} onclick={() => toggleTopMenu('data-list')} style={menuStyle(item.tone, 40 - topIndex)} title="Data list" type="button">
                  <span aria-hidden="true" class="system-menu-data-list-icon">
                    <svg viewBox="0 0 24 24"><circle cx="5" cy="6" r="1" /><circle cx="5" cy="12" r="1" /><circle cx="5" cy="18" r="1" /><path d="M9 6h10" /><path d="M9 12h10" /><path d="M9 18h10" /></svg>
                  </span>
                </button>
                {#if openTopMenu === 'data-list'}
                  <div aria-label="Data list" class="system-menu-bookmark-dropdown" role="menu">
                    {#each visibleDataListItems as dataListItem, dataListIndex (dataListItem.id)}
                      <a
                        aria-current={isActiveMenuItem(dataListItem) ? 'page' : undefined}
                        class={`system-menu-pill system-menu-pill-secondary system-menu-pill-bookmark-subitem ${isActiveMenuItem(dataListItem) ? 'system-menu-pill-active' : ''}`}
                        href={menuHref(dataListItem)}
                        onclick={() => handleLeafClick(dataListItem)}
                        role="menuitem"
                        style={menuLevelStyle(dataListItem.tone, 2, 80 - dataListIndex)}
                      >
                        {dataListItem.label}
                      </a>
                    {/each}
                  </div>
                {/if}
              </div>
            {:else if item.id === 'tools'}
              <div class="system-menu-bookmark-cluster">
                <button aria-expanded={openTopMenu === 'tools'} aria-label="Tools" class={`system-menu-pill system-menu-pill-top system-menu-pill-icon-only ${isOpenTopMenu(item.id) ? 'system-menu-pill-active' : ''}`} onclick={() => toggleTopMenu('tools')} style={menuStyle(item.tone, 40 - topIndex)} title="Tools" type="button">
                  <span aria-hidden="true" class="system-menu-tools-icon">
                    <svg viewBox="0 0 24 24"><path d="M14.7 6.3a4 4 0 0 0-5 5L4 17v3h3l5.7-5.7a4 4 0 0 0 5-5l-2.8 2.8-2-2z" /></svg>
                  </span>
                </button>
                {#if openTopMenu === 'tools'}
                  <div aria-label="Tools" class="system-menu-bookmark-dropdown" role="menu">
                    {#each visibleToolsItems as toolsItem, toolsIndex (toolsItem.id)}
                      <a
                        aria-current={isActiveMenuItem(toolsItem) ? 'page' : undefined}
                        class={`system-menu-pill system-menu-pill-secondary system-menu-pill-bookmark-subitem ${isActiveMenuItem(toolsItem) ? 'system-menu-pill-active' : ''}`}
                        href={menuHref(toolsItem)}
                        onclick={() => handleLeafClick(toolsItem)}
                        role="menuitem"
                        style={menuLevelStyle(toolsItem.tone, 2, 80 - toolsIndex)}
                      >
                        {toolsItem.label}
                      </a>
                    {/each}
                  </div>
                {/if}
              </div>
            {:else if item.id === 'diagnostics'}
              <div class="system-menu-bookmark-cluster">
                <button aria-expanded={openTopMenu === 'diagnostics'} aria-label="Diagnostics" class={`system-menu-pill system-menu-pill-top system-menu-pill-icon-only ${isOpenTopMenu(item.id) ? 'system-menu-pill-active' : ''}`} onclick={() => toggleTopMenu('diagnostics')} style={menuStyle(item.tone, 40 - topIndex)} title="Diagnostics" type="button">
                  <span aria-hidden="true" class="system-menu-diagnostics-icon">
                    <svg viewBox="0 0 24 24"><path d="M3 12h3l2-5 4 10 3-7 2 4h4" /><path d="M5 19h14" /></svg>
                  </span>
                </button>
                {#if openTopMenu === 'diagnostics'}
                  <div aria-label="Diagnostics" class="system-menu-bookmark-dropdown" role="menu">
                    {#each visibleDiagnosticsItems as diagnosticsItem, diagnosticsIndex (diagnosticsItem.id)}
                      <a
                        aria-current={isActiveMenuItem(diagnosticsItem) ? 'page' : undefined}
                        class={`system-menu-pill system-menu-pill-secondary system-menu-pill-bookmark-subitem ${isActiveMenuItem(diagnosticsItem) ? 'system-menu-pill-active' : ''}`}
                        href={menuHref(diagnosticsItem)}
                        onclick={() => handleLeafClick(diagnosticsItem)}
                        role="menuitem"
                        style={menuLevelStyle(diagnosticsItem.tone, 2, 80 - diagnosticsIndex)}
                      >
                        {diagnosticsItem.label}
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
                  aria-current={isActiveMenuItem(item) ? 'page' : undefined}
                  class={`system-menu-pill system-menu-pill-top ${isActiveMenuItem(item) ? 'system-menu-pill-active' : ''}`}
                  href={menuHref(item)}
                onclick={() => handleLeafClick(item)}
                style={menuStyle(item.tone, 40 - topIndex)}
              >
                  <span>{item.label}</span>
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
              {#if openDataBranch && isMenuItemVisible(dataMenuItem)}
                <button
                  aria-expanded="true"
                  class="system-menu-pill system-menu-pill-secondary system-menu-pill-open system-menu-pill-overlap"
                  onclick={() => toggleDataBranch('data')}
                  style={menuLevelStyle(dataMenuItem.tone, 2, 39 - topIndex)}
                  type="button"
                >
                  {dataMenuItem.label}
                </button>

                {@const activeDataBranch = dataBranchFor(openDataBranch)}
                {#if activeDataBranch}
                  <button
                    aria-expanded="true"
                    class="system-menu-pill system-menu-pill-tertiary system-menu-pill-open system-menu-pill-overlap"
                    onclick={() => toggleDataBranch(activeDataBranch.id as DataBranchID)}
                    style={menuLevelStyle(activeDataBranch.tone, 3, 38 - topIndex)}
                    type="button"
                  >
                    {activeDataBranch.label}
                  </button>
                {/if}

                {#if openDataBranch === 'reference' && isMenuItemVisible(dataBranchItems[0])}
                  {#each visibleSubmenuItems(visibleReferenceItems) as referenceLeaf, referenceIndex (referenceLeaf.id)}
                    <a
                      aria-current={isActiveMenuItem(referenceLeaf) ? 'page' : undefined}
                      class={`system-menu-pill system-menu-pill-tertiary system-menu-pill-overlap ${isActiveMenuItem(referenceLeaf) ? 'system-menu-pill-active' : ''}`}
                      href={menuHref(referenceLeaf)}
                      onclick={() => handleLeafClick(referenceLeaf)}
                      style={menuLevelStyle(referenceLeaf.tone, 4, 37 - topIndex - referenceIndex)}
                    >
                      {referenceLeaf.label}
                    </a>
                  {/each}
                {:else}
                  {#each visibleDataBranchItems as branchItem, branchIndex (branchItem.id)}
                    <button
                      aria-expanded="false"
                      class="system-menu-pill system-menu-pill-tertiary system-menu-pill-overlap"
                      onclick={() => toggleDataBranch(branchItem.id as DataBranchID)}
                      style={menuLevelStyle(branchItem.tone, 3, 38 - topIndex - branchIndex)}
                      type="button"
                    >
                      {branchItem.label}
                    </button>
                  {/each}
                {/if}
              {:else}
                {#each visibleSystemBranchItems as branchItem, branchIndex (branchItem.id)}
                  <button
                    aria-expanded="false"
                    class="system-menu-pill system-menu-pill-secondary system-menu-pill-overlap"
                    onclick={() => toggleDataBranch(branchItem.id as DataBranchID)}
                    style={menuLevelStyle(branchItem.tone, 2, 39 - topIndex - branchIndex)}
                    type="button"
                  >
                    {branchItem.label}
                  </button>
                {/each}
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
            <Toggle bind:checked={traceMode} label="Trace Mode" labelVisible={false} onchange={handleTraceModeChange} />
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
{/if}
