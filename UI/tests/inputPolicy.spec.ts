import { expect, test } from '@playwright/test';
import {
  canonicalDecimalText,
  formatInputPolicyValue,
  inputPolicyStep,
  shiftDecimalText,
  toDisplayText,
  toStoredValue,
  validateInputPolicyValue
} from '../src/lib/inputPolicy';
import type { InputControlPolicy } from '../src/lib/types';

const quantityPolicy: InputControlPolicy = {
  allowNegative: false,
  controlKind: 'Quantity',
  currency: null,
  decimalPlaces: 4,
  formatPattern: '#,##0.####',
  formatSource: 'Global',
  maxValue: null,
  minValue: 0.0001,
  validationMessages: []
};

const moneyPolicy: InputControlPolicy = {
  allowNegative: false,
  controlKind: 'Money',
  currency: 'GBP',
  decimalPlaces: 2,
  formatPattern: '#,##0.00',
  formatSource: 'Global',
  maxValue: null,
  minValue: 0,
  validationMessages: []
};

test('normalizes grouped values to canonical decimal text', () => {
  expect(canonicalDecimalText('001,234.5000')).toBe('1234.5000');
});

test('formats values with the resolved decimal pattern', () => {
  expect(formatInputPolicyValue('1234.5', moneyPolicy)).toBe('1,234.50');
  expect(formatInputPolicyValue('1234.5678', quantityPolicy)).toBe('1,234.5678');
});

test('rejects too many decimal places without rounding', () => {
  const result = validateInputPolicyValue('123.45678', quantityPolicy);

  expect(result.canonicalValue).toBe('123.45678');
  expect(result.messages).toContain('Value can have at most 4 decimal places.');
});

test('negative money is controlled by the field policy', () => {
  const rejected = validateInputPolicyValue('-1.00', moneyPolicy);
  const allowed = validateInputPolicyValue('-1.00', { ...moneyPolicy, allowNegative: true, minValue: null });

  expect(rejected.messages).toContain('Value must not be negative.');
  expect(allowed.messages).not.toContain('Value must not be negative.');
});

const pricePolicy: InputControlPolicy = {
  allowNegative: false,
  controlKind: 'Price',
  currency: 'GBP',
  decimalPlaces: 8,
  formatPattern: '#,##0.00######',
  formatSource: 'Global',
  maxValue: null,
  minValue: 0,
  validationMessages: []
};

const percentPolicy: InputControlPolicy = {
  allowNegative: false,
  controlKind: 'Percent',
  currency: null,
  decimalPlaces: 6,
  formatPattern: '#,##0.####',
  formatSource: 'Global',
  maxValue: 1,
  minValue: 0,
  validationMessages: []
};

test('a price keeps its precision regardless of the currency', () => {
  const result = validateInputPolicyValue('123.45678912', pricePolicy);

  expect(result.messages).toEqual([]);
  expect(formatInputPolicyValue('123.45678912', pricePolicy)).toBe('123.45678912');
});

test('shifts the decimal point without floating point drift', () => {
  expect(shiftDecimalText('0.12', -2)).toBe('0.0012');
  expect(shiftDecimalText('0.0012', 2)).toBe('0.12');
  expect(shiftDecimalText('1.5', 2)).toBe('150');
  expect(shiftDecimalText('-0.075', 2)).toBe('-7.5');
  expect(shiftDecimalText('0', 2)).toBe('0');
  expect(shiftDecimalText('', 2)).toBe('');
});

test('percent values round trip between stored and displayed units', () => {
  for (const stored of ['0.0012', '0.1', '1', '0.000001', '0.123456']) {
    expect(toStoredValue(toDisplayText(stored, 2), 2)).toBe(stored);
  }

  // 0.12 / 100 in floating point is 0.0012000000000000001, which would fail the decimal place check.
  expect(toStoredValue('0.12', 2)).toBe('0.0012');
  expect(validateInputPolicyValue(toStoredValue('0.12', 2), percentPolicy).messages).toEqual([]);
});

test('percent limits are expressed as fractions', () => {
  expect(validateInputPolicyValue(toStoredValue('100', 2), percentPolicy).messages).toEqual([]);
  expect(validateInputPolicyValue(toStoredValue('150', 2), percentPolicy).messages)
    .toContain('Value must be no more than 1.');
});

test('percent formatting and step describe the displayed scale', () => {
  expect(formatInputPolicyValue('0.12', percentPolicy, 2)).toBe('0.12');
  expect(formatInputPolicyValue('12.3456', percentPolicy, 2)).toBe('12.3456');
  expect(inputPolicyStep(percentPolicy, 2)).toBe('0.0001');
  expect(inputPolicyStep(pricePolicy)).toBe('0.00000001');
});

test('a percent entered beyond the stored precision is rejected', () => {
  // Six fraction places is four places once shown as a percentage.
  const tooPrecise = validateInputPolicyValue(toStoredValue('0.12345', 2), percentPolicy);

  expect(tooPrecise.messages).toContain('Value can have at most 6 decimal places.');
});
