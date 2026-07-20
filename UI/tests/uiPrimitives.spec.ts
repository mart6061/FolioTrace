import { expect, test } from '@playwright/test';
import { readFile } from 'node:fs/promises';

async function source(relativePath: string) {
  return readFile(new URL('../' + relativePath, import.meta.url), 'utf8');
}

test('Card exposes semantic intents and PageCard remains an adapter', async () => {
  const card = await source('src/lib/components/page/Card.svelte');
  const pageCard = await source('src/lib/components/page/PageCard.svelte');

  expect(card).toContain("'content' | 'filter' | 'data' | 'success' | 'warning' | 'error'");
  expect(pageCard).toContain("accent === 'gold' ? 'filter' : 'content'");
});

test('ComplexSelect owns rich single and multiple option rendering', async () => {
  const select = await source('src/lib/components/forms/ComplexSelect.svelte');

  expect(select).toContain('generics="T extends SelectValue"');
  expect(select).toContain('aria-multiselectable={multiple || undefined}');
  expect(select).toContain('option.meta');
  expect(select).toContain('option.badge');
  expect(select).toContain('complex-select-check-icon');
  expect(select).toContain('{#each values as selectedValue');
});

test('Field supports explicitly labelled composite controls', async () => {
  const field = await source('src/lib/components/forms/Field.svelte');

  expect(field).toContain('controlId?: string');
  expect(field).toContain('for={controlId}');
  expect(field).toContain("dense && 'house-field-dense'");
});

test('Ideas consumes the canonical primitives without legacy wrappers', async () => {
  const ideas = await source('src/routes/Ideas/+page.svelte');
  const appCss = await source('src/app.css');

  expect(ideas).not.toContain('<PageCard');
  expect(ideas).not.toContain('create-ticket-field');
  expect(ideas).not.toContain('.complex-select-trigger');
  expect(ideas).toContain('<Card');
  expect(ideas).toContain('<Field');
  expect(appCss).not.toContain('.create-ticket-field');
});
