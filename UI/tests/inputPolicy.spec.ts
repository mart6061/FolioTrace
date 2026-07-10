import { expect, test } from '@playwright/test';
import {
  canonicalDecimalText,
  formatInputPolicyValue,
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
