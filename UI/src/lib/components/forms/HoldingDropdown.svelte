<script lang="ts">
  import { tick } from 'svelte';
  import { closeOnOutside } from '$lib/actions/dropdown';
  import type { Holding } from '$lib/types';
  import { classNames } from './controls';

  type Props = {
    accountID?: string;
    class?: string;
    compactBrand?: boolean;
    disabled?: boolean;
    holdings: Holding[];
    multiple?: boolean;
    name?: string;
    nameOnlySummary?: boolean;
    placeholder?: string;
    showInstrumentID?: boolean;
    selectedHoldingID?: string;
    selectedHoldingIDs?: string[];
  };

  let {
    accountID = '',
    class: className = '',
    compactBrand = false,
    disabled = false,
    holdings,
    multiple = false,
    name = '',
    nameOnlySummary = false,
    placeholder = 'Select holding',
    showInstrumentID = true,
    selectedHoldingID = $bindable(''),
    selectedHoldingIDs = $bindable([])
  }: Props = $props();

  let filterText = $state('');
  let open = $state(false);
  let dropdownElement = $state<HTMLElement | null>(null);
  let searchInput = $state<HTMLInputElement | null>(null);

  const accountHoldings = $derived(holdings.filter((holding) => !accountID || holding.accountID === accountID));
  const sortedHoldings = $derived([...accountHoldings].sort((left, right) => holdingLabel(left).localeCompare(holdingLabel(right))));
  const showFilter = $derived(multiple || sortedHoldings.length > 8);
  const filteredHoldings = $derived(showFilter ? sortedHoldings.filter(holdingMatchesFilter) : sortedHoldings);
  const selectedHolding = $derived(sortedHoldings.find((holding) => holding.holdingID === selectedHoldingID) ?? null);
  const selectedHoldings = $derived(sortedHoldings.filter((holding) => selectedHoldingIDs.includes(holding.holdingID)));
  const allHoldingsSelected = $derived(sortedHoldings.length > 0 && sortedHoldings.every((holding) => selectedHoldingIDs.includes(holding.holdingID)));
  const noHoldingsSelected = $derived(selectedHoldingIDs.length === 0);
  const summary = $derived(multiple ? multiSummary() : selectedHolding ? selectedSummary(selectedHolding) : placeholder);

  function holdingMatchesFilter(holding: Holding) {
    const filter = filterText.trim().toLocaleLowerCase();
    if (!filter)
      return true;

    return [
      holdingLabel(holding),
      holding.holdingKind,
      holding.instrumentID,
      holding.active ? 'active' : 'inactive',
      holding.includeInValuation ? 'valuation' : 'excluded',
      holding.holdingID
    ].some((value) => value.toLocaleLowerCase().includes(filter));
  }

  function holdingLabel(holding: Holding) {
    return holding.name || holding.holdingKind;
  }

  function selectedSummary(holding: Holding) {
    return nameOnlySummary ? holdingLabel(holding) : `${holdingLabel(holding)} - ${holdingMeta(holding)}`;
  }

  function holdingMeta(holding: Holding) {
    return [
      holding.holdingKind,
      showInstrumentID ? holding.instrumentID : '',
      holding.includeInValuation ? '' : 'Excluded',
      holding.active ? '' : 'Inactive'
    ].filter(Boolean).join(' - ');
  }

  function multiSummary() {
    if (!selectedHoldings.length)
      return placeholder;

    if (selectedHoldings.length === 1)
      return selectedSummary(selectedHoldings[0]);

    return `${selectedHoldings.length} selected`;
  }

  function captureDropdown(element: Element) {
    const node = element as HTMLElement;
    dropdownElement = node;

    return () => {
      if (dropdownElement === node)
        dropdownElement = null;
    };
  }

  function captureSearchInput(element: Element) {
    const node = element as HTMLInputElement;
    searchInput = node;

    return () => {
      if (searchInput === node)
        searchInput = null;
    };
  }

  async function openDropdown() {
    if (disabled)
      return;

    open = true;
    await tick();
    searchInput?.focus();
    searchInput?.select();
  }

  function toggleDropdown() {
    if (open) {
      open = false;
      return;
    }

    openDropdown();
  }

  function chooseHolding(holdingID: string) {
    selectedHoldingID = holdingID;
    filterText = '';
    open = false;
  }

  function toggleHolding(holdingID: string) {
    selectedHoldingIDs = selectedHoldingIDs.includes(holdingID)
      ? selectedHoldingIDs.filter((selectedID) => selectedID !== holdingID)
      : [...selectedHoldingIDs, holdingID];
  }

  function selectAllHoldings() {
    selectedHoldingIDs = sortedHoldings.map((holding) => holding.holdingID);
  }

  function clearSelectedHoldings() {
    selectedHoldingIDs = [];
  }

  function closeDropdown() {
    filterText = '';
    open = false;
  }

  function isSelected(holdingID: string) {
    return multiple ? selectedHoldingIDs.includes(holdingID) : selectedHoldingID === holdingID;
  }

  function handleSearchKeydown(event: KeyboardEvent) {
    if (event.key === 'Escape') {
      open = false;
      return;
    }

    if (event.key === 'Enter' && filteredHoldings[0]) {
      event.preventDefault();
      if (multiple)
        toggleHolding(filteredHoldings[0].holdingID);
      else
        chooseHolding(filteredHoldings[0].holdingID);
    }
  }
</script>

<div
  class={classNames('holding-combobox', multiple && 'holding-combobox-multi', compactBrand && 'holding-combobox-compact-brand', className)}
  use:closeOnOutside={{ close: closeDropdown, enabled: open }}
  {@attach captureDropdown}
>
  {#if name}
    {#if multiple}
      {#each selectedHoldingIDs as holdingID (holdingID)}
        <input name={name} type="hidden" value={holdingID} />
      {/each}
    {:else}
      <input name={name} type="hidden" value={selectedHoldingID} />
    {/if}
  {/if}

  <button
    aria-expanded={open}
    aria-haspopup={multiple ? 'true' : 'listbox'}
    class="holding-combobox-trigger"
    {disabled}
    onclick={toggleDropdown}
    type="button"
  >
    <span>{summary}</span>
  </button>

  {#if open}
    <div aria-label="Holdings" class="holding-combobox-menu" role={multiple ? 'group' : 'listbox'}>
      {#if showFilter}
        <div class="holding-combobox-search-row">
          <input
            bind:value={filterText}
            class="house-control house-control-md house-control-full"
            onkeydown={handleSearchKeydown}
            placeholder="Search holdings"
            type="search"
            {@attach captureSearchInput}
          />
          {#if multiple}
            <div class="holding-combobox-bulk-row" aria-label="Bulk holding selection">
              <button class="house-button house-button-secondary house-button-sm" disabled={allHoldingsSelected} onclick={selectAllHoldings} type="button">All</button>
              <button class="house-button house-button-secondary house-button-sm" disabled={noHoldingsSelected} onclick={clearSelectedHoldings} type="button">None</button>
            </div>
          {/if}
        </div>
      {/if}
      <div class="holding-combobox-options">
        {#each filteredHoldings as holding (holding.holdingID)}
          {#if multiple}
            <label class={classNames('holding-combobox-option', !holding.active && 'holding-combobox-option-alert', 'holding-combobox-option-check', isSelected(holding.holdingID) && 'holding-combobox-option-selected')}>
              <input checked={isSelected(holding.holdingID)} onchange={() => toggleHolding(holding.holdingID)} type="checkbox" value={holding.holdingID} />
              <span class="holding-combobox-option-copy">
                <span>{holdingLabel(holding)}</span>
                <small>{holdingMeta(holding)}</small>
              </span>
              <span aria-hidden="true" class="holding-combobox-check-icon">
                {#if isSelected(holding.holdingID)}
                  <svg viewBox="0 0 24 24"><path d="M20 6 9 17l-5-5" /></svg>
                {/if}
              </span>
            </label>
          {:else}
            <button
              aria-selected={isSelected(holding.holdingID)}
              class={classNames('holding-combobox-option', !holding.active && 'holding-combobox-option-alert', isSelected(holding.holdingID) && 'holding-combobox-option-selected')}
              onclick={() => chooseHolding(holding.holdingID)}
              role="option"
              type="button"
            >
              <span>{holdingLabel(holding)}</span>
              <small>{holdingMeta(holding)}</small>
            </button>
          {/if}
        {:else}
          <div class="holding-combobox-empty">No holdings match the selected account</div>
        {/each}
      </div>
      {#if multiple}
        <div class="holding-combobox-action-row">
          <button class="house-button house-button-primary house-button-sm" onclick={closeDropdown} type="button">OK</button>
        </div>
      {/if}
    </div>
  {/if}
</div>

<style>
  .holding-combobox {
    min-width: 0;
    position: relative;
    width: 100%;
  }

  .holding-combobox:has(.holding-combobox-menu) {
    z-index: 1000;
  }

  .holding-combobox-trigger {
    align-items: center;
    background: var(--panel);
    border: 1px solid var(--line);
    border-radius: var(--house-radius-sm);
    box-shadow: 0 1px 2px var(--surface-shadow);
    color: var(--ink);
    cursor: pointer;
    display: grid;
    font-size: 0.75rem;
    font-weight: 700;
    gap: 0.45rem;
    grid-template-columns: minmax(0, 1fr) auto;
    line-height: 1.15;
    min-height: var(--house-control-height);
    outline: none;
    padding: 0.25rem 0.5rem;
    text-align: left;
    width: 100%;
  }

  .holding-combobox-trigger::after {
    color: var(--muted);
    content: 'v';
    font-size: 0.625rem;
    font-weight: 800;
  }

  .holding-combobox-trigger[aria-expanded='true'] {
    border-color: var(--accent);
    box-shadow: 0 0 0 3px var(--focus-ring);
  }

  .holding-combobox-trigger[aria-expanded='true']::after {
    content: '^';
  }

  .holding-combobox-trigger:disabled {
    cursor: not-allowed;
    opacity: 0.65;
  }

  .holding-combobox-compact-brand {
    width: min(100%, 14rem);
  }

  .holding-combobox-compact-brand .holding-combobox-trigger {
    background: color-mix(in srgb, var(--brand-green) 16%, var(--panel));
    border-color: color-mix(in srgb, var(--brand-green) 54%, var(--line));
    color: var(--ink);
  }

  .holding-combobox-compact-brand .holding-combobox-trigger::after {
    color: var(--muted);
  }

  .holding-combobox-trigger span {
    min-width: 0;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
  }

  .holding-combobox-menu {
    background: var(--panel);
    border: 1px solid var(--line);
    border-radius: var(--house-radius-sm);
    box-shadow: 0 0.9rem 1.8rem color-mix(in srgb, var(--surface-shadow) 88%, transparent);
    box-sizing: border-box;
    display: grid;
    gap: 0.35rem;
    left: 0;
    padding: 0.4rem;
    position: absolute;
    top: calc(100% + 0.25rem);
    width: min(150%, calc(100vw - 1rem), 48rem);
    z-index: 1000;
  }

  .holding-combobox-compact-brand .holding-combobox-menu {
    border-color: color-mix(in srgb, var(--brand-green) 46%, var(--line));
  }

  .holding-combobox-options {
    display: grid;
    gap: 0.06rem;
    max-height: 15rem;
    overflow: auto;
  }

  .holding-combobox-search-row,
  .holding-combobox-bulk-row,
  .holding-combobox-action-row {
    display: flex;
    gap: 0.25rem;
  }

  .holding-combobox-search-row {
    align-items: center;
  }

  .holding-combobox-search-row input {
    min-width: 0;
  }

  .holding-combobox-bulk-row {
    align-items: center;
    flex: 0 0 auto;
  }

  .holding-combobox-action-row {
    border-top: 1px solid var(--line);
    justify-content: flex-end;
    padding-top: 0.45rem;
  }

  .holding-combobox-option {
    background: transparent;
    border: 1px solid transparent;
    border-radius: calc(var(--house-radius-sm) - 2px);
    color: var(--ink);
    cursor: pointer;
    display: grid;
    gap: 0.04rem;
    min-width: 0;
    padding: 0.22rem 0.36rem;
    text-align: left;
  }

  .holding-combobox-option:hover,
  .holding-combobox-option:focus-visible,
  .holding-combobox-option-selected {
    background: color-mix(in srgb, var(--accent-soft) 62%, transparent);
    border-color: color-mix(in srgb, var(--accent) 42%, var(--line));
    outline: none;
  }

  .holding-combobox-option-alert {
    background: color-mix(in srgb, var(--warning-soft) 42%, transparent);
    border-color: color-mix(in srgb, var(--warning) 44%, var(--line));
    color: var(--warning-text);
  }

  .holding-combobox-option-alert:hover,
  .holding-combobox-option-alert:focus-visible {
    background: color-mix(in srgb, var(--warning-soft) 78%, transparent);
    border-color: color-mix(in srgb, var(--warning) 68%, var(--line));
  }

  .holding-combobox-option-alert.holding-combobox-option-selected {
    background: color-mix(in srgb, var(--danger-soft) 82%, transparent);
    border-color: color-mix(in srgb, var(--danger) 76%, var(--line));
    color: var(--danger-text);
  }

  .holding-combobox-option span {
    font-size: 0.75rem;
    font-weight: 750;
    line-height: 1.12;
    overflow-wrap: anywhere;
  }

  .holding-combobox-option small,
  .holding-combobox-empty {
    color: color-mix(in srgb, var(--muted) 72%, var(--panel));
    font-size: 0.6rem;
    font-weight: 500;
    line-height: 1.08;
    overflow-wrap: anywhere;
  }

  .holding-combobox-option-alert small {
    color: var(--warning-strong);
  }

  .holding-combobox-option-alert.holding-combobox-option-selected small {
    color: var(--danger-strong);
  }

  .holding-combobox-option-check {
    align-items: start;
    grid-template-columns: minmax(0, 1fr) 1.25rem;
    position: relative;
  }

  .holding-combobox-option-check input {
    height: 1px;
    right: 0.55rem;
    opacity: 0;
    pointer-events: none;
    position: absolute;
    top: 0.7rem;
    width: 1px;
  }

  .holding-combobox-option-check:has(input:focus-visible) {
    border-color: var(--accent);
    box-shadow: 0 0 0 3px var(--focus-ring);
    outline: none;
  }

  .holding-combobox-check-icon {
    align-items: center;
    color: var(--accent-strong);
    display: inline-flex;
    height: 1.15rem;
    justify-content: center;
    margin-top: 0.03rem;
    width: 1.15rem;
  }

  .holding-combobox-option-alert .holding-combobox-check-icon {
    color: var(--warning-strong);
  }

  .holding-combobox-option-alert.holding-combobox-option-selected .holding-combobox-check-icon {
    color: var(--danger-strong);
  }

  .holding-combobox-check-icon svg {
    display: block;
    fill: none;
    height: 1rem;
    stroke: currentColor;
    stroke-linecap: round;
    stroke-linejoin: round;
    stroke-width: 3;
    width: 1rem;
  }

  .holding-combobox-option-copy {
    display: grid;
    gap: 0.05rem;
    min-width: 0;
  }

  .holding-combobox-empty {
    padding: 0.6rem 0.55rem;
  }

  .holding-combobox-menu .house-button {
    font-size: 0.6rem;
    min-height: 1.45rem;
    padding-inline: 0.35rem;
  }
</style>
