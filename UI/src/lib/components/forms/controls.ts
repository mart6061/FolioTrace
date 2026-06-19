export type ControlSize = 'sm' | 'md' | 'compact';
export type ButtonSize = ControlSize | 'icon' | 'compactIcon';
export type ButtonVariant = 'primary' | 'secondary' | 'danger' | 'ghost';

export function classNames(...values: Array<string | false | null | undefined>) {
  return values.filter(Boolean).join(' ');
}

export function controlClass(size: ControlSize, fullWidth: boolean, invalid: boolean, className = '') {
  return classNames(
    'house-control',
    `house-control-${size}`,
    fullWidth && 'house-control-full',
    invalid && 'house-control-invalid',
    className
  );
}

export function buttonClass(variant: ButtonVariant, size: ButtonSize, fullWidth: boolean, className = '') {
  return classNames(
    'house-button',
    `house-button-${variant}`,
    `house-button-${size}`,
    fullWidth && 'house-button-full',
    className
  );
}
