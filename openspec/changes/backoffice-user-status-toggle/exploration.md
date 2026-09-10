# Exploration: backoffice-user-status-toggle

**Phase:** explore
**Status:** done
**Branch:** `claude/backoffice-user-toggle-jy1gqu`
**Artifact store:** openspec (Engram unavailable in this session)

## Request

> "añade la opción de deshabilitar y activar usuarios en backoffice, añade también
> el estado actual del usuario en la tabla"

Intent: in the React backoffice, allow disabling and re-enabling user accounts, and
show each user's current status as a column in the users table.

## Headline finding

The requested capability **already exists end to end** on the current branch, on both
the API and the backoffice. It was not introduced by this branch — it predates it
(the users screen last changed in `5d41ca5`, PR #110). The proposal phase must start
from "what is the real remaining gap", not from "build this from scratch".

Every claim below was verified against the files on disk.

## Current state — domain and persistence

- `src/NicaRunner.Domain/Entities/User.cs:27` — `public bool IsActive { get; set; } = true;`
  is the dedicated status flag. There is no separate soft-delete column, no `DeletedAt`,
  and no ASP.NET Identity lockout field repurposed for this. The unrelated login-lockout
  fields (`FailedLoginCount`, `LockedUntilUtc`, `LastFailedLoginUtc`) live alongside it and
  serve a different feature.
- Authentication is hand-rolled, not ASP.NET Identity:
  `src/NicaRunner.Application/Auth/AuthService.cs` performs its own password hashing and
  verification and issues its own JWT plus refresh tokens.
- DbContext: `src/NicaRunner.Infrastructure/Data/NicaRunnerDbContext.cs`.
  Migrations: `src/NicaRunner.Infrastructure/Migrations/`. The `IsActive` column already
  exists in a prior migration — no new migration is required.
- Roles: `UserRole { Capturista, Administrador, Lector }`
  (`src/NicaRunner.Domain/Entities/User.cs:3`). Only `Administrador` manages users.

## Current state — API surface

`src/NicaRunner.Api/Controllers/UsersController.cs`, class-level
`[Authorize(Roles = nameof(UserRole.Administrador))]` at line 19:

| Route | Line |
| --- | --- |
| `GET /api/users` | 24 |
| `POST /api/users` | 28 |
| `PATCH /api/users/{id}` | 35 |
| `POST /api/users/{id}/unlock` | 40 |
| `GET /api/users/{id}/audit` | 45 |

`PATCH /api/users/{id}` already accepts `IsActive` and is the house pattern for a
single-entity mutation: controller -> `IUserManagementService` -> `IUserRepository` ->
EF `SaveChangesAsync`. `UserManagementService.UpdateAsync`
(`src/NicaRunner.Application/Users/UserManagementService.cs:68-134`) diffs the entity
in memory into `FieldChange` objects and passes them to
`auditService.TrackChanges(AuditEntityTypes.User, ...)` before persisting.

DTOs already carry the field: `UpdateUserRequest` has `bool? IsActive`
(`src/NicaRunner.Application/Users/Dtos/UpdateUserRequest.cs`), and `UserDto` exposes
`IsActive`.

Guards already implemented in `UpdateAsync` (lines 73-87):

- Self-deactivation is blocked — `ForbiddenException("No puedes desactivar tu propia cuenta.")`.
- Protected seed admins (`src/NicaRunner.Domain/Constants/ProtectedSeedUsers.cs`) cannot be
  deactivated nor have their role changed.

## Current state — authorization and token lifecycle

Disabling a user is only meaningful if it actually stops access. What the code enforces:

- `AuthService.LoginAsync:55` — `if (IsLocked(user) || !user.IsActive || user.PasswordHash is null)`
  rejects login with the same generic 401 as any other failure, so there is no account
  enumeration leak.
- `AuthService` lines 123 and 136 — same `IsActive` check on the Google login path.
- `RefreshTokenService.ValidateAndRotateAsync:40` — refresh is refused as soon as
  `IsActive` flips to false.

**Uncovered gap:** access tokens are stateless JWTs with a 60-minute lifetime
(`src/NicaRunner.Infrastructure/Security/JwtSettings.cs:8`,
`src/NicaRunner.Api/appsettings.json:9`). There is no revocation list and no per-request
`IsActive` re-check. A user disabled mid-session keeps working for up to 60 minutes until
their access token expires, even though login and refresh are already blocked. This is
pre-existing behaviour, not a regression introduced here.

## Current state — audit trail

`AuditEntityTypes.User` plus `auditService.TrackChanges` is the existing bitácora
mechanism, the same one used for races. `IsActive` changes already land in it — asserted
at `tests/NicaRunner.Tests/UserManagementServiceTests.cs:250`.

## Current state — backoffice

- Page: `frontend/src/features/users/UsersPage.tsx`.
- Table: `DataTable` from `@nicarunner/ui` (`frontend/packages/ui/src/DataTable.tsx`).
- API client: `frontend/src/api/endpoints.ts` — `getUsers`, `updateUser`, `unlockUser`,
  `getUserAudit` (lines 336-360).
- **No TanStack Query hooks exist for this feature.** `@tanstack/react-query` is a
  dependency and `QueryClientProvider` wraps tests
  (`frontend/src/test/renderWithProviders.tsx`), but `UsersPage` fetches with local
  `useState`/`useEffect` plus a manual `reload()` callback (lines 16-39). Later phases
  must match this actual pattern rather than an assumed one.
- The status column already exists — `UsersPage.tsx:98-99`,
  `header: 'Estado'`, `render: (u) => (u.isActive ? 'Activo' : 'Inactivo')` — but renders
  plain text.
- The toggle already exists — `handleToggleActive` (lines 51-54) calls
  `updateUser(target.id, { isActive: !target.isActive })` then reloads. The row button
  (lines 126-133) switches `variant` between `destructive` and `primary`, is labelled
  "Desactivar"/"Activar", and is `disabled={isSelf}`, mirroring the backend self-lockout
  guard on the client.
- Copy: there is no i18n layer. All strings are hardcoded Spanish inline in JSX. Later
  phases must match this and must not introduce i18n.

## Real gaps identified

### Gap 1 — the status column breaks the house visual pattern

`@nicarunner/ui` has no generic Badge/Chip/Pill component (the package exports Button,
DataTable, EmptyState, ErrorAlert, LoadingText, MetricCard, Modal, Tabs, form). The house
pattern for status rendering is `frontend/src/components/StatusBadge.tsx`, used by
`RacesPage.tsx:77`. That component is hard-typed to `RaceStatus`
(`Record<RaceStatus, CSSProperties>`, line 4; `LABELS` line 22; signature line 28), so it
cannot be reused for users as-is. The users "Estado" column renders inline plain text
instead, diverging from how every other status is shown in the app.

### Gap 2 — no test coverage for the status column or the toggle

The only frontend test touching `UsersPage` is
`frontend/src/__tests__/users-page.pagination.under-load.test.tsx`, whose single `it(...)`
(line 48) asserts pagination offsets only. Its `isActive: true` at line 31 is fixture data,
not an assertion. There is no test that the "Estado" column renders Activo/Inactivo, none
that the button fires `updateUser({ isActive: ... })`, and none for the `disabled={isSelf}`
guard. `openspec/config.yaml` records `strict_tdd: true`, so this is the TDD-relevant gap.

## Cross-client contract

The mobile app (`/home/user/NicaRunner`) has no `UserDto` equivalent and no users screen.
A grep across `*.kt` returns only `UserRoleTest.kt`, which tests the `UserRole` enum used
for JWT-claim-based authorization, not a fetched user object. This matches the documented
mobile role model, where the app only ever knows its own role from the JWT claim.

**This change is backoffice-only. No mobile blast radius.**

## Testing templates

- Backend: `tests/NicaRunner.Tests/UserManagementServiceTests.cs` already covers the guards
  and the audit assertion. `AliasIntegrationTests.cs` is the `WebApplicationFactory`-style
  integration template.
- Frontend: `frontend/src/__tests__/users-page.pagination.under-load.test.tsx` is the closest
  render/mock template (Vitest + Testing Library, `renderWithProviders`).

## Approaches considered

### A. Confirm and close the real gaps (recommended)

Scope the change to the two verified gaps: add the missing frontend test coverage, and
decide explicitly whether to generalize `StatusBadge` so the Estado column matches the rest
of the app. No backend changes.

- Pros: does not re-implement correct, already-tested code; smallest possible diff; fits
  well inside the 400-line review budget; respects the repository rule to do what was asked
  and nothing more.
- Cons: the proposal must state plainly why the toggle itself is not being touched, or the
  result reads as a missed requirement.
- Effort: low.

### B. Re-implement from the literal request

Ignore what is on disk and build a parallel status field, toggle and column.

- Pros: none identified.
- Cons: either redundant with `IsActive` or actively harmful — it would introduce a second
  status vocabulary, which the cross-client contract rule explicitly forbids. It would spend
  the review budget rewriting passing code and risk breaking existing assertions in
  `UserManagementServiceTests.cs`.
- Effort: medium-high, for no net gain.

**Recommendation: A.**

## Open design questions for the proposal phase

These are surfaced, not decided.

1. **Status column presentation** — generalize `StatusBadge` to accept a variant, add a
   sibling component, or leave the Estado column as plain text.
2. **Token revocation** — accept the 60-minute window in which a disabled user's existing
   access token still works, or add a per-request `IsActive` check / revocation mechanism.
3. **Last-admin protection** — `UpdateAsync` blocks self-deactivation and seed-admin
   deactivation, but nothing prevents two non-seed admins from deactivating each other down
   to zero active non-seed admins. Needs a product decision.
4. **In-flight Capturista** — nothing checks whether the user being deactivated is the active
   Capturista of a race currently `EnCurso`. Behaviour is undefined today.

## Risks

- Access-token revocation gap (60-minute window) — pre-existing, but directly relevant to
  whether disabling actually works.
- No last-admin guard — a tenant could be left with zero usable non-seed admins.
- Undefined behaviour when deactivating a Capturista mid-race.
- Scope-creep risk: the next phase must not re-describe or re-implement working code.

## Next

`sdd-propose` — but only after the user confirms the scope, since most of the requested
functionality already exists.
