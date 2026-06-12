<script lang="ts">
  import DateTimeInput from '$lib/components/DateTimeInput.svelte';
  import { holdingDateBasisOptions } from '$lib/valuationPreferences';

  let { data } = $props();
</script>

<svelte:head>
  <title>Report | FolioTrace</title>
</svelte:head>

<section class="min-h-screen bg-slate-50 px-6 py-6 text-slate-900">
  <div class="mx-auto grid max-w-7xl gap-5">
    <div>
      <p class="text-xs font-semibold uppercase tracking-wide text-teal-700">Report</p>
      <h1 class="text-2xl font-semibold text-slate-950">Report</h1>
    </div>

    <form class="grid gap-4 rounded-md border border-slate-200 bg-white p-4 shadow-sm" method="GET">
      {#if data.auditDateTime}
        <input name="auditDateTime" type="hidden" value={data.auditDateTime} />
      {/if}

      <div class="grid gap-3 md:grid-cols-[minmax(240px,24rem)]">
        <label class="grid min-w-0 gap-1 text-sm font-medium text-slate-700">
          Valuation date
          <DateTimeInput class="h-10 w-full min-w-0 rounded-md border border-slate-300 bg-white px-3 text-slate-950 shadow-sm outline-none focus:border-teal-600 focus:ring-2 focus:ring-teal-600/20" name="valuationDate" step="1" value={data.valuationDate} />
        </label>
      </div>

      <div class="grid gap-3 border-t border-slate-200 pt-4 md:grid-cols-3">
        <label class="grid min-w-0 gap-1 text-sm font-medium text-slate-700">
          Account
          <select class="h-10 w-full min-w-0 rounded-md border border-slate-300 bg-white px-3 text-slate-950 shadow-sm outline-none focus:border-teal-600 focus:ring-2 focus:ring-teal-600/20" disabled={!data.accounts.length} name="accountID" required>
            {#each data.accounts as account (account.accountID)}
              <option selected={account.accountID === data.accountID} value={account.accountID}>{account.name}</option>
            {:else}
              <option value="">No active accounts</option>
            {/each}
          </select>
        </label>

        <label class="grid min-w-0 gap-1 text-sm font-medium text-slate-700">
          Price basis
          <select class="h-10 w-full min-w-0 rounded-md border border-slate-300 bg-white px-3 text-slate-950 shadow-sm outline-none focus:border-teal-600 focus:ring-2 focus:ring-teal-600/20" name="instrumentPriceBasis">
            {#each data.instrumentPriceBasisOptions as option (option)}
              <option selected={option === data.instrumentPriceBasis} value={option}>{option}</option>
            {/each}
          </select>
        </label>

        <label class="grid min-w-0 gap-1 text-sm font-medium text-slate-700">
          Holding basis
          <select class="h-10 w-full min-w-0 rounded-md border border-slate-300 bg-white px-3 text-slate-950 shadow-sm outline-none focus:border-teal-600 focus:ring-2 focus:ring-teal-600/20" name="holdingDateBasis">
            {#each holdingDateBasisOptions as option (option.value)}
              <option selected={option.value === data.holdingDateBasis} value={option.value}>{option.label}</option>
            {/each}
          </select>
        </label>
      </div>

      <div class="grid gap-3 border-t border-slate-200 pt-4 lg:grid-cols-[1fr_auto] lg:items-end">
        <fieldset class="grid gap-2">
          <legend class="text-sm font-medium text-slate-700">Valuation config</legend>

          {#if data.valuationSettings.length}
            <div class="flex flex-wrap gap-2">
              {#each data.valuationSettings as setting (setting.assetAllocationID)}
                <label class="inline-flex h-9 cursor-pointer items-center gap-2 rounded-md border border-slate-300 bg-white px-3 text-sm font-medium text-slate-700 shadow-sm hover:border-teal-600 hover:text-teal-700 has-[:checked]:border-teal-700 has-[:checked]:bg-teal-50 has-[:checked]:text-teal-900">
                  <input class="h-4 w-4 accent-teal-700" checked={setting.assetAllocationID === data.valuationSettingID} name="valuationSettingID" required type="radio" value={setting.assetAllocationID} />
                  <span>{setting.name}</span>
                </label>
              {/each}
            </div>
          {:else}
            <div class="rounded-md border border-amber-200 bg-amber-50 px-3 py-2 text-sm text-amber-900">No active valuation configs are available for the selected valuation date.</div>
          {/if}
        </fieldset>

        <button class="h-10 rounded-md bg-teal-700 px-5 text-sm font-semibold text-white shadow-sm hover:bg-teal-800 disabled:cursor-not-allowed disabled:bg-slate-300" disabled={!data.accounts.length || !data.valuationSettings.length} type="submit">Apply</button>
      </div>
    </form>

    {#if data.error}
      <div class="rounded-md border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-800" role="status">{data.error}</div>
    {/if}
  </div>
</section>
