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
  return new Intl.DateTimeFormat(undefined, {
    dateStyle: 'medium',
    timeStyle: 'short'
  }).format(new Date(value));
}
