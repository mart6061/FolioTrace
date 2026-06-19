function pad(value: number) {
  return value.toString().padStart(2, '0');
}

function inputDateOrNow(valueOrNow: string | Date = new Date()) {
  if (valueOrNow instanceof Date)
    return Number.isNaN(valueOrNow.getTime()) ? new Date() : valueOrNow;

  if (!valueOrNow)
    return new Date();

  const date = new Date(valueOrNow);
  return Number.isNaN(date.getTime()) ? new Date() : date;
}

function inputDateTime(date: Date) {
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}:${pad(date.getSeconds())}`;
}

function inputDate(date: Date) {
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}`;
}

export function todayEndForInput(now = new Date()) {
  return endOfDayForInput(now);
}

export function nowForInput(now = new Date()) {
  return inputDateTime(now);
}

export function dateTimeForInput(valueOrNow: string | Date = new Date()) {
  return inputDateTime(inputDateOrNow(valueOrNow));
}

export function dateForInput(valueOrNow: string | Date = new Date()) {
  return inputDate(inputDateOrNow(valueOrNow));
}

export function nextWorkingDayForInput(valueOrNow: string | Date = new Date()) {
  const date = inputDateOrNow(valueOrNow);
  const next = new Date(date.getFullYear(), date.getMonth(), date.getDate() + 1, date.getHours(), date.getMinutes(), date.getSeconds());

  while (next.getDay() === 0 || next.getDay() === 6)
    next.setDate(next.getDate() + 1);

  return inputDateTime(next);
}

export function nextWorkingDayDateForInput(valueOrNow: string | Date = new Date()) {
  const date = inputDateOrNow(valueOrNow);
  const next = new Date(date.getFullYear(), date.getMonth(), date.getDate() + 1);

  while (next.getDay() === 0 || next.getDay() === 6)
    next.setDate(next.getDate() + 1);

  return inputDate(next);
}

export function startOfDayForInput(valueOrNow: string | Date = new Date()) {
  const date = inputDateOrNow(valueOrNow);
  return inputDateTime(new Date(date.getFullYear(), date.getMonth(), date.getDate()));
}

export function endOfDayForInput(valueOrNow: string | Date = new Date()) {
  const date = inputDateOrNow(valueOrNow);
  const end = new Date(date.getFullYear(), date.getMonth(), date.getDate() + 1);

  end.setSeconds(end.getSeconds() - 1);

  return inputDateTime(end);
}

export function clampFutureInputDateTime(value: string, now = new Date()) {
  if (!value)
    return '';

  const date = new Date(value);

  if (Number.isNaN(date.getTime()))
    return value;

  return date.getTime() > now.getTime() ? nowForInput(now) : value;
}

export function toApiDateTime(value: string) {
  const date = new Date(value);

  if (isEndOfDayInput(date)) {
    const nextLocalMidnight = new Date(date.getFullYear(), date.getMonth(), date.getDate() + 1);
    const previousSecond = new Date(nextLocalMidnight.getTime() - 1000);

    return previousSecond.toISOString().replace('.000Z', '.9999999Z');
  }

  return date.toISOString();
}

export function dateInputToApiStartOfDay(value: string) {
  return `${value}T00:00:00.000Z`;
}

export function formatDateTime(value: string) {
  const date = new Date(value);

  if (date.getFullYear() <= 1)
    return 'now';

  if (date.getFullYear() >= 9999)
    return '-';

  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())} ${pad(date.getHours())}:${pad(date.getMinutes())}:${pad(date.getSeconds())}`;
}

export function formatDisplayDateTime(value: string) {
  const date = new Date(value);

  if (date.getFullYear() <= 1)
    return 'now';

  if (date.getFullYear() >= 9999)
    return '-';

  return new Intl.DateTimeFormat(undefined, {
    dateStyle: 'medium',
    timeStyle: 'medium'
  }).format(date);
}

export function formatTableDateTime(value: string) {
  const date = new Date(value);

  if (date.getFullYear() <= 1 || date.getFullYear() >= 9999)
    return '-';

  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())} ${pad(date.getHours())}:${pad(date.getMinutes())}:${pad(date.getSeconds())}`;
}

export function formatShortDate(value: string) {
  const date = new Date(value);

  if (Number.isNaN(date.getTime()) || date.getFullYear() <= 1 || date.getFullYear() >= 9999)
    return '-';

  return `${pad(date.getDate())}/${pad(date.getMonth() + 1)}/${date.getFullYear()}`;
}

export function isSameInputDateTime(left: string, right: string) {
  const leftDate = new Date(left);
  const rightDate = new Date(right);

  return !Number.isNaN(leftDate.getTime()) &&
    !Number.isNaN(rightDate.getTime()) &&
    leftDate.getFullYear() === rightDate.getFullYear() &&
    leftDate.getMonth() === rightDate.getMonth() &&
    leftDate.getDate() === rightDate.getDate();
}

function isEndOfDayInput(date: Date) {
  return !Number.isNaN(date.getTime()) &&
    date.getHours() === 23 &&
    date.getMinutes() === 59 &&
    date.getSeconds() === 59 &&
    date.getMilliseconds() === 0;
}
