import type { Action } from 'svelte/action';

export type CloseOnOutsideOptions = {
  close: () => void;
  enabled?: boolean;
};

export type FloatingPopoverOptions = CloseOnOutsideOptions;

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

export const floatingPopover: Action<HTMLElement, FloatingPopoverOptions> = (node, options) => {
  let current = options;
  let positionFrame = 0;

  function supportsPopover() {
    return typeof node.showPopover === 'function' && typeof node.hidePopover === 'function';
  }

  function anchorElement() {
    return node.parentElement?.querySelector<HTMLElement>(':scope > summary, :scope > button') ?? null;
  }

  function positionPopover() {
    positionFrame = 0;

    if (!current?.enabled || !node.matches(':popover-open'))
      return;

    const anchor = anchorElement();
    if (!anchor)
      return;

    const viewportPadding = 8;
    const gap = 6;
    const anchorRect = anchor.getBoundingClientRect();
    const viewportWidth = window.innerWidth;
    const viewportHeight = window.innerHeight;

    node.style.position = 'fixed';
    node.style.inset = 'auto';
    node.style.margin = '0';
    node.style.minWidth = `${anchorRect.width}px`;

    const configuredMaxWidth = Number.parseFloat(getComputedStyle(node).maxWidth);
    const maximumWidth = Math.min(
      Number.isFinite(configuredMaxWidth) ? configuredMaxWidth : viewportWidth - viewportPadding * 2,
      viewportWidth - viewportPadding * 2
    );
    const width = Math.min(Math.max(anchorRect.width, node.scrollWidth), maximumWidth);
    const left = Math.min(
      Math.max(viewportPadding, anchorRect.left),
      Math.max(viewportPadding, viewportWidth - width - viewportPadding)
    );

    node.style.width = `${width}px`;
    node.style.left = `${left}px`;

    const roomBelow = viewportHeight - anchorRect.bottom - gap - viewportPadding;
    const roomAbove = anchorRect.top - gap - viewportPadding;
    const openAbove = roomBelow < Math.min(node.scrollHeight, 240) && roomAbove > roomBelow;
    const availableHeight = Math.max(96, openAbove ? roomAbove : roomBelow);
    const configuredMaxHeight = Number.parseFloat(getComputedStyle(node).maxHeight);
    const maximumHeight = Number.isFinite(configuredMaxHeight)
      ? Math.min(configuredMaxHeight, availableHeight)
      : availableHeight;

    node.style.maxHeight = `${maximumHeight}px`;
    node.style.top = openAbove
      ? `${Math.max(viewportPadding, anchorRect.top - gap - Math.min(node.scrollHeight, maximumHeight))}px`
      : `${anchorRect.bottom + gap}px`;
  }

  function schedulePosition() {
    if (positionFrame)
      cancelAnimationFrame(positionFrame);

    positionFrame = requestAnimationFrame(positionPopover);
  }

  function syncPopover() {
    if (!supportsPopover())
      return;

    if (current?.enabled) {
      if (!node.matches(':popover-open'))
        node.showPopover();
      schedulePosition();
    } else if (node.matches(':popover-open')) {
      node.hidePopover();
    }
  }

  function handleToggle() {
    if (current?.enabled && !node.matches(':popover-open'))
      current.close();
  }

  node.addEventListener('toggle', handleToggle);
  window.addEventListener('resize', schedulePosition);
  window.addEventListener('scroll', schedulePosition, true);
  syncPopover();

  return {
    update(next) {
      current = next;
      syncPopover();
    },
    destroy() {
      if (positionFrame)
        cancelAnimationFrame(positionFrame);
      if (supportsPopover() && node.matches(':popover-open'))
        node.hidePopover();
      node.removeEventListener('toggle', handleToggle);
      window.removeEventListener('resize', schedulePosition);
      window.removeEventListener('scroll', schedulePosition, true);
    }
  };
};
