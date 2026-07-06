DataTable from @nicarunner/ui. Use via `window.NicaRunnerUI.DataTable` (bundle loaded from the root `_ds_bundle.js`).

## Props

```ts
interface DataTableProps {
  columns: Column<T>[];
  data: T[];
  rowKey: (row: T) => string | number;
  emptyState?: React.ReactNode;
}
```
