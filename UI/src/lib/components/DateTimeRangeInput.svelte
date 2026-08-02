<script lang="ts">
  import { closeOnOutside } from '$lib/actions/dropdown';
  import DateTimeInput from '$lib/components/DateTimeInput.svelte';
  import { getDateControlConfiguration } from '$lib/dateControlContext';
  import { endOfDayForInput, formatDisplayDateTimeRange, startOfDayForInput } from '$lib/dates';
  import { resolveRangeRule, selectableRangeOptions } from '$lib/dateRules';
  import type { DateControlConfiguration, DateRangeRuleOption } from '$lib/types';
  import { onMount } from 'svelte';

  type Props = {
    configuration?: DateControlConfiguration; disabled?: boolean; end?: string; endName: string; form?: string;
    fullWidth?: boolean; invalid?: boolean; onchange?: (event: Event) => void; relative?: string;
    required?: boolean; start?: string; startName: string;
  };
  let { configuration, disabled = false, end = $bindable(''), endName, form, fullWidth = false, invalid = false,
    onchange, relative = $bindable(''), required = false, start = $bindable(''), startName }: Props = $props();

  const contextConfiguration = getDateControlConfiguration();
  const effectiveConfiguration = $derived(configuration ?? contextConfiguration());
  const options = $derived(selectableRangeOptions(effectiveConfiguration));
  const selectedOption = $derived(options.find((item) => item.expression === relative) ?? null);
  let menuOpen = $state(false);
  let search = $state('');
  let activeIndex = $state(-1);
  let refreshTimer: number | undefined;
  const visibleOptions = $derived(options.filter((item) => item.label.toLowerCase().includes(search.toLowerCase())));

  onMount(() => { scheduleRelativeRefresh(); return () => window.clearTimeout(refreshTimer); });

  function applyRelative(expression: string) {
    const resolved = resolveRangeRule(expression, effectiveConfiguration);
    start = resolved.start; end = resolved.end;
    return resolved;
  }
  function scheduleRelativeRefresh() {
    window.clearTimeout(refreshTimer);
    if (!relative) return;
    const resolved = applyRelative(relative);
    refreshTimer = window.setTimeout(scheduleRelativeRefresh, Math.max(1000, resolved.expiresAt.getTime() - Date.now()));
  }
  function choose(option: DateRangeRuleOption) {
    menuOpen = false; search = '';
    if (option.kind === 'Custom' || !option.expression) { relative = ''; return; }
    relative = option.expression; scheduleRelativeRefresh(); onchange?.(new Event('change'));
  }
  function toggleMenu() {
    menuOpen = !menuOpen;
    if (!menuOpen) return;
    search = '';
    activeIndex = options.findIndex((option) => option.expression === relative);
  }
  function handleStartChange(event: Event) {
    relative = ''; window.clearTimeout(refreshTimer);
    start = startOfDayForInput(start);
    if (start && end && new Date(start) > new Date(end)) end = endOfDayForInput(start);
    onchange?.(event);
  }
  function handleEndChange(event: Event) {
    relative = ''; window.clearTimeout(refreshTimer);
    end = endOfDayForInput(end);
    if (start && end && new Date(start) > new Date(end)) start = startOfDayForInput(end);
    onchange?.(event);
  }
  function handleMenuKey(event: KeyboardEvent) {
    if (event.key === 'ArrowDown') { event.preventDefault(); activeIndex = visibleOptions.length ? Math.min(activeIndex + 1, visibleOptions.length - 1) : -1; }
    else if (event.key === 'ArrowUp') { event.preventDefault(); activeIndex = visibleOptions.length ? (activeIndex <= 0 ? visibleOptions.length - 1 : activeIndex - 1) : -1; }
    else if (event.key === 'Enter' && visibleOptions[activeIndex]) { event.preventDefault(); choose(visibleOptions[activeIndex]); }
    else if (event.key === 'Escape') menuOpen = false;
  }
  function captureControl(node: HTMLElement) {
    const targetForm = form ? document.getElementById(form) as HTMLFormElement | null : node.closest('form');
    const refresh = () => { if (relative) applyRelative(relative); };
    targetForm?.addEventListener('submit', refresh);
    return () => targetForm?.removeEventListener('submit', refresh);
  }
</script>

<div {@attach captureControl} class="date-range-control" class:date-range-control-full={fullWidth} use:closeOnOutside={{ close: () => menuOpen = false, enabled: menuOpen }}>
  {#if relative && selectedOption}
    <input {disabled} {form} name={startName} type="hidden" value={start} />
    <input {disabled} {form} name={endName} type="hidden" value={end} />
    <button class="house-control house-control-md range-selection" class:house-control-full={fullWidth} class:house-control-invalid={invalid} {disabled} onclick={toggleMenu} type="button">
      <span>{selectedOption.label}</span>
      <small>{formatDisplayDateTimeRange(start, end)}</small>
    </button>
  {:else}
    <div class="date-range-native">
      <DateTimeInput bind:value={start} disabled={disabled} {form} fullWidth={fullWidth} id={`${startName}-input`} name={startName} onchange={handleStartChange} relativePresets={false} {required} />
      <span aria-hidden="true">to</span>
      <DateTimeInput bind:value={end} disabled={disabled} {form} fullWidth={fullWidth} id={`${endName}-input`} min={start || undefined} name={endName} onchange={handleEndChange} relativePresets={false} {required} />
    </div>
  {/if}
  <button aria-expanded={menuOpen} aria-haspopup="listbox" aria-label="Choose a relative date range" class="range-menu-button" class:range-menu-button-selected={Boolean(relative && selectedOption)} disabled={disabled} onclick={toggleMenu} type="button">▾</button>
  {#if menuOpen}
    <div class="range-menu" onkeydown={handleMenuKey} role="presentation">
      <input aria-label="Search date range choices" bind:value={search} class="house-control house-control-sm w-full" oninput={() => activeIndex = -1} placeholder="Search ranges…" />
      <div role="listbox" tabindex="-1">
        {#each visibleOptions as option, index (option.optionID)}
          <button aria-selected={option.expression === relative} class:active={index === activeIndex} onclick={() => choose(option)} role="option" type="button">
            <span>{option.label}</span>
            {#if option.expression}
              {@const resolvedRange = resolveRangeRule(option.expression, effectiveConfiguration)}
              <small>{formatDisplayDateTimeRange(resolvedRange.start, resolvedRange.end)}</small>
            {/if}
          </button>
        {/each}
      </div>
    </div>
  {/if}
</div>

<style>
  .date-range-control { display: inline-flex; position: relative; align-items: stretch; gap: .25rem; }
  .date-range-control-full { width: 100%; }
  .date-range-native { display: flex; align-items: center; gap: .4rem; flex-wrap: nowrap; }
  .date-range-control-full .date-range-native { flex: 1; }
  .date-range-control-full > .range-selection { flex: 1; min-width: 0; }
  .range-selection { display: flex; align-items: baseline; justify-content: flex-start; gap: .45rem; text-align: left; white-space: nowrap; cursor: pointer; }
  .range-selection span { color: var(--ink); font-size: var(--control-font-md); font-weight: 700; line-height: 1.5; }
  .range-selection small { overflow: hidden; color: var(--muted); font-size: var(--control-font-sm); font-weight: 400; font-variant-numeric: tabular-nums; line-height: 1.1; text-overflow: ellipsis; }
  .range-selection:focus-visible { border-color: var(--accent); box-shadow: 0 0 0 3px var(--focus-ring); }
  .range-menu-button { min-width: 1.8rem; border: 1px solid var(--accent); border-radius: var(--house-radius-sm); background: var(--panel); color: var(--accent); cursor: pointer; }
  .range-menu-button:hover,.range-menu-button:focus-visible { border-color: var(--accent-strong); background: var(--accent-soft); color: var(--accent-strong); outline: none; }
  .range-menu-button-selected { border-color: var(--accent); background: var(--accent); color: #fff; }
  .range-menu-button-selected:hover,.range-menu-button-selected:focus-visible { border-color: var(--accent-strong); background: var(--accent-strong); color: #fff; }
  .range-menu { position: absolute; z-index: 80; top: calc(100% + .3rem); left: 0; width: min(34rem, 92vw); padding: .45rem; border: 1px solid var(--line); border-radius: .4rem; background: var(--panel); box-shadow: 0 .65rem 1.5rem rgb(0 0 0 / .16); }
  .range-menu [role='listbox'] { max-height: 18rem; overflow: auto; margin-top: .35rem; }
  .range-menu [role='option'] { width: 100%; border: 0; background: transparent; color: inherit; display: grid; gap: .15rem; padding: .45rem; text-align: left; cursor: pointer; }
  .range-menu [role='option']:hover,.range-menu [role='option'].active { background: var(--accent-soft); }
  .range-menu [role='option'].active { outline: 1px solid var(--accent); outline-offset: -1px; }
  .range-menu [aria-selected='true'] { background: #e5f4ee; font-weight: 700; }
  .range-menu small { opacity: .7; font-variant-numeric: tabular-nums; }
</style>
