<script lang="ts">
  import type { InputControlPolicy } from '$lib/types';
  import PolicyDecimalInput from './PolicyDecimalInput.svelte';
  import type { ControlSize } from './controls';

  type Props = {
    bare?: boolean;
    class?: string;
    currency: string;
    disabled?: boolean;
    displayValue?: string;
    form?: string;
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
    bare = false,
    class: className = '',
    currency,
    disabled = false,
    displayValue = $bindable(''),
    form,
    formattedValue = $bindable(''),
    id,
    label = 'Price',
    name = 'price',
    policy,
    required = false,
    size = 'md',
    validationMessages = $bindable<string[]>([]),
    value = $bindable('')
  }: Props = $props();

  const resolvedLabel = $derived(currency ? `${label} (${currency})` : label);
</script>

<PolicyDecimalInput
  {bare}
  class={className}
  {disabled}
  bind:displayValue
  {form}
  bind:formattedValue
  {id}
  label={resolvedLabel}
  {name}
  {policy}
  {required}
  {size}
  bind:validationMessages
  bind:value
/>
