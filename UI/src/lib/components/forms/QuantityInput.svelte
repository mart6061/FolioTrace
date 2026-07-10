<script lang="ts">
  import type { InputControlPolicy } from '$lib/types';
  import PolicyDecimalInput from './PolicyDecimalInput.svelte';
  import type { ControlSize } from './controls';

  type Props = {
    class?: string;
    disabled?: boolean;
    displayValue?: string;
    formattedValue?: string;
    id?: string;
    label?: string;
    name?: string;
    policy: InputControlPolicy;
    required?: boolean;
    size?: ControlSize;
    validationMessages?: string[];
    value?: string;
  };

  let {
    class: className = '',
    disabled = false,
    displayValue = $bindable(''),
    formattedValue = $bindable(''),
    id,
    label = 'Quantity',
    name = 'quantity',
    policy,
    required = false,
    size = 'md',
    validationMessages = $bindable<string[]>([]),
    value = $bindable('')
  }: Props = $props();
</script>

<PolicyDecimalInput
  class={className}
  {disabled}
  bind:displayValue
  bind:formattedValue
  {id}
  {label}
  {name}
  {policy}
  {required}
  {size}
  bind:validationMessages
  bind:value
/>
