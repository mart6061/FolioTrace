<script lang="ts">
  import type { HTMLTextareaAttributes } from 'svelte/elements';
  import { controlClass, type ControlSize } from './controls';

  type Props = Omit<HTMLTextareaAttributes, 'value'> & {
    class?: string;
    fullWidth?: boolean;
    invalid?: boolean;
    size?: ControlSize;
    value?: string;
  };

  let {
    class: className = '',
    fullWidth = false,
    invalid = false,
    size = 'md',
    value = $bindable(''),
    ...rest
  }: Props = $props();
</script>

<textarea
  {...rest}
  aria-invalid={invalid ? 'true' : undefined}
  class={controlClass(size, fullWidth, invalid, className)}
  bind:value
></textarea>
