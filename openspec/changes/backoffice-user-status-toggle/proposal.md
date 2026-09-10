# Proposal: Backoffice User Status — close the real gaps

## Starting point: the requested feature already exists

The original request was "añade la opción de deshabilitar y activar usuarios en backoffice,
añade también el estado actual del usuario en la tabla". **That capability is already
implemented end to end and predates this branch.** Verified on disk:

| Already working | Evidence |
| --- | --- |
| Status flag | `src/NicaRunner.Domain/Entities/User.cs:27` — `IsActive` |
| Admin-only mutation | `UsersController.cs:19` `[Authorize(Roles=Administrador)]`, `PATCH /api/users/{id}` line 35 |
| Wire contract | `UpdateUserRequest.cs:12` `bool? IsActive`, `UserDto.cs:13` `IsActive` |
| Login/refresh enforcement | `AuthService.cs:55,123,136`; `RefreshTokenService.cs:40` |
| Audit trail | `UserManagementService.cs:107,130`; asserted `UserManagementServiceTests.cs:250` |
| "Estado" column | `UsersPage.tsx:98-99` |
| Activar/Desactivar action | `UsersPage.tsx:51-54,126-133`, `disabled={isSelf}` |
| Existing guards | `UserManagementService.cs:73-87` — self-deactivation, protected seed admins |

This proposal therefore does **not** rebuild the toggle or the column, and does **not**
introduce a second status vocabulary alongside `IsActive` (forbidden by the cross-client
contract rule in `CLAUDE.md`). It scopes to five approved gaps.

## Intent

Disabling a user in the backoffice today is *cosmetically* complete but *operationally*
incomplete: the status is hard to read at a glance, untested, does not actually cut the
session, can strand an installation with too few admins, and can silently pull the judge
out from under a running race. Close those five gaps.

## Scope

### In Scope

1. **Status column presentation** (frontend) — render Estado as a badge matching the house pattern.
2. **Frontend test coverage** (frontend) — cover the column, the toggle, and the self guard.
3. **Immediate session termination** (API) — a disabled user's in-flight access token stops working now, not in ≤60 min.
4. **"At least two active admins" guard** (API) — reject deactivation/role change that drops below two active `Administrador`.
5. **In-flight Capturista warning** (API + frontend) — warn and confirm, never block.

### Out of Scope / Non-Goals

- **Not rebuilding the existing toggle or the Estado column** — they work; they are refined, not replaced.
- No second status field, enum, or wire vocabulary; `IsActive` stays the single source of truth.
- No new EF migration — the `IsActive` column already exists.
- No i18n layer — `UsersPage` strings are hardcoded Spanish inline and stay that way.
- No TanStack Query migration — `UsersPage.tsx:16-39` uses `useState`/`useEffect`/`reload()`; match it.
- No change to `POST /api/users`, `/unlock`, `/audit`, or the login-lockout feature.
- No mobile app changes (see Cross-client impact).
- Blocking deactivation of an in-flight Capturista (explicitly rejected — the decision is warn-and-continue).

## Capabilities

`openspec/specs/` is empty and there is no archive, so every capability below is new.

### New Capabilities

- `user-status-management`: admin-driven activation/deactivation of user accounts, its guards (self, protected seed, minimum active admins), audit trail, and the backoffice presentation of current status.
- `session-revocation`: enforcement that a deactivated account loses access on its next authenticated request rather than at access-token expiry, including live SignalR connections.

### Modified Capabilities

None.

## Approach

### 1. Status column presentation — keep it in `frontend/src/components/`

`frontend/src/components/StatusBadge.tsx` is hard-typed to `RaceStatus`
(`Record<RaceStatus, CSSProperties>` line 4, `LABELS` line 22, signature line 28) and is used
by `RacesPage.tsx:77`. Generalize it to a tone-based primitive and add a user-status consumer.

**Decision: do NOT promote it into `@nicarunner/ui`.** Rationale from the code, not convention:
the badge design tokens (`--badge-ok-bg`, `--badge-cl-bg`, `--radius-badge`) are defined only in
`frontend/src/index.css` and `frontend/src/theme/styles.ts` — both app-owned. `@nicarunner/ui`
(`packages/ui/src/index.ts`) exports 11 domain-agnostic components and owns none of those tokens.
Promoting the badge would force either duplicating the tokens into the package or making the
package depend on CSS variables it does not define, for a single consumer. Keep the generalized
badge app-side; promote later if a second package consumer appears.

### 2. Frontend test coverage

`frontend/src/__tests__/users-page.pagination.under-load.test.tsx` is the only `UsersPage` test;
its single `it(...)` at line 48 asserts pagination offsets, and `isActive: true` at line 31 is
fixture data, not an assertion. `openspec/config.yaml` sets `strict_tdd: true`. Add a sibling
Vitest + Testing Library file using the same `renderWithProviders` harness, covering: Estado
renders Activo/Inactivo, the button label/variant flips, clicking calls
`updateUser(id, { isActive: ... })`, and the self row is `disabled`.

Note: `handleToggleActive` (`UsersPage.tsx:51-54`) has **no** `try/catch`, unlike
`handleRoleChange` and `handleUnlock`. A failed toggle is an unhandled rejection with no toast.
Work items 4 and 5 make failures routine, so error handling must be added here.

### 3. Immediate session termination — `OnTokenValidated`, returning 401

**Correction to the assumed premise: this repo has no distributed cache.** Grep confirms no
`IDistributedCache`, no `IConnectionMultiplexer`, no `AddMemoryCache`, no `IMemoryCache` anywhere
in `src/`. Redis appears only as a **SignalR backplane** (`Program.cs:107-117`), it is registered
**conditionally** on `ConnectionStrings:Redis`, and the inline comment states it is inactive today
because the Render free plan runs a single instance. Any cache-backed design must therefore
*introduce* the cache, not reuse one.

Proposed mechanism:

- **Hook**: extend the existing `JwtBearerEvents` at `Program.cs:246-260` with `OnTokenValidated`.
  That block is already customized (`OnMessageReceived` cookie fallback), so this is the
  established seam.
- **Why not an authorization policy/requirement**: an authorization failure yields **403**, and
  neither client reacts to 403. Failing authentication yields **401**, which both clients already
  handle: `frontend/src/api/client.ts:91-128` retries via `/auth/refresh`, that refresh is refused
  by `RefreshTokenService.cs:40` because `IsActive` is false, and `clearSession()` logs the user
  out. Mobile does the identical dance in `TokenAuthenticator.kt:40-87`. **401 gives a graceful
  auto-logout on both clients with zero client code changes.** This is the deciding argument.
- **Avoiding a DB round-trip per request**: register `AddMemoryCache()` and memoize
  `userId -> IsActive` with a short absolute TTL. Invalidate explicitly in
  `UserManagementService.UpdateAsync` right where `IsActive` is assigned (line 105-109), via a
  small Application-layer abstraction so the Application layer does not reference
  `Microsoft.Extensions.Caching` directly (Clean layering rule, `config.yaml`).
- **Multi-instance caveat, stated honestly**: an in-process cache does not invalidate across
  instances. Today that is moot (single instance, Redis backplane inactive). If the app ever
  scales horizontally, staleness is bounded by the TTL. Design phase picks the TTL and decides
  whether to leave a Redis-backed seam.
- **SignalR**: `RaceDashboardHub.cs:14` is `[Authorize(Roles = Administrador,Lector)]`, so a
  **Capturista never holds a hub connection** — the in-flight-judge case (item 5) and the hub case
  do not overlap. For a connected Admin/Lector, the WebSocket authenticated at handshake and is not
  re-validated per message. The hub is read-only (`resultsChanged` fan-out, no mutations), so blast
  radius is low. Design phase decides between aborting tracked connections on deactivation or
  accepting that the stream ends at reconnect; this proposal does not pre-commit.

### 4. "At least two active admins" guard

Add to `UserManagementService.UpdateAsync`, **after** the existing self and seed guards
(lines 73-87), which remain unchanged and are not replaced.

- **Evaluated against active admins only.** `IUserRepository.GetByRoleAsync` already filters
  exactly that — `UserRepository.cs:50`: `Where(u => u.Role == role && u.IsActive)`. Its own doc
  comment says "usuarios activos con un rol dado". **No repository change is required.**
- **Triggers**: `IsActive: false` on an `Administrador`, and `Role` changed *away from*
  `Administrador`. Both would reduce the active-admin count.
- **Error surfaced to the admin**: a `ForbiddenException`, which
  `ExceptionHandlingMiddleware.cs:27-30` maps to **403** with a ProblemDetails body whose `detail`
  string the frontend reads via `apiErrorMessage` (`client.ts:22-28`). Proposed wording:
  *"No se puede dejar el sistema con menos de dos administradores activos."*
- **Interaction with seed accounts**: `ProtectedSeedUsers` lists three admin emails
  (`ProtectedSeedUsers.cs:8-13`) that can never be deactivated nor re-roled, and `AdminUserSeeder.cs:44`
  seeds them `IsActive = true`. In a normally-seeded installation the count therefore never drops
  below three, and this guard will effectively never fire. It matters for installations seeded
  differently or where a seed row was altered out-of-band.
- **This rule is stricter than the status quo and can block a legitimate operation.** In a small
  installation with only one or two real admins, an admin who wants to demote or deactivate a
  colleague will be refused and must first promote someone else. That is an intentional
  availability-over-convenience tradeoff, and it must be called out in the release note.

### 5. In-flight Capturista warning — dedicated pre-check endpoint

**How a judge is modelled (verified):** `Race.Judges` is `ICollection<RaceJudge>`
(`Race.cs:36`); `RaceJudge` is a join row `{ RaceId, UserId, JoinedAt }` (`RaceJudge.cs:1-12`).
Race state is `Race.Estado` of type `RaceStatus { Planeada, EnCurso, Terminada }` (`Race.cs:3-8,24`).
So "active judge of a running race" = `RaceJudge` rows for the target user whose race has
`Estado == EnCurso`. (`Result.CapturistaId` records who captured a result; it is history, not an
assignment, so it is not the right signal.)

**Decision: a dedicated read-only pre-check endpoint**, e.g. `GET /api/users/{id}/active-races`.

Justification against the two alternatives:

- **A `409`-style response is wrong for this decision.** `ConflictException` maps to 409 and
  *blocks* the write (`ExceptionHandlingMiddleware.cs:19-22`), but the product decision is
  "avisar y dejar seguir". Making 409 work would require a `force`/`confirm` flag on
  `UpdateUserRequest` — mutating a shared wire DTO — plus a two-round-trip retry. Worse,
  `WriteProblemAsync` (line 42-49) emits a fixed `{status,title,detail}` shape with no room for a
  structured race list, so the race names would have to be smuggled into a display string.
- **A field on the existing user payload is wrong too**: it would make `GET /api/users` join
  `RaceJudge` and `Race` for every row on every page load, to serve a rare confirmation dialog.
- **The pre-check endpoint matches the house pattern exactly.** `UsersController` already exposes
  sub-resource routes on the same id: `POST /api/users/{id}/unlock` (line 40) and
  `GET /api/users/{id}/audit` (line 45). A third `GET` sibling is the established shape, needs no
  DTO change, and leaves `PATCH` semantics untouched so deactivation is never blocked.

Frontend: on clicking Desactivar, call the pre-check; if it returns races, open a confirm `Modal`
naming them. The precedent is `RestartRaceDialog.tsx:9-16`, whose doc comment states the dialog
"nombra las categorías afectadas una por una" before a risky action. Same idea, lighter.

**Accepted tradeoff**: TOCTOU — a race could start between the pre-check and the `PATCH`. Acceptable
because the warning is advisory, not an invariant.

## Affected Areas

| Area | Impact | Description |
| --- | --- | --- |
| `frontend/src/components/StatusBadge.tsx` | Modified | Generalize to tone-based; keep `RaceStatus` consumer working for `RacesPage.tsx:77` |
| `frontend/src/features/users/UsersPage.tsx` | Modified | Badge in Estado column; confirm dialog; error handling on `handleToggleActive` |
| `frontend/src/__tests__/` | New | Users status/toggle/self-guard tests |
| `frontend/src/api/endpoints.ts` | Modified | Pre-check client fn (after `getUserAudit`, lines 336-360) |
| `frontend/src/api/types.ts` | Modified | Pre-check response type |
| `src/NicaRunner.Api/Program.cs` | Modified | `OnTokenValidated` in existing `JwtBearerEvents`; `AddMemoryCache()` |
| `src/NicaRunner.Application/Users/UserManagementService.cs` | Modified | Min-active-admins guard; cache invalidation; pre-check query |
| `src/NicaRunner.Api/Controllers/UsersController.cs` | Modified | `GET /api/users/{id}/active-races` |
| `src/NicaRunner.Application/Common/Interfaces/` | New | Account-status cache abstraction; race-judge lookup |
| `tests/NicaRunner.Tests/UserManagementServiceTests.cs` | Modified | Guard + invalidation tests |
| `src/NicaRunner.Infrastructure/Migrations/` | **Unchanged** | No migration needed |

## Changed-line estimate and PR split

| Item | Area | Est. lines |
| --- | --- | --- |
| 1. Status badge | frontend | ~70 |
| 2. Frontend tests | frontend | ~120 |
| 3. Session termination | API | ~200 |
| 4. Min-active-admins guard | API | ~70 |
| 5. In-flight warning | API + frontend | ~200 |
| **Total** | | **~660** |

**This exceeds the 400-line review budget by roughly 260 lines. A split is required.**

Note that a clean "frontend PRs vs API PRs" cut does **not** work, because item 5 spans both and
its frontend half is meaningless without its API half. Two candidate splits, surfaced for the
user to choose — **this proposal does not decide**:

- **Split A (2 PRs, frontend/API boundary)**: PR1 = items 1+2 (~190). PR2 = items 3+4+5 (~470).
  Simple, but PR2 is still over budget and mixes the auth hot path with product guards.
- **Split B (3 PRs, recommended for review safety)**: PR1 = items 1+2, frontend presentation and
  tests (~190). PR2 = items 4+5, product guards plus the pre-check endpoint and confirm dialog
  (~270). PR3 = item 3, session termination alone (~200). Every slice is under budget, each has an
  autonomous scope and its own rollback, and the authentication hot path gets a reviewer's
  undivided attention instead of riding along with UI work.

## Cross-client impact (mobile)

Confirmed backoffice-only for **code**: the mobile app has no users screen and no user DTO.

**But item 3 changes runtime behaviour that mobile will feel, with no mobile code change.** Mobile
holds the same JWT and has the same 401 → refresh → clear-session flow
(`TokenAuthenticator.kt:40-87`). Today a Capturista disabled mid-shift keeps capturing for up to
60 minutes; afterwards their very next request 401s, the refresh is refused, `tokenStore.clear()`
fires and the app logs out reactively. That is the intended outcome and it degrades gracefully —
but it is a behavioural change to a shipped Play Store client and must be validated against the
sibling repo before apply, per the `config.yaml` proposal rule. No wire contract (DTO, enum, auth
shape) changes.

## Risks

| Risk | Likelihood | Mitigation |
| --- | --- | --- |
| Item 3 touches the authentication hot path — a bug locks out **every** user, not just disabled ones | Low / **Critical** impact | Isolate in its own PR (Split B, PR3); fail-open on cache/DB error so an infra hiccup never denies a valid session; integration test on the `WebApplicationFactory` template (`AliasIntegrationTests.cs`) |
| Per-request `IsActive` check adds latency | Medium | `AddMemoryCache` + short TTL; the fallback is one indexed PK lookup |
| In-process cache goes stale across instances | Low today | Single Render instance; Redis backplane inactive (`Program.cs:109-117`); bounded by TTL; leave a Redis-backed seam |
| Item 4 blocks a legitimate operation in a small installation | Medium | Guard counts *active* admins only; three seed admins are permanently active and un-deactivatable; clear 403 message; document in the release note |
| Item 4 has no override — an admin cannot force past it | Low | Deliberate. Escalation path is "promote someone first". Revisit only if it bites in practice |
| Generalizing `StatusBadge` regresses `RacesPage` | Low | `RacesPage.tsx:77` is the only other consumer; keep the `RaceStatus` signature intact |
| TOCTOU between pre-check and PATCH (item 5) | Medium / low impact | Accepted — the warning is advisory by product decision |
| A disabled Admin/Lector keeps a live SignalR stream | Low | Hub is read-only and Admin/Lector only; Capturista unaffected; design phase decides abort-vs-accept |
| Scope creep back into rebuilding working code | Medium | The non-goals list is binding on later phases |

## Rollback Plan

Per slice, since each is independently revertable:

- **Items 1-2 (frontend)**: revert the commit. `StatusBadge` returns to its `RaceStatus`-only form
  and the Estado column to plain text. No data, no API, no contract touched.
- **Item 3 (API)**: highest-risk, and cheapest to disable. Remove the `OnTokenValidated` handler
  from `Program.cs` — behaviour returns exactly to today's ≤60-minute window, which is pre-existing
  and not a regression. Design should put this behind a config flag so rollback is a redeploy, not
  a code change.
- **Item 4 (API)**: remove the guard block from `UpdateAsync`. Pure in-memory validation, no
  persisted state, so nothing to unwind.
- **Item 5 (API + frontend)**: revert. The endpoint is read-only additive and the dialog is
  client-side; `PATCH` semantics were never modified, so deactivation keeps working either way.

No migration is created, so **no database rollback exists or is needed** in any slice.

## Dependencies

- `Microsoft.Extensions.Caching.Memory` via `AddMemoryCache()` — in the ASP.NET Core shared
  framework, so no new NuGet package reference.
- No new frontend dependency; `Modal` is already exported by `@nicarunner/ui`.
- Item 5's frontend half depends on its API half landing first.
- Items 1, 2, 3, 4 are mutually independent.

## Success Criteria

- [ ] Estado renders as a badge consistent with `RacesPage`, and `RacesPage` is visually unchanged.
- [ ] Tests assert the Estado value, the toggle call payload, and the `disabled={isSelf}` guard; `npm test` and `dotnet test` both pass.
- [ ] A user disabled mid-session is rejected on their **next** authenticated request with 401, and both web and mobile clients auto-logout without client code changes.
- [ ] A valid, active user is never rejected by the new check — verified by integration test.
- [ ] Deactivating or re-roling the second-to-last active admin returns 403 with an actionable Spanish message; the existing self and seed guards still pass their tests.
- [ ] Deactivating a Capturista judging an `EnCurso` race shows a dialog naming that race and **succeeds** on confirm.
- [ ] `IsActive` remains the only status vocabulary; no new migration; no mobile source change.
- [ ] Every PR slice lands under the 400-line review budget.

## Proposal question round

Interactive mode requires offering a question round, but this executor has no direct channel to
the user. The scope and decisions for items 1-5 were already confirmed. These remain open and
should be put to the user before or during the design phase:

1. **PR split** — Split A or Split B above? (Explicitly not decided here.)
2. **Item 4 severity** — is "at least two active admins" right, or should it be "at least one"?
   Given the three permanently-active seed admins, the rule will almost never fire in a normally
   seeded installation; confirm two is the intended number and not over-engineering.
3. **Item 3 staleness tolerance** — is a short cache TTL (a disabled user surviving a few more
   seconds) acceptable, or must revocation be strictly immediate even at one DB lookup per request?
4. **Item 3 rollout** — should the per-request check ship behind a config flag so it can be
   disabled in production without a redeploy of code?
5. **SignalR live connections** — for a disabled Admin/Lector with an open dashboard socket:
   forcibly abort the connection, or accept that the read-only stream ends at reconnect?

Assumptions applied in the absence of answers: Split B is recommended but not chosen; TTL is short
and design-phase-tunable; the check fails **open** on infrastructure error; SignalR abort is
deferred to design.

## Decisions (resolved with the user)

Every question raised in `## Proposal question round` is now settled. Later phases must
treat these as fixed inputs, not as open options to revisit.

| # | Question | Decision |
| --- | --- | --- |
| 1 | PR split | **Split B — three chained PRs.** PR1 items 1+2 (~190), PR2 items 4+5 (~270), PR3 item 3 alone (~200). Every slice stays under the 400-line review budget and the authentication hot path is reviewed in isolation. |
| 2 | Item 4 threshold | **At least two active `Administrador` users.** Confirmed deliberately, with the small-installation cost accepted. |
| 3 | Item 3 staleness | **Short-TTL cache.** A disabled user may survive a few extra seconds rather than paying a database lookup on every authenticated request. The exact TTL is a design-phase decision. |
| 4 | Item 3 rollout | **Ship behind a configuration flag,** so the per-request check can be switched off in production without redeploying. This is in addition to failing open on infrastructure error, not a replacement for it. |
| 5 | SignalR live connections | **Do not forcibly abort.** An already-open hub connection is allowed to end on its own; the disabled user is refused at reconnect. Accepted cost: a disabled Admin or Lector may keep watching a live dashboard for a while. |

### Consequences for the design phase

- Both mitigations for item 3 apply together: the config flag governs whether the check runs
  at all, and fail-open governs what happens when the cache or database is unreachable while
  the check is enabled.
- Item 3 must introduce its own cache. The exploration confirmed there is no
  `IDistributedCache`, `IMemoryCache`, or `IConnectionMultiplexer` registered anywhere in
  `src/`; Redis appears only as a SignalR backplane, conditional on `ConnectionStrings:Redis`
  and inactive in the current single-instance deployment (`Program.cs:107-117`). A design that
  assumes a shared cache must therefore also state what happens when the API scales to more
  than one instance and the cache is per-process.
- Deactivation must invalidate the cached entry at the moment `IsActive` flips in
  `UserManagementService.UpdateAsync`, or the short TTL becomes the only bound on how long a
  disabled user survives.
- The chained-PR delivery makes ordering a hard constraint: PR2 and PR3 both touch
  `UserManagementService.UpdateAsync`, so the design must keep their edits separable.

### Additional defect to fix (found while scoping)

`handleToggleActive` (`frontend/src/features/users/UsersPage.tsx:51-54`) has no `try/catch`,
unlike its sibling `handleRoleChange` (lines 41-49) which catches and raises a toast. A failed
toggle is currently an unhandled promise rejection with no user feedback. Items 4 and 5 make
server-side rejections a routine outcome rather than an edge case, so this must be fixed in
PR1 alongside the test coverage that would otherwise not catch it.
