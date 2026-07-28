import type { Action } from 'svelte/action';

/**
 * Shared drag and drop behaviour for reordering lists and trees, and for moving items between buckets.
 *
 * The kind of thing being dragged is encoded in the MIME type rather than the payload, because a dragover
 * handler can read `dataTransfer.types` but not `getData`. A drop zone therefore has to decide whether it
 * accepts a drag before it can see what the drag actually carries.
 */

const mimePrefix = 'application/x-foliotrace-';

export const dragMimeType = (kind: string) => `${mimePrefix}${kind}`;

export type DraggableOptions = {
  /** Groups drags so a zone can accept some and ignore others, for example a palette item versus a placed one. */
  kind: string;
  value: string;
  /** `copy` for drags out of a palette that leave the original in place, `move` for anything else. */
  effect?: 'copy' | 'move';
  onstart?: (value: string) => void;
  onend?: () => void;
};

export type DropZoneOptions = {
  accepts: readonly string[];
  ondrop: (kind: string, value: string, event: DragEvent) => void;
  /**
   * Rejects a drag the zone would otherwise accept, for moves that are structurally impossible such as
   * dropping a tree node into its own descendant. Declining here leaves the browser showing its no-drop
   * cursor, which is the honest signal; accepting and then ignoring the drop is not.
   */
  canDrop?: (kind: string, event: DragEvent) => boolean;
  /** Called with true while an accepted drag is over the zone, and false when it leaves or drops. */
  onhover?: (over: boolean) => void;
};

export const draggable: Action<HTMLElement, DraggableOptions> = (node, options) => {
  let current = options;

  function handleDragStart(event: DragEvent) {
    // Nested draggables would otherwise all fire, and the outermost would win.
    event.stopPropagation();

    if (!event.dataTransfer)
      return;

    event.dataTransfer.setData(dragMimeType(current.kind), current.value);
    // Plain text keeps the drag meaningful outside the app, and to other drop targets.
    event.dataTransfer.setData('text/plain', current.value);
    event.dataTransfer.effectAllowed = current.effect ?? 'move';
    current.onstart?.(current.value);
  }

  function handleDragEnd() {
    current.onend?.();
  }

  node.draggable = true;
  node.addEventListener('dragstart', handleDragStart);
  node.addEventListener('dragend', handleDragEnd);

  return {
    update(next: DraggableOptions) {
      current = next;
    },
    destroy() {
      node.removeEventListener('dragstart', handleDragStart);
      node.removeEventListener('dragend', handleDragEnd);
    }
  };
};

export const dropZone: Action<HTMLElement, DropZoneOptions> = (node, options) => {
  let current = options;
  // dragenter and dragleave fire for descendants too, so a plain boolean flickers as the pointer moves
  // across children. Counting entries and leaves keeps the hover state steady.
  let depth = 0;

  function acceptedKind(event: DragEvent) {
    const types = event.dataTransfer?.types ?? [];
    const kind = current.accepts.find((candidate) => types.includes(dragMimeType(candidate)));

    if (kind === undefined)
      return undefined;

    return current.canDrop?.(kind, event) === false ? undefined : kind;
  }

  function handleDragEnter(event: DragEvent) {
    if (!acceptedKind(event))
      return;

    depth += 1;

    if (depth === 1)
      current.onhover?.(true);
  }

  function handleDragOver(event: DragEvent) {
    if (!acceptedKind(event))
      return;

    // Only preventDefault for a drag this zone accepts; otherwise the browser keeps its "no drop" cursor.
    event.preventDefault();
    event.stopPropagation();

    if (event.dataTransfer)
      event.dataTransfer.dropEffect = event.dataTransfer.effectAllowed === 'copy' ? 'copy' : 'move';
  }

  function handleDragLeave(event: DragEvent) {
    if (!acceptedKind(event))
      return;

    depth = Math.max(0, depth - 1);

    if (depth === 0)
      current.onhover?.(false);
  }

  function handleDrop(event: DragEvent) {
    const kind = acceptedKind(event);

    if (!kind)
      return;

    event.preventDefault();
    event.stopPropagation();
    depth = 0;
    current.onhover?.(false);

    const value = event.dataTransfer?.getData(dragMimeType(kind)) ?? '';

    if (value)
      current.ondrop(kind, value, event);
  }

  node.addEventListener('dragenter', handleDragEnter);
  node.addEventListener('dragover', handleDragOver);
  node.addEventListener('dragleave', handleDragLeave);
  node.addEventListener('drop', handleDrop);

  return {
    update(next: DropZoneOptions) {
      current = next;
    },
    destroy() {
      node.removeEventListener('dragenter', handleDragEnter);
      node.removeEventListener('dragover', handleDragOver);
      node.removeEventListener('dragleave', handleDragLeave);
      node.removeEventListener('drop', handleDrop);
    }
  };
};
