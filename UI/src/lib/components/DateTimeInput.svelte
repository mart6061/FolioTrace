<script lang="ts">
  import { closeOnOutside } from '$lib/actions/dropdown';
  import { classNames, controlClass, type ControlSize } from '$lib/components/forms/controls';
  import { getDateControlConfiguration } from '$lib/dateControlContext';
  import { clampFutureInputDateTime, endOfDayForInput, formatDisplayDateTime, nowForInput, startOfDayForInput } from '$lib/dates';
  import { resolveDateRule, selectableDateOptions } from '$lib/dateRules';
  import type { DateControlConfiguration, DateRuleOption } from '$lib/types';
  import { onMount } from 'svelte';

  type Props = {
    class?: string; configuration?: DateControlConfiguration; disabled?: boolean; form?: string;
    fullWidth?: boolean; futureLimited?: boolean; invalid?: boolean; id?: string; max?: string; min?: string;
    name?: string; onchange?: (event: Event) => void; relative?: string; relativePresets?: boolean;
    required?: boolean; showShortcuts?: boolean; shortcutMode?: 'adjacent' | 'embedded'; size?: ControlSize;
    step?: string | number; value?: string;
  };

  let {
    class: className = '', configuration, disabled = false, form, fullWidth = false, futureLimited = false,
    invalid = false, id, max, min, name, onchange, relative = $bindable(''), relativePresets = true,
    required = false, showShortcuts = true, shortcutMode = 'embedded', size = 'md', step = '1', value = $bindable('')
  }: Props = $props();

  const contextConfiguration = getDateControlConfiguration();
  const effectiveConfiguration = $derived(configuration ?? contextConfiguration());
  const options = $derived(selectableDateOptions(effectiveConfiguration));
  const selectedOption = $derived(options.find((item) => item.expression === relative) ?? null);
  let menuOpen = $state(false);
  let search = $state('');
  let activeIndex = $state(0);
  let maxRefreshKey = $state(0);
  let activeShortcut = $state<'start' | 'end' | null>(null);
  let activeShortcutValue = $state('');
  let refreshTimer: number | undefined;
  const effectiveMax = $derived(currentEffectiveMax(maxRefreshKey));
  const visibleOptions = $derived(options.filter((item) => item.label.toLowerCase().includes(search.toLowerCase())));
  const useEmbeddedShortcuts = $derived(showShortcuts && shortcutMode === 'embedded');
  const containerClass = $derived(classNames('datetime-input-control', useEmbeddedShortcuts && 'datetime-input-control-embedded', `datetime-input-control-${size}`, fullWidth && 'datetime-input-control-full', invalid && 'datetime-input-control-invalid'));
  const inputClass = $derived(controlClass(size, fullWidth, invalid, className));
  const startShortcutActive = $derived(activeShortcut === 'start' && isSameDateTimeValue(value, activeShortcutValue));
  const endShortcutActive = $derived(activeShortcut === 'end' && isSameDateTimeValue(value, activeShortcutValue));

  onMount(() => { scheduleRelativeRefresh(); return () => window.clearTimeout(refreshTimer); });

  function applyRelative(expression: string) {
    const resolved = resolveDateRule(expression, effectiveConfiguration);
    value = clampToLimits(resolved.value);
    return { ...resolved, value };
  }
  function scheduleRelativeRefresh() {
    window.clearTimeout(refreshTimer);
    if (!relative) return;
    const resolved = applyRelative(relative);
    const delay = relative === 'now' ? 1000 : Math.max(1000, resolved.expiresAt.getTime() - Date.now());
    refreshTimer = window.setTimeout(scheduleRelativeRefresh, delay);
  }
  function currentEffectiveMax(_refreshKey: number) { return futureLimited ? nowForInput() : (max ?? ''); }
  function refreshEffectiveMax() { maxRefreshKey += 1; }
  function clampToLimits(nextValue: string) {
    const maxLimit = futureLimited ? nowForInput() : max;
    let clamped = futureLimited ? clampFutureInputDateTime(nextValue) : nextValue;
    if (maxLimit && new Date(clamped).getTime() > new Date(maxLimit).getTime()) clamped = maxLimit;
    if (min && new Date(clamped).getTime() < new Date(min).getTime()) clamped = min;
    return clamped;
  }
  function choose(option: DateRuleOption) {
    menuOpen = false;
    search = '';
    if (option.kind === 'Custom' || !option.expression) { relative = ''; return; }
    relative = option.expression;
    scheduleRelativeRefresh();
    onchange?.(new Event('change'));
  }
  function clearRelative() { relative = ''; menuOpen = false; window.clearTimeout(refreshTimer); }
  function setShortcut(nextValue: string, shortcut: 'start' | 'end' | null = null) {
    refreshEffectiveMax(); relative = ''; value = clampToLimits(nextValue); activeShortcut = shortcut;
    activeShortcutValue = shortcut ? value : ''; onchange?.(new Event('change'));
  }
  function setStartOfDay() { setShortcut(startOfDayForInput(value), 'start'); }
  function setEndOfDay() { setShortcut(endOfDayForInput(value), 'end'); }
  function setNow() { setShortcut(nowForInput()); }
  function isSameDateTimeValue(left: string, right: string) {
    if (!left || !right) return false;
    const leftDate = new Date(left); const rightDate = new Date(right);
    return !Number.isNaN(leftDate.getTime()) && !Number.isNaN(rightDate.getTime()) && leftDate.getTime() === rightDate.getTime();
  }
  function handleInput(event: Event) { relative = ''; value = (event.currentTarget as HTMLInputElement).value; refreshEffectiveMax(); }
  function handleChange(event: Event) { refreshEffectiveMax(); if (futureLimited || max || min) value = clampToLimits(value); onchange?.(event); }
  function handleMenuKey(event: KeyboardEvent) {
    if (event.key === 'ArrowDown') { event.preventDefault(); activeIndex = Math.min(activeIndex + 1, visibleOptions.length - 1); }
    else if (event.key === 'ArrowUp') { event.preventDefault(); activeIndex = Math.max(activeIndex - 1, 0); }
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

<span {@attach captureControl} class="relative-date-wrap" class:relative-date-wrap-full={fullWidth} use:closeOnOutside={{ close: () => menuOpen = false, enabled: menuOpen }}>
  <span class={containerClass}>
    {#if relative && selectedOption}
      <input {form} {name} type="hidden" value={value} />
      <button class="relative-date-chip" class:relative-date-chip-invalid={invalid} disabled={disabled} onclick={() => menuOpen = !menuOpen} type="button">
        <span>{selectedOption.label}</span><small>{formatDisplayDateTime(value)}</small>
      </button>
      <button aria-label="Use a custom date" class="relative-date-clear" disabled={disabled} onclick={clearRelative} title="Use a custom date" type="button">×</button>
    {:else}
      <input aria-invalid={invalid ? 'true' : undefined} class={inputClass} {id} bind:value {disabled} {form}
        max={effectiveMax || undefined} {min} {name} {required} {step} onclick={refreshEffectiveMax}
        onchange={handleChange} oninput={handleInput} onfocus={refreshEffectiveMax} type="datetime-local" />
      {#if showShortcuts}
        <span class="datetime-input-shortcuts" aria-label="Date time shortcuts">
          <button aria-label="Start of day" class:datetime-input-shortcut-active={startShortcutActive} disabled={disabled} onclick={setStartOfDay} title="Start of day" type="button">S</button>
          <button aria-label="Now" disabled={disabled} onclick={setNow} title="Now" type="button">N</button>
          <button aria-label="End of day" class:datetime-input-shortcut-active={endShortcutActive} disabled={disabled} onclick={setEndOfDay} title="End of day" type="button">E</button>
        </span>
      {/if}
    {/if}
  </span>
  {#if relativePresets}
    <button aria-expanded={menuOpen} aria-haspopup="listbox" aria-label="Choose a relative date" class="relative-date-menu-button" disabled={disabled} onclick={() => menuOpen = !menuOpen} type="button">▾</button>
  {/if}
  {#if menuOpen}
    <div class="relative-date-menu" onkeydown={handleMenuKey} role="presentation">
      <input aria-label="Search date choices" bind:value={search} class="house-control house-control-sm w-full" oninput={() => activeIndex = 0} placeholder="Search dates…" />
      <div role="listbox" tabindex="-1">
        {#each visibleOptions as option, index (option.optionID)}
          <button aria-selected={option.expression === relative} class:active={index === activeIndex} onclick={() => choose(option)} role="option" type="button">
            <span>{option.label}</span>{#if option.expression}<small>{resolveDateRule(option.expression, effectiveConfiguration).value}</small>{/if}
          </button>
        {/each}
      </div>
    </div>
  {/if}
</span>

<style>
  .relative-date-wrap { display: inline-flex; position: relative; align-items: stretch; min-width: var(--house-datetime-input-column); }
  .relative-date-wrap-full { display: flex; width: 100%; }
  .relative-date-wrap-full > .datetime-input-control { flex: 1; }
  .relative-date-menu-button,.relative-date-clear { border: 1px solid var(--house-border); background: var(--house-surface); color: var(--house-text); min-width: 1.8rem; cursor: pointer; }
  .relative-date-chip { min-height: 2.5rem; min-width: var(--house-datetime-input-column); border: 1px solid #2e8b68; background: #edf9f4; color: #176448; display: grid; text-align: left; padding: .25rem .55rem; cursor: pointer; }
  .relative-date-chip small { opacity: .78; font-variant-numeric: tabular-nums; }
  .relative-date-chip-invalid { border-color: #b42318; }
  .relative-date-menu { position: absolute; z-index: 80; top: calc(100% + .3rem); left: 0; width: min(22rem, 90vw); padding: .45rem; border: 1px solid var(--house-border); border-radius: .4rem; background: var(--house-surface); box-shadow: 0 .65rem 1.5rem rgb(0 0 0 / .16); }
  .relative-date-menu [role='listbox'] { max-height: 18rem; overflow: auto; margin-top: .35rem; }
  .relative-date-menu [role='option'] { width: 100%; border: 0; background: transparent; color: inherit; display: flex; justify-content: space-between; gap: 1rem; padding: .45rem; text-align: left; cursor: pointer; }
  .relative-date-menu [role='option']:hover,.relative-date-menu [role='option'].active,.relative-date-menu [aria-selected='true'] { background: #e5f4ee; }
  .relative-date-menu small { opacity: .7; }
</style>
