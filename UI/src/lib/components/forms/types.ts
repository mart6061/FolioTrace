export type SelectValue = string | number;

export type ComplexSelectOption<T extends SelectValue = string> = {
  badge?: string;
  badgeTone?: 'default' | 'positive';
  disabled?: boolean;
  id: T;
  name: string;
  meta?: string;
  search?: string;
  summary?: string;
  tone?: 'default' | 'alert';
};

export type PillOption = {
  disabled?: boolean;
  label: string;
  tone?: 'buy' | 'sell' | 'default';
  value: string;
};
