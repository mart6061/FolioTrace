<script lang="ts">
  import { formatDateTime } from '$lib/dates';

  let { data } = $props();

  const countryCount = $derived(data.countries?.items.length ?? 0);
</script>

<main class="min-h-screen">
  <section class="border-b border-slate-200 bg-white">
    <div class="mx-auto flex max-w-7xl flex-col gap-5 px-4 py-6 sm:px-6 lg:px-8">
      <div class="flex flex-col gap-1">
        <p class="text-sm font-medium text-teal-700">FolioTrace</p>
        <h1 class="text-2xl font-semibold text-slate-950">Reference Data</h1>
      </div>

      <form class="grid gap-4 md:grid-cols-[minmax(220px,280px)_minmax(220px,280px)_auto] md:items-end">
        <label class="grid gap-1 text-sm font-medium text-slate-700">
          Valuation date
          <input
            class="h-10 rounded-md border border-slate-300 bg-white px-3 text-slate-950 shadow-sm outline-none focus:border-teal-600 focus:ring-2 focus:ring-teal-600/20"
            name="valuationDate"
            type="datetime-local"
            value={data.valuationDate}
          />
        </label>

        <label class="grid gap-1 text-sm font-medium text-slate-700">
          As-of date
          <input
            class="h-10 rounded-md border border-slate-300 bg-white px-3 text-slate-950 shadow-sm outline-none focus:border-teal-600 focus:ring-2 focus:ring-teal-600/20"
            name="auditDateTime"
            type="datetime-local"
            value={data.auditDateTime}
          />
        </label>

        <button
          class="h-10 rounded-md bg-teal-700 px-4 text-sm font-semibold text-white shadow-sm hover:bg-teal-800 focus:outline-none focus:ring-2 focus:ring-teal-600/30"
          type="submit"
        >
          Apply
        </button>
      </form>
    </div>
  </section>

  <section class="mx-auto max-w-7xl px-4 py-6 sm:px-6 lg:px-8">
    {#if data.error}
      <div class="rounded-md border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-800">
        {data.error}
      </div>
    {:else if data.countries}
      <div class="mb-4 flex flex-col gap-2 text-sm text-slate-600 md:flex-row md:items-center md:justify-between">
        <div>
          <span class="font-semibold text-slate-950">{countryCount}</span>
          countries from {data.apiBaseUrl}
        </div>
        <div>
          Valuation {formatDateTime(data.countries.valuationDateTime)} · As-of {formatDateTime(data.countries.asOfDateTime)}
        </div>
      </div>

      <div class="overflow-hidden rounded-lg border border-slate-200 bg-white shadow-sm">
        <div class="overflow-x-auto">
          <table class="min-w-full divide-y divide-slate-200 text-sm">
            <thead class="bg-slate-50 text-left text-xs font-semibold uppercase tracking-wide text-slate-600">
              <tr>
                <th class="w-16 px-4 py-3">Flag</th>
                <th class="px-4 py-3">Country</th>
                <th class="px-4 py-3">Alpha-2</th>
                <th class="px-4 py-3">Alpha-3</th>
                <th class="px-4 py-3 text-right">Numeric</th>
                <th class="px-4 py-3">Last audit</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-slate-100">
              {#each data.countries.items as country}
                <tr class="hover:bg-slate-50">
                  <td class="px-4 py-3">
                    {#if country.flag?.svg}
                      <span class="flag" aria-label={`${country.name} flag`}>{@html country.flag.svg}</span>
                    {/if}
                  </td>
                  <td class="px-4 py-3 font-medium text-slate-950">{country.name}</td>
                  <td class="px-4 py-3 font-mono text-slate-700">{country.alpha2}</td>
                  <td class="px-4 py-3 font-mono text-slate-700">{country.alpha3}</td>
                  <td class="px-4 py-3 text-right font-mono text-slate-700">
                    {country.numeric.toString().padStart(3, '0')}
                  </td>
                  <td class="px-4 py-3 text-slate-600">{formatDateTime(country.lastAuditDateTime)}</td>
                </tr>
              {/each}
            </tbody>
          </table>
        </div>
      </div>
    {/if}
  </section>
</main>
