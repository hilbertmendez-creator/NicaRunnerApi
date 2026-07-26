# EmptyState

## When to use

Any zero-results state: an empty table, an unselected list, a search with no matches. Almost always pair it with an `action` when there's a real next step available — a bare message with no path forward is a dead end for the user.

```tsx
<EmptyState
  message="Sin resultados capturados todavía."
  action={{ label: 'Registrar primer resultado', onClick: openCaptureModal }}
/>
```

Skip `action` only when there's genuinely nothing the user can do right now (e.g. "this race hasn't started yet" with no manual override).

## Copy guidance

Keep `message` specific to what's empty and why, not a generic "No data." — "Sin resultados capturados todavía." tells the user this is expected/temporary, not an error.
