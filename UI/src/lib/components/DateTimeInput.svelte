<script lang="ts">
  import { clampFutureInputDateTime, endOfDayForInput, nowForInput, startOfDayForInput } from '$lib/dates';

  type Props = {
    class?: string;
    disabled?: boolean;
    form?: string;
    futureLimited?: boolean;
    max?: string;
    min?: string;
    name?: string;
    onchange?: (event: Event) => void;
    required?: boolean;
    step?: string | number;
    value?: string;
  };

  let {
    class: className = '',
    disabled = false,
    form,
    futureLimited = false,
    max,
    min,
    name,
    onchange,
    required = false,
    step = '1',
    value = $bindable('')
  }: Props = $props();

  let effectiveMax = $state('');

  $effect(() => {
    effectiveMax = futureLimited ? nowForInput() : (max ?? '');
  });

  function refreshEffectiveMax() {
    effectiveMax = futureLimited ? nowForInput() : (max ?? '');
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
    setShortcut(endOfDayForInput(value, step));
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

<span class="datetime-input-control">
  <input
    class={className}
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
  <span class="datetime-input-shortcuts" aria-label="Date time shortcuts">
    <button aria-label="Start of day" disabled={disabled} onclick={setStartOfDay} title="Start of day" type="button">S</button>
    <button aria-label="Now" disabled={disabled} onclick={setNow} title="Now" type="button">N</button>
    <button aria-label="End of day" disabled={disabled} onclick={setEndOfDay} title="End of day" type="button">E</button>
  </span>
</span>
