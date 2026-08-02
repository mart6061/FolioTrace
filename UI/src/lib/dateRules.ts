import type { DateControlConfiguration, DateRangeRuleOption, DateRuleOption } from '$lib/types';
import { endOfDayForInput, nowForInput, startOfDayForInput } from '$lib/dates';

export type ResolvedDateRule = { value: string; expiresAt: Date };
export type ResolvedRangeRule = { start: string; end: string; expiresAt: Date };

const datePattern = /^(day|bd|week|month|quarter|year|fy)\.([+-]\d+)\.(start|end|at\(([01]\d|2[0-3]):([0-5]\d)\))$/;
const alignedRangePattern = /^range\.(day|bd|week|month|quarter|year|fy)\.([+-]\d+)$/;
const rollingRangePattern = /^range\.(next|last)\.([1-9]\d*)\.(day|week|month|quarter|year)$/;

export const defaultDateControlConfiguration: DateControlConfiguration = {
  financialYearStartMonth: 4,
  financialYearStartDay: 6,
  dateOptions: [
    dateOption('39f4e6b9-2b9d-4793-a377-1f2f4b500001', 'Today', 'day.+0.end', 1, true),
    dateOption('39f4e6b9-2b9d-4793-a377-1f2f4b500002', 'Now', 'now', 2),
    dateOption('39f4e6b9-2b9d-4793-a377-1f2f4b500003', 'Yesterday', 'day.-1.end', 3),
    dateOption('39f4e6b9-2b9d-4793-a377-1f2f4b500004', 'Last business day', 'bd.-1.end', 4),
    dateOption('39f4e6b9-2b9d-4793-a377-1f2f4b500005', 'End of last week', 'week.-1.end', 5),
    dateOption('39f4e6b9-2b9d-4793-a377-1f2f4b500006', 'End of last month', 'month.-1.end', 6),
    dateOption('39f4e6b9-2b9d-4793-a377-1f2f4b500007', 'T + 1', 'bd.+1.end', 7),
    dateOption('39f4e6b9-2b9d-4793-a377-1f2f4b500008', 'T + 2', 'bd.+2.end', 8),
    dateOption('39f4e6b9-2b9d-4793-a377-1f2f4b500009', 'T + 3', 'bd.+3.end', 9),
    { optionID: '39f4e6b9-2b9d-4793-a377-1f2f4b500010', kind: 'Custom', label: 'Custom', expression: null, displayOrder: 10, isDefault: false }
  ],
  rangeOptions: [
    rangeOption('4bdb6172-b590-427d-aa5f-734f94f30001', 'Today', 'range.day.+0', 1),
    rangeOption('4bdb6172-b590-427d-aa5f-734f94f30002', 'Yesterday', 'range.day.-1', 2),
    rangeOption('4bdb6172-b590-427d-aa5f-734f94f30003', 'Last business day', 'range.bd.-1', 3, true),
    rangeOption('4bdb6172-b590-427d-aa5f-734f94f30004', 'This week', 'range.week.+0', 4),
    rangeOption('4bdb6172-b590-427d-aa5f-734f94f30005', 'Last week', 'range.week.-1', 5),
    rangeOption('4bdb6172-b590-427d-aa5f-734f94f30006', 'This month', 'range.month.+0', 6),
    rangeOption('4bdb6172-b590-427d-aa5f-734f94f30007', 'Last month', 'range.month.-1', 7),
    rangeOption('4bdb6172-b590-427d-aa5f-734f94f30008', 'Month to date', 'range.mtd', 8),
    rangeOption('4bdb6172-b590-427d-aa5f-734f94f30009', 'Year to date', 'range.ytd', 9),
    { optionID: '4bdb6172-b590-427d-aa5f-734f94f30010', kind: 'Custom', label: 'Custom', expression: null, displayOrder: 10, isDefault: false }
  ]
};

export function cloneDateControlConfiguration(value: DateControlConfiguration): DateControlConfiguration {
  return { ...value, dateOptions: value.dateOptions.map((item) => ({ ...item })), rangeOptions: value.rangeOptions.map((item) => ({ ...item })) };
}

export function useGlobalFinancialYear(configuration: DateControlConfiguration, globalConfiguration: DateControlConfiguration): DateControlConfiguration {
  return {
    ...cloneDateControlConfiguration(configuration),
    financialYearStartMonth: globalConfiguration.financialYearStartMonth,
    financialYearStartDay: globalConfiguration.financialYearStartDay
  };
}

export function selectableDateOptions(configuration: DateControlConfiguration) {
  return [...configuration.dateOptions].filter((item) => item.kind !== 'Separator').sort((a, b) => a.displayOrder - b.displayOrder);
}

export function selectableRangeOptions(configuration: DateControlConfiguration) {
  return [...configuration.rangeOptions].filter((item) => item.kind !== 'Separator').sort((a, b) => a.displayOrder - b.displayOrder);
}

export function defaultDateOption(configuration: DateControlConfiguration) {
  const items = selectableDateOptions(configuration);
  return items.find((item) => item.isDefault) ?? items[0] ?? null;
}

export function defaultRangeOption(configuration: DateControlConfiguration) {
  const items = selectableRangeOptions(configuration);
  return items.find((item) => item.isDefault) ?? items[0] ?? null;
}

export function isValidDateExpression(value: string) { return value === 'now' || datePattern.test(value); }
export function isValidRangeExpression(value: string) { return value === 'range.mtd' || value === 'range.ytd' || alignedRangePattern.test(value) || rollingRangePattern.test(value); }

export function resolveDateRule(expression: string, configuration: DateControlConfiguration, now = new Date()): ResolvedDateRule {
  if (expression === 'now') return { value: nowForInput(now), expiresAt: new Date(now.getTime() + 1000) };
  const match = expression.match(datePattern);
  if (!match) throw new Error(`Unsupported date expression: ${expression}`);
  const [, period, rawOffset, boundary, hour = '00', minute = '00'] = match;
  const offset = Number(rawOffset);
  let date = startOfDay(now);
  if (period === 'day') date.setDate(date.getDate() + offset);
  else if (period === 'bd') date = addBusinessDays(date, offset);
  else if (period === 'week') { date = startOfWeek(date); date.setDate(date.getDate() + offset * 7); }
  else if (period === 'month') date = new Date(date.getFullYear(), date.getMonth() + offset, 1);
  else if (period === 'quarter') date = new Date(date.getFullYear(), Math.floor(date.getMonth() / 3) * 3 + offset * 3, 1);
  else if (period === 'year') date = new Date(date.getFullYear() + offset, 0, 1);
  else if (period === 'fy') { date = financialYearStart(date, configuration); date = recurringDate(date.getFullYear() + offset, configuration); }
  if (boundary === 'end') {
    if (period === 'week') date.setDate(date.getDate() + 6);
    else if (period === 'month') date = new Date(date.getFullYear(), date.getMonth() + 1, 0);
    else if (period === 'quarter') date = new Date(date.getFullYear(), date.getMonth() + 3, 0);
    else if (period === 'year') date = new Date(date.getFullYear(), 12, 0);
    else if (period === 'fy') { const next = recurringDate(date.getFullYear() + 1, configuration); next.setDate(next.getDate() - 1); date = next; }
    return { value: endOfDayForInput(date), expiresAt: expiry(period, now, configuration) };
  }
  if (boundary.startsWith('at(')) date.setHours(Number(hour), Number(minute), 0, 0);
  return { value: boundary === 'start' ? startOfDayForInput(date) : nowForInput(date), expiresAt: expiry(period, now, configuration) };
}

export function resolveRangeRule(expression: string, configuration: DateControlConfiguration, now = new Date()): ResolvedRangeRule {
  const today = startOfDay(now);
  let start: Date;
  let endExclusive: Date;
  let expiryPeriod = 'day';
  if (expression === 'range.mtd' || expression === 'range.ytd') {
    start = expression === 'range.mtd' ? new Date(today.getFullYear(), today.getMonth(), 1) : new Date(today.getFullYear(), 0, 1);
    endExclusive = addDays(today, 1);
  } else {
    const rolling = expression.match(rollingRangePattern);
    if (rolling) {
      const count = Number(rolling[2]);
      if (rolling[1] === 'next') { start = today; endExclusive = addUnits(start, count, rolling[3]); }
      else { endExclusive = addDays(today, 1); start = addUnits(endExclusive, -count, rolling[3]); }
    } else {
      const aligned = expression.match(alignedRangePattern);
      if (!aligned) throw new Error(`Unsupported range expression: ${expression}`);
      const period = aligned[1]; const offset = Number(aligned[2]); expiryPeriod = period;
      if (period === 'day') start = addDays(today, offset);
      else if (period === 'bd') start = addBusinessDays(today, offset);
      else if (period === 'week') start = addDays(startOfWeek(today), offset * 7);
      else if (period === 'month') start = new Date(today.getFullYear(), today.getMonth() + offset, 1);
      else if (period === 'quarter') start = new Date(today.getFullYear(), Math.floor(today.getMonth() / 3) * 3 + offset * 3, 1);
      else if (period === 'year') start = new Date(today.getFullYear() + offset, 0, 1);
      else { const fy = financialYearStart(today, configuration); start = recurringDate(fy.getFullYear() + offset, configuration); }
      if (period === 'day' || period === 'bd') endExclusive = addDays(start, 1);
      else if (period === 'week') endExclusive = addDays(start, 7);
      else if (period === 'month') endExclusive = new Date(start.getFullYear(), start.getMonth() + 1, 1);
      else if (period === 'quarter') endExclusive = new Date(start.getFullYear(), start.getMonth() + 3, 1);
      else if (period === 'year') endExclusive = new Date(start.getFullYear() + 1, 0, 1);
      else endExclusive = recurringDate(start.getFullYear() + 1, configuration);
    }
  }
  return { start: startOfDayForInput(start), end: endOfDayForInput(addDays(endExclusive, -1)), expiresAt: expiry(expiryPeriod, now, configuration) };
}

export function describeDateExpression(expression: string) {
  if (expression === 'now') return 'Now';
  const match = expression.match(datePattern); if (!match) return expression;
  const period = match[1]; const offset = Number(match[2]); const subject = relativePhrase(period, offset);
  return match[3] === 'start' ? `Start of ${subject}` : match[3] === 'end' ? `End of ${subject}` : `${match[4]} on ${subject}`;
}

export function describeRangeExpression(expression: string) {
  if (expression === 'range.mtd') return 'Month to date'; if (expression === 'range.ytd') return 'Year to date';
  const rolling = expression.match(rollingRangePattern); if (rolling) return `${rolling[1] === 'next' ? 'Next' : 'Last'} ${rolling[2]} ${pluralPeriod(rolling[3], Number(rolling[2]))}`;
  const aligned = expression.match(alignedRangePattern); return aligned ? title(relativePhrase(aligned[1], Number(aligned[2]))) : expression;
}

function dateOption(optionID: string, label: string, expression: string, displayOrder: number, isDefault = false): DateRuleOption { return { optionID, kind: 'Rule', label, expression, displayOrder, isDefault }; }
function rangeOption(optionID: string, label: string, expression: string, displayOrder: number, isDefault = false): DateRangeRuleOption { return { optionID, kind: 'Rule', label, expression, displayOrder, isDefault }; }
function startOfDay(value: Date) { return new Date(value.getFullYear(), value.getMonth(), value.getDate()); }
function startOfWeek(value: Date) { const date = startOfDay(value); date.setDate(date.getDate() - ((date.getDay() + 6) % 7)); return date; }
function addDays(value: Date, count: number) { const date = new Date(value); date.setDate(date.getDate() + count); return date; }
function addBusinessDays(value: Date, count: number) { let date = startOfDay(value); if (count === 0) { while ([0, 6].includes(date.getDay())) date = addDays(date, 1); return date; } let left = Math.abs(count); const direction = Math.sign(count); while (left) { date = addDays(date, direction); if (![0, 6].includes(date.getDay())) left--; } return date; }
function addUnits(value: Date, count: number, period: string) { const date = new Date(value); if (period === 'day') date.setDate(date.getDate() + count); else if (period === 'week') date.setDate(date.getDate() + count * 7); else if (period === 'month') date.setMonth(date.getMonth() + count); else if (period === 'quarter') date.setMonth(date.getMonth() + count * 3); else date.setFullYear(date.getFullYear() + count); return date; }
function recurringDate(year: number, config: DateControlConfiguration) { return new Date(year, config.financialYearStartMonth - 1, Math.min(config.financialYearStartDay, new Date(year, config.financialYearStartMonth, 0).getDate())); }
function financialYearStart(value: Date, config: DateControlConfiguration) { const current = recurringDate(value.getFullYear(), config); return value >= current ? current : recurringDate(value.getFullYear() - 1, config); }
function expiry(period: string, now: Date, config: DateControlConfiguration) { if (period === 'week') return addDays(startOfWeek(now), 7); if (period === 'month') return new Date(now.getFullYear(), now.getMonth() + 1, 1); if (period === 'quarter') return new Date(now.getFullYear(), Math.floor(now.getMonth() / 3) * 3 + 3, 1); if (period === 'year') return new Date(now.getFullYear() + 1, 0, 1); if (period === 'fy') return recurringDate(financialYearStart(now, config).getFullYear() + 1, config); return addDays(startOfDay(now), 1); }
function relativePhrase(period: string, offset: number) { if (period === 'bd') return offset === 0 ? 'T' : `T${offset > 0 ? '+' : '-'}${Math.abs(offset)}`; if (period === 'day' && offset === 0) return 'today'; if (period === 'day' && offset === 1) return 'tomorrow'; if (period === 'day' && offset === -1) return 'yesterday'; const name = period === 'fy' ? 'financial year' : period === 'year' ? 'calendar year' : period === 'bd' ? 'business day' : period; if (offset === 0) return `this ${name}`; if (offset === 1) return `next ${name}`; if (offset === -1) return `previous ${name}`; return offset > 1 ? `${offset} ${name}s from now` : `${Math.abs(offset)} ${name}s ago`; }
function pluralPeriod(period: string, count: number) { const name = period === 'year' ? 'year' : period; return count === 1 ? name : `${name}s`; }
function title(value: string) { return value.replace(/\b\w/g, (character) => character.toUpperCase()); }
