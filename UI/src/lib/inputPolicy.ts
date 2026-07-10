import type { InputControlPolicy } from '$lib/types';

export type InputPolicyValidationResult = {
  canonicalValue: string;
  messages: string[];
  parsedValue: number | null;
};

export function canonicalDecimalText(value: string) {
  const trimmed = value.trim().replaceAll(',', '');

  if (!trimmed)
    return '';

  if (trimmed === '-' || trimmed === '.' || trimmed === '-.')
    return trimmed;

  const negative = trimmed.startsWith('-');
  const unsigned = negative ? trimmed.slice(1) : trimmed;
  const [rawInteger = '', ...decimalParts] = unsigned.split('.');
  const integerPart = rawInteger.replace(/^0+(?=\d)/, '') || '0';
  const decimalPart = decimalParts.join('');

  return `${negative ? '-' : ''}${integerPart}${trimmed.includes('.') ? `.${decimalPart}` : ''}`;
}

export function formatInputPolicyValue(value: string, policy: InputControlPolicy) {
  const canonical = canonicalDecimalText(value);
  const validation = validateInputPolicyValue(canonical, policy);

  if (validation.parsedValue === null || validation.messages.length)
    return value;

  return formatDecimal(validation.parsedValue, policy.formatPattern, policy.decimalPlaces);
}

export function validateInputPolicyValue(value: string, policy: InputControlPolicy): InputPolicyValidationResult {
  const canonicalValue = canonicalDecimalText(value);
  const messages = [...policy.validationMessages];

  if (!canonicalValue)
    return { canonicalValue, messages, parsedValue: null };

  if (!/^-?\d+(\.\d*)?$/.test(canonicalValue)) {
    messages.push('Value must be a number.');
    return { canonicalValue, messages, parsedValue: null };
  }

  if (canonicalValue === '-' || canonicalValue.endsWith('.'))
    return { canonicalValue, messages, parsedValue: null };

  const parsedValue = Number(canonicalValue);

  if (!Number.isFinite(parsedValue)) {
    messages.push('Value must be a number.');
    return { canonicalValue, messages, parsedValue: null };
  }

  if (!policy.allowNegative && parsedValue < 0)
    messages.push('Value must not be negative.');

  if (policy.minValue !== null && parsedValue < policy.minValue)
    messages.push(`Value must be at least ${formatLimit(policy.minValue)}.`);

  if (policy.maxValue !== null && parsedValue > policy.maxValue)
    messages.push(`Value must be no more than ${formatLimit(policy.maxValue)}.`);

  if (decimalPlaces(canonicalValue) > policy.decimalPlaces)
    messages.push(`Value can have at most ${policy.decimalPlaces} decimal places.`);

  return { canonicalValue, messages, parsedValue };
}

export function inputPolicyStep(policy: InputControlPolicy) {
  if (policy.decimalPlaces <= 0)
    return '1';

  return `0.${'0'.repeat(Math.max(0, policy.decimalPlaces - 1))}1`;
}

function formatDecimal(value: number, pattern: string, decimalPlacesCap: number) {
  const { fixedDecimals, optionalDecimals, useGrouping } = readPattern(pattern);
  const decimals = Math.min(decimalPlacesCap, fixedDecimals + optionalDecimals);
  const formatter = new Intl.NumberFormat('en-GB', {
    maximumFractionDigits: decimals,
    minimumFractionDigits: Math.min(fixedDecimals, decimals),
    useGrouping
  });

  return formatter.format(value);
}

function readPattern(pattern: string) {
  const [integerPart = '', decimalPart = ''] = pattern.split('.');

  return {
    fixedDecimals: [...decimalPart].filter((character) => character === '0').length,
    optionalDecimals: [...decimalPart].filter((character) => character === '#').length,
    useGrouping: integerPart.includes(',')
  };
}

function decimalPlaces(value: string) {
  const [, decimalPart = ''] = value.split('.');
  return decimalPart.length;
}

function formatLimit(value: number) {
  return new Intl.NumberFormat('en-GB', {
    maximumFractionDigits: 8,
    useGrouping: true
  }).format(value);
}
