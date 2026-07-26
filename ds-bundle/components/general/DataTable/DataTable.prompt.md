DataTable from @nicarunner/ui. Use via `window.NicaRunnerUI.DataTable` (bundle loaded from the root `_ds_bundle.js`).

# DataTable

## When to use

The primary way to show tabular data (runners, results, categories). Below the `sm` breakpoint it automatically switches to a stacked-card layout per row — you don't need to build a separate mobile view.

## Loading, empty, and error states

- Pass `isLoading` while a request is in flight — it renders `LoadingText` internally instead of an empty or stale table. Don't roll your own loading check around `<DataTable>`; use the prop.
- Pass `emptyState` (usually an `<EmptyState message="..." action={{...}} />`) for the zero-results case — it only renders when `data.length === 0`.
- There is no dedicated "error" slot. On a failed fetch, render an `<ErrorAlert>` above the table yourself rather than passing malformed data through `emptyState` — don't conflate "no results" with "the request failed."

## Columns

`columns` is `Column<T>[]`, each with a `header` and a `render(row)` function. A column with an empty-string `header` (`header: ''`) is treated as an "actions" column and gets special layout treatment in the mobile card view (grouped together below the labeled fields, right-aligned). Use this for row-level Button/Tabs actions, not for real data fields.

## Accessibility

- Column headers render as `<th scope="col">` — keep `header` text meaningful even for action columns' visual label (an empty string is fine there since it's excluded from `labeledColumns`).
- `rowKey` must return a stable, unique value per row (an ID, not the array index) — React uses it for reconciliation and it affects state preservation across re-renders when rows are added/removed/reordered.
- The desktop table header is `sticky` — verify it doesn't get clipped by a parent with `overflow: hidden` and a fixed height smaller than the table.

## Theming note

Card background/border/header shading come from app-level theme tokens (`--bg-card`, `--bd-card`, `--bg-th`, `--text-hi`, `--text-th`, `--bd-row`, `--bd`) with built-in fallbacks — see Modal's docs for the same note.

## Props

```ts
interface DataTableProps {
  columns: Column<T>[];
  data: T[];
  rowKey: (row: T) => string | number;
  emptyState?: React.ReactNode;
  isLoading?: boolean;
  loadingMessage?: string;
}
```
