function pad(value: number) {
  return value.toString().padStart(2, '0');
}

export function todayEndForInput(now = new Date()) {
  return `${now.getFullYear()}-${pad(now.getMonth() + 1)}-${pad(now.getDate())}T23:59:59`;
}

export function nowForInput(now = new Date()) {
  return `${now.getFullYear()}-${pad(now.getMonth() + 1)}-${pad(now.getDate())}T${pad(now.getHours())}:${pad(now.getMinutes())}:${pad(now.getSeconds())}`;
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
  return new Date(value).toISOString();
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
