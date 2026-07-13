export type ComplexSelectOption = {
  badge?: string;
  badgeTone?: 'default' | 'positive';
  id: string;
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
