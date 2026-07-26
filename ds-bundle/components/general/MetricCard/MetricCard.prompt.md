MetricCard from @nicarunner/ui. Use via `window.NicaRunnerUI.MetricCard` (bundle loaded from the root `_ds_bundle.js`).

# MetricCard

## When to use

At-a-glance numeric status on a dashboard (inscritos, tiempos capturados, resultados en disputa). Each card is one number plus one label — don't overload it with secondary metrics or trend arrows; use a separate component for that.

## Variant guidance

| Variant | Color | Semantic |
|---|---|---|
| `gray` | Neutral | Total / count, no status implied |
| `teal` | official-600/700 | Confirmed / complete |
| `amber` | dispute-600/700 | Pending / awaiting review |
| `orange` | orange-300/800 | In progress / active, distinct from `amber`'s "pending" |
| `red` | critical-600/700 | Error / failed |

`amber` and `orange` are intentionally close in hue (both warm) but tuned to be distinguishable at a glance — `orange` is deliberately more saturated/darker. If you're picking between them for a new use case: `amber` means "waiting on someone," `orange` means "actively happening." Don't introduce a third warm variant without widening the palette further, or the set becomes indistinguishable again.

## Accessibility

Label text uses the `-700` (or darker) token variants specifically because the `-600` shades fail WCAG AA contrast at the label's small (`text-xs`) size on their tinted backgrounds — don't downgrade label color back to `-600` even if it "looks fine" in isolation; verify contrast against the actual tint if you change it.

## Props

```ts
interface MetricCardProps {
  label: string;
  value: string | number;
  variant?: "gray" | "orange" | "teal" | "amber" | "red";
  size?: "sm" | "md";
  className?: string;
}
```
