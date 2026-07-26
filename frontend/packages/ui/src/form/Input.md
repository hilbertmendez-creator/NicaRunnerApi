# Input

## When to use

Single-line text/number/date entry. For multi-line content use `Textarea`; for a fixed set of choices use `Select`.

## Validation state

Pass `invalid` to switch to the red error border/ring and set `aria-invalid="true"` automatically:

```tsx
<Input invalid={!!errors.dorsal} value={dorsal} onChange={...} />
```

Pair it with a visible error message (e.g. an `ErrorAlert` or inline text) — `aria-invalid` alone tells assistive tech something is wrong but doesn't say what; don't rely on color alone to communicate the error.

## Accessibility

Always pair with a `<Label htmlFor="the-input-id">` — this component doesn't render its own label. `forwardRef` is supported, so it composes with form libraries that need a ref (react-hook-form, etc.) without extra wiring.
