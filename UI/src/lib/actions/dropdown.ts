import type { Action } from 'svelte/action';

export type CloseOnOutsideOptions = {
  close: () => void;
  enabled?: boolean;
};

export const closeOnOutside: Action<HTMLElement, CloseOnOutsideOptions> = (node, options) => {
  let current = options;

  function isEnabled() {
    return current?.enabled ?? true;
  }

  function isInside(event: Event) {
    const target = event.target;
    return target instanceof Node && node.contains(target);
  }

  function closeIfOutside(event: Event) {
    if (!isEnabled() || isInside(event))
      return;

    current?.close();
  }

  function closeOnEscape(event: KeyboardEvent) {
    if (!isEnabled() || event.key !== 'Escape')
      return;

    current?.close();
  }

  document.addEventListener('pointerdown', closeIfOutside, true);
  document.addEventListener('focusin', closeIfOutside, true);
  document.addEventListener('keydown', closeOnEscape, true);

  return {
    update(next) {
      current = next;
    },
    destroy() {
      document.removeEventListener('pointerdown', closeIfOutside, true);
      document.removeEventListener('focusin', closeIfOutside, true);
      document.removeEventListener('keydown', closeOnEscape, true);
    }
  };
};
