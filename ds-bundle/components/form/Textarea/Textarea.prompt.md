Textarea from @nicarunner/ui. Use via `window.NicaRunnerUI.Textarea` (bundle loaded from the root `_ds_bundle.js`).

# Textarea

## When to use

Multi-line free text (race description, notes on a disputed result). For single-line entry use `Input`.

## Validation state

Same contract as `Input`/`Select`: pass `invalid` for the red border/ring + `aria-invalid="true"`.

## Accessibility

Pair with `<Label htmlFor="...">`. Resize behavior is the browser default (vertical only, per the kit's base styles) — don't disable resize unless the surrounding layout genuinely can't accommodate it, since users often rely on it for longer content.

## Props

```ts
interface TextareaProps {
  invalid?: boolean;
  className?: string;
  id?: string;
  style?: react.CSSProperties;
  children?: React.ReactNode;
}
```
