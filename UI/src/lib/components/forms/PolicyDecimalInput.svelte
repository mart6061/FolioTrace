<script lang="ts">
  import { formatInputPolicyValue, inputPolicyStep, toDisplayText, toStoredValue, validateInputPolicyValue } from '$lib/inputPolicy';
  import type { InputControlPolicy } from '$lib/types';
  import Field from './Field.svelte';
  import TextInput from './TextInput.svelte';
  import { classNames, type ControlSize } from './controls';

  type Props = {
    /**
     * Renders the control on its own, without the Field label and message wrapper, for table and grid cells
     * where the column heading already labels the value. Validation messages move to the title attribute.
     */
    bare?: boolean;
    class?: string;
    disabled?: boolean;
    /**
     * Powers of ten between the stored value and the value shown to the user. Percent inputs pass 2 so that a
     * stored fraction of 0.0012 is entered and displayed as 0.12. Everything else stores what it shows.
     */
    displayExponent?: number;
    displayValue?: string;
    /** Associates the submitted value with a form by id, for inputs rendered outside their form element. */
    form?: string;
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
    bare = false,
    class: className = '',
    disabled = false,
    displayExponent = 0,
    displayValue = $bindable(''),
    form,
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
  const step = $derived(inputPolicyStep(policy, displayExponent));

  $effect(() => {
    const sourceValue = displayValue || toDisplayText(value, displayExponent);
    const validation = validateInputPolicyValue(toStoredValue(sourceValue, displayExponent), policy);

    inputValue = sourceValue;
    formattedValue = sourceValue ? formatInputPolicyValue(sourceValue, policy, displayExponent) : '';
    validationMessages = validation.messages;
  });

  function updateValue(rawValue: string) {
    const validation = validateInputPolicyValue(toStoredValue(rawValue, displayExponent), policy);

    inputValue = rawValue;
    value = validation.canonicalValue;
    displayValue = rawValue;
    formattedValue = formatInputPolicyValue(rawValue, policy, displayExponent);
    validationMessages = validation.messages;
  }

  function handleInput(event: Event) {
    updateValue((event.currentTarget as HTMLInputElement).value);
  }

  function handleBlur() {
    const formatted = formatInputPolicyValue(inputValue, policy, displayExponent);

    inputValue = formatted;
    displayValue = formatted;
    formattedValue = formatted;
  }

  function handleFocus() {
    const raw = toDisplayText(value, displayExponent);

    inputValue = raw;
    displayValue = raw;
  }
</script>

{#snippet control(fullWidth: boolean)}
  <!--
    The visible control carries no name. It holds display text, which is scaled for percentages and grouped
    with separators after blur, so submitting it would post 0.12 for a stored 0.0012, or 1 for "1,234.56".
    The hidden field posts the canonical stored value instead.
  -->
  <TextInput
    aria-label={label}
    autocomplete="off"
    {disabled}
    {fullWidth}
    id={fieldID}
    inputmode="decimal"
    invalid={Boolean(errorText)}
    onblur={handleBlur}
    onfocus={handleFocus}
    oninput={handleInput}
    pattern="-?[0-9,]*(\.[0-9]*)?"
    {required}
    {size}
    {step}
    title={errorText || undefined}
    type="text"
    bind:value={inputValue}
  />
  <input {disabled} {form} {name} type="hidden" {value} />
{/snippet}

{#if bare}
  <!-- display:contents so the control itself becomes the grid or table child, exactly as a raw input would. -->
  <span class={classNames('policy-decimal-input-bare', className)}>
    {@render control(true)}
  </span>
{:else}
  <Field class={className} controlId={fieldID} error={errorText} {label} {required}>
    {@render control(true)}
  </Field>
{/if}

<style>
  .policy-decimal-input-bare {
    display: contents;
  }
</style>
