function pad(value: number) {
  return value.toString().padStart(2, '0');
}

export function todayEndForInput(now = new Date()) {
  return `${now.getFullYear()}-${pad(now.getMonth() + 1)}-${pad(now.getDate())}T23:59`;
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

  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())} ${pad(date.getHours())}:${pad(date.getMinutes())}`;
}

export function formatTableDateTime(value: string) {
  const date = new Date(value);

  if (date.getFullYear() <= 1 || date.getFullYear() >= 9999)
    return '-';

  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())} ${pad(date.getHours())}:${pad(date.getMinutes())}`;
}
