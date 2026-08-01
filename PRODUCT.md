# Product

<!-- impeccable:product-schema 1 -->

## Platform

web

## Users

The primary user is the product owner operating the backoffice alone. Confirmed
directly by the owner: "solo yo por el momento" — a single operator, for now.

This is corroborated by the code: `Admin` is the only role literal present in
either the API or the frontend. There is no role hierarchy, no per-organization
scoping, and no multi-tenant boundary.

Runners and the public are not backoffice users. They are readers of the public
result surfaces (`features/public-results`, `features/public-links`), reached by
a shared link rather than an account.

**Open decision:** whether the operator set stays a single administrator or grows
into distinct roles (organizer staff, timing crew, federation officials). Nothing
in the current product commits to either. Future work must not assume a role
model that does not exist.

## Product Purpose

NicaRunner manages running-race results for events in Nicaragua and publishes
them for runners to read.

The backoffice covers the full lifecycle of a race record: races, runners,
categories, results, result disputes, users, notifications, and public sharing
links. Results enter the system both by direct entry and by Excel import
(`ImportExcelModal`).

Success is a race whose published results are correct, reachable by the runners
who ran it, and defensible when a runner contests them.

## Positioning

**Dispute resolution is the mechanism.** Confirmed by the owner as the thing a
neighboring product could not truthfully copy: a runner contests a result, and
the contest is worked to a resolution inside the backoffice with the conflicting
records visible side by side.

`features/results/DisputeResolutionGrid.tsx` is the surface that carries this,
and it is the densest screen in the product. Its density is not a defect to
design away — it is the product's differentiating claim rendered on screen. The
grid must let one person hold several conflicting records in view at once and
decide between them.

Publishing results and maintaining a runner registry are table stakes that any
competitor also offers. Dispute resolution is not.

## Operating Context

- **Primary environment: desktop/office PC.** Confirmed by the owner. Work is
  done sitting down, at a full-size screen, not trackside.
- **Mobile is for occasional lookups**, not for operating the product. It must
  work genuinely and responsively, but it is the secondary target.
- Dispute resolution specifically is a desktop task. The owner directed that
  this screen be designed for PC/laptop; a degraded or read-only mobile
  treatment is acceptable there.
- Results arrive in bulk via Excel spreadsheets, so the system ingests an
  external document rather than being the sole point of data entry.
- Public results are distributed by link, so the reader's first contact with the
  product is frequently a shared URL on a phone rather than the backoffice.

## Capabilities and Constraints

Confirmed feature domains, from the codebase: `races`, `runners`, `results`,
`categories`, `users`, `dashboard`, `notifications`, `public-results`,
`public-links`. Authentication includes password reset and change flows.

Technical constraints future work must respect:

- The interface language is Spanish. This is a product fact, not a default.
- Theme selection is client-only, stored in `localStorage` under `nr_theme`.
  There is no server-side user preference, no DTO, and no endpoint for it.
- **The frontend has zero test tooling and no automated verification of any
  kind.** `.github/workflows/api-ci.yml` excludes `frontend/**` entirely — not
  even a build runs there. Any claim about frontend correctness is a claim made
  by a human looking at a screen.
- `@nicarunner/ui` (`frontend/packages/ui`) is a shared primitive library, but
  eight of its ten primitives use hardcoded Tailwind palette classes and never
  read the theme token layer.
- A generated design-system pipeline exists — `.design-sync/` (15 tracked files)
  and `ds-bundle/` (88 tracked files), plus a compiled
  `frontend/packages/ui/nicarunner-ds.css` snapshot and 12 sibling component
  markdown docs. Changes to the primitives invalidate these generated artifacts.

## Brand Commitments

- **The name NicaRunner is fixed.** Confirmed by the owner.
- **Spanish terminology is fixed.** The domain vocabulary — corredor, categoría,
  disputa, carrera, resultado — stays in Spanish and is not translated or
  neutralized. Confirmed by the owner.

Everything else visual has been explicitly released by the owner: palette,
typography, and the brand mark are all open to replacement. The existing palette
is inspiration only, not a constraint.

There is consequently **no binding visual identity today**. What ships now is not
a commitment: the production favicon and `routes/NicaRunnerLogo.tsx` are a purple
lightning bolt inherited from a Bolt.new project template, while
`assets/logo-emblem.png` is an unrelated blue runner emblem, and
`logo-wordmark.png` and `logo-app-icon.png` are unreferenced. Four marks, none
of them chosen. `docs/sparc/email-html-templates-spec.md:61` already records the
resulting purple/navy conflict as a known problem.

## Evidence on Hand

**There is no confirmed real production data.** The owner did not affirm existing
real races, runners, or results as something to preserve.

Future work must not fabricate what does not exist here: no testimonials, no
named sponsors, no partner logos, no participation figures, no event names
presented as real, no race photography presented as documentary, and no claims
about adoption or scale. Where a surface needs sample content, it must be
evidently illustrative.

The one real asset class the product does have is its own domain structure —
races, categories, runners, times, and disputes — which is genuine and can be
shown truthfully.

## Product Principles

1. **The record must be defensible.** Every screen exists so that a published
   result can be trusted and, when contested, examined. Clarity of the record
   outranks visual expression on operating surfaces.
2. **Density on the dispute surface is a feature.** Comparison requires holding
   conflicting records in view together. Do not thin this screen to make it
   prettier.
3. **The operator sits at a desk; the runner stands with a phone.** These are two
   different situations with two different demands, and the product should not
   pretend they are one.
4. **Say only what is true.** The product has no scale, no roster of sponsors,
   and no track record to display. Content must never manufacture credibility
   the product has not earned.
5. **Spanish is the product's voice**, not a localization layer applied to an
   English original.

## Accessibility & Inclusion

No formal standard has been established as a product requirement.

One concrete defect is confirmed and in scope for correction: `index.html`
declares `lang="en"` on an interface that is entirely in Spanish, which
misinforms screen readers about the document language.
