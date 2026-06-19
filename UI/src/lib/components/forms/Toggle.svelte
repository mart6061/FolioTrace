<script lang="ts">
  import type { HTMLInputAttributes } from 'svelte/elements';
  import { classNames } from './controls';

  type Props = Omit<HTMLInputAttributes, 'checked' | 'type'> & {
    checked?: boolean;
    class?: string;
    label: string;
    labelVisible?: boolean;
  };

  let {
    checked = $bindable(false),
    class: className = '',
    disabled = false,
    label,
    labelVisible = true,
    value = 'true',
    ...rest
  }: Props = $props();
</script>

<label class={classNames('house-toggle-field', className)}>
  {#if labelVisible}
    <span>{label}</span>
  {/if}
  <span class="trace-toggle">
    <input {...rest} aria-label={label} bind:checked {disabled} type="checkbox" {value} />
    <span aria-hidden="true"></span>
  </span>
</label>
