---
name: nicaRunner
description: Race-day control room back office — clear, soft-layered, operator-first.
colors:
  signal-blue: "#2563EB"
  signal-blue-dark: "#3B82F6"
  cool-paper: "#F4F6FA"
  night-console: "#1C2333"
  clean-sheet: "#FFFFFF"
  ink-slate: "#0F172A"
  ink-muted: "#475569"
  ink-faint: "#5B6B7C"
  hairline: "#E2E8F0"
  hairline-inner: "#F1F5F9"
  hover-wash: "#F8FAFC"
  official-green: "#15803D"
  official-green-bg: "#F0FDF4"
  official-green-bd: "#BBF7D0"
  dispute-amber: "#92400E"
  dispute-amber-bg: "#FFFBEB"
  dispute-amber-bd: "#FDE68A"
  critical-red: "#DC2626"
  critical-red-bg: "#FEF2F2"
  critical-red-bd: "#FECACA"
  info-blue: "#1D4ED8"
  info-blue-bg: "#EFF6FF"
  info-blue-bd: "#BFDBFE"
  live-dot: "#22C55E"
  console-on: "#E2E8F0"
  console-muted: "#94A3B8"
  night-console-deep: "#070C16"
  dark-app: "#080E1A"
  dark-surface: "#0D1522"
  dark-ink: "#E2E8F0"
  dark-ink-muted: "#94A3B8"
  dark-ink-faint: "#6B8499"
typography:
  display:
    fontFamily: "Inter, system-ui, Segoe UI, sans-serif"
    fontSize: "1.25rem"
    fontWeight: 600
    lineHeight: 1.3
    letterSpacing: "normal"
  headline:
    fontFamily: "Inter, system-ui, Segoe UI, sans-serif"
    fontSize: "1.125rem"
    fontWeight: 600
    lineHeight: 1.35
    letterSpacing: "normal"
  title:
    fontFamily: "Inter, system-ui, Segoe UI, sans-serif"
    fontSize: "1rem"
    fontWeight: 600
    lineHeight: 1.4
    letterSpacing: "normal"
  body:
    fontFamily: "Inter, system-ui, Segoe UI, sans-serif"
    fontSize: "0.875rem"
    fontWeight: 400
    lineHeight: 1.5
    letterSpacing: "normal"
  label:
    fontFamily: "Inter, system-ui, Segoe UI, sans-serif"
    fontSize: "0.75rem"
    fontWeight: 600
    lineHeight: 1.3
    letterSpacing: "normal"
  mono:
    fontFamily: "IBM Plex Mono, ui-monospace, monospace"
    fontSize: "0.875rem"
    fontWeight: 500
    lineHeight: 1.4
    letterSpacing: "normal"
rounded:
  btn: "6px"
  card: "7px"
  badge: "20px"
spacing:
  xs: "4px"
  sm: "8px"
  md: "12px"
  lg: "16px"
  xl: "24px"
  page: "24px"
components:
  button-primary:
    backgroundColor: "#1D4ED8"
    textColor: "#FFFFFF"
    rounded: "{rounded.btn}"
    padding: "0 12px"
    height: "32px"
    typography: "{typography.body}"
  button-primary-hover:
    backgroundColor: "#1E40AF"
    textColor: "#FFFFFF"
  button-secondary:
    backgroundColor: "{colors.clean-sheet}"
    textColor: "#3F3F46"
    rounded: "{rounded.btn}"
    padding: "0 12px"
    height: "32px"
  button-destructive:
    backgroundColor: "{colors.critical-red-bg}"
    textColor: "{colors.critical-red}"
    rounded: "{rounded.btn}"
    padding: "0 12px"
    height: "32px"
  card-surface:
    backgroundColor: "{colors.clean-sheet}"
    textColor: "{colors.ink-slate}"
    rounded: "{rounded.card}"
    padding: "16px"
  input-field:
    backgroundColor: "{colors.clean-sheet}"
    textColor: "{colors.ink-slate}"
    rounded: "{rounded.btn}"
    padding: "0 12px"
    height: "32px"
  badge-status:
    backgroundColor: "{colors.official-green-bg}"
    textColor: "{colors.official-green}"
    rounded: "{rounded.badge}"
    padding: "2px 8px"
    typography: "{typography.label}"
  nav-item-active:
    backgroundColor: "{colors.info-blue-bg}"
    textColor: "{colors.signal-blue}"
    rounded: "{rounded.btn}"
    padding: "8px 10px"
  nav-item-inactive:
    backgroundColor: "transparent"
    textColor: "{colors.console-muted}"
    rounded: "{rounded.btn}"
    padding: "8px 10px"
---

# Design System: nicaRunner

## Overview

**Creative North Star: "Race-Day Control Room"**

nicaRunner’s visual system is a race-day control room rendered as a clear, approachable corporate back office. Surfaces stay calm and readable under operational pressure: cool paper canvases, a dark console rail for navigation, soft card layering, and a single Signal Blue accent for selection and focus. Personality is corporate-clear and kind — never neon, never gaming-dark theatrics, never decorative SaaS glow.

Density favors scanability over spectacle. Operators find the active race, KPIs, tables, and dispute queues without competing chrome. Public result surfaces inherit the same token language so published standings feel continuous with the back office, not like a separate marketing skin.

Anti-references confirmed with product stakeholders: dark-mode gaming aesthetics, neon accents, purple gradient SaaS kits, and decorative card stacks that invent urgency.

**Key Characteristics:**
- Dual theme (light default, dark operator night) driven by CSS custom properties on `[data-theme]`
- Soft-layered cards with light ambient shadow; borders carry most structure
- Signal Blue as the rare action/selection voice
- Inter for UI; IBM Plex Mono for dorsals, times, and numeric truth
- Semantic status chips: Official Green / Dispute Amber / Critical Red
- Compact controls (32px inputs/buttons) sized for dense tables and forms

## Colors

A cool slate-and-paper palette with one Signal Blue voice and strict semantic status colors. Light theme values are normative in the frontmatter; dark theme remaps the same roles via tokens in `frontend/src/index.css`.

### Primary
- **Signal Blue** (`#2563EB` light / `#3B82F6` dark): selection, focus rings, active nav, links, and “live” accent wash. Kept scarce so it reads as signal, not decoration.

### Secondary
Omit as a brand secondary. Status semantics fill the second voice.

### Neutral
- **Cool Paper** (`#F4F6FA`): app canvas behind cards.
- **Night Console** (`#1C2333` light / `#070C16` dark dock): sidebar rail — always a dark surface.
- **Console On** (`#E2E8F0`): primary text/icons on Night Console (`--sb-fg`).
- **Console Muted** (`#94A3B8`): inactive nav and secondary console copy (`--sb-muted`, ≥4.5:1 on console).
- **Clean Sheet** (`#FFFFFF`): cards, topbar, modal bodies.
- **Ink Slate** (`#0F172A`): primary text on paper surfaces.
- **Ink Muted** (`#475569`): secondary copy / labels on paper.
- **Ink Faint** (`#5B6B7C`): tertiary / table headers (AA ≥4.5:1 on Clean Sheet / Cool Paper).
- **Hairline** (`#E2E8F0`) / **Hairline Inner** (`#F1F5F9`): card and row borders.
- **Hover Wash** (`#F8FAFC`): input fills, table header wash, hover rows.

### Named status
- **Official Green** (`#15803D` on `#F0FDF4` / `#BBF7D0`): confirmed / en curso / success.
- **Dispute Amber** (`#92400E` on `#FFFBEB` / `#FDE68A`): pending / terminada / caution.
- **Critical Red** (`#DC2626` on `#FEF2F2` / `#FECACA`): errors, conflicts, destructive.
- **Live Dot** (`#22C55E`): pulsing “en vivo” indicator only.

**The Signal Scarcity Rule.** Signal Blue occupies accent, focus, and active states only — never large fills that turn the screen into a blue field.

**The Token Source Rule.** Never hardcode hex outside `frontend/src/index.css` theme blocks (or intentional `@theme` semantic scales). App UI consumes `var(--*)` / `theme/styles.ts`.

**The Console Ink Rule.** Never paint Night Console chrome with content `--tx-*` (paper ink). Use `--sb-fg` / `--sb-muted` / `--sb-*` so inactive drawer labels stay AA.

## Typography

**Display Font:** Inter (system-ui / Segoe UI fallback)
**Body Font:** Inter
**Label/Mono Font:** IBM Plex Mono for dorsals, finish times, and tabular metrics

**Character:** Neutral corporate UI type — clear, compact, unornamented. Mono is reserved for race-truth numbers so operators trust digits at a glance.

### Hierarchy
- **Display** (600, ~20px / `text-xl`): auth hero titles, rare page moments.
- **Headline** (600, ~18px / `text-lg`): page section titles (e.g. public race name).
- **Title** (600, 16px): card titles, modal headings.
- **Body** (400–500, 14px / `text-sm`): default UI copy, table cells, forms.
- **Label** (600, 11–12px): badges, overlines, meta.
- **Mono** (500, 14px): dorsals, tiempos, KPI numerals (`tabular-nums` when present).

**The Mono-Is-Truth Rule.** IBM Plex Mono marks measurable race data. Do not use mono for marketing headlines or nav labels.

## Layout

App shell: fixed Night Console sidebar + topbar (theme switcher, active-race select, account) + scrollable content. Content padding ≈ 24px. Cards and tables stack in a single-column operational rhythm; wide grids collapse to one column at ≤900px (`.nr-content-grid`). Sidebar becomes an off-canvas drawer at ≤640px (labels always visible; no hover-only dependency).

Spacing rhythm: 4 / 8 / 12 / 16 / 24. Card internal padding defaults to 16px (`theme/styles.ts` `card`). Compact form rows use 32px control height on fine pointers; `@media (pointer: coarse)` enlarges shell and Controversias controls to ≥44px.

Race context is global: the active race in the topbar scopes dashboard, results, and controversias badges.

## Elevation & Depth

Soft-layered: surfaces are mostly flat, with light ambient shadows that separate Clean Sheet cards from Cool Paper — tactile but quiet, never dramatic.

### Shadow Vocabulary
- **Ambient low** (`0 1px 2px rgba(0,0,0,.05)` light / stronger in dark): subtle resting lift.
- **Ambient mid** (`0 1px 3px rgba(0,0,0,.07), 0 2px 8px rgba(0,0,0,.04)` light): cards and elevated panels.
- **Modal scrim** (`bg-black/30`): dialogs only.

Structure still leans on Hairline borders more than shadow stacks.

**The Quiet Lift Rule.** Prefer border + tonal step first; add shadow only when a surface must separate from Cool Paper. No neon glows, no multi-layer purple shadows.

## Shapes

Gently curved operational geometry: buttons/inputs at 6px, cards/modals at 7px, status badges fully pill at 20px. Empty states use dashed Hairline frames. Focus is a 2px Signal Blue outline with 2px offset (`:focus-visible`), not a glow blob.

**The Pill-For-Status Rule.** Pill radius (20px) is reserved for badges/chips. Do not pill primary buttons or cards.

## Components

### Buttons
Tactile but quiet. Compact (`h-8` / 32px), medium weight, 6px radius.
- **Primary:** solid blue-700 fill, white text; hover deepens.
- **Secondary:** Clean Sheet + zinc border; default for most actions.
- **Destructive:** Critical Red wash + border (not solid red fills by default).
- **Info:** Official Green wash for affirmative/official actions.
- **Focus:** ring-2 Signal/blue-700 with offset.

### Cards / Containers
- **Corner:** 7px. **Background:** Clean Sheet (or dark surface). **Border:** Hairline. **Padding:** 16px typical. Soft ambient mid shadow acceptable; never heavy stacks.

### Inputs / Fields
- **Style:** 32px height, Hairline border, Clean Sheet / `--bg-input` fill, 6px radius.
- **Focus:** Signal Blue border + ring-1 (or `:focus-visible` outline on themed `.nr-input`).
- **Invalid:** Critical Red border/ring.

### Navigation
Night Console rail with icon + label; inactive items use `--sb-muted` (`#94A3B8`, ≥4.5:1 on `--bg-sb`); active item uses Signal Blue (`--sb-text` / `--ac`) on `--sb-active-bg`. Do not paint console chrome with content `--tx-*` (those are ink for paper surfaces). Mobile: hamburger opens drawer; tooltips on collapsed icon-only affordances.

### Status badges / chips
Pill badges using Official Green / Dispute Amber / Critical Red token triples. En curso may include the Live Dot pulse. Badge copy must reflect real state (product honesty constraint).

### Tables
Clean Sheet wrapper, Hairline row dividers, faint header text, hover wash on rows. Empty tables use dashed EmptyState — never fake rows.

### Modals
Centered dialog on 30% black scrim; Clean Sheet body, 7px radius, Escape/backdrop dismiss, focus trapped to first field.

### Signature: Active race + live pulse
Topbar race select and `.dot-live` pulse are signature race-day signals — use sparingly and only when data is actually live/in-progress. Under `prefers-reduced-motion: reduce`, the dot stays solid Official/Live green (no scale/opacity loop). Loading uses `.nr-spinner` (static ring when reduced motion).

### Brand mark
`NicaRunnerLogo` uses `currentColor` (Signal Blue via `--ac` on auth). Never ship the legacy purple fill (`#863bff`) — purple SaaS kits are an anti-reference. Auth Night Console panels use `--sb-fg` / `--sb-muted`, not theme `--text-hi` / translucent white.

## Do's and Don'ts

### Do:
- **Do** consume theme tokens (`var(--bg-card)`, `var(--accent)`, etc.) so light/dark stay coherent.
- **Do** keep Signal Blue scarce — accents, focus, active nav, links.
- **Do** use IBM Plex Mono for dorsals, times, and KPI numerals.
- **Do** use Official Green / Dispute Amber / Critical Red for real status only.
- **Do** prefer soft card lift + Hairline borders over heavy shadows.
- **Do** use `--sb-fg` / `--sb-muted` for Night Console and auth brand panels.
- **Do** collapse chrome honestly when data is empty (EmptyState, zero badges).

### Don't:
- **Don't** invent neon, gaming glow, purple gradients, or decorative glassmorphism.
- **Don't** hardcode one-off hex in feature components outside the theme blocks.
- **Don't** alias console muted text to content `--tx-md` (breaks AA on the dark rail).
- **Don't** pill buttons/cards; pills are for status chips.
- **Don't** show urgency badges, fake KPIs, or mock dispute chrome without real counts.
- **Don't** replace Inter/Plex with display novelty fonts for “sporty” marketing energy inside the back office.
