<script lang="ts" generics="T extends SelectValue">
  import { tick } from 'svelte';
  import { closeOnOutside } from '$lib/actions/dropdown';
  import { classNames } from './controls';
  import type { ComplexSelectOption, SelectValue } from './types';

  type Props = {
    ariaLabel?: string;
    class?: string;
    compactBrand?: boolean;
    confirmSelection?: boolean;
    disabled?: boolean;
    emptyText?: string;
    minimumSelections?: number;
    multiple?: boolean;
    name?: string;
    onchange?: (selection: T | T[] | undefined, event: Event) => void;
    onclose?: () => void;
    onopenchange?: (open: boolean) => void;
    open?: boolean;
    options: ComplexSelectOption<T>[];
    placeholder?: string;
    searchPlaceholder?: string;
    showClear?: boolean;
    showSelectAll?: boolean;
    summary?: string;
    value?: T;
    values?: T[];
  };

  let {
    ariaLabel = 'Options',
    class: className = '',
    compactBrand = false,
    confirmSelection = false,
    disabled = false,
    emptyText = 'No options match the search',
    minimumSelections = 0,
    multiple = false,
    name = '',
    onchange,
    onclose,
    onopenchange,
    open = $bindable(false),
    options,
    placeholder = 'Select',
    searchPlaceholder = 'Search',
    showClear = multiple,
    showSelectAll = multiple,
    summary,
    value = $bindable(),
    values = $bindable([])
  }: Props = $props();

  let filterText = $state('');
  let searchInput = $state<HTMLInputElement | null>(null);

  const selectedOption = $derived(options.find((option) => option.id === value) ?? null);
  const selectedOptions = $derived(options.filter((option) => values.includes(option.id)));
  const selectableOptions = $derived(options.filter((option) => !option.disabled));
  const allSelected = $derived(selectableOptions.length > 0 && selectableOptions.every((option) => values.includes(option.id)));
  const noneSelected = $derived(values.length === 0);
  const showFilter = $derived(multiple || options.length > 8);
  const filteredOptions = $derived(showFilter ? options.filter(optionMatchesFilter) : options);
  const displaySummary = $derived(summary ?? selectionSummary());

  function selectionSummary() {
    if (!multiple)
      return selectedOption ? selectedOption.summary ?? selectedOption.name : placeholder;

    if (!selectedOptions.length)
      return placeholder;

    if (selectedOptions.length === 1)
      return selectedOptions[0].summary ?? selectedOptions[0].name;

    return `${selectedOptions.length} selected`;
  }

  function optionMatchesFilter(option: ComplexSelectOption<T>) {
    const filter = filterText.trim().toLocaleLowerCase();
    if (!filter)
      return true;

    return [option.name, option.badge ?? '', option.meta ?? '', option.search ?? '', String(option.id)]
      .some((optionValue) => optionValue.toLocaleLowerCase().includes(filter));
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
    onopenchange?.(true);
    await tick();
    searchInput?.focus();
    searchInput?.select();
  }

  function toggleDropdown() {
    if (open)
      closeDropdown();
    else
      openDropdown();
  }

  function chooseOption(option: ComplexSelectOption<T>, event: Event) {
    if (option.disabled)
      return;

    if (multiple) {
      if (values.includes(option.id) && values.length <= minimumSelections)
        return;

      values = values.includes(option.id)
        ? values.filter((selectedID) => selectedID !== option.id)
        : [...values, option.id];
      onchange?.(values, event);
      return;
    }

    value = option.id;
    onchange?.(value, event);
    closeDropdown();
  }

  function selectAll(event: MouseEvent) {
    values = selectableOptions.map((option) => option.id);
    onchange?.(values, event);
  }

  function clearSelection(event: MouseEvent) {
    values = [];
    onchange?.(values, event);
  }

  function closeDropdown() {
    filterText = '';
    open = false;
    onopenchange?.(false);
    onclose?.();
  }

  function handleTriggerKeydown(event: KeyboardEvent) {
    if (event.key !== 'ArrowDown')
      return;

    event.preventDefault();
    openDropdown();
  }

  function handleSearchKeydown(event: KeyboardEvent) {
    if (event.key === 'Escape') {
      closeDropdown();
      return;
    }

    const firstOption = filteredOptions.find((option) => !option.disabled);
    if (event.key === 'Enter' && firstOption) {
      event.preventDefault();
      chooseOption(firstOption, event);
    }
  }

  function isSelected(optionID: T) {
    return multiple ? values.includes(optionID) : value === optionID;
  }
</script>

<div
  class={classNames('complex-select', multiple && 'complex-select-multiple', compactBrand && 'complex-select-compact-brand', className)}
  use:closeOnOutside={{ close: closeDropdown, enabled: open }}
>
  {#if name}
    {#if multiple}
      {#each values as selectedValue (selectedValue)}
        <input name={name} type="hidden" value={String(selectedValue)} />
      {/each}
    {:else if value !== undefined}
      <input name={name} type="hidden" value={String(value)} />
    {/if}
  {/if}

  <button
    aria-expanded={open}
    aria-haspopup="listbox"
    class="complex-select-trigger"
    {disabled}
    onkeydown={handleTriggerKeydown}
    onclick={toggleDropdown}
    type="button"
  >
    <span>{displaySummary}</span>
  </button>

  {#if open}
    <div aria-label={ariaLabel} aria-multiselectable={multiple || undefined} class="complex-select-menu" role="listbox">
      {#if showFilter}
        <div class="complex-select-search-row">
          <input
            aria-label={searchPlaceholder}
            bind:value={filterText}
            class="house-control house-control-md house-control-full"
            onkeydown={handleSearchKeydown}
            placeholder={searchPlaceholder}
            type="search"
            {@attach captureSearchInput}
          />
          {#if multiple && (showSelectAll || showClear)}
            <div aria-label="Bulk selection" class="complex-select-bulk-row">
              {#if showSelectAll}
                <button class="house-button house-button-secondary house-button-sm" disabled={allSelected} onclick={selectAll} type="button">All</button>
              {/if}
              {#if showClear}
                <button class="house-button house-button-secondary house-button-sm" disabled={noneSelected || minimumSelections > 0} onclick={clearSelection} type="button">None</button>
              {/if}
            </div>
          {/if}
        </div>
      {/if}
      <div class="complex-select-options">
        {#each filteredOptions as option (option.id)}
          <button
            aria-selected={isSelected(option.id)}
            class={classNames('complex-select-option', option.tone === 'alert' && 'complex-select-option-alert', isSelected(option.id) && 'complex-select-option-selected')}
            disabled={option.disabled}
            onclick={(event) => chooseOption(option, event)}
            role="option"
            type="button"
          >
            <span class="complex-select-option-copy">
              <span class="complex-select-option-heading">
                <span>{option.name}</span>
                {#if option.badge}
                  <small class:complex-select-option-badge-positive={option.badgeTone === 'positive'}>{option.badge}</small>
                {/if}
              </span>
              {#if option.meta}
                <small>{option.meta}</small>
              {/if}
            </span>
            <span aria-hidden="true" class="complex-select-check-icon">
              {#if isSelected(option.id)}
                <svg viewBox="0 0 24 24"><path d="M20 6 9 17l-5-5" /></svg>
              {/if}
            </span>
          </button>
        {:else}
          <div class="complex-select-empty">{emptyText}</div>
        {/each}
      </div>
      {#if multiple && confirmSelection}
        <div class="complex-select-action-row">
          <button class="house-button house-button-primary house-button-sm" onclick={closeDropdown} type="button">OK</button>
        </div>
      {/if}
    </div>
  {/if}
</div>

<style>
  .complex-select {
    min-width: 0;
    position: relative;
    width: 100%;
  }

  .complex-select:has(.complex-select-menu) {
    z-index: 1000;
  }

  .complex-select-trigger {
    align-items: center;
    background: var(--panel);
    border: 1px solid var(--line);
    border-radius: var(--house-radius-sm);
    box-shadow: 0 1px 2px var(--surface-shadow);
    color: var(--ink);
    cursor: pointer;
    display: grid;
    font-size: var(--control-font-sm);
    font-weight: 700;
    gap: 0.45rem;
    grid-template-columns: minmax(0, 1fr) auto;
    min-height: var(--control-h-md);
    outline: none;
    padding: 0.25rem var(--control-pad-x-sm);
    text-align: left;
    width: 100%;
    line-height: 1.15;
  }

  .complex-select-trigger::after {
    color: var(--muted);
    content: 'v';
    font-size: 0.625rem;
    font-weight: 800;
  }

  .complex-select-trigger[aria-expanded='true'] {
    border-color: var(--accent);
    box-shadow: 0 0 0 3px var(--focus-ring);
  }

  .complex-select-trigger[aria-expanded='true']::after {
    content: '^';
  }

  .complex-select-trigger:disabled {
    cursor: not-allowed;
    opacity: 0.65;
  }

  .complex-select-compact-brand {
    width: min(100%, 14rem);
  }

  .complex-select-compact-brand .complex-select-trigger {
    background: color-mix(in srgb, var(--brand-green) 16%, var(--panel));
    border-color: color-mix(in srgb, var(--brand-green) 54%, var(--line));
    color: var(--ink);
  }

  .complex-select-trigger span {
    min-width: 0;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
  }

  .complex-select-menu {
    background: var(--panel);
    border: 1px solid var(--line);
    border-radius: var(--house-radius-sm);
    box-sizing: border-box;
    box-shadow: 0 0.9rem 1.8rem color-mix(in srgb, var(--surface-shadow) 88%, transparent);
    display: grid;
    gap: 0.35rem;
    left: 0;
    padding: 0.4rem;
    position: absolute;
    top: calc(100% + 0.25rem);
    width: min(150%, calc(100vw - 1rem), 48rem);
    z-index: 1000;
  }

  .complex-select-compact-brand .complex-select-menu {
    border-color: color-mix(in srgb, var(--brand-green) 46%, var(--line));
  }

  .complex-select-options {
    display: grid;
    gap: 0.06rem;
    max-height: 15rem;
    overflow: auto;
  }

  .complex-select-search-row,
  .complex-select-bulk-row,
  .complex-select-action-row {
    display: flex;
    gap: 0.25rem;
  }

  .complex-select-search-row {
    align-items: center;
  }

  .complex-select-search-row input {
    min-width: 0;
  }

  .complex-select-bulk-row {
    align-items: center;
    flex: 0 0 auto;
  }

  .complex-select-action-row {
    border-top: 1px solid var(--line);
    justify-content: flex-end;
    padding-top: 0.45rem;
  }

  .complex-select-option {
    background: transparent;
    border: 1px solid transparent;
    border-radius: calc(var(--house-radius-sm) - 2px);
    color: var(--ink);
    cursor: pointer;
    display: grid;
    grid-template-columns: minmax(0, 1fr) 1.15rem;
    gap: 0.04rem;
    min-width: 0;
    padding: 0.22rem 0.36rem;
    text-align: left;
  }

  .complex-select-option:hover,
  .complex-select-option:focus-visible,
  .complex-select-option-selected {
    background: color-mix(in srgb, var(--accent-soft) 62%, transparent);
    border-color: color-mix(in srgb, var(--accent) 42%, var(--line));
    outline: none;
  }

  .complex-select-option:disabled {
    cursor: not-allowed;
    opacity: 0.55;
  }

  .complex-select-option-alert {
    background: color-mix(in srgb, var(--warning-soft) 42%, transparent);
    border-color: color-mix(in srgb, var(--warning) 44%, var(--line));
    color: var(--warning-text);
  }

  .complex-select-option-alert:hover,
  .complex-select-option-alert:focus-visible {
    background: color-mix(in srgb, var(--warning-soft) 78%, transparent);
    border-color: color-mix(in srgb, var(--warning) 68%, var(--line));
  }

  .complex-select-option-alert.complex-select-option-selected {
    background: color-mix(in srgb, var(--danger-soft) 82%, transparent);
    border-color: color-mix(in srgb, var(--danger) 76%, var(--line));
    color: var(--danger-text);
  }

  .complex-select-option-copy {
    display: grid;
    gap: 0.04rem;
    min-width: 0;
  }

  .complex-select-option-heading {
    align-items: center;
    display: flex;
    gap: 0.3rem;
    min-width: 0;
  }

  .complex-select-option-heading > span {
    font-size: var(--control-font-sm);
    font-weight: 750;
    line-height: 1.12;
    overflow-wrap: anywhere;
  }

  .complex-select-option-heading > small {
    border: 1px solid var(--line);
    border-radius: 999px;
    color: var(--muted);
    flex: 0 0 auto;
    font-size: 0.55rem;
    font-weight: 750;
    line-height: 1;
    padding: 0.12rem 0.3rem;
  }

  .complex-select-option-heading > .complex-select-option-badge-positive {
    background: color-mix(in srgb, var(--brand-green) 12%, var(--panel));
    border-color: color-mix(in srgb, var(--brand-green) 48%, var(--line));
    color: var(--brand-green);
  }

  .complex-select-check-icon {
    align-items: center;
    color: var(--accent-strong);
    display: inline-flex;
    height: 1.15rem;
    justify-content: center;
    margin-top: 0.03rem;
    width: 1.15rem;
  }

  .complex-select-option-alert .complex-select-check-icon {
    color: var(--warning-strong);
  }

  .complex-select-option-alert.complex-select-option-selected .complex-select-check-icon {
    color: var(--danger-strong);
  }

  .complex-select-check-icon svg {
    display: block;
    fill: none;
    height: 1rem;
    stroke: currentColor;
    stroke-linecap: round;
    stroke-linejoin: round;
    stroke-width: 3;
    width: 1rem;
  }

  .complex-select-option-copy > small,
  .complex-select-empty {
    color: color-mix(in srgb, var(--muted) 72%, var(--panel));
    font-size: 0.6rem;
    font-weight: 500;
    line-height: 1.08;
    overflow-wrap: anywhere;
  }

  .complex-select-option-alert .complex-select-option-copy > small {
    color: var(--warning-strong);
  }

  .complex-select-option-alert.complex-select-option-selected .complex-select-option-copy > small {
    color: var(--danger-strong);
  }

  .complex-select-empty {
    padding: 0.6rem 0.55rem;
  }

  .complex-select-menu .house-button {
    font-size: 0.6rem;
    min-height: 1.45rem;
    padding-inline: 0.35rem;
  }
</style>
