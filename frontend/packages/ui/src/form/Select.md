# Select

## When to use

A fixed, known set of choices (category, race status) where the user recognizes options rather than typing them. For free text use `Input`; for a long or searchable list consider a combobox pattern instead — this is a plain native `<select>`, not a searchable dropdown.

## Validation state

Same contract as `Input`: pass `invalid` for the red border/ring + `aria-invalid="true"`.

## Accessibility

Native `<select>` under the hood, so keyboard support (arrow keys, type-ahead) and screen reader behavior come from the browser for free — don't reimplement this as a custom `<div>`-based dropdown unless you have a specific need the native element can't meet. Pair with `<Label htmlFor="...">` exactly like `Input`.
