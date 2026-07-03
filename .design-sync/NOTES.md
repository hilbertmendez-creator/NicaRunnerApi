# NicaRunner UI — Design Sync Notes

## Build

- Package: `frontend/packages/ui`
- Build cmd: `cd frontend/packages/ui && npm run build`
- node_modules for converter: `frontend/node_modules` (react hoisted to frontend root by npm workspaces)
- Dist entry: `frontend/packages/ui/dist/index.js`
- CSS entry: `./nicarunner-ds.css` (Tailwind v4 compiled output in the package dir)

## Known render warns

- **Modal `[RENDER_THIN]`**: Modal uses `position: fixed; inset: 0` — measured DOM height collapses to 0 in headless Chromium even though the component renders correctly. The config already has `cardMode: "single", viewport: "700x500"` to handle this. Confirmed benign — screenshot shows the modal rendering with real content. Non-blocking.

## Re-sync risks

- **Tailwind token classes**: conventions.md lists token utility classes (e.g. `bg-official-50`, `text-dispute-600`). If new semantic tokens are added to the Tailwind theme in the future, update conventions.md to include them — they won't appear automatically.
- **MetricCard/Button variants**: variant enums are hand-documented in conventions.md. If a new variant is added to the component props, update both the `.d.ts` (auto-generated) and conventions.md.
- **Fonts from Google CDN**: Inter and JetBrains Mono are runtime-loaded from Google Fonts, not bundled. If the CDN URL changes or the DS switches to self-hosted fonts, update `runtimeFontPrefixes` config and the conventions.md typography section.
- **Modal viewport override**: the `700x500` override in config is sized for the current modal content (race creation form). If modal content grows significantly, revisit the viewport.
- **Slate-blue tokens**: CSS only defines `--color-slate-blue-900` and `--color-slate-blue-800` (no 50/200/600 ladder). The conventions.md lists it as "neutral accent" without specifying shades — correct as-is, but note this if shades are added later.
