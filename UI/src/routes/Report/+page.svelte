<script lang="ts">
  import DateTimeInput from '$lib/components/DateTimeInput.svelte';
  import { holdingDateBasisOptions } from '$lib/valuationPreferences';

  let { data } = $props();
</script>

<svelte:head>
  <title>Report | FolioTrace</title>
</svelte:head>

<main class="min-h-screen">
  <section class="page-header">
    <div class="page-container">
      <div class="page-header-content">
        <div class="page-header-main">
          <p class="page-kicker">Report</p>
          <div class="page-title-row">
            <h1 class="page-title">Report</h1>
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
        <div class="grid gap-2">
          <label class="grid min-w-0 gap-1 text-sm font-medium text-slate-700">
            Report config
            <select class="h-10 w-full min-w-0 rounded-md border border-slate-300 bg-white px-3 text-slate-950 shadow-sm outline-none focus:border-teal-600 focus:ring-2 focus:ring-teal-600/20" disabled={!data.reportConfigs.length} name="reportID" required>
              {#each data.reportConfigs as reportConfig (reportConfig.reportID)}
                <option selected={reportConfig.reportID === data.reportID} value={reportConfig.reportID}>{reportConfig.name}</option>
              {:else}
                <option value="">No matching report configs</option>
              {/each}
            </select>
          </label>

          {#if data.accounts.length && !data.reportConfigs.length}
            <div class="rounded-md border border-amber-200 bg-amber-50 px-3 py-2 text-sm text-amber-900">No active report configs match the selected account and valuation date.</div>
          {/if}
        </div>

        <button class="h-10 rounded-md bg-teal-700 px-5 text-sm font-semibold text-white shadow-sm hover:bg-teal-800 disabled:cursor-not-allowed disabled:bg-slate-300" disabled={!data.accounts.length || !data.reportConfigs.length} type="submit">Create</button>
      </div>
    </form>

    {#if data.error}
      <div class="rounded-md border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-800" role="status">{data.error}</div>
    {/if}
  </section>
</main>
