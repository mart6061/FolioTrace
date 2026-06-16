<script lang="ts">
  import { enhance } from '$app/forms';
  import { formatBookmarkType, formatBookmarkUrl } from '$lib/bookmarks';
  import BookmarkButton from '$lib/components/BookmarkButton.svelte';
  import ThemeModeControl from '$lib/components/ThemeModeControl.svelte';
  import { menuPreferenceDefinitions, normalizeMenuPreferenceItems } from '$lib/menuPreferences';
  import { defaultEndValuationDateOption, defaultHoldingDateBasis, defaultShowZeroBalances, defaultStartValuationDateOption, normalizeHoldingDateBasis, normalizeValuationDateOption, holdingDateBasisOptions, valuationDateOptions } from '$lib/valuationPreferences';
  import type { HoldingDateBasis, UserBookmarkItem, UserValuationDateOption } from '$lib/types';
  import type { SubmitFunction } from './$types';

  let { data, form } = $props();

  let submitting = $state(false);
  let visibleByID = $state<Record<string, boolean>>(createVisibleByID());
  let originalVisibleByID = $state<Record<string, boolean>>(createVisibleByID());
  let startValuationDateOption = $state<UserValuationDateOption>(defaultStartValuationDateOption);
  let endValuationDateOption = $state<UserValuationDateOption>(defaultEndValuationDateOption);
  let holdingDateBasis = $state<HoldingDateBasis>(defaultHoldingDateBasis);
  let showZeroBalances = $state(defaultShowZeroBalances);
  let originalStartValuationDateOption = $state<UserValuationDateOption>(defaultStartValuationDateOption);
  let originalEndValuationDateOption = $state<UserValuationDateOption>(defaultEndValuationDateOption);
  let originalHoldingDateBasis = $state<HoldingDateBasis>(defaultHoldingDateBasis);
  let originalShowZeroBalances = $state(defaultShowZeroBalances);
  let bookmarks = $state<UserBookmarkItem[]>(createBookmarks());
  let originalBookmarks = $state<UserBookmarkItem[]>(createBookmarks());
  let syncedMenuSignature = $state('');
  let syncedValuationSignature = $state('');
  let syncedBookmarkSignature = $state('');
  let draggedBookmarkID = $state<string | null>(null);
  let dragOverBookmarkID = $state<string | null>(null);
  const menuPreferenceParentByID = new Map(menuPreferenceDefinitions.map((item) => [item.id, item.parentID]));

  $effect(() => {
    const nextMenuSignature = menuSignature();

    if (nextMenuSignature !== syncedMenuSignature) {
      visibleByID = createVisibleByID();
      originalVisibleByID = createVisibleByID();
      syncedMenuSignature = nextMenuSignature;
    }

    const nextValuationSignature = valuationSignature();

    if (nextValuationSignature !== syncedValuationSignature) {
      startValuationDateOption = normalizeValuationDateOption(data.valuationPreferences.startValuationDateOption ?? data.valuationPreferences.valuationDateOption, defaultStartValuationDateOption);
      endValuationDateOption = normalizeValuationDateOption(data.valuationPreferences.endValuationDateOption ?? data.valuationPreferences.valuationDateOption, defaultEndValuationDateOption);
      holdingDateBasis = normalizeHoldingDateBasis(data.valuationPreferences.holdingDateBasis);
      showZeroBalances = Boolean(data.valuationPreferences.showZeroBalances);
      originalStartValuationDateOption = startValuationDateOption;
      originalEndValuationDateOption = endValuationDateOption;
      originalHoldingDateBasis = holdingDateBasis;
      originalShowZeroBalances = showZeroBalances;
      syncedValuationSignature = nextValuationSignature;
    }

    const nextBookmarkSignature = bookmarkSignature();

    if (nextBookmarkSignature !== syncedBookmarkSignature) {
      bookmarks = createBookmarks();
      originalBookmarks = createBookmarks();
      syncedBookmarkSignature = nextBookmarkSignature;
    }
  });

  const enhanceSavePreferences: SubmitFunction = () => {
    submitting = true;

    return async ({ result, update }) => {
      await update({ reset: false });

      if (result.type === 'success') {
        originalVisibleByID = { ...visibleByID };
        originalStartValuationDateOption = startValuationDateOption;
        originalEndValuationDateOption = endValuationDateOption;
        originalHoldingDateBasis = holdingDateBasis;
        originalShowZeroBalances = showZeroBalances;
        originalBookmarks = cloneBookmarks(bookmarks);
      }

      submitting = false;
    };
  };

  function createVisibleByID() {
    return Object.fromEntries(normalizeMenuPreferenceItems(data.menuPreferences.items).map((item) => [item.menuItemID, item.visible]));
  }

  function createBookmarks() {
    return sortBookmarks(data.userBookmarks?.items ?? []);
  }

  function isChildDisabled(parentID: string | undefined): boolean {
    if (!parentID)
      return false;

    return visibleByID[parentID] === false || isChildDisabled(menuPreferenceParentByID.get(parentID));
  }

  function setMenuVisibility(menuItemID: string, visible: boolean) {
    visibleByID = {
      ...visibleByID,
      [menuItemID]: visible
    };
  }

  function menuSignature() {
    return JSON.stringify(normalizeMenuPreferenceItems(data.menuPreferences.items));
  }

  function valuationSignature() {
    return [
      data.valuationPreferences.startValuationDateOption ?? data.valuationPreferences.valuationDateOption,
      data.valuationPreferences.endValuationDateOption ?? data.valuationPreferences.valuationDateOption,
      data.valuationPreferences.holdingDateBasis,
      String(data.valuationPreferences.showZeroBalances)
    ].join('|');
  }

  function bookmarkSignature() {
    return JSON.stringify(data.userBookmarks?.items ?? []);
  }

  function cloneBookmarks(items: UserBookmarkItem[]) {
    return items.map((item, index) => ({
      bookmarkID: item.bookmarkID,
      bookmarkType: item.bookmarkType,
      url: item.url,
      displayOrder: index + 1
    }));
  }

  function sortBookmarks(items: UserBookmarkItem[]) {
    return cloneBookmarks([...items].sort((left, right) =>
      left.displayOrder - right.displayOrder
      || left.url.localeCompare(right.url)
      || left.bookmarkID.localeCompare(right.bookmarkID)));
  }

  function deleteBookmark(bookmarkID: string) {
    bookmarks = cloneBookmarks(bookmarks.filter((bookmark) => bookmark.bookmarkID !== bookmarkID));
  }

  function serializeBookmarks(items: UserBookmarkItem[]) {
    return JSON.stringify(items);
  }

  function startBookmarkDrag(event: DragEvent, bookmarkID: string) {
    draggedBookmarkID = bookmarkID;
    dragOverBookmarkID = bookmarkID;
    event.dataTransfer?.setData('text/plain', bookmarkID);

    if (event.dataTransfer)
      event.dataTransfer.effectAllowed = 'move';
  }

  function dragOverBookmark(event: DragEvent, bookmarkID: string) {
    event.preventDefault();
    dragOverBookmarkID = bookmarkID;

    if (event.dataTransfer)
      event.dataTransfer.dropEffect = 'move';
  }

  function dropBookmark(event: DragEvent, targetBookmarkID: string) {
    event.preventDefault();
    const sourceBookmarkID = event.dataTransfer?.getData('text/plain') || draggedBookmarkID;
    draggedBookmarkID = null;
    dragOverBookmarkID = null;

    if (!sourceBookmarkID || sourceBookmarkID === targetBookmarkID)
      return;

    const sourceIndex = bookmarks.findIndex((bookmark) => bookmark.bookmarkID === sourceBookmarkID);
    const targetIndex = bookmarks.findIndex((bookmark) => bookmark.bookmarkID === targetBookmarkID);

    if (sourceIndex < 0 || targetIndex < 0)
      return;

    const next = [...bookmarks];
    const [moved] = next.splice(sourceIndex, 1);
    next.splice(targetIndex, 0, moved);
    bookmarks = cloneBookmarks(next);
  }

  function endBookmarkDrag() {
    draggedBookmarkID = null;
    dragOverBookmarkID = null;
  }
</script>

<main class="min-h-screen">
  <section class="page-header">
    <div class="page-container">
      <p class="page-kicker">User</p>
      <div class="page-title-row">
        <h1 class="page-title">Preferences</h1>
        <BookmarkButton />
      </div>
      <p class="page-subtitle">My Options</p>
      {#if data.currentUser}
        <p class="page-subtitle">{data.currentUser.displayName} · {data.currentUser.email}</p>
      {/if}
    </div>
  </section>

  <section class="page-container page-section">
    <div class="data-panel menu-preference-card developer-jwt-card">
      <h2 class="menu-preference-title">Developer JWT (REMOVE)</h2>
      {#if data.userJWT}
        <div class="grid gap-4">
          <label class="grid gap-2">
            <span class="text-sm font-semibold text-slate-800">Formatted token</span>
            <textarea
              aria-label="Formatted user JWT"
              class="min-h-32 w-full resize-y rounded-md border border-slate-300 bg-slate-50 p-3 font-mono text-xs text-slate-800"
              readonly
              spellcheck="false"
              value={data.decodedUserJWT.formattedToken}
            ></textarea>
          </label>
          {#if data.decodedUserJWT.error}
            <p class="status-panel status-panel-warning">{data.decodedUserJWT.error}</p>
          {:else}
            <div class="grid gap-4 lg:grid-cols-2">
              <label class="grid gap-2">
                <span class="text-sm font-semibold text-slate-800">Header JSON</span>
                <textarea
                  aria-label="Decoded JWT header JSON"
                  class="min-h-56 w-full resize-y rounded-md border border-slate-300 bg-white p-3 font-mono text-xs text-slate-800"
                  readonly
                  spellcheck="false"
                  value={data.decodedUserJWT.headerJSON}
                ></textarea>
              </label>
              <label class="grid gap-2">
                <span class="text-sm font-semibold text-slate-800">Payload JSON</span>
                <textarea
                  aria-label="Decoded JWT payload JSON"
                  class="min-h-56 w-full resize-y rounded-md border border-slate-300 bg-white p-3 font-mono text-xs text-slate-800"
                  readonly
                  spellcheck="false"
                  value={data.decodedUserJWT.payloadJSON}
                ></textarea>
              </label>
            </div>
          {/if}
        </div>
      {:else}
        <p class="menu-preference-empty">No JWT is available for this session.</p>
      {/if}
    </div>

    <div class="data-panel menu-preference-card">
      <h2 class="menu-preference-title">Appearance</h2>
      <div class="menu-preference-list">
        <ThemeModeControl class="theme-mode-control-preference" />
      </div>
    </div>

    <form id="preferences-form" method="POST" action="?/savePreferences" use:enhance={enhanceSavePreferences}>
      <div class="data-panel menu-preference-card">
        <h2 class="menu-preference-title">Menu Options</h2>

        {#if data.error}
          <div class="status-panel status-panel-warning mb-4">
            {data.error}
          </div>
        {/if}

        {#if form?.intent === 'savePreferences'}
          <div class={['status-panel mb-4', form.status === 'success' ? 'status-panel-success' : 'status-panel-error']}>
            {form.message}
          </div>
        {/if}

        <input type="hidden" name="hasStoredMenuPreferences" value={String(data.menuPreferences.hasStoredPreferences)} />

        <div class="menu-preference-list">
          {#each menuPreferenceDefinitions as item (item.id)}
            {@const disabled = isChildDisabled(item.parentID)}
            <label class={`menu-preference-row ${item.parentID ? 'menu-preference-row-child' : ''}`}>
              <span>{item.label}</span>
              <span class="menu-preference-toggle">
                <input type="hidden" name={`menu:${item.id}`} value={String(visibleByID[item.id] ?? true)} />
                <input type="hidden" name={`originalMenu:${item.id}`} value={String(originalVisibleByID[item.id] ?? true)} />
                <span class="trace-toggle">
                  <input
                    aria-label={`${item.label} menu visibility`}
                    checked={visibleByID[item.id] ?? true}
                    disabled={disabled}
                    name={`menu:${item.id}`}
                    onchange={(event) => setMenuVisibility(item.id, event.currentTarget.checked)}
                    type="checkbox"
                    value="true"
                  />
                  <span></span>
                </span>
              </span>
            </label>
          {/each}
        </div>
      </div>

      <div class="data-panel menu-preference-card">
        <h2 class="menu-preference-title">Valuation Options</h2>

        <input type="hidden" name="hasStoredValuationPreferences" value={String(data.valuationPreferences.hasStoredPreferences)} />
        <input type="hidden" name="originalStartValuationDateOption" value={originalStartValuationDateOption} />
        <input type="hidden" name="originalEndValuationDateOption" value={originalEndValuationDateOption} />
        <input type="hidden" name="originalHoldingDateBasis" value={originalHoldingDateBasis} />
        <input type="hidden" name="originalShowZeroBalances" value={String(originalShowZeroBalances)} />

        <div class="menu-preference-list">
          <label class="menu-preference-row">
            <span>Valuation Start</span>
            <select
              class="menu-preference-select"
              name="startValuationDateOption"
              value={startValuationDateOption}
              onchange={(event) => startValuationDateOption = normalizeValuationDateOption(event.currentTarget.value, defaultStartValuationDateOption)}
            >
              {#each valuationDateOptions as option (option.value)}
                <option value={option.value}>{option.label}</option>
              {/each}
            </select>
          </label>

          <label class="menu-preference-row">
            <span>Valuation End</span>
            <select
              class="menu-preference-select"
              name="endValuationDateOption"
              value={endValuationDateOption}
              onchange={(event) => endValuationDateOption = normalizeValuationDateOption(event.currentTarget.value, defaultEndValuationDateOption)}
            >
              {#each valuationDateOptions as option (option.value)}
                <option value={option.value}>{option.label}</option>
              {/each}
            </select>
          </label>

          <label class="menu-preference-row">
            <span>Holding Date Basis</span>
            <select
              class="menu-preference-select"
              name="holdingDateBasis"
              value={holdingDateBasis}
              onchange={(event) => holdingDateBasis = normalizeHoldingDateBasis(event.currentTarget.value)}
            >
              {#each holdingDateBasisOptions as option (option.value)}
                <option value={option.value}>{option.label}</option>
              {/each}
            </select>
          </label>

          <label class="menu-preference-row">
            <span>Display Nil Balances</span>
            <span class="menu-preference-toggle">
              <input type="hidden" name="showZeroBalances" value={String(showZeroBalances)} />
              <span class="trace-toggle">
                <input
                  aria-label="Display nil balances"
                  checked={showZeroBalances}
                  onchange={(event) => showZeroBalances = event.currentTarget.checked}
                  type="checkbox"
                  value="true"
                />
                <span></span>
              </span>
            </span>
          </label>
        </div>
      </div>

      <div class="data-panel menu-preference-card">
        <h2 class="menu-preference-title">Bookmarks</h2>
        <input type="hidden" name="bookmarks" value={serializeBookmarks(bookmarks)} />
        <input type="hidden" name="originalBookmarks" value={serializeBookmarks(originalBookmarks)} />

        {#if bookmarks.length === 0}
          <p class="menu-preference-empty">No bookmarks yet.</p>
        {:else}
          <div class="bookmark-preference-list" role="list">
            {#each bookmarks as bookmark (bookmark.bookmarkID)}
              <div
                class={`bookmark-preference-row ${dragOverBookmarkID === bookmark.bookmarkID ? 'bookmark-preference-row-over' : ''}`}
                ondragover={(event) => dragOverBookmark(event, bookmark.bookmarkID)}
                ondrop={(event) => dropBookmark(event, bookmark.bookmarkID)}
                role="listitem"
              >
                <button
                  aria-label={`Drag ${bookmark.url}`}
                  class="bookmark-preference-grip"
                  draggable="true"
                  ondragend={endBookmarkDrag}
                  ondragstart={(event) => startBookmarkDrag(event, bookmark.bookmarkID)}
                  title="Drag to reorder"
                  type="button"
                >
                  <span aria-hidden="true"></span>
                </button>
                <div class="bookmark-preference-main">
                  <span class="bookmark-preference-url">{formatBookmarkUrl(bookmark.url)}</span>
                  <span class="bookmark-preference-kind">{formatBookmarkType(bookmark.bookmarkType)}</span>
                </div>
                <div class="bookmark-preference-actions">
                  <button
                    aria-label={`Delete ${bookmark.url}`}
                    class="bookmark-remove-action"
                    onclick={() => deleteBookmark(bookmark.bookmarkID)}
                    title="Remove"
                    type="button"
                  >
                    Remove
                  </button>
                </div>
              </div>
            {/each}
          </div>
        {/if}
      </div>
    </form>

    <div class="data-panel menu-preference-save-card">
      <a class="btn btn-secondary" href="/sign-out">Sign out</a>
      <button class="btn btn-primary" disabled={submitting} form="preferences-form" type="submit">
        {submitting ? 'Saving...' : 'Save'}
      </button>
    </div>
  </section>
</main>
