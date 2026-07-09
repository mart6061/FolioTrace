export type ComplexSelectOption = {
  id: string;
  name: string;
  meta?: string;
  search?: string;
};

export type PillOption = {
  disabled?: boolean;
  label: string;
  tone?: 'buy' | 'sell' | 'default';
  value: string;
};
