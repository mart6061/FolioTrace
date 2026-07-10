<script lang="ts">
  import { classNames } from './controls';
  import type { PillOption } from './types';

  type Props = {
    ariaLabel: string;
    class?: string;
    compact?: boolean;
    mode?: 'radio' | 'checkbox';
    name: string;
    onchange?: (event: Event) => void;
    options: PillOption[];
    value?: string;
    values?: string[];
  };

  let {
    ariaLabel,
    class: className = '',
    compact = false,
    mode = 'radio',
    name,
    onchange,
    options,
    value = $bindable(''),
    values = $bindable<string[]>([])
  }: Props = $props();

  function isChecked(optionValue: string) {
    return mode === 'radio' ? value === optionValue : values.includes(optionValue);
  }

  function handleChange(optionValue: string, event: Event) {
    if (mode === 'radio') {
      value = optionValue;
    } else {
      const checked = (event.currentTarget as HTMLInputElement).checked;
      values = checked ? [...values, optionValue] : values.filter((item) => item !== optionValue);
    }

    onchange?.(event);
  }
</script>

<div class={classNames('house-pill-group', compact && 'house-pill-group-compact', className)} role={mode === 'radio' ? 'radiogroup' : 'group'} aria-label={ariaLabel}>
  {#each options as option (option.value)}
    <label class={classNames('house-pill', option.tone && `house-pill-${option.tone}`)}>
      <input
        checked={isChecked(option.value)}
        disabled={option.disabled}
        {name}
        onchange={(event) => handleChange(option.value, event)}
        type={mode}
        value={option.value}
      />
      <span>{option.label}</span>
    </label>
  {/each}
</div>
