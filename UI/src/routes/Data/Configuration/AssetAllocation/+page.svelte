<script lang="ts">
  import AggregateUpdateWatcher from '$lib/components/AggregateUpdateWatcher.svelte';
  import BookmarkButton from '$lib/components/BookmarkButton.svelte';
  import DateTimeInput from '$lib/components/DateTimeInput.svelte';
  import Card from '$lib/components/page/Card.svelte';
  import { ComplexSelect, type ComplexSelectOption } from '$lib/components/forms';
  import AssetAllocationMappingEditor from './AssetAllocationMappingEditor.svelte';

  let { data, form } = $props();

  let selectedValuationSettingIDOverride = $state('');
  let selectedAccountIDOverrides = $state<string[] | null>(null);
  let accountDropdownOpen = $state(false);

  const selectedValuationSettingID = $derived(validSelectedValuationSettingID());
  const selectedControlSetting = $derived(data.valuationSettings.find((setting) => setting.assetAllocationID === selectedValuationSettingID) ?? null);
  const accountOptions = $derived(accountsForSetting(selectedValuationSettingID));
  const accountSelectOptions = $derived<ComplexSelectOption[]>(accountOptions.map((account) => ({
    id: account.accountID,
    name: account.name
  })));
  const selectedAccountIDs = $derived(validSelectedAccountIDs());
  const selectedAccounts = $derived(accountOptions.filter((account) => selectedAccountIDs.includes(account.accountID)));
  const selectedAccountSummary = $derived(accountSummary());
  const pageKey = $derived(`${data.valuationDate}|${data.auditDateTime}|${data.accountIDs.join(',')}|${data.valuationSettingID}|${data.holdingDateBasis}`);

  function validSelectedValuationSettingID() {
    if (selectedValuationSettingIDOverride && data.valuationSettings.some((setting) => setting.assetAllocationID === selectedValuationSettingIDOverride))
      return selectedValuationSettingIDOverride;

    return data.valuationSettingID;
  }

  function accountsForSetting(assetAllocationID: string) {
    const setting = data.valuationSettings.find((item) => item.assetAllocationID === assetAllocationID);

    if (!setting)
      return [];

    const allowedAccountIDs = new Set(setting.accountIDs);
    return data.accounts.filter((account) => allowedAccountIDs.has(account.accountID));
  }

  function changeValuationSetting(value: string) {
    selectedValuationSettingIDOverride = value;
    selectedAccountIDOverrides = accountsForSetting(value).map((account) => account.accountID);
  }

  function changeAccounts(selection: string | string[] | undefined) {
    if (Array.isArray(selection))
      selectedAccountIDOverrides = selection;
  }

  function validSelectedAccountIDs() {
    const accountOptionIDSet = new Set(accountOptions.map((account) => account.accountID));
    const accountIDs = selectedAccountIDOverrides ?? data.accountIDs;
    return accountIDs.filter((accountID) => accountOptionIDSet.has(accountID));
  }

  function accountSummary() {
    if (!selectedAccountIDs.length)
      return 'No accounts selected';

    if (selectedAccountIDs.length === accountOptions.length)
      return `${selectedAccountIDs.length} accounts selected`;

    const selectedNames = accountOptions
      .filter((account) => selectedAccountIDs.includes(account.accountID))
      .map((account) => account.name);

    if (selectedNames.length <= 2)
      return selectedNames.join(', ');

    return `${selectedNames.length} accounts selected`;
  }
</script>

<svelte:head>
  <title>Asset Allocation | FolioTrace</title>
</svelte:head>

<main class="min-h-screen">
  <section class="page-header">
    <div class="page-container">
      <div class="page-header-content">
        <div class="page-header-main">
          <p class="page-kicker">Configuration</p>
          <div class="page-title-row">
            <h1 class="page-title">Asset Allocation</h1>
            <BookmarkButton />
          </div>
        </div>
      </div>
    </div>
  </section>

  <section class="page-container page-section grid gap-5">
    <form class="house-form grid gap-4" method="GET">
      {#if data.auditDateTime}
        <input name="auditDateTime" type="hidden" value={data.auditDateTime} />
      {/if}

      <div class="grid gap-3 lg:grid-cols-[var(--house-datetime-width)_minmax(220px,1fr)_minmax(220px,1fr)_auto] lg:items-end">
        <label class="grid min-w-0 gap-1 text-sm font-medium text-slate-700">
          Valuation date
          <DateTimeInput fullWidth name="valuationDate" step="1" value={data.valuationDate} />
        </label>

        <label class="grid min-w-0 gap-1 text-sm font-medium text-slate-700">
          Valuation config
          <select class="house-control house-control-md house-control-full" disabled={!data.valuationSettings.length} name="valuationSettingID" onchange={(event) => changeValuationSetting(event.currentTarget.value)} required>
            {#each data.valuationSettings as setting (setting.assetAllocationID)}
              <option selected={setting.assetAllocationID === selectedValuationSettingID} value={setting.assetAllocationID}>{setting.name}</option>
            {:else}
              <option value="">No active valuation configs</option>
            {/each}
          </select>
        </label>

        <div class="grid min-w-0 gap-1 text-sm font-medium text-slate-700">
          Account
          <ComplexSelect
            ariaLabel="Accounts"
            bind:open={accountDropdownOpen}
            disabled={!selectedControlSetting || !accountOptions.length}
            emptyText="No accounts are assigned to this config."
            multiple
            name="accountIDs"
            onchange={changeAccounts}
            options={accountSelectOptions}
            placeholder="No accounts are assigned to this config."
            searchPlaceholder="Search accounts"
            showClear={false}
            showSelectAll={false}
            summary={selectedAccountSummary}
            values={selectedAccountIDs}
          />
        </div>

        <button class="house-button house-button-primary house-button-md" disabled={!selectedValuationSettingID || !selectedAccountIDs.length} type="submit">Apply</button>
      </div>
    </form>

    {#if data.error}
      <Card density="compact" intent="error" role="status">{data.error}</Card>
    {/if}

    {#if form?.message}
      <div
        class={`rounded-md border px-4 py-3 text-sm ${
          form.status === 'success'
            ? 'border-emerald-200 bg-emerald-50 text-emerald-800'
            : 'border-red-200 bg-red-50 text-red-800'
        }`}
        role="status"
      >
        {form.message}
      </div>
    {/if}

    <AggregateUpdateWatcher
      aggregateKind={['ValuationSettings', 'AssetAllocationMappings']}
      auditDateTime={data.auditDateTime}
      lastEventID={data.assetAllocationMappings?.lastEventID ?? data.selectedValuationSetting?.lastEventID ?? null}
      valuationDate={data.valuationDate}
    />

    {#if data.selectedValuationSetting && data.accountIDs.length}
      {#key pageKey}
        <AssetAllocationMappingEditor
          accountIDs={data.accountIDs}
          accounts={selectedAccounts}
          assetAllocationMappings={data.assetAllocationMappings?.items ?? []}
          auditDateTime={data.auditDateTime}
          holdingPositions={data.holdingPositions?.items ?? []}
          mappingEventDateTime={data.valuationDate}
          valuationSetting={data.selectedValuationSetting}
        />
      {/key}
    {/if}
  </section>
</main>
