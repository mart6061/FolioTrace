<script lang="ts">
  import type { Snippet } from 'svelte';
  import type { HTMLButtonAttributes } from 'svelte/elements';
  import { buttonClass, type ButtonSize, type ButtonVariant } from './controls';

  type Props = Omit<HTMLButtonAttributes, 'size'> & {
    children?: Snippet;
    class?: string;
    fullWidth?: boolean;
    size?: ButtonSize;
    variant?: ButtonVariant;
  };

  let {
    children,
    class: className = '',
    fullWidth = false,
    size = 'md',
    type = 'button',
    variant = 'secondary',
    ...rest
  }: Props = $props();
</script>

<button {...rest} class={buttonClass(variant, size, fullWidth, className)} {type}>
  {@render children?.()}
</button>
