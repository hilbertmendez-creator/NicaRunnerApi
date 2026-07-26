Modal from @nicarunner/ui. Use via `window.NicaRunnerUI.Modal` (bundle loaded from the root `_ds_bundle.js`).

# Modal

## When to use

Use for focused, blocking tasks that need the user's full attention before returning to the underlying screen: confirming a destructive action, editing a single record (a race, a runner's result), or a short form that doesn't warrant its own page. Don't use Modal for multi-step wizards or content the user needs to reference alongside the rest of the page — those deserve a dedicated route instead.

## Accessibility

- Already wired: `role="dialog"`, `aria-modal="true"`, focus is moved to the first focusable descendant on mount, and restored to whatever had focus before the modal opened when it closes.
- Escape closes the modal; clicking the backdrop (not the card itself) also closes it. Both call `onClose` — you don't need to wire these yourself.
- **Always pass `labelledBy`** pointing at the `id` of your dialog's heading element (e.g. `<h2 id="new-race-title">`). Without it, `aria-labelledby` is empty and screen reader users aren't told what the dialog is for.
- Modal renders with `position: fixed; inset: 0` — always mount it at the top level of the page, never inside a `flex`/`grid` container, or the fixed positioning can behave unexpectedly relative to a transformed ancestor.

## Theming note

Modal's card background/border/radius come from `--bg-card`, `--bd-card`, `--radius-card` — these are app-level theme tokens (see the app's `data-theme` light/dark/brand definitions), not defined by this package. They have sensible fallback values baked in, so Modal still renders a visible card even outside a themed ancestor, but for the intended look, render it under an element carrying `data-theme`.

## Sizing

`maxWidth` accepts `"md"` (default) or `"lg"` — pick `"lg"` only when the content genuinely needs the extra width (e.g. a form with side-by-side fields); default to `"md"` otherwise to avoid an oversized dialog for short content.

## Props

```ts
interface ModalProps {
  onClose: () => void;
  children: React.ReactNode;
  maxWidth?: "md" | "lg";
  labelledBy?: string;
}
```
