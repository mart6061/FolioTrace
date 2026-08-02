<script lang="ts">
  import { closeOnOutside } from '$lib/actions/dropdown';
  import { classNames, controlClass, type ControlSize } from '$lib/components/forms/controls';
  import { getDateControlConfiguration } from '$lib/dateControlContext';
  import { clampFutureInputDateTime, formatDisplayDateTime, nowForInput } from '$lib/dates';
  import { resolveDateRule, selectableDateOptions } from '$lib/dateRules';
  import type { DateControlConfiguration, DateRuleOption } from '$lib/types';
  import { onMount } from 'svelte';

  type Props = {
    class?: string; configuration?: DateControlConfiguration; disabled?: boolean; form?: string;
    fullWidth?: boolean; futureLimited?: boolean; invalid?: boolean; id?: string; max?: string; min?: string;
    name?: string; onchange?: (event: Event) => void; relative?: string; relativePresets?: boolean;
    required?: boolean; size?: ControlSize;
    step?: string | number; value?: string;
  };

  let {
    class: className = '', configuration, disabled = false, form, fullWidth = false, futureLimited = false,
    invalid = false, id, max, min, name, onchange, relative = $bindable(''), relativePresets = true,
    required = false, size = 'md', step = '1', value = $bindable('')
  }: Props = $props();

  const contextConfiguration = getDateControlConfiguration();
  const effectiveConfiguration = $derived(configuration ?? contextConfiguration());
  const options = $derived(selectableDateOptions(effectiveConfiguration));
  const selectedOption = $derived(options.find((item) => item.expression === relative) ?? null);
  let menuOpen = $state(false);
  let search = $state('');
  let activeIndex = $state(-1);
  let maxRefreshKey = $state(0);
  let refreshTimer: number | undefined;
  const effectiveMax = $derived(currentEffectiveMax(maxRefreshKey));
  const visibleOptions = $derived(options.filter((item) => item.label.toLowerCase().includes(search.toLowerCase())));
  const containerClass = $derived(classNames('datetime-input-control', fullWidth && 'datetime-input-control-full'));
  const inputClass = $derived(controlClass(size, fullWidth, invalid, className));

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
  function toggleMenu() {
    menuOpen = !menuOpen;
    if (!menuOpen) return;
    search = '';
    activeIndex = options.findIndex((option) => option.expression === relative);
  }
  function handleInput(event: Event) { relative = ''; window.clearTimeout(refreshTimer); value = (event.currentTarget as HTMLInputElement).value; refreshEffectiveMax(); }
  function handleChange(event: Event) { refreshEffectiveMax(); if (futureLimited || max || min) value = clampToLimits(value); onchange?.(event); }
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

<span {@attach captureControl} class="relative-date-wrap" class:relative-date-wrap-full={fullWidth} use:closeOnOutside={{ close: () => menuOpen = false, enabled: menuOpen }}>
  <span class={containerClass}>
    {#if relative && selectedOption}
      <input {disabled} {form} {name} type="hidden" value={value} />
      <button class={classNames(inputClass, 'relative-date-selection')} {disabled} {id} onclick={toggleMenu} type="button">
        <span>{selectedOption.label}</span>
        <small>{formatDisplayDateTime(value)}</small>
      </button>
    {:else}
      <input aria-invalid={invalid ? 'true' : undefined} class={inputClass} {id} bind:value {disabled} {form}
        max={effectiveMax || undefined} {min} {name} {required} {step} onclick={refreshEffectiveMax}
        onchange={handleChange} oninput={handleInput} onfocus={refreshEffectiveMax} type="datetime-local" />
    {/if}
  </span>
  {#if relativePresets}
    <button aria-expanded={menuOpen} aria-haspopup="listbox" aria-label="Choose a relative date" class="relative-date-menu-button" class:relative-date-menu-button-selected={Boolean(relative && selectedOption)} disabled={disabled} onclick={toggleMenu} type="button">▾</button>
  {/if}
  {#if menuOpen}
    <div class="relative-date-menu" onkeydown={handleMenuKey} role="presentation">
      <input aria-label="Search date choices" bind:value={search} class="house-control house-control-sm w-full" oninput={() => activeIndex = -1} placeholder="Search dates…" />
      <div role="listbox" tabindex="-1">
        {#each visibleOptions as option, index (option.optionID)}
          <button aria-selected={option.expression === relative} class:active={index === activeIndex} onclick={() => choose(option)} role="option" type="button">
            <span>{option.label}</span>
            {#if option.expression}
              <small>{formatDisplayDateTime(resolveDateRule(option.expression, effectiveConfiguration).value)}</small>
            {/if}
          </button>
        {/each}
      </div>
    </div>
  {/if}
</span>

<style>
  .relative-date-wrap { display: inline-flex; position: relative; align-items: stretch; gap: .25rem; min-width: var(--house-datetime-input-column); }
  .relative-date-wrap-full { display: flex; width: 100%; }
  .relative-date-wrap-full > .datetime-input-control { flex: 1; }
  .relative-date-selection { display: flex; align-items: baseline; justify-content: flex-start; gap: .45rem; text-align: left; white-space: nowrap; cursor: pointer; }
  .relative-date-selection span { color: var(--ink); font-size: var(--control-font-md); font-weight: 700; line-height: 1.05; }
  .relative-date-selection small { color: var(--muted); font-size: var(--control-font-sm); font-weight: 400; font-variant-numeric: tabular-nums; line-height: 1.1; }
  .relative-date-selection:focus-visible { border-color: var(--accent); box-shadow: 0 0 0 3px var(--focus-ring); }
  .relative-date-menu-button { min-width: 1.8rem; border: 1px solid var(--accent); border-radius: var(--house-radius-sm); background: var(--panel); color: var(--accent); cursor: pointer; }
  .relative-date-menu-button:hover,.relative-date-menu-button:focus-visible { border-color: var(--accent-strong); background: var(--accent-soft); color: var(--accent-strong); outline: none; }
  .relative-date-menu-button-selected { border-color: var(--accent); background: var(--accent); color: #fff; }
  .relative-date-menu-button-selected:hover,.relative-date-menu-button-selected:focus-visible { border-color: var(--accent-strong); background: var(--accent-strong); color: #fff; }
  .relative-date-menu { position: absolute; z-index: 80; top: calc(100% + .3rem); left: 0; width: min(22rem, 90vw); padding: .45rem; border: 1px solid var(--line); border-radius: .4rem; background: var(--panel); box-shadow: 0 .65rem 1.5rem rgb(0 0 0 / .16); }
  .relative-date-menu [role='listbox'] { max-height: 18rem; overflow: auto; margin-top: .35rem; }
  .relative-date-menu [role='option'] { width: 100%; border: 0; background: transparent; color: inherit; display: grid; gap: .15rem; padding: .45rem; text-align: left; cursor: pointer; }
  .relative-date-menu [role='option']:hover,.relative-date-menu [role='option'].active { background: var(--accent-soft); }
  .relative-date-menu [role='option'].active { outline: 1px solid var(--accent); outline-offset: -1px; }
  .relative-date-menu [aria-selected='true'] { background: #e5f4ee; font-weight: 700; }
  .relative-date-menu small { opacity: .7; font-variant-numeric: tabular-nums; }
</style>
