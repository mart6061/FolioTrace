<script lang="ts">
  import type { Snippet } from 'svelte';
  import { closeOnOutside, floatingPopover } from '$lib/actions/dropdown';
  import { classNames } from './controls';

  type Props = {
    children?: Snippet;
    class?: string;
    close?: () => void;
    compactBrand?: boolean;
    disabled?: boolean;
    ontoggle?: (event: Event) => void;
    open?: boolean;
    summary: string;
  };

  let {
    children,
    class: className = '',
    close,
    compactBrand = false,
    disabled = false,
    ontoggle,
    open = $bindable(false),
    summary
  }: Props = $props();

  function preventDisabledToggle(event: MouseEvent) {
    if (!disabled)
      return;

    event.preventDefault();
  }

  function closeDropdown() {
    if (close) {
      close();
    } else {
      open = false;
    }
  }
</script>

<details
  aria-disabled={disabled ? 'true' : undefined}
  bind:open
  class={classNames('house-multiselect', compactBrand && 'house-multiselect-compact-brand', className)}
  ontoggle={ontoggle}
  use:closeOnOutside={{ close: closeDropdown, enabled: open }}
>
  <summary onclick={preventDisabledToggle}>
    <span class="truncate">{summary}</span>
  </summary>
  <div
    class="house-multiselect-options"
    popover="auto"
    use:floatingPopover={{ close: closeDropdown, enabled: open }}
  >
    {@render children?.()}
  </div>
</details>
