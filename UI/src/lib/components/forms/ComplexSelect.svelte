<script lang="ts">
  import { tick } from 'svelte';
  import { closeOnOutside } from '$lib/actions/dropdown';
  import { classNames } from './controls';
  import type { ComplexSelectOption } from './types';

  type Props = {
    class?: string;
    compactBrand?: boolean;
    disabled?: boolean;
    name?: string;
    options: ComplexSelectOption[];
    placeholder?: string;
    value?: string;
  };

  let {
    class: className = '',
    compactBrand = false,
    disabled = false,
    name = '',
    options,
    placeholder = 'Select',
    value = $bindable('')
  }: Props = $props();

  let filterText = $state('');
  let open = $state(false);
  let dropdownElement = $state<HTMLElement | null>(null);
  let searchInput = $state<HTMLInputElement | null>(null);

  const selectedOption = $derived(options.find((option) => option.id === value) ?? null);
  const showFilter = $derived(options.length > 8);
  const filteredOptions = $derived(showFilter ? options.filter(optionMatchesFilter) : options);
  const summary = $derived(selectedOption ? selectedOption.name : placeholder);

  function optionMatchesFilter(option: ComplexSelectOption) {
    const filter = filterText.trim().toLocaleLowerCase();
    if (!filter)
      return true;

    return [option.name, option.meta ?? '', option.search ?? '', option.id]
      .some((optionValue) => optionValue.toLocaleLowerCase().includes(filter));
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

  function chooseOption(optionID: string) {
    value = optionID;
    filterText = '';
    open = false;
  }

  function handleSearchKeydown(event: KeyboardEvent) {
    if (event.key === 'Escape') {
      open = false;
      return;
    }

    if (event.key === 'Enter' && filteredOptions[0]) {
      event.preventDefault();
      chooseOption(filteredOptions[0].id);
    }
  }

  function closeDropdown() {
    open = false;
  }
</script>

<div
  class={classNames('complex-select', compactBrand && 'complex-select-compact-brand', className)}
  use:closeOnOutside={{ close: closeDropdown, enabled: open }}
  {@attach captureDropdown}
>
  {#if name}
    <input name={name} type="hidden" {value} />
  {/if}

  <button
    aria-expanded={open}
    aria-haspopup="listbox"
    class="complex-select-trigger"
    {disabled}
    onclick={toggleDropdown}
    type="button"
  >
    <span>{summary}</span>
  </button>

  {#if open}
    <div aria-label="Options" class="complex-select-menu" role="listbox">
      {#if showFilter}
        <input
          bind:value={filterText}
          class="house-control house-control-md house-control-full"
          onkeydown={handleSearchKeydown}
          placeholder="Search"
          type="search"
          {@attach captureSearchInput}
        />
      {/if}
      <div class="complex-select-options">
        {#each filteredOptions as option (option.id)}
          <button
            aria-selected={option.id === value}
            class={classNames('complex-select-option', option.id === value && 'complex-select-option-selected')}
            onclick={() => chooseOption(option.id)}
            role="option"
            type="button"
          >
            <span>{option.name}</span>
            {#if option.meta}
              <small>{option.meta}</small>
            {/if}
          </button>
        {:else}
          <div class="complex-select-empty">No options match the search</div>
        {/each}
      </div>
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
    font-size: 0.75rem;
    font-weight: 700;
    gap: 0.45rem;
    grid-template-columns: minmax(0, 1fr) auto;
    min-height: var(--house-control-height);
    outline: none;
    padding: 0.25rem 0.5rem;
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

  .complex-select-compact-brand .complex-select-trigger::after {
    color: var(--muted);
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

  .complex-select-option {
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

  .complex-select-option:hover,
  .complex-select-option:focus-visible,
  .complex-select-option-selected {
    background: color-mix(in srgb, var(--accent-soft) 62%, transparent);
    border-color: color-mix(in srgb, var(--accent) 42%, var(--line));
    outline: none;
  }

  .complex-select-option span {
    font-size: 0.75rem;
    font-weight: 750;
    line-height: 1.12;
    overflow-wrap: anywhere;
  }

  .complex-select-option small,
  .complex-select-empty {
    color: color-mix(in srgb, var(--muted) 72%, var(--panel));
    font-size: 0.6rem;
    font-weight: 500;
    line-height: 1.08;
    overflow-wrap: anywhere;
  }

  .complex-select-empty {
    padding: 0.6rem 0.55rem;
  }
</style>
