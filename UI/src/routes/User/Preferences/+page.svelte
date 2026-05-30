<script lang="ts">
  import { enhance } from '$app/forms';
  import { menuPreferenceDefinitions, normalizeMenuPreferenceItems } from '$lib/menuPreferences';
  import { defaultShowZeroBalances, defaultValuationDateBasis, defaultValuationDateOption, normalizeValuationDateBasis, normalizeValuationDateOption, valuationDateBasisOptions, valuationDateOptions } from '$lib/valuationPreferences';
  import { formatDisplayDateTime } from '$lib/dates';
  import type { UserValuationDateOption, ValuationDateBasis } from '$lib/types';
  import type { SubmitFunction } from './$types';

  let { data, form } = $props();

  let submitting = $state(false);
  let visibleByID = $state<Record<string, boolean>>(createVisibleByID());
  let originalVisibleByID = $state<Record<string, boolean>>(createVisibleByID());
  let valuationDateOption = $state<UserValuationDateOption>(defaultValuationDateOption);
  let valuationDateBasis = $state<ValuationDateBasis>(defaultValuationDateBasis);
  let showZeroBalances = $state(defaultShowZeroBalances);
  let originalValuationDateOption = $state<UserValuationDateOption>(defaultValuationDateOption);
  let originalValuationDateBasis = $state<ValuationDateBasis>(defaultValuationDateBasis);
  let originalShowZeroBalances = $state(defaultShowZeroBalances);
  let syncedMenuSignature = $state('');
  let syncedValuationSignature = $state('');
  const asOfSummary = $derived(data.auditDateTime && data.menuPreferences.asOfDateTime ? formatDisplayDateTime(data.menuPreferences.asOfDateTime) : 'now');

  $effect(() => {
    const nextMenuSignature = menuSignature();

    if (nextMenuSignature !== syncedMenuSignature) {
      visibleByID = createVisibleByID();
      originalVisibleByID = createVisibleByID();
      syncedMenuSignature = nextMenuSignature;
    }

    const nextValuationSignature = valuationSignature();

    if (nextValuationSignature !== syncedValuationSignature) {
      valuationDateOption = normalizeValuationDateOption(data.valuationPreferences.valuationDateOption);
      valuationDateBasis = normalizeValuationDateBasis(data.valuationPreferences.valuationDateBasis);
      showZeroBalances = Boolean(data.valuationPreferences.showZeroBalances);
      originalValuationDateOption = valuationDateOption;
      originalValuationDateBasis = valuationDateBasis;
      originalShowZeroBalances = showZeroBalances;
      syncedValuationSignature = nextValuationSignature;
    }
  });

  const enhanceSavePreferences: SubmitFunction = () => {
    submitting = true;

    return async ({ result, update }) => {
      await update({ reset: false });

      if (result.type === 'success') {
        originalVisibleByID = { ...visibleByID };
        originalValuationDateOption = valuationDateOption;
        originalValuationDateBasis = valuationDateBasis;
        originalShowZeroBalances = showZeroBalances;
      }

      submitting = false;
    };
  };

  function createVisibleByID() {
    return Object.fromEntries(normalizeMenuPreferenceItems(data.menuPreferences.items).map((item) => [item.menuItemID, item.visible]));
  }

  function isChildDisabled(parentID: string | undefined) {
    return parentID ? visibleByID[parentID] === false : false;
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
      data.valuationPreferences.valuationDateOption,
      data.valuationPreferences.valuationDateBasis,
      String(data.valuationPreferences.showZeroBalances)
    ].join('|');
  }
</script>

<main class="min-h-screen">
  <section class="page-header">
    <div class="page-container">
      <p class="page-kicker">User</p>
      <h1 class="page-title">Preferences</h1>
      <p class="page-subtitle">Menu visibility as of {asOfSummary}.</p>
    </div>
  </section>

  <section class="page-container page-section">
    <form method="POST" action="?/savePreferences" use:enhance={enhanceSavePreferences}>
      <div class="data-panel menu-preference-card">
        <h2 class="menu-preference-title">Menu Options</h2>

        {#if data.error}
          <div class="mb-4 rounded-md border border-amber-200 bg-amber-50 px-3 py-2 text-sm text-amber-800">
            {data.error}
          </div>
        {/if}

        {#if form?.intent === 'savePreferences'}
          <div class={`mb-4 rounded-md border px-3 py-2 text-sm ${form.status === 'success' ? 'border-emerald-200 bg-emerald-50 text-emerald-800' : 'border-rose-200 bg-rose-50 text-rose-800'}`}>
            {form.message}
          </div>
        {/if}

        <input type="hidden" name="hasStoredMenuPreferences" value={String(data.menuPreferences.hasStoredPreferences)} />

        <div class="menu-preference-list">
          {#each menuPreferenceDefinitions as item}
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
        <input type="hidden" name="originalValuationDateOption" value={originalValuationDateOption} />
        <input type="hidden" name="originalValuationDateBasis" value={originalValuationDateBasis} />
        <input type="hidden" name="originalShowZeroBalances" value={String(originalShowZeroBalances)} />

        <div class="menu-preference-list">
          <label class="menu-preference-row">
            <span>Valuation Date</span>
            <select
              class="menu-preference-select"
              name="valuationDateOption"
              value={valuationDateOption}
              onchange={(event) => valuationDateOption = normalizeValuationDateOption(event.currentTarget.value)}
            >
              {#each valuationDateOptions as option}
                <option value={option.value}>{option.label}</option>
              {/each}
            </select>
          </label>

          <label class="menu-preference-row">
            <span>Valuation Date Basis</span>
            <select
              class="menu-preference-select"
              name="valuationDateBasis"
              value={valuationDateBasis}
              onchange={(event) => valuationDateBasis = normalizeValuationDateBasis(event.currentTarget.value)}
            >
              {#each valuationDateBasisOptions as option}
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
      </div>

      <div class="data-panel menu-preference-save-card">
        <button class="primary-action" disabled={submitting} type="submit">
          {submitting ? 'Saving...' : 'Save'}
        </button>
      </div>
    </form>
  </section>
</main>
