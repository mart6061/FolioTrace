<script lang="ts">
  import AggregateUpdateWatcher from '$lib/components/AggregateUpdateWatcher.svelte';
  import DateTimeInput from '$lib/components/DateTimeInput.svelte';
  import AssetAllocationMappingEditor from './AssetAllocationMappingEditor.svelte';

  let { data, form } = $props();

  let selectedValuationSettingIDOverride = $state('');
  let selectedAccountIDOverrides = $state<string[] | null>(null);

  const selectedValuationSettingID = $derived(validSelectedValuationSettingID());
  const selectedControlSetting = $derived(data.valuationSettings.find((setting) => setting.assetAllocationID === selectedValuationSettingID) ?? null);
  const accountOptions = $derived(accountsForSetting(selectedValuationSettingID));
  const selectedAccountIDs = $derived(validSelectedAccountIDs());
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

  function toggleAccount(accountID: string, checked: boolean) {
    selectedAccountIDOverrides = checked
      ? [...new Set([...selectedAccountIDs, accountID])]
      : selectedAccountIDs.filter((selectedAccountID) => selectedAccountID !== accountID);
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
          </div>
        </div>
      </div>
    </div>
  </section>

  <section class="page-container page-section grid gap-5">
    <form class="grid gap-4 rounded-md border border-slate-200 bg-white p-4 shadow-sm" method="GET">
      {#if data.auditDateTime}
        <input name="auditDateTime" type="hidden" value={data.auditDateTime} />
      {/if}

      <div class="grid gap-3 lg:grid-cols-[minmax(220px,18rem)_minmax(220px,1fr)_minmax(220px,1fr)_auto] lg:items-end">
        <label class="grid min-w-0 gap-1 text-sm font-medium text-slate-700">
          Valuation date
          <DateTimeInput class="h-10 w-full min-w-0 rounded-md border border-slate-300 bg-white px-3 text-slate-950 shadow-sm outline-none focus:border-teal-600 focus:ring-2 focus:ring-teal-600/20" name="valuationDate" step="1" value={data.valuationDate} />
        </label>

        <label class="grid min-w-0 gap-1 text-sm font-medium text-slate-700">
          Valuation config
          <select class="h-10 w-full min-w-0 rounded-md border border-slate-300 bg-white px-3 text-slate-950 shadow-sm outline-none focus:border-teal-600 focus:ring-2 focus:ring-teal-600/20" disabled={!data.valuationSettings.length} name="valuationSettingID" onchange={(event) => changeValuationSetting(event.currentTarget.value)} required>
            {#each data.valuationSettings as setting (setting.assetAllocationID)}
              <option selected={setting.assetAllocationID === selectedValuationSettingID} value={setting.assetAllocationID}>{setting.name}</option>
            {:else}
              <option value="">No active valuation configs</option>
            {/each}
          </select>
        </label>

        <div class="grid min-w-0 gap-1 text-sm font-medium text-slate-700">
          Account
          <details class="relative">
            <summary class="flex h-10 cursor-pointer list-none items-center justify-between gap-2 rounded-md border border-slate-300 bg-white px-3 text-slate-950 shadow-sm outline-none hover:border-teal-600 focus:border-teal-600 focus:ring-2 focus:ring-teal-600/20">
              <span class="truncate">{selectedAccountSummary}</span>
              <span class="text-xs text-slate-500">v</span>
            </summary>

            <div class="absolute z-20 mt-2 grid max-h-64 w-full min-w-72 gap-1 overflow-auto rounded-md border border-slate-200 bg-white p-2 text-sm shadow-lg">
              {#if selectedControlSetting && accountOptions.length}
                {#each accountOptions as account (account.accountID)}
                  <label class="flex cursor-pointer items-center gap-2 rounded px-2 py-1.5 font-normal text-slate-800 hover:bg-teal-50">
                    <input
                      checked={selectedAccountIDs.includes(account.accountID)}
                      class="h-4 w-4 accent-teal-700"
                      name="accountIDs"
                      onchange={(event) => toggleAccount(account.accountID, event.currentTarget.checked)}
                      type="checkbox"
                      value={account.accountID}
                    />
                    <span class="truncate">{account.name}</span>
                  </label>
                {/each}
              {:else}
                <div class="px-2 py-1.5 font-normal text-slate-500">No accounts are assigned to this config.</div>
              {/if}
            </div>
          </details>
        </div>

        <button class="h-10 rounded-md bg-teal-700 px-5 text-sm font-semibold text-white shadow-sm hover:bg-teal-800 disabled:cursor-not-allowed disabled:bg-slate-300" disabled={!selectedValuationSettingID || !selectedAccountIDs.length} type="submit">Apply</button>
      </div>
    </form>

    {#if data.error}
      <div class="rounded-md border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-800" role="status">{data.error}</div>
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
