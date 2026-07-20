<script lang="ts">
  import type { Snippet } from 'svelte';
  import { classNames } from './controls';

  type Props = {
    children?: Snippet;
    class?: string;
    controlId?: string;
    dense?: boolean;
    error?: string;
    help?: string;
    inline?: boolean;
    label?: string;
    required?: boolean;
  };

  let {
    children,
    class: className = '',
    controlId,
    dense = false,
    error,
    help,
    inline = false,
    label,
    required = false
  }: Props = $props();
</script>

{#snippet fieldLabel()}
  {label}
  {#if required}
    <span aria-hidden="true" class="house-field-required">*</span>
  {/if}
{/snippet}

{#if controlId}
  <div class={classNames('house-field', dense && 'house-field-dense', inline && 'house-field-inline', className)}>
    {#if label}
      <label class="house-field-label" for={controlId}>{@render fieldLabel()}</label>
    {/if}
    {@render children?.()}
    {#if error}
      <span class="house-field-message house-field-error">{error}</span>
    {:else if help}
      <span class="house-field-message">{help}</span>
    {/if}
  </div>
{:else}
  <label class={classNames('house-field', dense && 'house-field-dense', inline && 'house-field-inline', className)}>
    {#if label}
      <span class="house-field-label">{@render fieldLabel()}</span>
    {/if}
    {@render children?.()}
    {#if error}
      <span class="house-field-message house-field-error">{error}</span>
    {:else if help}
      <span class="house-field-message">{help}</span>
    {/if}
  </label>
{/if}
