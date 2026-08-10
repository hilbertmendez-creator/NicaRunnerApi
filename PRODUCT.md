# Product

<!-- impeccable:product-schema 1 -->

## Platform

web

## Users

Primary audiences share one product:

- **Administradores** — setup and governance of races: users, categories, race configuration, Excel import, publishing, notifications.
- **Operadores / lectors on race day** — monitor dashboard KPIs, follow live results, resolve controversias, manage public links. `Lector` is read-oriented; mutation paths follow role permissions.
- **Capturistas** — role exists in the domain for capture workflows; backoffice UI currently gates shell access to `Administrador` and `Lector` (Capturista may use API or a future surface).

Secondary audience (no login): runners, family, and public visitors who open opaque public result and runner share links.

## Product Purpose

nicaRunner is the operations and administration platform for foot races: configure the race, manage runners and categories, capture and audit results, resolve disputes, notify participants, and publish shareable results. Success means operators can run race day without deceptive chrome, with trustworthy live data, and participants can reach real published results without accounts.

## Positioning

An integrated stack — authenticated back office + public opaque result/runner links + notifications — built for race operators, not a generic event tool or results-only board.

## Operating Context

- Spanish UI oriented to Nicaragua (`es-NI` date/time formatting).
- Race-scoped work: an active race drives dashboard, results, controversias badges, and related flows.
- Day-of-race: live-ish monitoring (polling / SignalR hub), capture and dispute resolution under audit.
- Pre/post race: race CRUD, runner import (Excel), category catalog, user management, public link issuance, notification sending.
- Auth: email or alias + password; Google auth supported in the domain; lockout and forced password change exist.

## Capabilities and Constraints

**Confirmed capabilities**

- Back office: Dashboard, Carreras, Resultados, Controversias, Notificaciones, Enlaces; admin-only Usuarios and Categorías.
- Public surfaces: `/resultados/:token`, `/corredor/:shareKey`.
- Roles: `Administrador`, `Lector`, `Capturista` with role-gated API and UI.
- Audit history on sensitive entities; honest empty/error states preferred over fake urgency.

**Constraints**

- UI honesty is binding: navigation, badges, and KPIs must reflect real data (no orphan mock chrome, no false urgency).
- Public links are opaque; do not invent fake share URLs or fabricate published standings. Race result links (`/resultados/:token`) expire on a set date and an administrator can revoke one at any time to cut off a leaked link — several may be live at once by design. Runner share links (`/corredor/:shareKey`) are permanent.
- Do not invent testimonials, customer logos, race results, or deployment claims for marketing surfaces.

**Open / undecided**

- Whether Capturista gets a first-class backoffice surface beyond API access.
- Formal accessibility standard beyond current WCAG-oriented practices in code (focus rings, ≥44px targets on some controls).

## Brand Commitments

- Product name: **nicaRunner** (wordmark/casing as used in UI).
- Voice: Spanish, operational, direct; honest error copy preferred over generic blame.
- Logo component: `frontend/src/routes/NicaRunnerLogo.tsx`.
- Locale/market signal: Nicaragua (`es-NI`).

## Evidence on Hand

- Runnable back office and public result UIs under `frontend/`.
- API and domain under `src/NicaRunner.*`.
- Logo SVG component (not a separate marketing asset library).
- No approved customer testimonials, press kit, or fabricated race datasets for design fiction — do not invent them.

## Product Principles

1. **Operator trust first** — every chrome element must earn its place with real state.
2. **One product, two moments** — setup and race-day operation share the same coherent system.
3. **Publish without accounts** — opaque public links are a first-class outcome, not an afterthought.
4. **Role clarity** — Administrador / Lector / Capturista permissions stay explicit and enforceable.
5. **Spanish, local, operational** — copy and formatting serve Nicaraguan race operators, not generic SaaS tone.

## Accessibility & Inclusion

No formal product-mandated standard was set in init. Existing UI already targets practical a11y (focus rings, aria labels, large touch targets on key controls). Future work should preserve those patterns unless a higher bar is adopted.
