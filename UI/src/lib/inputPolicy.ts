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

/**
 * Moves the decimal point by `places` powers of ten, working on the text rather than the number so that
 * repeated scaling stays exact. Dividing by 100 in floating point turns "0.12" into 0.0012000000000000001,
 * which would then fail the decimal place check in validateInputPolicyValue.
 */
export function shiftDecimalText(value: string, places: number) {
  const canonical = canonicalDecimalText(value);

  if (!canonical || canonical === '-' || canonical === '.' || canonical === '-.' || places === 0)
    return canonical;

  const negative = canonical.startsWith('-');
  const unsigned = negative ? canonical.slice(1) : canonical;
  const [integerPart = '', decimalPart = ''] = unsigned.split('.');
  const digits = `${integerPart}${decimalPart}`;

  let shiftedDigits = digits;
  let pointIndex = integerPart.length + places;

  if (pointIndex <= 0) {
    shiftedDigits = `${'0'.repeat(1 - pointIndex)}${digits}`;
    pointIndex = 1;
  } else if (pointIndex > digits.length) {
    shiftedDigits = `${digits}${'0'.repeat(pointIndex - digits.length)}`;
  }

  const nextInteger = shiftedDigits.slice(0, pointIndex).replace(/^0+(?=\d)/, '') || '0';
  // Padding above can leave trailing zeros that the original value did not have, which would make the shift
  // irreversible: 0.1 displayed as 10 would otherwise come back as 0.10.
  const nextDecimal = shiftedDigits.slice(pointIndex).replace(/0+$/, '');

  return `${negative ? '-' : ''}${nextInteger}${nextDecimal ? `.${nextDecimal}` : ''}`;
}

/** Converts text the user sees into the value that is stored and sent to the API. */
export function toStoredValue(displayText: string, displayExponent = 0) {
  return shiftDecimalText(displayText, -displayExponent);
}

/** Converts a stored value into the units the user sees. */
export function toDisplayText(storedValue: string, displayExponent = 0) {
  return shiftDecimalText(storedValue, displayExponent);
}

/**
 * Formats a value for display. `value` is in display units; the policy's limits are in stored units, so the
 * decimal place cap is reduced by `displayExponent` to describe the same precision on the displayed scale.
 */
export function formatInputPolicyValue(value: string, policy: InputControlPolicy, displayExponent = 0) {
  const canonical = canonicalDecimalText(value);
  const validation = validateInputPolicyValue(toStoredValue(canonical, displayExponent), policy);

  if (validation.parsedValue === null || validation.messages.length)
    return value;

  return formatDecimal(Number(canonical), policy.formatPattern, Math.max(0, policy.decimalPlaces - displayExponent));
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

export function inputPolicyStep(policy: InputControlPolicy, displayExponent = 0) {
  const displayDecimalPlaces = policy.decimalPlaces - displayExponent;

  if (displayDecimalPlaces <= 0)
    return '1';

  return `0.${'0'.repeat(displayDecimalPlaces - 1)}1`;
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
