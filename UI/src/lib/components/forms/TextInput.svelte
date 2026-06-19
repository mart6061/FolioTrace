<script lang="ts">
  import type { HTMLInputAttributes } from 'svelte/elements';
  import { controlClass, type ControlSize } from './controls';

  type InputValue = string | number | undefined;
  type Props = Omit<HTMLInputAttributes, 'size' | 'value'> & {
    class?: string;
    fullWidth?: boolean;
    invalid?: boolean;
    size?: ControlSize;
    value?: InputValue;
  };

  let {
    class: className = '',
    fullWidth = false,
    invalid = false,
    size = 'md',
    type = 'text',
    value = $bindable<InputValue>(''),
    ...rest
  }: Props = $props();
</script>

<input
  {...rest}
  aria-invalid={invalid ? 'true' : undefined}
  class={controlClass(size, fullWidth, invalid, className)}
  {type}
  bind:value
/>
