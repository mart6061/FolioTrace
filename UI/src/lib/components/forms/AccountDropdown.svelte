<script lang="ts">
  import { tick } from 'svelte';
  import { closeOnOutside } from '$lib/actions/dropdown';
  import type { Account } from '$lib/types';
  import { classNames } from './controls';

  type Props = {
    accounts: Account[];
    class?: string;
    compactBrand?: boolean;
    disabled?: boolean;
    multiple?: boolean;
    name?: string;
    nameOnlySummary?: boolean;
    placeholder?: string;
    selectedAccountID?: string;
    selectedAccountIDs?: string[];
  };

  let {
    accounts,
    class: className = '',
    compactBrand = false,
    disabled = false,
    multiple = false,
    name = '',
    nameOnlySummary = false,
    placeholder = 'Select account',
    selectedAccountID = $bindable(''),
    selectedAccountIDs = $bindable([])
  }: Props = $props();

  let filterText = $state('');
  let open = $state(false);
  let dropdownElement = $state<HTMLElement | null>(null);
  let searchInput = $state<HTMLInputElement | null>(null);

  const sortedAccounts = $derived([...accounts].sort((left, right) => left.displayOrder - right.displayOrder || left.name.localeCompare(right.name)));
  const showFilter = $derived(multiple || sortedAccounts.length > 8);
  const filteredAccounts = $derived(showFilter ? sortedAccounts.filter(accountMatchesFilter) : sortedAccounts);
  const selectedAccount = $derived(sortedAccounts.find((account) => account.accountID === selectedAccountID) ?? null);
  const selectedAccounts = $derived(sortedAccounts.filter((account) => selectedAccountIDs.includes(account.accountID)));
  const allAccountsSelected = $derived(sortedAccounts.length > 0 && sortedAccounts.every((account) => selectedAccountIDs.includes(account.accountID)));
  const noAccountsSelected = $derived(selectedAccountIDs.length === 0);
  const summary = $derived(multiple ? multiSummary() : selectedAccount ? selectedSummary(selectedAccount) : placeholder);

  function accountMatchesFilter(account: Account) {
    const filter = filterText.trim().toLocaleLowerCase();
    if (!filter)
      return true;

    return [account.name, account.formalName, account.bookCurrency, account.bookCostBasis, account.active ? 'active' : 'inactive', account.accountID]
      .some((value) => value.toLocaleLowerCase().includes(filter));
  }

  function accountLabel(account: Account) {
    return `${account.name} - ${account.formalName} (${account.bookCurrency}, ${account.bookCostBasis}, ${account.active ? 'Active' : 'Inactive'})`;
  }

  function selectedSummary(account: Account) {
    return nameOnlySummary ? account.name : accountLabel(account);
  }

  function accountMeta(account: Account) {
    return `${account.formalName} - ${account.bookCurrency} - ${account.bookCostBasis} - ${account.active ? 'Active' : 'Inactive'}`;
  }

  function multiSummary() {
    if (!selectedAccounts.length)
      return placeholder;

    if (selectedAccounts.length === 1)
      return selectedSummary(selectedAccounts[0]);

    return `${selectedAccounts.length} selected`;
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

  function chooseAccount(accountID: string) {
    selectedAccountID = accountID;
    filterText = '';
    open = false;
  }

  function toggleAccount(accountID: string) {
    selectedAccountIDs = selectedAccountIDs.includes(accountID)
      ? selectedAccountIDs.filter((selectedID) => selectedID !== accountID)
      : [...selectedAccountIDs, accountID];
  }

  function selectAllAccounts() {
    selectedAccountIDs = sortedAccounts.map((account) => account.accountID);
  }

  function clearSelectedAccounts() {
    selectedAccountIDs = [];
  }

  function closeDropdown() {
    filterText = '';
    open = false;
  }

  function isSelected(accountID: string) {
    return multiple ? selectedAccountIDs.includes(accountID) : selectedAccountID === accountID;
  }

  function handleSearchKeydown(event: KeyboardEvent) {
    if (event.key === 'Escape') {
      open = false;
      return;
    }

    if (event.key === 'Enter' && filteredAccounts[0]) {
      event.preventDefault();
      if (multiple)
        toggleAccount(filteredAccounts[0].accountID);
      else
        chooseAccount(filteredAccounts[0].accountID);
    }
  }

</script>

<div
  class={classNames('account-combobox', multiple && 'account-combobox-multi', compactBrand && 'account-combobox-compact-brand', className)}
  use:closeOnOutside={{ close: closeDropdown, enabled: open }}
  {@attach captureDropdown}
>
  {#if name}
    {#if multiple}
      {#each selectedAccountIDs as accountID (accountID)}
        <input name={name} type="hidden" value={accountID} />
      {/each}
    {:else}
      <input name={name} type="hidden" value={selectedAccountID} />
    {/if}
  {/if}

  <button
    aria-expanded={open}
    aria-haspopup={multiple ? 'true' : 'listbox'}
    class="account-combobox-trigger"
    {disabled}
    onclick={toggleDropdown}
    type="button"
  >
    <span>{summary}</span>
  </button>

  {#if open}
    <div aria-label="Accounts" class="account-combobox-menu" role={multiple ? 'group' : 'listbox'}>
      {#if showFilter}
        <div class="account-combobox-search-row">
          <input
            bind:value={filterText}
            class="house-control house-control-md house-control-full"
            onkeydown={handleSearchKeydown}
            placeholder="Search accounts"
            type="search"
            {@attach captureSearchInput}
          />
          {#if multiple}
            <div class="account-combobox-bulk-row" aria-label="Bulk account selection">
              <button class="house-button house-button-secondary house-button-sm" disabled={allAccountsSelected} onclick={selectAllAccounts} type="button">All</button>
              <button class="house-button house-button-secondary house-button-sm" disabled={noAccountsSelected} onclick={clearSelectedAccounts} type="button">None</button>
            </div>
          {/if}
        </div>
      {/if}
      <div class="account-combobox-options">
        {#each filteredAccounts as account (account.accountID)}
          {#if multiple}
            <label class={classNames('account-combobox-option', 'account-combobox-option-check', isSelected(account.accountID) && 'account-combobox-option-selected')}>
              <input checked={isSelected(account.accountID)} onchange={() => toggleAccount(account.accountID)} type="checkbox" value={account.accountID} />
              <span aria-hidden="true" class="account-combobox-check-icon">
                {#if isSelected(account.accountID)}
                  <svg viewBox="0 0 24 24"><path d="M20 6 9 17l-5-5" /></svg>
                {/if}
              </span>
              <span>
                <span>{account.name}</span>
                <small>{accountMeta(account)}</small>
              </span>
            </label>
          {:else}
            <button
              aria-selected={isSelected(account.accountID)}
              class={classNames('account-combobox-option', isSelected(account.accountID) && 'account-combobox-option-selected')}
              onclick={() => chooseAccount(account.accountID)}
              role="option"
              type="button"
            >
              <span>{account.name}</span>
              <small>{accountMeta(account)}</small>
            </button>
          {/if}
        {:else}
          <div class="account-combobox-empty">No accounts match the search</div>
        {/each}
      </div>
      {#if multiple}
        <div class="account-combobox-action-row">
          <button class="house-button house-button-primary house-button-sm" onclick={closeDropdown} type="button">OK</button>
        </div>
      {/if}
    </div>
  {/if}
</div>

<style>
  .account-combobox {
    min-width: 0;
    position: relative;
    width: 100%;
  }

  .account-combobox:has(.account-combobox-menu) {
    z-index: 1000;
  }

  .account-combobox-trigger {
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

  .account-combobox-trigger::after {
    color: var(--muted);
    content: 'v';
    font-size: 0.625rem;
    font-weight: 800;
  }

  .account-combobox-trigger[aria-expanded='true'] {
    border-color: var(--accent);
    box-shadow: 0 0 0 3px var(--focus-ring);
  }

  .account-combobox-trigger[aria-expanded='true']::after {
    content: '^';
  }

  .account-combobox-trigger:disabled {
    cursor: not-allowed;
    opacity: 0.65;
  }

  .account-combobox-compact-brand {
    width: min(100%, 14rem);
  }

  .account-combobox-compact-brand .account-combobox-trigger {
    background: color-mix(in srgb, var(--brand-green) 16%, var(--panel));
    border-color: color-mix(in srgb, var(--brand-green) 54%, var(--line));
    color: var(--ink);
  }

  .account-combobox-compact-brand .account-combobox-trigger::after {
    color: var(--muted);
  }

  .account-combobox-trigger span {
    min-width: 0;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
  }

  .account-combobox-menu {
    background: var(--panel);
    border: 1px solid var(--line);
    border-radius: var(--house-radius-sm);
    box-shadow: 0 0.9rem 1.8rem color-mix(in srgb, var(--surface-shadow) 88%, transparent);
    display: grid;
    gap: 0.35rem;
    left: 0;
    padding: 0.4rem;
    position: absolute;
    top: calc(100% + 0.25rem);
    width: min(100%, 48rem);
    z-index: 1000;
  }

  .account-combobox-compact-brand .account-combobox-menu {
    border-color: color-mix(in srgb, var(--brand-green) 46%, var(--line));
    width: 100%;
  }

  .account-combobox-options {
    display: grid;
    gap: 0.06rem;
    max-height: 15rem;
    overflow: auto;
  }

  .account-combobox-search-row,
  .account-combobox-bulk-row,
  .account-combobox-action-row {
    display: flex;
    gap: 0.25rem;
  }

  .account-combobox-search-row {
    align-items: center;
  }

  .account-combobox-search-row input {
    min-width: 0;
  }

  .account-combobox-bulk-row {
    align-items: center;
    flex: 0 0 auto;
  }

  .account-combobox-action-row {
    border-top: 1px solid var(--line);
    justify-content: flex-end;
    padding-top: 0.45rem;
  }

  .account-combobox-option {
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

  .account-combobox-option:hover,
  .account-combobox-option:focus-visible,
  .account-combobox-option-selected {
    background: color-mix(in srgb, var(--accent-soft) 62%, transparent);
    border-color: color-mix(in srgb, var(--accent) 42%, var(--line));
    outline: none;
  }

  .account-combobox-option span {
    font-size: 0.75rem;
    font-weight: 750;
    line-height: 1.12;
    overflow-wrap: anywhere;
  }

  .account-combobox-option small,
  .account-combobox-empty {
    color: color-mix(in srgb, var(--muted) 72%, var(--panel));
    font-size: 0.6rem;
    font-weight: 500;
    line-height: 1.08;
    overflow-wrap: anywhere;
  }

  .account-combobox-option-check {
    align-items: start;
    grid-template-columns: 1.25rem minmax(0, 1fr);
    position: relative;
  }

  .account-combobox-option-check input {
    height: 1px;
    left: 0.55rem;
    opacity: 0;
    pointer-events: none;
    position: absolute;
    top: 0.7rem;
    width: 1px;
  }

  .account-combobox-option-check:has(input:focus-visible) {
    border-color: var(--accent);
    box-shadow: 0 0 0 3px var(--focus-ring);
    outline: none;
  }

  .account-combobox-check-icon {
    align-items: center;
    color: var(--accent-strong);
    display: inline-flex;
    height: 1.15rem;
    justify-content: center;
    margin-top: 0.03rem;
    width: 1.15rem;
  }

  .account-combobox-check-icon svg {
    display: block;
    fill: none;
    height: 1rem;
    stroke: currentColor;
    stroke-linecap: round;
    stroke-linejoin: round;
    stroke-width: 3;
    width: 1rem;
  }

  .account-combobox-option-check > span {
    display: grid;
    gap: 0.05rem;
    min-width: 0;
  }

  .account-combobox-empty {
    padding: 0.6rem 0.55rem;
  }

  .account-combobox-menu .house-button {
    font-size: 0.6rem;
    min-height: 1.45rem;
    padding-inline: 0.35rem;
  }
</style>
