<script lang="ts">
  import type { Broker } from '$lib/types';
  import ComplexSelect from './ComplexSelect.svelte';
  import type { ComplexSelectOption } from './types';

  type Props = {
    brokers: Broker[];
    class?: string;
    compactBrand?: boolean;
    disabled?: boolean;
    method?: 'FIX' | 'TradeFile';
    name?: string;
    placeholder?: string;
    selectedBrokerLEI?: string;
  };

  let {
    brokers,
    class: className = '',
    compactBrand = false,
    disabled = false,
    method,
    name = '',
    placeholder = 'Select broker',
    selectedBrokerLEI = $bindable('')
  }: Props = $props();

  const eligibleBrokers = $derived(
    brokers
      .filter((broker) => !method || (broker.active && brokerHasEnabledMethod(broker, method)))
      .sort((left, right) => left.name.localeCompare(right.name))
  );
  const options = $derived<ComplexSelectOption[]>(eligibleBrokers.map((broker) => ({
    id: broker.lei,
    name: broker.name,
    meta: broker.lei + (broker.active ? '' : ' - Inactive'),
    search: broker.name + ' ' + broker.lei + ' ' + broker.notes,
    tone: broker.active ? undefined : 'alert'
  })));

  function brokerHasEnabledMethod(broker: Broker, methodKind: 'FIX' | 'TradeFile') {
    return (broker.tradeMethods ?? [])
      .some((tradeMethod) => tradeMethod.type === methodKind && tradeMethod.enabled !== false);
  }
</script>

<ComplexSelect
  class={className}
  {compactBrand}
  {disabled}
  {name}
  {options}
  placeholder={options.length ? placeholder : 'No ' + (method ?? '') + ' brokers available'}
  bind:value={selectedBrokerLEI}
/>
