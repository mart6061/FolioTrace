export type ComplexSelectOption = {
  id: string;
  name: string;
  meta?: string;
  search?: string;
  tone?: 'default' | 'alert';
};

export type PillOption = {
  disabled?: boolean;
  label: string;
  tone?: 'buy' | 'sell' | 'default';
  value: string;
};
