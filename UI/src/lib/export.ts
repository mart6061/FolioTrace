export type TableExportCell = string | number | boolean | Date | null | undefined;

export type TableExportColumn = {
  key: string;
  label: string;
  kind?: 'text' | 'number' | 'boolean' | 'date' | 'datetime';
  numberFormat?: string;
};

export type TableExportRow = Record<string, TableExportCell>;

export type TableExportDefinition = {
  fileName: string;
  sheetName?: string;
  columns: readonly TableExportColumn[];
  rows: readonly TableExportRow[];
  summaryRows?: readonly TableExportRow[];
};

export type TableExportFormat = 'json' | 'csv' | 'xlsx';

const spreadsheetFormulaPattern = /^[=+\-@]/;

export function downloadFile(fileName: string, content: BlobPart, mimeType: string) {
  const blob = content instanceof Blob ? content : new Blob([content], { type: mimeType });
  const url = URL.createObjectURL(blob);
  const link = document.createElement('a');
  link.href = url;
  link.download = fileName;
  link.click();
  URL.revokeObjectURL(url);
}

export function csvValue(value: TableExportCell) {
  const text = typeof value === 'string' ? protectSpreadsheetText(value) : cellText(value);
  return `"${text.replaceAll('"', '""')}"`;
}

export function htmlValue(value: string) {
  return value.replaceAll('&', '&amp;').replaceAll('<', '&lt;').replaceAll('>', '&gt;').replaceAll('"', '&quot;').replaceAll("'", '&#39;');
}

export function tableExportJson(definition: TableExportDefinition) {
  const project = (row: TableExportRow) => Object.fromEntries(
    definition.columns.map((column) => [column.key, jsonCell(row[column.key])])
  );

  return JSON.stringify({
    rows: definition.rows.map(project),
    summaries: (definition.summaryRows ?? []).map(project)
  }, null, 2);
}

export function tableExportCsv(definition: TableExportDefinition) {
  const rowLine = (row: TableExportRow) => definition.columns.map((column) => csvValue(row[column.key])).join(',');
  const lines = [
    definition.columns.map((column) => csvValue(column.label)).join(','),
    ...definition.rows.map(rowLine)
  ];

  if (definition.summaryRows?.length)
    lines.push('', ...definition.summaryRows.map(rowLine));

  return `\uFEFF${lines.join('\r\n')}`;
}

export async function tableExportXlsx(definition: TableExportDefinition) {
  const module = await import('exceljs');
  const ExcelJS = module.default;
  const workbook = new ExcelJS.Workbook();
  workbook.creator = 'FolioTrace';
  workbook.created = new Date();

  const worksheet = workbook.addWorksheet(sanitizeSheetName(definition.sheetName || definition.fileName));
  worksheet.columns = definition.columns.map((column) => ({
    header: column.label,
    key: column.key,
    width: columnWidth(column, definition.rows, definition.summaryRows ?? [])
  }));
  worksheet.views = [{ state: 'frozen', ySplit: 1 }];
  worksheet.autoFilter = {
    from: { row: 1, column: 1 },
    to: { row: 1, column: Math.max(definition.columns.length, 1) }
  };

  const header = worksheet.getRow(1);
  header.font = { bold: true, color: { argb: 'FFFFFFFF' } };
  header.fill = { type: 'pattern', pattern: 'solid', fgColor: { argb: 'FF33418E' } };

  for (const row of definition.rows)
    worksheet.addRow(workbookRow(definition.columns, row));

  if (definition.summaryRows?.length) {
    worksheet.addRow([]);
    for (const summary of definition.summaryRows) {
      const row = worksheet.addRow(workbookRow(definition.columns, summary));
      row.font = { bold: true };
      row.fill = { type: 'pattern', pattern: 'solid', fgColor: { argb: 'FFE8EEF8' } };
    }
  }

  definition.columns.forEach((column, index) => {
    const numberFormat = column.numberFormat ?? (column.kind === 'date' ? 'yyyy-mm-dd' : column.kind === 'datetime' ? 'yyyy-mm-dd hh:mm:ss' : '');
    if (!numberFormat)
      return;
    worksheet.getColumn(index + 1).numFmt = numberFormat;
  });

  return workbook.xlsx.writeBuffer();
}

export async function exportTable(definition: TableExportDefinition, format: TableExportFormat) {
  const baseName = sanitizeFileName(definition.fileName);

  if (format === 'json') {
    downloadFile(`${baseName}.json`, tableExportJson(definition), 'application/json;charset=utf-8');
    return;
  }

  if (format === 'csv') {
    downloadFile(`${baseName}.csv`, tableExportCsv(definition), 'text/csv;charset=utf-8');
    return;
  }

  const buffer = await tableExportXlsx(definition);
  downloadFile(
    `${baseName}.xlsx`,
    buffer as BlobPart,
    'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet'
  );
}

export function sanitizeFileName(value: string) {
  const withoutExtension = value.trim().replace(/\.(json|csv|xlsx?|html)$/i, '');
  return withoutExtension
    .replace(/[<>:"/\\|?*\u0000-\u001F]/g, '-')
    .replace(/[. ]+$/g, '')
    .replace(/\s+/g, '-')
    .slice(0, 120) || 'export';
}

export function sanitizeSheetName(value: string) {
  const normalized = value.trim().replace(/[\\/?*\[\]:]/g, '-').replace(/^'+|'+$/g, '');
  return (normalized || 'Export').slice(0, 31);
}

function workbookRow(columns: readonly TableExportColumn[], row: TableExportRow) {
  return Object.fromEntries(columns.map((column) => [column.key, workbookCell(row[column.key], column.kind)]));
}

function workbookCell(value: TableExportCell, kind: TableExportColumn['kind']) {
  if (value === null || value === undefined)
    return null;
  if (value instanceof Date)
    return value;
  if (kind === 'date' || kind === 'datetime') {
    const date = new Date(String(value));
    return Number.isNaN(date.valueOf()) ? protectSpreadsheetText(String(value)) : date;
  }
  if (typeof value === 'string')
    return protectSpreadsheetText(value);
  return value;
}

function columnWidth(column: TableExportColumn, rows: readonly TableExportRow[], summaries: readonly TableExportRow[]) {
  const contentWidth = [...rows, ...summaries].reduce(
    (width, row) => Math.max(width, cellText(row[column.key]).length),
    column.label.length
  );
  return Math.min(Math.max(contentWidth + 2, 10), 48);
}

function protectSpreadsheetText(value: string) {
  return spreadsheetFormulaPattern.test(value.trimStart()) ? `'${value}` : value;
}

function cellText(value: TableExportCell) {
  if (value === null || value === undefined)
    return '';
  if (value instanceof Date)
    return value.toISOString();
  return String(value);
}

function jsonCell(value: TableExportCell) {
  return value instanceof Date ? value.toISOString() : value ?? null;
}
