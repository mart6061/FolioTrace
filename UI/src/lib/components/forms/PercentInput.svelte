<script lang="ts">
  import type { InputControlPolicy } from '$lib/types';
  import PolicyDecimalInput from './PolicyDecimalInput.svelte';
  import type { ControlSize } from './controls';

  type Props = {
    bare?: boolean;
    class?: string;
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
    disabled = false,
    displayValue = $bindable(''),
    form,
    formattedValue = $bindable(''),
    id,
    label = 'Percent',
    name = 'percent',
    policy,
    required = false,
    size = 'md',
    validationMessages = $bindable<string[]>([]),
    value = $bindable('')
  }: Props = $props();
</script>

<!--
  `value` is the stored fraction: a bound value of 0.0012 is shown to the user as 0.12. The policy's decimal
  places and Min/Max are fractions too, so a "no more than 100%" ceiling is a MaxValue of 1 rather than 100.
-->
<PolicyDecimalInput
  {bare}
  class={className}
  {disabled}
  displayExponent={2}
  bind:displayValue
  {form}
  bind:formattedValue
  {id}
  label={`${label} (%)`}
  {name}
  {policy}
  {required}
  {size}
  bind:validationMessages
  bind:value
/>
