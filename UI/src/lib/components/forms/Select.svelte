<script lang="ts">
  import type { Snippet } from 'svelte';
  import type { HTMLSelectAttributes } from 'svelte/elements';
  import { controlClass, type ControlSize } from './controls';

  type SelectValue = string | string[] | number | undefined;
  type Props = Omit<HTMLSelectAttributes, 'size' | 'value'> & {
    children?: Snippet;
    class?: string;
    fullWidth?: boolean;
    invalid?: boolean;
    size?: ControlSize;
    value?: SelectValue;
  };

  let {
    children,
    class: className = '',
    fullWidth = false,
    invalid = false,
    size = 'md',
    value = $bindable<SelectValue>(undefined),
    ...rest
  }: Props = $props();
</script>

<select
  {...rest}
  aria-invalid={invalid ? 'true' : undefined}
  class={controlClass(size, fullWidth, invalid, className)}
  bind:value
>
  {@render children?.()}
</select>
