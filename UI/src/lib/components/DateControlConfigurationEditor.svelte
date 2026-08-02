<script lang="ts">
  import { cloneDateControlConfiguration, describeDateExpression, describeRangeExpression, resolveDateRule, resolveRangeRule } from '$lib/dateRules';
  import type { DateControlConfiguration, DateRangeRuleOption, DateRuleOption } from '$lib/types';

  type Props = { configuration?: DateControlConfiguration; inherited?: boolean; oncustomize?: () => void; readonly?: boolean; showFinancialYearSettings?: boolean };
  let { configuration = $bindable(), inherited = false, oncustomize, readonly = false, showFinancialYearSettings = true }: Props = $props();
  let tab = $state<'dates' | 'ranges'>('dates');
  let period = $state('day'); let offset = $state(0); let boundary = $state('end'); let atTime = $state('17:00');
  let rangeShape = $state<'aligned' | 'rolling'>('aligned'); let rangePeriod = $state('month'); let rangeDirection = $state('last'); let rangeCount = $state(7);

  type AnyOption = DateRuleOption | DateRangeRuleOption;
  const list = $derived(tab === 'dates' ? configuration?.dateOptions ?? [] : configuration?.rangeOptions ?? []);
  const dateExpression = $derived(period === 'now' ? 'now' : `${period}.${offset >= 0 ? '+' : ''}${offset}.${boundary === 'at' ? `at(${atTime})` : boundary}`);
  const rangeExpression = $derived(rangeShape === 'aligned' ? `range.${rangePeriod}.${offset >= 0 ? '+' : ''}${offset}` : `range.${rangeDirection}.${rangeCount}.${rangePeriod}`);

  function update(mutator: (draft: DateControlConfiguration) => void) {
    if (!configuration || readonly || inherited) return;
    const draft = cloneDateControlConfiguration(configuration); mutator(draft); normalize(draft); configuration = draft;
  }
  function options(draft: DateControlConfiguration): AnyOption[] { return tab === 'dates' ? draft.dateOptions : draft.rangeOptions; }
  function normalize(draft: DateControlConfiguration) {
    for (const items of [draft.dateOptions, draft.rangeOptions]) items.forEach((item, index) => item.displayOrder = index + 1);
  }
  function addRule() {
    const expression = tab === 'dates' ? dateExpression : rangeExpression;
    update((draft) => {
      const items = options(draft); items.push({ optionID: crypto.randomUUID(), kind: 'Rule', label: tab === 'dates' ? describeDateExpression(expression) : describeRangeExpression(expression), expression, displayOrder: items.length + 1, isDefault: !items.some((item) => item.isDefault) });
    });
  }
  function addCustom() { update((draft) => { const items = options(draft); if (!items.some((item) => item.kind === 'Custom')) items.push({ optionID: crypto.randomUUID(), kind: 'Custom', label: 'Custom', expression: null, displayOrder: items.length + 1, isDefault: false }); }); }
  function addSeparator(after: number) { update((draft) => options(draft).splice(after + 1, 0, { optionID: crypto.randomUUID(), kind: 'Separator', label: '', expression: null, displayOrder: 0, isDefault: false })); }
  function remove(index: number) { update((draft) => { const items = options(draft); const removed = items.splice(index, 1)[0]; const replacement = items.find((item) => item.kind !== 'Separator'); if (removed?.isDefault && replacement) replacement.isDefault = true; }); }
  function move(index: number, direction: number) { update((draft) => { const items = options(draft); const target = index + direction; if (target < 0 || target >= items.length) return; [items[index], items[target]] = [items[target], items[index]]; }); }
  function rename(index: number, label: string) { update((draft) => { options(draft)[index].label = label; }); }
  function setDefault(index: number) { update((draft) => options(draft).forEach((item, itemIndex) => item.isDefault = itemIndex === index)); }
  function setFinancialYear(field: 'financialYearStartMonth' | 'financialYearStartDay', value: number) { update((draft) => { draft[field] = value; }); }
  function preview(item: AnyOption) {
    if (!configuration || !item.expression) return '';
    try { const result = tab === 'dates' ? resolveDateRule(item.expression, configuration) : resolveRangeRule(item.expression, configuration); return 'value' in result ? result.value : `${result.start} → ${result.end}`; } catch { return 'Invalid expression'; }
  }
</script>

<section class="date-rule-editor">
  {#if inherited}
    <div class="inheritance-banner"><span><strong>Using global settings.</strong> Changes made globally apply here automatically.</span><button class="house-button house-button-primary house-button-sm" onclick={oncustomize} type="button">Customize</button></div>
  {/if}
  <div class="date-rule-tabs" role="tablist" aria-label="Date control configuration">
    <button aria-selected={tab === 'dates'} onclick={() => tab = 'dates'} role="tab" type="button">Date</button>
    <button aria-selected={tab === 'ranges'} onclick={() => tab = 'ranges'} role="tab" type="button">Date ranges</button>
  </div>
  {#if configuration}
    {#if showFinancialYearSettings}
      <div class="fy-row">
        <span class="fy-title">Financial year starts</span>
        <label>Month <input class="house-control house-control-sm" disabled={readonly || inherited} max="12" min="1" onchange={(event) => setFinancialYear('financialYearStartMonth', Number(event.currentTarget.value))} type="number" value={configuration.financialYearStartMonth} /></label>
        <label>Day <input class="house-control house-control-sm" disabled={readonly || inherited} max="31" min="1" onchange={(event) => setFinancialYear('financialYearStartDay', Number(event.currentTarget.value))} type="number" value={configuration.financialYearStartDay} /></label>
      </div>
    {/if}
    {#if !readonly && !inherited}
      <div class="builder">
        {#if tab === 'dates'}
          <label>Period <select class="house-control house-control-sm" bind:value={period}><option value="now">Now</option><option value="day">Day</option><option value="bd">Business day</option><option value="week">Week</option><option value="month">Month</option><option value="quarter">Quarter</option><option value="year">Year</option><option value="fy">Financial year</option></select></label>
          {#if period !== 'now'}<label>Offset <input class="house-control house-control-sm" bind:value={offset} type="number" /></label><label>Point <select class="house-control house-control-sm" bind:value={boundary}><option value="start">Start</option><option value="end">End</option><option value="at">At time</option></select></label>{/if}
          {#if boundary === 'at' && period !== 'now'}<label>Time <input class="house-control house-control-sm" bind:value={atTime} type="time" /></label>{/if}
        {:else}
          <label>Shape <select class="house-control house-control-sm" bind:value={rangeShape}><option value="aligned">Aligned period</option><option value="rolling">Rolling period</option></select></label>
          {#if rangeShape === 'rolling'}<label>Direction <select class="house-control house-control-sm" bind:value={rangeDirection}><option value="last">Last</option><option value="next">Next</option></select></label><label>Count <input class="house-control house-control-sm" bind:value={rangeCount} min="1" type="number" /></label>{:else}<label>Offset <input class="house-control house-control-sm" bind:value={offset} type="number" /></label>{/if}
          <label>Period <select class="house-control house-control-sm" bind:value={rangePeriod}><option value="day">Day</option>{#if rangeShape === 'aligned'}<option value="bd">Business day</option>{/if}<option value="week">Week</option><option value="month">Month</option><option value="quarter">Quarter</option><option value="year">Year</option>{#if rangeShape === 'aligned'}<option value="fy">Financial year</option>{/if}</select></label>
        {/if}
        <button class="house-button house-button-primary house-button-sm" onclick={addRule} type="button">Add choice</button>
        <code>{tab === 'dates' ? dateExpression : rangeExpression}</code>
      </div>
    {/if}
    <div class="option-list">
      {#each list as item, index (item.optionID)}
        {#if item.kind === 'Separator'}
          <div class="separator-row"><hr /><span>Separator</span>{#if !readonly && !inherited}<button aria-label="Remove separator" onclick={() => remove(index)} type="button">×</button>{/if}</div>
        {:else}
          <div class="option-row">
            <input aria-label="Choice label" class="house-control house-control-sm" disabled={readonly || inherited} onchange={(event) => rename(index, event.currentTarget.value)} value={item.label} />
            <div class="rule-preview"><code>{item.expression ?? 'native date input'}</code><small>{preview(item)}</small></div>
            {#if !readonly && !inherited}
              <div class="row-actions">
                <button
                  aria-label={item.isDefault ? 'Default choice' : `Set ${item.label} as default`}
                  aria-pressed={item.isDefault}
                  class="default-control"
                  disabled={item.isDefault}
                  onclick={() => setDefault(index)}
                  type="button"
                >{item.isDefault ? 'Default' : 'Set default'}</button>
                <button aria-label="Move up" class="row-action-button" disabled={index === 0} onclick={() => move(index, -1)} type="button">↑</button>
                <button aria-label="Move down" class="row-action-button" disabled={index === list.length - 1} onclick={() => move(index, 1)} type="button">↓</button>
                <button aria-label="Add separator after" class="row-action-button" onclick={() => addSeparator(index)} type="button">—</button>
                <button aria-label="Remove choice" class="row-action-button" onclick={() => remove(index)} type="button">×</button>
              </div>
            {:else if item.isDefault}
              <span class="default-control default-control-readonly">Default</span>
            {/if}
          </div>
        {/if}
      {/each}
    </div>
    {#if !readonly && !inherited && !list.some((item) => item.kind === 'Custom')}<button class="house-button house-button-secondary house-button-sm" onclick={addCustom} type="button">Add Custom</button>{/if}
  {/if}
</section>

<style>
  .date-rule-editor { display: grid; gap: .5rem; }
  .inheritance-banner { display: flex; align-items: center; justify-content: space-between; gap: 1rem; padding: .7rem; border: 1px solid #8dc9b4; border-radius: .45rem; background: #edf9f4; color: #176448; }
  .date-rule-tabs { display: flex; border-bottom: 1px solid var(--line); }
  .date-rule-tabs button { padding: .4rem .7rem; border: 0; border-bottom: 3px solid transparent; background: transparent; color: inherit; cursor: pointer; }
  .date-rule-tabs button[aria-selected='true'] { border-color: var(--accent); font-weight: 700; }
  .fy-row,.builder { display: flex; flex-wrap: wrap; gap: .45rem; padding: .45rem; border: 1px solid var(--line); border-radius: .4rem; }
  .fy-row { align-items: center; }
  .builder { align-items: end; }
  .fy-title { color: var(--muted); font-size: var(--control-font-sm); font-weight: 400; line-height: 1.2; white-space: nowrap; }
  .fy-row label,.builder label { display: grid; gap: .15rem; color: var(--muted); font-size: var(--control-font-sm); font-weight: 400; }
  .builder label { grid-template-rows: auto var(--control-h-sm); }
  .fy-row input { width: 4.5rem; }.builder input,.builder select { min-width: 7rem; }
  .builder input,.builder select,.builder > button { height: var(--control-h-sm); min-height: var(--control-h-sm); box-sizing: border-box; }
  .builder select.house-control.house-control-sm { height: var(--control-h-sm); min-height: var(--control-h-sm); padding-block: .25rem; }
  .builder code { display: flex; min-height: var(--control-h-sm); align-items: center; align-self: end; opacity: .75; line-height: 1; }
  .option-list { display: grid; gap: .25rem; }
  .option-row { display: grid; grid-template-columns: minmax(9rem, 1fr) minmax(15rem, 2fr) auto; align-items: center; gap: .6rem; padding: .35rem; border: 1px solid var(--line); border-radius: .35rem; }
  .rule-preview { display: flex; align-items: baseline; gap: .45rem; min-width: 0; }.rule-preview code,.rule-preview small { overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }.rule-preview small { opacity: .7; }
  .row-actions { display: flex; align-items: center; gap: .3rem; }
  .row-action-button,.separator-row button { display: inline-flex; align-items: center; justify-content: center; box-sizing: border-box; width: 2.25rem; min-width: 2.25rem; height: 2.25rem; padding: 0; border: 1px solid var(--line); border-radius: var(--house-radius-sm); background: var(--panel); color: inherit; cursor: pointer; }
  .default-control { display: inline-flex; align-items: center; justify-content: center; min-height: 1.5rem; border: 1px solid var(--accent); border-radius: 999px; padding: 0 .55rem; background: transparent; color: var(--accent); font-size: .7rem; font-weight: 700; line-height: 1; white-space: nowrap; cursor: pointer; }
  .default-control[aria-pressed='true'],.default-control-readonly { background: var(--accent); color: #fff; cursor: default; }
  .row-action-button:disabled { cursor: not-allowed; opacity: .45; }
  .separator-row { display: flex; align-items: center; gap: .4rem; color: var(--muted); font-size: .75rem; }.separator-row hr { flex: 1; border: 0; border-top: 1px solid var(--line); }
  @media (max-width: 700px) { .option-row { grid-template-columns: minmax(0, 1fr) auto; }.rule-preview { grid-column: 1 / -1; }.row-actions { grid-column: 1 / -1; justify-content: flex-end; } }
</style>
