<script lang="ts">
  import { tick } from 'svelte';
  import BookmarkButton from '$lib/components/BookmarkButton.svelte';
  import DateTimeInput from '$lib/components/DateTimeInput.svelte';
  import { Button, Field, Select, TextInput, Toggle } from '$lib/components/forms';
  import { formatDisplayDateTime, formatTableDateTime, startOfDayForInput } from '$lib/dates';
  import type { Account, Holding, HoldingKind, Instrument, ProfitLossMethod, ValuationSetting } from '$lib/types';

  let { data, form } = $props();

  const HOLDING_KINDS: HoldingKind[] = [
    'CashInvestable',
    'NominalInflow',
    'NominalOutflow',
    'NominalInSpecieIn',
    'NominalInSpecieOut',
    'NominalFeesCustodian',
    'NominalFeesAdministrator',
    'NominalFeesBank',
    'NominalIncome',
    'NominalInterest',
    'CashDebt',
    'CashNonInvestable',
    'PositionCash',
    'PositionAsset',
    'PositionMemo'
  ];
  const NOMINAL_HOLDING_KINDS: HoldingKind[] = ['NominalInflow', 'NominalOutflow', 'NominalInSpecieIn', 'NominalInSpecieOut', 'NominalFeesCustodian', 'NominalFeesAdministrator', 'NominalFeesBank', 'NominalIncome', 'NominalInterest'];
  const BANK_HOLDING_KINDS: HoldingKind[] = ['CashDebt', 'CashInvestable', 'CashNonInvestable'];
  const profitLossMethodOptions: { value: ProfitLossMethod; label: string }[] = [
    { value: 'FIFO', label: 'FIFO' },
    { value: 'LIFO', label: 'LIFO' },
    { value: 'RunningAverage', label: 'Weighted average' }
  ];

  let selectedAccountID = $state('');
  let filterText = $state('');
  let accountPickerOpen = $state(false);
  let accountPickerElement = $state<HTMLElement | null>(null);
  let accountSearchInput = $state<HTMLInputElement | null>(null);
  let createActive = $state(true);
  let newHoldingActive = $state(true);
  let newHoldingDefault = $state(false);
  let newHoldingKind = $state<HoldingKind>('CashInvestable');

  const accounts = $derived(data.accounts?.items ?? []);
  const currencies = $derived([...(data.currencies?.items ?? [])].sort((left, right) => left.alphabeticCode.localeCompare(right.alphabeticCode)));
  const holdings = $derived(data.holdings?.items ?? []);
  const instruments = $derived([...(data.instruments?.items ?? [])].sort((left, right) => left.name.localeCompare(right.name)));
  const valuationSettings = $derived(data.valuationSettings?.items ?? []);
  const transactionAccountIDs = $derived(new Set(data.transactionAccountIDs ?? []));
  const defaultEventDate = $derived(startOfDayForInput(data.valuationDate));

  const filteredAccounts = $derived(
    accounts.filter((account) => {
      const filter = filterText.trim().toLocaleLowerCase();
      if (!filter)
        return true;

      return [account.name, account.formalName, account.bookCurrency, profitLossMethodLabel(account.bookCostBasis), account.active ? 'active' : 'inactive']
        .some((value) => value.toLocaleLowerCase().includes(filter));
    })
  );

  const selectedAccount = $derived(
    accounts.find((account) => account.accountID === selectedAccountID) ?? accounts[0] ?? null
  );
  const selectedHoldings = $derived(selectedAccount ? holdingsForAccount(selectedAccount.accountID) : []);
  const selectedHasTransactions = $derived(Boolean(selectedAccount && transactionAccountIDs.has(selectedAccount.accountID)));
  const selectedMissingNominals = $derived(selectedAccount ? missingNominalKinds(selectedHoldings) : []);
  const selectedHasCapitalAccount = $derived(Boolean(selectedAccount && hasCapitalAccount(selectedAccount, selectedHoldings)));
  const needsRequiredHoldings = $derived(selectedMissingNominals.length > 0 || !selectedHasCapitalAccount);

  function holdingsForAccount(accountID: string) {
    return holdings
      .filter((holding) => holding.accountID === accountID)
      .sort((left, right) => holdingKindLabel(left.holdingKind).localeCompare(holdingKindLabel(right.holdingKind)) || left.name.localeCompare(right.name));
  }

  function holdingKindLabel(kind: HoldingKind | string) {
    return kind
      .replace(/([a-z])([A-Z])/g, '$1 $2')
      .replace(/^Nominal /, '')
      .replace('Cash Investable', 'Cash Investable');
  }

  function profitLossMethodLabel(value: ProfitLossMethod | undefined) {
    return profitLossMethodOptions.find((option) => option.value === value)?.label ?? 'FIFO';
  }

  function instrumentName(instrumentID: string) {
    const instrument = instruments.find((item) => item.instrumentID === instrumentID);
    return instrument ? `${instrument.name} (${instrument.priceCurrency})` : instrumentID;
  }

  function hasCapitalAccount(account: Account, accountHoldings: Holding[]) {
    return accountHoldings.some((holding) => {
      if (holding.holdingKind !== 'CashInvestable')
        return false;

      return instruments.find((instrument) => instrument.instrumentID === holding.instrumentID)?.priceCurrency === account.bookCurrency;
    });
  }

  function missingNominalKinds(accountHoldings: Holding[]) {
    return NOMINAL_HOLDING_KINDS.filter((kind) => !accountHoldings.some((holding) => holding.holdingKind === kind));
  }

  function allocationHasAccount(allocation: ValuationSetting, accountID: string) {
    return allocation.accountIDs.includes(accountID);
  }

  function isBankHoldingKind(kind: HoldingKind) {
    return BANK_HOLDING_KINDS.includes(kind);
  }

  function accountOptionLabel(account: Account) {
    return `${account.name} - ${account.formalName} (${account.bookCurrency}, ${profitLossMethodLabel(account.bookCostBasis)}, ${account.active ? 'Active' : 'Inactive'})`;
  }

  const selectedAccountLabel = $derived(selectedAccount ? accountOptionLabel(selectedAccount) : 'Select account');

  function captureAccountPicker(element: Element) {
    const node = element as HTMLElement;
    accountPickerElement = node;

    return () => {
      if (accountPickerElement === node)
        accountPickerElement = null;
    };
  }

  function captureAccountSearchInput(element: Element) {
    const node = element as HTMLInputElement;
    accountSearchInput = node;

    return () => {
      if (accountSearchInput === node)
        accountSearchInput = null;
    };
  }

  async function openAccountPicker() {
    accountPickerOpen = true;
    await tick();
    accountSearchInput?.focus();
    accountSearchInput?.select();
  }

  function toggleAccountPicker() {
    if (accountPickerOpen) {
      accountPickerOpen = false;
      return;
    }

    openAccountPicker();
  }

  function chooseAccount(accountID: string) {
    selectedAccountID = accountID;
    filterText = '';
    accountPickerOpen = false;
  }

  function handleAccountSearchKeydown(event: KeyboardEvent) {
    if (event.key === 'Escape') {
      accountPickerOpen = false;
      return;
    }

    if (event.key === 'Enter' && filteredAccounts[0]) {
      event.preventDefault();
      chooseAccount(filteredAccounts[0].accountID);
    }
  }

  function handleDocumentClick(event: MouseEvent) {
    const target = event.target;

    if (!accountPickerOpen || !(target instanceof Node) || !accountPickerElement)
      return;

    if (!accountPickerElement.contains(target))
      accountPickerOpen = false;
  }
</script>

<svelte:document onclick={handleDocumentClick} />

<svelte:head>
  <title>Account Tools | FolioTrace</title>
</svelte:head>

<main class="min-h-screen">
  <section class="page-header">
    <div class="page-container">
      <div class="page-header-content">
        <div class="page-header-main">
          <p class="page-kicker">System / Data / Configuration</p>
          <div class="page-title-row account-title-row">
            <h1 class="page-title">Account Tools</h1>
            <div class="account-bookmark-slot">
              <BookmarkButton />
            </div>
          </div>
          <p class="page-subtitle">Manage account setup, active state, required holdings, defaults, and asset allocation links.</p>
        </div>
        <div class="page-header-aside">
          <div class="page-header-summary">{accounts.length} accounts configured</div>
        </div>
      </div>
    </div>
  </section>

  <section class="page-container page-section account-tools-page">
    {#if form?.message}
      <p class={form.status === 'success' ? 'success-message' : 'failure-message'}>
        {form.message}
      </p>
    {/if}

    {#if data.error}
      <p class="failure-message">{data.error}</p>
    {/if}

    <div class="account-tools-layout">
      <section class="data-panel account-picker-panel">
        <div class="panel-heading">
          <div>
            <h2>Select Account</h2>
            <p>{filteredAccounts.length} of {accounts.length} accounts match</p>
          </div>
        </div>
        <div class="account-picker-grid">
          <Field label="Account">
            <div class="account-combobox" {@attach captureAccountPicker}>
              <button
                aria-expanded={accountPickerOpen}
                aria-haspopup="listbox"
                class="account-combobox-trigger"
                onclick={toggleAccountPicker}
                type="button"
              >
                <span>{selectedAccountLabel}</span>
              </button>

              {#if accountPickerOpen}
                <div class="account-combobox-menu" role="listbox" aria-label="Accounts">
                  <input
                    bind:value={filterText}
                    class="house-control house-control-md house-control-full"
                    onkeydown={handleAccountSearchKeydown}
                    placeholder="Search name, formal name, currency, basis, status"
                    type="search"
                    {@attach captureAccountSearchInput}
                  />
                  <div class="account-combobox-options">
                    {#each filteredAccounts as account (account.accountID)}
                      <button
                        aria-selected={account.accountID === selectedAccount?.accountID}
                        class:account-combobox-option-selected={account.accountID === selectedAccount?.accountID}
                        class="account-combobox-option"
                        onclick={() => chooseAccount(account.accountID)}
                        role="option"
                        type="button"
                      >
                        <span>{account.name}</span>
                        <small>{account.formalName} - {account.bookCurrency} - {profitLossMethodLabel(account.bookCostBasis)} - {account.active ? 'Active' : 'Inactive'}</small>
                      </button>
                    {:else}
                      <div class="account-combobox-empty">No accounts match the search</div>
                    {/each}
                  </div>
                </div>
              {/if}
            </div>
          </Field>
        </div>
      </section>

      <div class="account-workspace">
      {#if selectedAccount}
        <section class="data-panel selected-account-panel">
          <div class="panel-heading">
            <div>
              <h2>{selectedAccount.name}</h2>
              <p>Start date {formatDisplayDateTime(selectedAccount.valuationDateTime)} - As of {data.auditDateTime ? formatDisplayDateTime(selectedAccount.asOfDateTime) : 'now'}</p>
            </div>
            <form method="POST" action="?/setAccountActive">
              <input type="hidden" name="accountID" value={selectedAccount.accountID} />
              <input type="hidden" name="accountName" value={selectedAccount.name} />
              <input type="hidden" name="eventDateTime" value={defaultEventDate} />
              <input type="hidden" name="active" value={selectedAccount.active ? 'false' : 'true'} />
              <Button type="submit" variant={selectedAccount.active ? 'danger' : 'primary'}>
                {selectedAccount.active ? 'Deactivate' : 'Activate'}
              </Button>
            </form>
          </div>

          <div class="account-detail-grid">
            <div>
              <span>Status</span>
              <strong>{selectedAccount.active ? 'Active' : 'Inactive'}</strong>
            </div>
            <div>
              <span>Book currency</span>
              <strong>{selectedAccount.bookCurrency}</strong>
            </div>
            <div>
              <span>Book cost basis</span>
              <strong>{profitLossMethodLabel(selectedAccount.bookCostBasis)}</strong>
            </div>
            <div>
              <span>Holdings</span>
              <strong>{selectedHoldings.length}</strong>
            </div>
            <div>
              <span>Required setup</span>
              <strong>{needsRequiredHoldings ? 'Needs attention' : 'Complete'}</strong>
            </div>
          </div>

          <form class="tool-form" method="POST" action="?/modifyAccount">
            <input type="hidden" name="accountID" value={selectedAccount.accountID} />
            <div class="form-grid">
              <Field label="Change date" required>
                <DateTimeInput fullWidth name="eventDateTime" required value={defaultEventDate} />
              </Field>
              <Field label="Book currency">
                <TextInput disabled fullWidth value={selectedAccount.bookCurrency} />
              </Field>
              <Field label="Book cost basis" required>
                <Select fullWidth name="bookCostBasis" required value={selectedAccount.bookCostBasis}>
                  {#each profitLossMethodOptions as option (option.value)}
                    <option value={option.value}>{option.label}</option>
                  {/each}
                </Select>
              </Field>
              <Field label="Name" required>
                <TextInput fullWidth name="name" required value={selectedAccount.name} />
              </Field>
              <Field label="Formal name" required>
                <TextInput fullWidth name="formalName" required value={selectedAccount.formalName} />
              </Field>
            </div>
            <div class="field-note">
              Book currency is locked{selectedHasTransactions ? ' because this account has transactions' : ''}.
            </div>
            <div class="form-actions">
              <Button type="submit" variant="primary">Save Account</Button>
            </div>
          </form>
        </section>

        <section class="data-panel">
          <div class="panel-heading">
            <div>
              <h2>Required Holdings</h2>
              <p>
                {#if needsRequiredHoldings}
                  Missing {!selectedHasCapitalAccount ? 'Capital Account' : ''}{!selectedHasCapitalAccount && selectedMissingNominals.length ? ', ' : ''}{selectedMissingNominals.map(holdingKindLabel).join(', ')}
                {:else}
                  Capital Account and nominal defaults are present.
                {/if}
              </p>
            </div>
          </div>
          <form class="tool-form compact-form" method="POST" action="?/ensureRequiredHoldings">
            <input type="hidden" name="accountID" value={selectedAccount.accountID} />
            <div class="form-grid">
              <Field label="Change date" required>
                <DateTimeInput fullWidth name="eventDateTime" required value={defaultEventDate} />
              </Field>
              <Field label="Bank name">
                <TextInput fullWidth name="bankName" />
              </Field>
              <Field label="Account name">
                <TextInput fullWidth name="accountName" value="Capital Account" />
              </Field>
              <Field label="Sort code">
                <TextInput fullWidth name="sortCode" />
              </Field>
              <Field label="Account number">
                <TextInput fullWidth name="accountNumber" />
              </Field>
              <Field label="BIC">
                <TextInput fullWidth name="bic" />
              </Field>
              <Field label="IBAN">
                <TextInput fullWidth name="iban" />
              </Field>
            </div>
            <div class="form-actions">
              <Button disabled={!needsRequiredHoldings} type="submit" variant="primary">Create Missing Holdings</Button>
            </div>
          </form>
        </section>

        <section class="data-panel">
          <div class="panel-heading">
            <div>
              <h2>Holdings</h2>
              <p>{selectedHoldings.length} holdings on this account</p>
            </div>
          </div>

          <div class="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>Kind</th>
                  <th>Name</th>
                  <th>Instrument</th>
                  <th>Status</th>
                  <th>Default</th>
                  <th>Last audit</th>
                  <th></th>
                </tr>
              </thead>
              <tbody>
                {#each selectedHoldings as holding (holding.holdingID)}
                  <tr>
                    <td>{holdingKindLabel(holding.holdingKind)}</td>
                    <td>{holding.name || '-'}</td>
                    <td>{instrumentName(holding.instrumentID)}</td>
                    <td>{holding.active ? 'Active' : 'Inactive'}</td>
                    <td>
                      {#if holding.default}
                        <span class="status-pill">Default</span>
                      {:else}
                        <form method="POST" action="?/setHoldingDefault">
                          <input type="hidden" name="holdingID" value={holding.holdingID} />
                          <input type="hidden" name="eventDateTime" value={defaultEventDate} />
                          <Button size="sm" type="submit">Set Default</Button>
                        </form>
                      {/if}
                    </td>
                    <td>{formatTableDateTime(holding.lastAuditDateTime)}</td>
                    <td>
                      <form method="POST" action="?/setHoldingActive">
                        <input type="hidden" name="holdingID" value={holding.holdingID} />
                        <input type="hidden" name="holdingName" value={holding.name} />
                        <input type="hidden" name="eventDateTime" value={defaultEventDate} />
                        <input type="hidden" name="active" value={holding.active ? 'false' : 'true'} />
                        <Button size="sm" type="submit" variant={holding.active ? 'danger' : 'primary'}>
                          {holding.active ? 'Deactivate' : 'Activate'}
                        </Button>
                      </form>
                    </td>
                  </tr>
                {:else}
                  <tr>
                    <td colspan="7" class="empty-cell">No holdings exist for this account.</td>
                  </tr>
                {/each}
              </tbody>
            </table>
          </div>

          <form class="tool-form add-holding-form" method="POST" action="?/createHolding">
            <input type="hidden" name="accountID" value={selectedAccount.accountID} />
            <div class="form-grid">
              <Field label="Change date" required>
                <DateTimeInput fullWidth name="eventDateTime" required value={defaultEventDate} />
              </Field>
              <Field label="Kind" required>
                <Select bind:value={newHoldingKind} fullWidth name="holdingKind" required>
                  {#each HOLDING_KINDS as kind (kind)}
                    <option value={kind}>{holdingKindLabel(kind)}</option>
                  {/each}
                </Select>
              </Field>
              <Field label="Instrument" required>
                <Select fullWidth name="instrumentID" required>
                  <option value="">Select instrument</option>
                  {#each instruments as instrument (instrument.instrumentID)}
                    <option value={instrument.instrumentID}>{instrument.name} ({instrument.priceCurrency})</option>
                  {/each}
                </Select>
              </Field>
              <Field label="Name" required>
                <TextInput fullWidth name="name" required />
              </Field>
            </div>
            {#if isBankHoldingKind(newHoldingKind)}
              <div class="bank-grid">
                <Field label="Bank name" required>
                  <TextInput fullWidth name="bankName" required />
                </Field>
                <Field label="Account name" required>
                  <TextInput fullWidth name="accountName" required />
                </Field>
                <Field label="Sort code" required>
                  <TextInput fullWidth name="sortCode" required />
                </Field>
                <Field label="Account number" required>
                  <TextInput fullWidth name="accountNumber" required />
                </Field>
                <Field label="BIC" required>
                  <TextInput fullWidth name="bic" required />
                </Field>
                <Field label="IBAN" required>
                  <TextInput fullWidth name="iban" required />
                </Field>
              </div>
            {/if}
            <div class="form-actions">
              <Toggle bind:checked={newHoldingActive} label="Active" name="active" />
              <Toggle bind:checked={newHoldingDefault} label="Default" name="default" />
              <Button type="submit" variant="primary">Create Holding</Button>
            </div>
          </form>
        </section>

        <section class="data-panel">
          <div class="panel-heading">
            <div>
              <h2>Asset Allocation Links</h2>
              <p>Link or unlink this account from active allocation configurations.</p>
            </div>
          </div>
          <div class="allocation-list">
            {#each valuationSettings as allocation (allocation.assetAllocationID)}
              <div class="allocation-row">
                <span>
                  <strong>{allocation.name}</strong>
                  <small>{allocation.active ? 'Active' : 'Inactive'} - {allocation.accountIDs.length} accounts</small>
                </span>
                <form method="POST" action="?/linkAllocation">
                  <input type="hidden" name="assetAllocationID" value={allocation.assetAllocationID} />
                  <input type="hidden" name="accountID" value={selectedAccount.accountID} />
                  <input type="hidden" name="eventDateTime" value={defaultEventDate} />
                  <input type="hidden" name="link" value={allocationHasAccount(allocation, selectedAccount.accountID) ? 'false' : 'true'} />
                  <Button size="sm" type="submit" variant={allocationHasAccount(allocation, selectedAccount.accountID) ? 'danger' : 'primary'}>
                    {allocationHasAccount(allocation, selectedAccount.accountID) ? 'Unlink' : 'Link'}
                  </Button>
                </form>
              </div>
            {:else}
              <p class="empty-state">No asset allocations are configured.</p>
            {/each}
          </div>
        </section>
      {:else}
        <section class="data-panel">
          <div class="panel-heading">
            <div>
              <h2>No Account Selected</h2>
              <p>Adjust the search or create a new account below.</p>
            </div>
          </div>
        </section>
      {/if}

        <section class="data-panel create-account-panel">
          <div class="panel-heading">
            <div>
              <h2>Create Account</h2>
              <p>Creates the account, capital account, and required nominal default holdings.</p>
            </div>
          </div>
          <form class="tool-form" method="POST" action="?/createAccount">
            <div class="form-grid">
              <Field label="Start date" required>
                <DateTimeInput fullWidth name="eventDateTime" required showShortcuts={true} value={defaultEventDate} />
              </Field>
              <Field label="Book currency" required>
                <Select fullWidth name="bookCurrency" required>
                  <option value="">Select currency</option>
                  {#each currencies as currency (currency.alphabeticCode)}
                    <option value={currency.alphabeticCode}>{currency.alphabeticCode} - {currency.name}</option>
                  {/each}
                </Select>
              </Field>
              <Field label="Book cost basis" required>
                <Select fullWidth name="bookCostBasis" required value="FIFO">
                  {#each profitLossMethodOptions as option (option.value)}
                    <option value={option.value}>{option.label}</option>
                  {/each}
                </Select>
              </Field>
              <Field label="Name" required>
                <TextInput fullWidth name="name" required />
              </Field>
              <Field label="Formal name" required>
                <TextInput fullWidth name="formalName" required />
              </Field>
            </div>
            <div class="bank-grid">
              <Field label="Bank name" required>
                <TextInput fullWidth name="bankName" required />
              </Field>
              <Field label="Account name" required>
                <TextInput fullWidth name="accountName" required value="Capital Account" />
              </Field>
              <Field label="Sort code" required>
                <TextInput fullWidth name="sortCode" placeholder="12-34-56" required />
              </Field>
              <Field label="Account number" required>
                <TextInput fullWidth name="accountNumber" placeholder="12345678" required />
              </Field>
              <Field label="BIC" required>
                <TextInput fullWidth name="bic" required />
              </Field>
              <Field label="IBAN" required>
                <TextInput fullWidth name="iban" required />
              </Field>
            </div>
            <div class="form-actions">
              <Toggle bind:checked={createActive} label="Active" name="active" />
              <Button type="submit" variant="primary">Create Account</Button>
            </div>
          </form>
        </section>
      </div>
    </div>
  </section>
</main>

<style>
  .account-tools-page {
    box-sizing: border-box;
    display: grid;
    gap: 1rem;
    max-width: min(80rem, calc(100vw - 2rem));
    min-width: 0;
    overflow-x: clip;
    padding-left: 0;
    padding-right: 0;
    width: 100%;
  }

  .page-header > .page-container {
    box-sizing: border-box;
    max-width: min(80rem, calc(100vw - 2rem));
    min-width: 0;
    padding-left: 0;
    padding-right: 0;
    width: 100%;
  }

  .page-header-main,
  .page-header-aside,
  .page-subtitle,
  .page-header-summary,
  .account-tools-layout,
  .account-picker-panel,
  .account-picker-grid,
  .account-workspace,
  .selected-account-panel {
    min-width: 0;
  }

  .page-subtitle,
  .page-header-summary,
  .panel-heading p,
  .field-note {
    overflow-wrap: anywhere;
  }

  .account-title-row {
    display: grid;
    grid-template-columns: minmax(0, 1fr) auto;
    align-items: center;
    gap: 0.75rem;
  }

  .account-bookmark-slot {
    justify-self: end;
    max-width: 100%;
    min-width: 0;
  }

  .account-bookmark-slot :global(.page-bookmark-control) {
    margin-left: auto;
    max-width: 100%;
  }

  .account-tools-layout {
    display: grid;
    gap: 1rem;
  }

  .account-tools-page .data-panel {
    box-sizing: border-box;
    max-width: 100%;
    padding: 1rem;
  }

  .account-picker-panel {
    overflow: visible;
    position: relative;
    z-index: 20;
  }

  .account-picker-grid {
    display: grid;
    grid-template-columns: minmax(0, 1fr);
    gap: 0.75rem;
    align-items: end;
  }

  .account-picker-grid :global(.house-control) {
    max-width: 100%;
    min-width: 0;
  }

  .account-combobox {
    position: relative;
    width: 100%;
    min-width: 0;
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
    font-size: 0.875rem;
    font-weight: 700;
    gap: 0.75rem;
    grid-template-columns: minmax(0, 1fr) auto;
    min-height: var(--house-control-height);
    outline: none;
    padding: 0.375rem 0.625rem;
    text-align: left;
    width: 100%;
  }

  .account-combobox-trigger::after {
    color: var(--muted);
    content: 'v';
    font-size: 0.75rem;
    font-weight: 800;
  }

  .account-combobox-trigger[aria-expanded='true'] {
    border-color: var(--accent);
    box-shadow: 0 0 0 3px var(--focus-ring);
  }

  .account-combobox-trigger[aria-expanded='true']::after {
    content: '^';
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
    gap: 0.5rem;
    left: 0;
    padding: 0.5rem;
    position: absolute;
    top: calc(100% + 0.35rem);
    width: min(100%, 48rem);
    z-index: 80;
  }

  .account-combobox-options {
    display: grid;
    gap: 0.25rem;
    max-height: 18rem;
    overflow: auto;
  }

  .account-combobox-option {
    background: transparent;
    border: 1px solid transparent;
    border-radius: calc(var(--house-radius-sm) - 2px);
    color: var(--ink);
    cursor: pointer;
    display: grid;
    gap: 0.15rem;
    min-width: 0;
    padding: 0.45rem 0.55rem;
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
    font-size: 0.875rem;
    font-weight: 750;
    overflow-wrap: anywhere;
  }

  .account-combobox-option small,
  .account-combobox-empty {
    color: var(--muted);
    font-size: 0.75rem;
    font-weight: 650;
    overflow-wrap: anywhere;
  }

  .account-combobox-empty {
    padding: 0.6rem 0.55rem;
  }

  .account-workspace,
  .tool-form,
  .allocation-list {
    display: grid;
    gap: 1rem;
  }

  .panel-heading,
  .form-actions,
  .allocation-row {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 1rem;
  }

  .form-actions {
    flex-wrap: wrap;
  }

  .panel-heading h2,
  .panel-heading p {
    margin: 0;
  }

  .panel-heading p,
  .allocation-row small,
  .field-note {
    color: var(--text-muted, #64748b);
  }

  .form-grid,
  .bank-grid {
    display: grid;
    grid-template-columns: repeat(2, minmax(0, 1fr));
    gap: 0.75rem;
  }

  .bank-grid {
    grid-template-columns: repeat(3, minmax(0, 1fr));
  }

  .compact-form {
    gap: 0.75rem;
  }

  .account-detail-grid {
    display: grid;
    grid-template-columns: repeat(4, minmax(0, 1fr));
    gap: 0.75rem;
  }

  .account-detail-grid div {
    border: 1px solid var(--border-subtle, #d7dee8);
    border-radius: 8px;
    background: color-mix(in srgb, var(--surface, #fff) 88%, var(--accent-soft, #e0f2fe));
    display: grid;
    gap: 0.2rem;
    min-width: 0;
    padding: 0.65rem 0.75rem;
  }

  .account-detail-grid span {
    color: var(--text-muted, #64748b);
    font-size: 0.76rem;
    font-weight: 700;
    text-transform: uppercase;
  }

  .account-detail-grid strong {
    overflow-wrap: anywhere;
  }

  .allocation-row span {
    display: grid;
    gap: 0.15rem;
    min-width: 0;
  }

  .allocation-row strong,
  .allocation-row small {
    overflow-wrap: anywhere;
  }

  .table-wrap {
    overflow-x: auto;
  }

  table {
    border-collapse: collapse;
    min-width: 62rem;
    width: 100%;
  }

  th,
  td {
    border-bottom: 1px solid var(--border-subtle, #d7dee8);
    padding: 0.55rem 0.5rem;
    text-align: left;
    vertical-align: middle;
  }

  th {
    color: var(--text-muted, #64748b);
    font-size: 0.78rem;
    font-weight: 700;
    text-transform: uppercase;
  }

  .status-pill {
    background: color-mix(in srgb, var(--success, #15803d) 14%, transparent);
    border: 1px solid color-mix(in srgb, var(--success, #15803d) 38%, transparent);
    border-radius: 999px;
    color: var(--success, #15803d);
    display: inline-block;
    font-size: 0.78rem;
    font-weight: 700;
    padding: 0.2rem 0.5rem;
  }

  .empty-state,
  .empty-cell {
    color: var(--text-muted, #64748b);
    margin: 0;
  }

  .empty-cell {
    padding: 1rem 0.5rem;
    text-align: center;
  }

  .success-message,
  .failure-message {
    border-radius: 8px;
    margin: 0;
    padding: 0.75rem 1rem;
  }

  .success-message {
    background: color-mix(in srgb, var(--success, #15803d) 12%, transparent);
    border: 1px solid color-mix(in srgb, var(--success, #15803d) 38%, transparent);
    color: var(--success, #15803d);
  }

  .failure-message {
    background: color-mix(in srgb, var(--danger, #dc2626) 12%, transparent);
    border: 1px solid color-mix(in srgb, var(--danger, #dc2626) 38%, transparent);
    color: var(--danger, #dc2626);
  }

  @media (max-width: 900px) {
    .account-picker-grid,
    .account-detail-grid,
    .form-grid,
    .bank-grid {
      grid-template-columns: 1fr;
    }

    .panel-heading,
    .form-actions,
    .allocation-row {
      align-items: stretch;
      flex-direction: column;
    }

  }

  @media (max-width: 640px) {
    .account-title-row {
      grid-template-columns: 1fr;
    }

    .account-bookmark-slot,
    .page-header-aside {
      align-items: flex-end;
      justify-self: end;
      text-align: left;
    }
  }
</style>
