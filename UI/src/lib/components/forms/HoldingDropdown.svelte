<script lang="ts">
  import type { Holding } from '$lib/types';
  import ComplexSelect from './ComplexSelect.svelte';
  import type { ComplexSelectOption } from './types';

  type Props = {
    accountID?: string;
    class?: string;
    compactBrand?: boolean;
    disabled?: boolean;
    holdings: Holding[];
    id?: string;
    multiple?: boolean;
    name?: string;
    nameOnlySummary?: boolean;
    placeholder?: string;
    showInstrumentID?: boolean;
    selectedHoldingID?: string;
    selectedHoldingIDs?: string[];
  };

  let {
    accountID = '',
    class: className = '',
    compactBrand = false,
    disabled = false,
    holdings,
    id,
    multiple = false,
    name = '',
    nameOnlySummary = false,
    placeholder = 'Select holding',
    showInstrumentID = true,
    selectedHoldingID = $bindable(''),
    selectedHoldingIDs = $bindable([])
  }: Props = $props();

  function holdingName(holding: Holding) {
    return holding.name || holding.holdingKind;
  }

  function holdingMeta(holding: Holding) {
    return [
      holding.holdingKind,
      showInstrumentID ? holding.instrumentID : '',
      holding.includeInValuation ? '' : 'Excluded',
      holding.active ? '' : 'Inactive'
    ].filter(Boolean).join(' - ');
  }

  const accountHoldings = $derived(holdings.filter((holding) => !accountID || holding.accountID === accountID));
  const sortedHoldings = $derived([...accountHoldings].sort((left, right) => holdingName(left).localeCompare(holdingName(right))));
  const options = $derived<ComplexSelectOption[]>(sortedHoldings.map((holding) => ({
    id: holding.holdingID,
    name: holdingName(holding),
    meta: holdingMeta(holding),
    search: `${holdingName(holding)} ${holding.holdingKind} ${holding.instrumentID} ${holding.active ? 'active' : 'inactive'} ${holding.includeInValuation ? 'valuation' : 'excluded'} ${holding.holdingID}`,
    summary: nameOnlySummary ? holdingName(holding) : `${holdingName(holding)} - ${holdingMeta(holding)}`,
    tone: holding.active ? undefined : 'alert'
  })));
</script>

<ComplexSelect
  ariaLabel="Holdings"
  class={className}
  {compactBrand}
  confirmSelection={multiple}
  {disabled}
  emptyText="No holdings match the selected account"
  {id}
  {multiple}
  {name}
  {options}
  {placeholder}
  searchPlaceholder="Search holdings"
  bind:value={selectedHoldingID}
  bind:values={selectedHoldingIDs}
/>
