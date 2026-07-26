# Label

## When to use

Always alongside a form field (`Input`, `Select`, `Textarea`) — never as standalone text styling; use a `<p>` or `<span>` for that.

## Accessibility

Set `htmlFor` to match the field's `id` — this is what associates the label with its control for screen readers and click-to-focus behavior. This component doesn't generate or enforce the pairing automatically; it's a plain `<label>` under the hood, so the same rules apply as raw HTML:

```tsx
<Label htmlFor="dorsal">Dorsal</Label>
<Input id="dorsal" ... />
```

Skipping `htmlFor` is the most common way this component gets misused — the label will still render, but clicking it won't focus the field, and screen readers won't announce the field's purpose.
