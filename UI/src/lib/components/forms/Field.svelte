<script lang="ts">
  import type { Snippet } from 'svelte';
  import { classNames } from './controls';

  type Props = {
    children?: Snippet;
    class?: string;
    error?: string;
    help?: string;
    inline?: boolean;
    label?: string;
    required?: boolean;
  };

  let {
    children,
    class: className = '',
    error,
    help,
    inline = false,
    label,
    required = false
  }: Props = $props();
</script>

<label class={classNames('house-field', inline && 'house-field-inline', className)}>
  {#if label}
    <span class="house-field-label">
      {label}
      {#if required}
        <span aria-hidden="true" class="house-field-required">*</span>
      {/if}
    </span>
  {/if}
  {@render children?.()}
  {#if error}
    <span class="house-field-message house-field-error">{error}</span>
  {:else if help}
    <span class="house-field-message">{help}</span>
  {/if}
</label>
