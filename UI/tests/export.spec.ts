import { expect, test } from '@playwright/test';
import ExcelJS from 'exceljs';
import { sanitizeFileName, sanitizeSheetName, tableExportCsv, tableExportJson, tableExportXlsx, type TableExportDefinition } from '../src/lib/export';

const definition: TableExportDefinition = {
  fileName: 'unsafe:/table.xlsx',
  sheetName: "A/very*long?worksheet[name]:that is unsafe",
  columns: [
    { key: 'name', label: 'Name' },
    { key: 'amount', label: 'Amount', kind: 'number', numberFormat: '#,##0.00' },
    { key: 'when', label: 'When', kind: 'datetime' },
    { key: 'active', label: 'Active', kind: 'boolean' },
    { key: 'empty', label: 'Empty' }
  ],
  rows: [
    { name: 'Alpha, "quoted"', amount: 12.5, when: new Date('2026-08-02T10:00:00Z'), active: true, empty: null },
    { name: '=HYPERLINK("bad")', amount: -2, when: '2026-08-02T11:00:00Z', active: false, empty: undefined },
    { name: 'Café 東京', amount: 0, when: 'not-a-date', active: true, empty: '' }
  ],
  summaryRows: [{ name: 'Total', amount: 10.5 }]
};

test('serializes normalized JSON rows and summaries', () => {
  expect(JSON.parse(tableExportJson(definition))).toEqual({
    rows: [
      { name: 'Alpha, "quoted"', amount: 12.5, when: '2026-08-02T10:00:00.000Z', active: true, empty: null },
      { name: '=HYPERLINK("bad")', amount: -2, when: '2026-08-02T11:00:00Z', active: false, empty: null },
      { name: 'Café 東京', amount: 0, when: 'not-a-date', active: true, empty: '' }
    ],
    summaries: [{ name: 'Total', amount: 10.5, when: null, active: null, empty: null }]
  });
});

test('creates UTF-8 CSV with quoting, formula protection, and summaries', () => {
  const csv = tableExportCsv(definition);
  expect(csv.startsWith('\uFEFF')).toBeTruthy();
  expect(csv).toContain('"Alpha, ""quoted"""');
  expect(csv).toContain('"\'=HYPERLINK(""bad"")"');
  expect(csv).toContain('\r\n\r\n"Total","10.5"');
});

test('creates a valid typed XLSX workbook', async () => {
  const bytes = await tableExportXlsx(definition);
  const workbook = new ExcelJS.Workbook();
  await workbook.xlsx.load(bytes);
  const worksheet = workbook.worksheets[0];

  expect(worksheet.name).toBe('A-very-long-worksheet-name--tha');
  expect(worksheet.getCell('B2').value).toBe(12.5);
  expect(worksheet.getCell('C2').value).toBeInstanceOf(Date);
  expect(worksheet.getCell('A3').value).toBe('\'=HYPERLINK("bad")');
  expect(worksheet.getCell('A6').value).toBe('Total');
  expect(worksheet.getCell('A6').font.bold).toBeTruthy();
});

test('sanitizes file and worksheet names', () => {
  expect(sanitizeFileName(' unsafe:/table.xlsx ')).toBe('unsafe--table');
  expect(sanitizeSheetName("[]:*?/\\''")).toBe('-------');
});

test('serializes an empty table without inventing rows', async () => {
  const empty: TableExportDefinition = {
    fileName: 'empty',
    columns: [{ key: 'name', label: 'Name' }],
    rows: []
  };

  expect(JSON.parse(tableExportJson(empty))).toEqual({ rows: [], summaries: [] });
  expect(tableExportCsv(empty)).toBe('\uFEFF"Name"');

  const bytes = await tableExportXlsx(empty);
  const workbook = new ExcelJS.Workbook();
  await workbook.xlsx.load(bytes);
  expect(workbook.worksheets[0].rowCount).toBe(1);
});
