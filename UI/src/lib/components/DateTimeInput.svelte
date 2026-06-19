<script lang="ts">
  import { classNames, controlClass, type ControlSize } from '$lib/components/forms/controls';
  import { clampFutureInputDateTime, endOfDayForInput, nowForInput, startOfDayForInput } from '$lib/dates';

  type Props = {
    class?: string;
    disabled?: boolean;
    form?: string;
    fullWidth?: boolean;
    futureLimited?: boolean;
    invalid?: boolean;
    max?: string;
    min?: string;
    name?: string;
    onchange?: (event: Event) => void;
    required?: boolean;
    showShortcuts?: boolean;
    shortcutMode?: 'adjacent' | 'embedded';
    size?: ControlSize;
    step?: string | number;
    value?: string;
  };

  let {
    class: className = '',
    disabled = false,
    form,
    fullWidth = false,
    futureLimited = false,
    invalid = false,
    max,
    min,
    name,
    onchange,
    required = false,
    showShortcuts = true,
    shortcutMode = 'embedded',
    size = 'md',
    step = '1',
    value = $bindable('')
  }: Props = $props();

  let maxRefreshKey = $state(0);
  const effectiveMax = $derived(currentEffectiveMax(maxRefreshKey));
  const useEmbeddedShortcuts = $derived(showShortcuts && shortcutMode === 'embedded');
  const containerClass = $derived(
    classNames(
      'datetime-input-control',
      useEmbeddedShortcuts && 'datetime-input-control-embedded',
      `datetime-input-control-${size}`,
      fullWidth && 'datetime-input-control-full',
      invalid && 'datetime-input-control-invalid'
    )
  );
  const inputClass = $derived(controlClass(size, fullWidth, invalid, className));

  function currentEffectiveMax(_refreshKey: number) {
    return futureLimited ? nowForInput() : (max ?? '');
  }

  function refreshEffectiveMax() {
    maxRefreshKey += 1;
  }

  function clampToLimits(nextValue: string) {
    const maxLimit = futureLimited ? nowForInput() : max;
    let clamped = futureLimited ? clampFutureInputDateTime(nextValue) : nextValue;

    if (maxLimit && new Date(clamped).getTime() > new Date(maxLimit).getTime())
      clamped = maxLimit;

    return clamped;
  }

  function setShortcut(nextValue: string) {
    refreshEffectiveMax();
    value = clampToLimits(nextValue);
    onchange?.(new Event('change'));
  }

  function setStartOfDay() {
    setShortcut(startOfDayForInput(value));
  }

  function setEndOfDay() {
    setShortcut(endOfDayForInput(value));
  }

  function setNow() {
    setShortcut(nowForInput());
  }

  function handleChange(event: Event) {
    refreshEffectiveMax();

    if (futureLimited || max)
      value = clampToLimits(value);

    onchange?.(event);
  }
</script>

<span class={containerClass}>
  <input
    aria-invalid={invalid ? 'true' : undefined}
    class={inputClass}
    bind:value
    {disabled}
    {form}
    max={effectiveMax || undefined}
    {min}
    {name}
    {required}
    {step}
    onclick={refreshEffectiveMax}
    onchange={handleChange}
    onfocus={refreshEffectiveMax}
    type="datetime-local"
  />
  {#if showShortcuts}
    <span class="datetime-input-shortcuts" aria-label="Date time shortcuts">
      <button aria-label="Start of day" disabled={disabled} onclick={setStartOfDay} title="Start of day" type="button">S</button>
      <button aria-label="Now" disabled={disabled} onclick={setNow} title="Now" type="button">N</button>
      <button aria-label="End of day" disabled={disabled} onclick={setEndOfDay} title="End of day" type="button">E</button>
    </span>
  {/if}
</span>
