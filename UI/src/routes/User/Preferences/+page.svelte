<script lang="ts">
  import { applyAction, enhance } from '$app/forms';
  import { invalidateAll } from '$app/navigation';
  import { formatBookmarkType, formatBookmarkUrl } from '$lib/bookmarks';
  import BookmarkButton from '$lib/components/BookmarkButton.svelte';
  import { draggable, dropZone } from '$lib/actions/dragDrop';
  import { Toggle } from '$lib/components/forms';
  import ThemeModeControl from '$lib/components/ThemeModeControl.svelte';
  import Card from '$lib/components/page/Card.svelte';
  import { menuPreferenceDefinitions, normalizeMenuPreferenceItems } from '$lib/menuPreferences';
  import { defaultEndValuationDateOption, defaultHoldingDateBasis, defaultShowZeroBalances, defaultStartValuationDateOption, defaultValuationPriceConvention, normalizeHoldingDateBasis, normalizeValuationDateOption, normalizeValuationPriceConvention, holdingDateBasisOptions, valuationDateOptions, valuationPriceConventionOptions } from '$lib/valuationPreferences';
  import type { HoldingDateBasis, UserBookmarkItem, UserValuationDateOption, ValuationPriceConvention } from '$lib/types';
  import type { ActionData, PageData, SubmitFunction } from './$types';

  interface Props {
    data: PageData;
    form: ActionData | null;
    onmenuvisibilitychange?: (menuItemID: string, visible: boolean) => void;
    onsaved?: () => void;
  }

  let { data, form, onmenuvisibilitychange, onsaved }: Props = $props();

  let submitting = $state(false);
  let actionFeedback = $state<ActionData | null | undefined>(undefined);
  const displayedForm = $derived(actionFeedback === undefined ? form : actionFeedback);
  let visibleByID = $state<Record<string, boolean>>(createVisibleByID());
  let originalVisibleByID = $state<Record<string, boolean>>(createVisibleByID());
  let startValuationDateOption = $state<UserValuationDateOption>(defaultStartValuationDateOption);
  let endValuationDateOption = $state<UserValuationDateOption>(defaultEndValuationDateOption);
  let holdingDateBasis = $state<HoldingDateBasis>(defaultHoldingDateBasis);
  let valuationPriceConvention = $state<ValuationPriceConvention>(defaultValuationPriceConvention);
  let showZeroBalances = $state(defaultShowZeroBalances);
  let originalStartValuationDateOption = $state<UserValuationDateOption>(defaultStartValuationDateOption);
  let originalEndValuationDateOption = $state<UserValuationDateOption>(defaultEndValuationDateOption);
  let originalHoldingDateBasis = $state<HoldingDateBasis>(defaultHoldingDateBasis);
  let originalValuationPriceConvention = $state<ValuationPriceConvention>(defaultValuationPriceConvention);
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
      valuationPriceConvention = normalizeValuationPriceConvention(data.valuationPreferences.valuationPriceConvention);
      showZeroBalances = Boolean(data.valuationPreferences.showZeroBalances);
      originalStartValuationDateOption = startValuationDateOption;
      originalEndValuationDateOption = endValuationDateOption;
      originalHoldingDateBasis = holdingDateBasis;
      originalValuationPriceConvention = valuationPriceConvention;
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
    actionFeedback = null;

    return async ({ result }) => {
      if (result.type === 'success') {
        actionFeedback = (result.data ?? null) as ActionData | null;
        originalVisibleByID = { ...visibleByID };
        originalStartValuationDateOption = startValuationDateOption;
        originalEndValuationDateOption = endValuationDateOption;
        originalHoldingDateBasis = holdingDateBasis;
        originalValuationPriceConvention = valuationPriceConvention;
        originalShowZeroBalances = showZeroBalances;
        originalBookmarks = cloneBookmarks(bookmarks);
        submitting = false;
        onsaved?.();
        await invalidateAll();
        return;
      }

      if (result.type === 'failure') {
        actionFeedback = result.data as ActionData;
      } else if (result.type === 'error') {
        actionFeedback = {
          intent: 'savePreferences',
          message: result.error instanceof Error ? result.error.message : 'Unable to save preferences.',
          status: 'failure'
        } as ActionData;
      } else {
        await applyAction(result);
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
    onmenuvisibilitychange?.(menuItemID, visible);
  }

  function menuSignature() {
    return JSON.stringify(normalizeMenuPreferenceItems(data.menuPreferences.items));
  }

  function valuationSignature() {
    return [
      data.valuationPreferences.startValuationDateOption ?? data.valuationPreferences.valuationDateOption,
      data.valuationPreferences.endValuationDateOption ?? data.valuationPreferences.valuationDateOption,
      data.valuationPreferences.holdingDateBasis,
      data.valuationPreferences.valuationPriceConvention,
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

  function moveBookmark(sourceBookmarkID: string, targetBookmarkID: string) {
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

  const bookmarkDragKind = 'bookmark';
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
    <div class="data-panel menu-preference-card">
      <h2 class="menu-preference-title">Appearance</h2>
      <div class="menu-preference-list">
        <ThemeModeControl class="theme-mode-control-preference" />
      </div>
    </div>

    <form id="preferences-form" method="POST" action="/User/Preferences?/savePreferences" use:enhance={enhanceSavePreferences}>
      <div class="data-panel menu-preference-card">
        <h2 class="menu-preference-title">Menu Options</h2>

        {#if data.error}
          <Card class="mb-4" density="compact" intent="warning">
            {data.error}
          </Card>
        {/if}

        {#if displayedForm?.intent === 'savePreferences'}
          <Card class="mb-4" density="compact" intent={displayedForm.status === 'success' ? 'success' : 'error'}>
            {displayedForm.message}
          </Card>
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
                <Toggle
                  checked={visibleByID[item.id] ?? true}
                  {disabled}
                  label={`${item.label} menu visibility`}
                  labelVisible={false}
                  name={`menu:${item.id}`}
                  onchange={(event) => setMenuVisibility(item.id, event.currentTarget.checked)}
                  value="true"
                />
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
        <input type="hidden" name="originalValuationPriceConvention" value={originalValuationPriceConvention} />
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
            <span>Price Convention</span>
            <select
              class="menu-preference-select"
              name="valuationPriceConvention"
              value={valuationPriceConvention}
              onchange={(event) => valuationPriceConvention = normalizeValuationPriceConvention(event.currentTarget.value)}
            >
              {#each valuationPriceConventionOptions as option (option.value)}
                <option value={option.value}>{option.label}</option>
              {/each}
            </select>
          </label>

          <label class="menu-preference-row">
            <span>Display Nil Balances</span>
            <span class="menu-preference-toggle">
              <input type="hidden" name="showZeroBalances" value={String(showZeroBalances)} />
              <Toggle
                checked={showZeroBalances}
                label="Display nil balances"
                labelVisible={false}
                onchange={(event) => showZeroBalances = event.currentTarget.checked}
                value="true"
              />
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
                role="listitem"
                use:dropZone={{
                  accepts: [bookmarkDragKind],
                  ondrop: (_kind, value) => moveBookmark(value, bookmark.bookmarkID),
                  onhover: (over) => dragOverBookmarkID = over ? bookmark.bookmarkID : null
                }}
              >
                <button
                  aria-label={`Drag ${bookmark.url}`}
                  class="bookmark-preference-grip"
                  title="Drag to reorder"
                  type="button"
                  use:draggable={{
                    kind: bookmarkDragKind,
                    value: bookmark.bookmarkID,
                    onstart: (value) => draggedBookmarkID = value,
                    onend: endBookmarkDrag
                  }}
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
      <button class="house-button house-button-primary house-button-md" disabled={submitting} form="preferences-form" type="submit">
        {submitting ? 'Saving...' : 'Save'}
      </button>
    </div>
  </section>
</main>
