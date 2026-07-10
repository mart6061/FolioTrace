<script lang="ts">
  import { formatInputPolicyValue, inputPolicyStep, validateInputPolicyValue } from '$lib/inputPolicy';
  import type { InputControlPolicy } from '$lib/types';
  import Field from './Field.svelte';
  import TextInput from './TextInput.svelte';
  import type { ControlSize } from './controls';

  type Props = {
    class?: string;
    disabled?: boolean;
    displayValue?: string;
    formattedValue?: string;
    id?: string;
    label?: string;
    name: string;
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
    label,
    name,
    policy,
    required = false,
    size = 'md',
    validationMessages = $bindable<string[]>([]),
    value = $bindable('')
  }: Props = $props();

  let inputValue = $state('');

  const fieldID = $derived(id ?? name);
  const errorText = $derived(validationMessages.join(' '));
  const step = $derived(inputPolicyStep(policy));

  $effect(() => {
    const sourceValue = displayValue || value;
    const validation = validateInputPolicyValue(sourceValue, policy);

    inputValue = sourceValue;
    formattedValue = sourceValue ? formatInputPolicyValue(sourceValue, policy) : '';
    validationMessages = validation.messages;
  });

  function updateValue(rawValue: string) {
    const validation = validateInputPolicyValue(rawValue, policy);

    inputValue = rawValue;
    value = validation.canonicalValue;
    displayValue = rawValue;
    formattedValue = formatInputPolicyValue(rawValue, policy);
    validationMessages = validation.messages;
  }

  function handleInput(event: Event) {
    updateValue((event.currentTarget as HTMLInputElement).value);
  }

  function handleBlur() {
    const formatted = formatInputPolicyValue(inputValue, policy);

    inputValue = formatted;
    displayValue = formatted;
    formattedValue = formatted;
  }

  function handleFocus() {
    inputValue = value;
    displayValue = value;
  }
</script>

<Field class={className} error={errorText} {label} {required}>
  <TextInput
    aria-label={label}
    autocomplete="off"
    {disabled}
    fullWidth
    id={fieldID}
    inputmode="decimal"
    invalid={Boolean(errorText)}
    {name}
    onblur={handleBlur}
    onfocus={handleFocus}
    oninput={handleInput}
    pattern="-?[0-9,]*(\.[0-9]*)?"
    {size}
    {step}
    type="text"
    bind:value={inputValue}
  />
</Field>
