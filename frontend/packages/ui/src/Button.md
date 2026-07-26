# Button

## When to use

The default action control for the kit. Use `primary` for the single main action on a screen (save, submit), `secondary` for cancel/dismiss actions, `destructive` for irreversible actions (delete a race, remove a runner), and `info` for navigation/view-details actions that aren't destructive but aren't the primary CTA either.

Don't use more than one `primary` button per screen or dialog — it should always be obvious which action is the intended next step.

## Accessibility

- Renders a native `<button>` with `type="button"` by default (prevents accidental form submits when placed inside a `<form>`; pass `type="submit"` explicitly when you want submit behavior).
- Ships a visible `focus-visible` ring (`ring-2 ring-blue-700 ring-offset-1`) — do not override `className` in a way that removes `focus-visible:` utilities, or keyboard users lose focus tracking.
- Disabled state uses `disabled:opacity-60` plus the native `disabled` attribute — screen readers announce it automatically, no extra ARIA needed.

## Variants at a glance

| Variant | Use case |
|---|---|
| `primary` | Main action (submit, save) |
| `secondary` | Cancel, secondary action |
| `destructive` | Delete, irreversible actions |
| `info` | Navigation, view details |

Size prop: `md` (default) or `sm`. Use `sm` inside dense contexts (table row actions, compact toolbars), `md` everywhere else.
