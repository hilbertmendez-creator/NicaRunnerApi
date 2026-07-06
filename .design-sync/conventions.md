# NicaRunner UI — Design Conventions

## Component import

All components are exported from `@nicarunner/ui` and available on the global `window.NicaRunnerUI` at runtime.

```tsx
import { Button, Modal, MetricCard, DataTable } from '@nicarunner/ui'
```

## Styling model — Tailwind CSS v4 utilities

Components are unstyled in isolation — they rely on Tailwind utility classes compiled into `nicarunner-ds.css`. **Always wrap components in a container that has Tailwind classes applied**, never inline styles.

```tsx
// Correct — Tailwind utilities available
<div className="flex gap-2 p-4">
  <Button variant="primary">Guardar</Button>
</div>

// Wrong — no Tailwind context
<Button style={{ backgroundColor: 'blue' }}>Guardar</Button>
```

## Custom design tokens

The theme defines three semantic color groups with 50/200/600 shades:

| Token group | CSS variable prefix | Semantic use |
|---|---|---|
| Official | `--color-official-*` | Confirmed / success / active (teal) |
| Dispute | `--color-dispute-*` | Pending / warning (amber) |
| Critical | `--color-critical-*` | Error / alert / destructive (red) |
| Slate blue | `--color-slate-blue-*` | Neutral accent |

Use these via Tailwind classes: `bg-official-50`, `text-dispute-600`, `border-critical-200`.

## Button variants

| Variant | Use case |
|---|---|
| `primary` | Main action (submit, save) |
| `secondary` | Cancel, secondary action |
| `destructive` | Delete, irreversible actions |
| `info` | Navigation, view details |

Size prop: `md` (default) or `sm`.

## MetricCard variants

| Variant | Color | Semantic |
|---|---|---|
| `gray` | Neutral | Total / count |
| `teal` | official-600 | Confirmed / complete |
| `amber` | dispute-600 | Pending / warning |
| `orange` | Orange | In progress |
| `red` | critical-600 | Error / failed |

Size prop: `md` (default) or `sm`.

## Modal usage

Modal renders with `position: fixed; inset: 0` — it always occupies full viewport. When building screens:
- Always render `Modal` at the top level of the page, not inside a flex/grid container
- Use `isOpen` prop to control visibility — start with `isOpen={true}` in design previews
- The modal content area scrolls independently if content overflows

## DataTable generics

`DataTable<T>` is generic. Define a concrete row type and pass typed `Column<T>[]`:

```tsx
type Runner = { pos: number; dorsal: number; nombre: string }
const cols: Column<Runner>[] = [
  { key: 'pos', header: 'Pos' },
  { key: 'nombre', header: 'Nombre' },
]
<DataTable columns={cols} data={runners} />
```

## Tabs state

`Tabs` is uncontrolled with `defaultActiveId`. For interactive previews, use `useState` + `activeId` prop:

```tsx
const [active, setActive] = useState('corredores')
<Tabs items={items} activeId={active} onTabChange={setActive} />
```

## Typography

Fonts are loaded from Google Fonts CDN — not bundled. Designs targeting this system should include:

```html
<link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&family=JetBrains+Mono&display=swap" rel="stylesheet">
```

`Inter` is used for all body text; `JetBrains Mono` for numeric/code values (e.g. race times, bib numbers).

## Spanish-first content

All UI copy is in Spanish. Labels, placeholders, error messages, and empty states use Spanish text. Example conventions:
- "Guardar" not "Save"
- "Cancelar" not "Cancel"
- "Cargando..." not "Loading..."
- "Sin resultados" not "No results"
