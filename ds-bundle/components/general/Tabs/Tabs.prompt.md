Tabs from @nicarunner/ui. Use via `window.NicaRunnerUI.Tabs` (bundle loaded from the root `_ds_bundle.js`).

# Tabs

## When to use

Switching between a small number of related views within the same context (e.g. "Corredores / Categorías / Resultados" for one race). Not a substitute for top-level navigation — if the sections are independently linkable/bookmarkable, use routes instead.

## Accessibility

Fully implements the WAI-ARIA tabs pattern, not just visual styling:

- `role="tablist"` on the container, `role="tab"` + `aria-selected` + `aria-controls` on each button.
- Roving `tabIndex` (only the active tab is in the Tab order; `-1` on the rest) plus **arrow-key navigation** (Left/Right moves and activates the adjacent tab, wrapping around at the ends).
- If you render tab panels, give each one `id="tabpanel-<tab.id>"` and `role="tabpanel"` to match the `aria-controls` this component already sets — the component only owns the tab strip, not the panel wiring.

## Usage

`tabs` is `{ id, label }[]`; `activeTab` and `onChange` are controlled — this component holds no internal state, so keep `activeTab` in sync with whatever drives the panel content.

## Props

```ts
interface TabsProps {
  tabs: TabItem[];
  activeTab: string;
  onChange: (id: string) => void;
  className?: string;
}
```
