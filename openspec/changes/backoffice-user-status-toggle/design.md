# Design: Backoffice User Status — close the real gaps

## Technical Approach

Five gaps, three chained PRs, no new wire vocabulary. `IsActive` stays the single source of
truth (`src/NicaRunner.Domain/Entities/User.cs:27`), no migration is created, and every guard
is added to the existing seam rather than a new one.

| PR | Items | Layer | Est. lines |
| --- | --- | --- | --- |
| PR1 | Status badge + frontend tests + `try/catch` fix | frontend only | ~190 |
| PR2 | Min-active-admins guard + in-flight pre-check + confirm dialog | API + frontend | ~270 |
| PR3 | Immediate session termination | API only | ~200 |

Order is fixed: PR1 → PR2 → PR3, each targeting the previous branch.

## Architecture Decisions

### D1 — Per-request `IsActive` check hooks into `JwtBearerEvents.OnTokenValidated`

**Choice**: extend the existing `options.Events = new JwtBearerEvents { ... }` block
(`src/NicaRunner.Api/Program.cs:246-260`) with `OnTokenValidated`, calling `context.Fail(...)`
for a deactivated user. The handler body lives in a new
`src/NicaRunner.Api/Auth/AccountStatusJwtEvents.cs`, not inline: `Program.cs` is already 567
lines and `CLAUDE.md` caps files at 500.

**Rejected — authorization requirement / policy**: an authorization failure produces **403**.
Neither client reacts to 403. `frontend/src/api/client.ts:91-128` retries only on
`status === 401`; `TokenAuthenticator.kt` (sibling repo) does the same.

**Rejected — custom middleware**: it would have to run after `UseAuthentication()`
(`Program.cs:498`) and hand-roll the 401 ProblemDetails body. `ExceptionHandlingMiddleware`
(`src/NicaRunner.Api/Middleware/ExceptionHandlingMiddleware.cs:15-39`) maps no exception to
401 except `InvalidCredentialsException`, which is an auth-flow type.

**Confirmation that this yields 401**: `context.Fail()` makes `HandleAuthenticateAsync` return
a failed `AuthenticateResult`; the request is then unauthenticated, so `[Authorize]` challenges
and `JwtBearerHandler` writes **401** with `WWW-Authenticate`. The web client's interceptor
then POSTs `/auth/refresh`, which is refused at
`src/NicaRunner.Infrastructure/Security/RefreshTokenService.cs:40`
(`!existing.User.IsActive`), and `clearSession()` logs the user out. Zero client code changes.

Two side effects, both desirable: `/auth/logout` and `/auth/refresh` carry no `[Authorize]`
(`AuthController.cs:65-81`), so a disabled user can still log out cleanly; and the SignalR
handshake authenticates through the same handler (see the `OnMessageReceived` comment,
`Program.cs:239-245`), so a disabled Admin/Lector is refused **at reconnect** — exactly
decision 5, achieved with no hub code.

### D2 — Cache: `IMemoryCache` behind an Application-owned abstraction, 30s TTL

**Choice**: register `builder.Services.AddMemoryCache()` and a scoped `IAccountStatusCache`.
Key `account-status:{userId}`, value `bool`, **absolute** expiration of 30 seconds. Miss →
one indexed PK lookup via `IUserRepository.GetByIdAsync` (`UserRepository.cs:52-53`).

**Why an abstraction, not `IMemoryCache` at the call site**: `NicaRunner.Application` is a plain
`net8.0` library (`NicaRunner.Application.csproj:16-20`) with no
`Microsoft.Extensions.Caching.Memory` reference, so `UserManagementService` literally cannot
see `IMemoryCache` without adding a package and breaking the layering rule in
`openspec/config.yaml:42-44`.

**TTL = 30s, justified**: the access token lives 60 minutes
(`src/NicaRunner.Api/appsettings.json:9`), so 30s cuts the worst-case revocation window by
~99.2%. A single active user costs at most 2 PK lookups per minute. Sliding expiration is
wrong here — an actively-abusing disabled user would keep their entry alive forever.

**Invalidation**: `IAccountStatusCache.Invalidate(userId)` is called in
`UserManagementService.UpdateAsync` **after** `await userRepository.SaveChangesAsync(ct)`
(line 132), not at the assignment on line 108. Evicting before the commit lets a concurrent
request re-populate the entry with the pre-commit value and re-arm the full TTL.

**Multi-instance caveat (must be stated, not assumed away)**: `IMemoryCache` is per-process.
With N instances behind a load balancer, deactivating on instance A evicts only A's entry;
instance B keeps serving the stale `true` until its own 30s entry expires. Revocation is
therefore bounded by 30s per instance, never "immediate", the moment N > 1. Today N = 1 —
the Redis backplane at `Program.cs:107-117` is registered only when
`ConnectionStrings:Redis` is set, and its own comment says Render's free plan runs a single
instance. **The seam**: a Redis-backed `IAccountStatusCache` registered under the same
`if (!string.IsNullOrWhiteSpace(redisConn))` condition already at `Program.cs:109` is a
one-line implementation swap with no call-site change. Not built now — there is no Redis in
the deployment to build it against.

### D3 — Configuration flag `AccountStatus:EnforcePerRequest`

**Choice**: new `AccountStatusOptions` at
`src/NicaRunner.Infrastructure/Security/AccountStatusOptions.cs`, next to `JwtSettings.cs`
which follows the identical pattern, bound in `Program.cs` beside line 171-175.

```json
"AccountStatus": { "EnforcePerRequest": true, "CacheSeconds": 30 }
```

**Default `true`** — shipping it off means PR3 delivers nothing. Precedent for
config-as-kill-switch is `LockoutOptions` (`src/NicaRunner.Application/Common/LockoutOptions.cs:10`,
"`Threshold = 0` desactiva el lockout sin necesidad de deploy").

**When off**: `OnTokenValidated` returns immediately — no cache read, no DB read, behaviour is
byte-for-byte today's ≤60-minute window, which is pre-existing and not a regression. The
invalidation call in `UpdateAsync` still runs and is a harmless no-op eviction, so PR3 rollback
is a single environment variable, not a revert.

**Honest limit**: `IOptions<T>` is bound at startup and env vars do not hot-reload. Turning the
flag off is a config change plus a service restart — cheaper than a code deploy, but not
zero-downtime-free. Do not describe it as "no restart".

### D4 — Fail-open semantics

| Failure | Behaviour | Visibility |
| --- | --- | --- |
| Cache miss + DB unreachable / query throws | **Allow** the request | `LogWarning` with userId + exception |
| User row not found (deleted between token issue and now) | **Allow** | `LogWarning` with userId |
| `NameIdentifier` claim missing or unparseable | **Allow** | `LogWarning` |
| Cache/DB returns `IsActive == false` | **`context.Fail()`** → 401 | `LogInformation` with userId |

The `try/catch` lives in `AccountStatusJwtEvents`, not inside the cache implementation, so the
security decision is readable at the policy site. Claim-missing fails open deliberately: the
token's signature was already validated by `TokenValidationParameters`
(`Program.cs:228-237`), so a subject-less token is our own bug, not an attack — rejecting would
lock out valid users for a non-security reason. Fail-open is silent by construction, so the
`LogWarning` is the only signal a degraded check leaves; it must carry a stable message
template so it is alertable in the CLEF sink (`Program.cs:57-64`).

### D5 — Two-active-admins guard

**Where**: `UserManagementService.UpdateAsync`, inserted after the seed-admin guard (line 87)
and before the diff block (line 89) — i.e. before any mutation, and after the two
identity-based guards.

**Ordering rationale**: self (lines 73-79) and seed (81-87) guards are pure in-memory checks
with no DB round trip, and their messages are more specific. If an admin tries to deactivate
themselves as the second-to-last admin, "No puedes desactivar tu propia cuenta." is the more
actionable error. Both existing guards are unchanged and keep their tests.

**Trigger** (evaluated on pre-mutation state): target is currently `Administrador` **and**
currently `IsActive` **and** (`request.IsActive is false` **or**
`request.Role is not null && request.Role != Administrador`).

**Count**: add `Task<int> CountActiveByRoleAsync(UserRole role, CancellationToken ct)` to
`IUserRepository`, implemented as `context.Users.CountAsync(u => u.Role == role && u.IsActive, ct)`.

**Rejected — counting `GetByRoleAsync(...).Count`**: it works (`UserRepository.cs:49-50`
already filters `u.Role == role && u.IsActive`) but it materializes every active admin row into
the change tracker inside a unit of work that ends in `SaveChangesAsync` at line 132. A
`COUNT(*)` states the intent and attaches nothing.

**Threshold arithmetic**: the target is still counted pre-mutation, so the post-operation count
is `n - 1`. Require `n - 1 >= 2`, i.e. **throw when `n < 3`**.

**Message** (exact): `ForbiddenException("No se puede dejar el sistema con menos de dos administradores activos.")`
→ 403 via `ExceptionHandlingMiddleware.cs:27-30`, `detail` read by `apiErrorMessage`
(`frontend/src/api/client.ts:22-28`).

### D6 — In-flight races: dedicated pre-check endpoint `GET /api/users/{id}/active-races`

**How the operator is actually modelled (verified, and it changes the query)**: `RaceJudge` is a
join row `{ Id, RaceId, UserId, JoinedAt }` (`src/NicaRunner.Domain/Entities/RaceJudge.cs:3-12`)
with a unique index on `(RaceId, UserId)` (`NicaRunnerDbContext.cs:99-101`). Rows are created in
exactly one place, `RaceService.JoinByCodeAsync` (`src/NicaRunner.Application/Races/RaceService.cs:143-162`)
— and line 152-153 **returns early when `race.AdminId == userId`, so the race's own admin never
gets a `RaceJudge` row**. A judges-only query would therefore silently miss a running race
operated by its admin. The query must be the union:

```csharp
context.Races.Where(r => r.Estado == RaceStatus.EnCurso
    && (r.AdminId == userId || r.Judges.Any(j => j.UserId == userId)))
```

`Result.CapturistaId` is capture history, not an assignment, and is not used.

**Rejected — 409 response**: `ConflictException` *blocks* the write
(`ExceptionHandlingMiddleware.cs:19-22`) and the product decision is warn-and-continue; making
it work needs a `force` flag on the shared `UpdateUserRequest` DTO, and `WriteProblemAsync`
(lines 42-49) emits a fixed `{status,title,detail}` with nowhere to put a race list.

**Rejected — field on `UserDto`**: `GET /api/users` would join `RaceJudge` and `Race` for every
row on every page load to serve a rare dialog.

**House-pattern fit**: `UsersController` already exposes `POST /{id}/unlock` (line 40) and
`GET /{id}/audit` (line 45) on the same id. A third sibling `GET` is the established shape and
leaves `PATCH` semantics untouched.

**Which service owns it — and this is what keeps PR2 and PR3 separable**: the method goes on
`IRaceService` (`GetActiveForUserAsync`), whose implementation already has `raceRepository`
injected, **not** on `UserManagementService`. `UsersController` injects `IRaceService` as a
third constructor dependency. Consequence: PR2 does not touch `UserManagementService`'s primary
constructor (lines 15-22) at all.

**Response DTO**: new `ActiveRaceSummaryDto(int Id, string Nombre, DateTime FechaCarrera)`.
Reusing `RaceDto` is rejected because it carries `JoinCode` (`RaceDto.cs:11`), the secret used to
join a race as judge — no reason to ship it to a confirmation dialog.

**Accepted tradeoff**: TOCTOU between pre-check and `PATCH`. The warning is advisory by product
decision, so a race starting in that window is not an invariant violation.

### D7 — `StatusBadge` generalization without breaking `RacesPage`

**Choice**: keep the component in `frontend/src/components/` (proposal decision — the tokens
`--badge-ok-bg` / `--badge-cl-bg` / `--radius-badge` are app-owned, and `@nicarunner/ui` exports
no Badge: `frontend/packages/ui/src/index.ts:1-14`). Introduce a tone-based inner primitive
(`tone: 'ok' | 'neutral' | 'muted'`, plus an optional `live` dot) and keep
`export function StatusBadge({ status }: { status: RaceStatus })` with its **exact current
signature** (`StatusBadge.tsx:28`) as a thin wrapper, so `RacesPage.tsx:77` is untouched by this
PR. Add a sibling `UserStatusBadge({ isActive }: { isActive: boolean })` rendering
Activo / Inactivo. The `dot-live` span (line 34) stays conditional on the race tone only.

### D8 — PR2/PR3 separability in `UpdateAsync`

| PR | Hunk in `UserManagementService.cs` | Constructor |
| --- | --- | --- |
| PR2 | insert guard block between line 87 and line 89 (top of method) | unchanged |
| PR3 | insert `accountStatusCache.Invalidate(user.Id)` after line 132 (bottom of method) | adds `IAccountStatusCache` param |

Disjoint hunks at opposite ends of a 66-line method, and only PR3 edits the primary constructor.
PR3 rebases onto PR2 without conflict. PR2 lands first (it is the lower-risk half).

## Data Flow

Session termination (PR3):

    request ──→ JwtBearerHandler ──→ OnTokenValidated
                                          │ flag off? ──→ allow (today's behaviour)
                                          ▼
                                   IAccountStatusCache
                                     hit ──→ bool
                                     miss ─→ IUserRepository.GetByIdAsync ─→ cache 30s
                                     throw ─→ LogWarning ──→ allow (fail open)
                                          │
                              IsActive == false ──→ context.Fail() ──→ 401
                                          │
                              web/mobile ──→ POST /auth/refresh ──→ refused (IsActive)
                                          └──→ clearSession() / tokenStore.clear()

    PATCH /api/users/{id} ──→ UpdateAsync ──→ SaveChangesAsync ──→ Invalidate(userId)

Deactivation with confirmation (PR2):

    Desactivar ──→ GET /api/users/{id}/active-races ──→ [] ──→ PATCH directly
                                                   └─→ [races] ──→ Modal (names them)
                                                                       └─ confirm ─→ PATCH

## File Changes

| File | PR | Action | Description |
| --- | --- | --- | --- |
| `frontend/src/components/StatusBadge.tsx` | 1 | Modify | Tone-based primitive; `StatusBadge(RaceStatus)` signature preserved; add `UserStatusBadge` |
| `frontend/src/features/users/UsersPage.tsx` | 1,2 | Modify | PR1: badge in Estado column (lines 98-100) + `try/catch` on `handleToggleActive` (lines 51-54) mirroring `handleRoleChange` (41-49). PR2: pre-check + confirm `Modal` |
| `frontend/src/__tests__/users-page.status-toggle.test.tsx` | 1 | Create | Estado value, toggle payload, `disabled={isSelf}`, error toast |
| `frontend/src/api/endpoints.ts` | 2 | Modify | `getUserActiveRaces(id)` after `getUserAudit` (lines 351-354) |
| `frontend/src/api/types.ts` | 2 | Modify | `ActiveRaceSummary` type |
| `src/NicaRunner.Application/Common/Interfaces/IUserRepository.cs` | 2 | Modify | `CountActiveByRoleAsync` |
| `src/NicaRunner.Infrastructure/Repositories/UserRepository.cs` | 2 | Modify | `CountActiveByRoleAsync` impl |
| `src/NicaRunner.Application/Users/UserManagementService.cs` | 2,3 | Modify | PR2: guard after line 87. PR3: ctor dep + invalidation after line 132 |
| `src/NicaRunner.Application/Common/Interfaces/IRaceRepository.cs` + `RaceRepository.cs` | 2 | Modify | `GetActiveForUserAsync(userId)` |
| `src/NicaRunner.Application/Races/IRaceService.cs` + `RaceService.cs` | 2 | Modify | `GetActiveForUserAsync` → `ActiveRaceSummaryDto` |
| `src/NicaRunner.Application/Races/Dtos/ActiveRaceSummaryDto.cs` | 2 | Create | `(Id, Nombre, FechaCarrera)` |
| `src/NicaRunner.Api/Controllers/UsersController.cs` | 2 | Modify | `GET /{id}/active-races`; inject `IRaceService` |
| `src/NicaRunner.Application/Common/Interfaces/IAccountStatusCache.cs` | 3 | Create | `IsActiveAsync` / `Invalidate` |
| `src/NicaRunner.Infrastructure/Security/AccountStatusCache.cs` | 3 | Create | `IMemoryCache` + `IUserRepository` |
| `src/NicaRunner.Infrastructure/Security/AccountStatusOptions.cs` | 3 | Create | `EnforcePerRequest`, `CacheSeconds` |
| `src/NicaRunner.Api/Auth/AccountStatusJwtEvents.cs` | 3 | Create | `OnTokenValidated` body + fail-open policy |
| `src/NicaRunner.Api/Program.cs` | 3 | Modify | `AddMemoryCache()`, `Configure<AccountStatusOptions>` (near 171-175), DI, `OnTokenValidated` (in the block at 246-260) |
| `src/NicaRunner.Api/appsettings.json` | 3 | Modify | `AccountStatus` section |
| `tests/NicaRunner.Tests/UserManagementServiceTests.cs` | 2,3 | Modify | Guard cases; invalidation-after-save |
| `src/NicaRunner.Infrastructure/Migrations/` | — | **Unchanged** | No schema change |

## Interfaces / Contracts

```csharp
// Application/Common/Interfaces — Application must not see Microsoft.Extensions.Caching
public interface IAccountStatusCache
{
    Task<bool> IsActiveAsync(int userId, CancellationToken ct = default);
    void Invalidate(int userId);
}

Task<int> CountActiveByRoleAsync(UserRole role, CancellationToken ct = default); // IUserRepository
Task<List<ActiveRaceSummaryDto>> GetActiveForUserAsync(int userId, CancellationToken ct = default); // IRaceService
```

```
GET /api/users/{id}/active-races   → 200 [{ id, nombre, fechaCarrera }]
                                     (Administrador only, inherited from UsersController.cs:19)
```

No change to `UserDto`, `UpdateUserRequest`, `RaceDto`, or any enum. No new status vocabulary.

## Testing Strategy

| Layer | What to test | Approach |
| --- | --- | --- |
| Unit (API) | Guard throws at `n < 3`; passes at `n >= 3`; not triggered when target is inactive or non-admin; self/seed guards still win | xUnit + Moq on `UserManagementService`, mirroring `UserManagementServiceTests.cs` |
| Unit (API) | `Invalidate` called **after** `SaveChangesAsync` | Moq `MockSequence` / callback ordering |
| Unit (API) | Fail-open: repository throws → request allowed; `IsActive == false` → `context.Fail()`; flag off → no cache/DB call at all | Direct test of `AccountStatusJwtEvents` |
| Unit (API) | Union query returns admin-operated `EnCurso` races **and** judge-joined ones; excludes `Planeada`/`Terminada` | Repository test on the in-memory/SQLite context |
| Integration | Active user is never rejected by the new check; disabled user gets **401** (not 403) | `WebApplicationFactory`, per `AliasIntegrationTests.cs` |
| Frontend | Estado badge renders Activo/Inactivo; toggle calls `updateUser(id, { isActive })`; self row `disabled`; failed toggle raises a toast; confirm modal names the race and PATCH still fires on confirm | Vitest + Testing Library + `renderWithProviders`, per `users-page.pagination.under-load.test.tsx` |

`openspec/config.yaml:53` sets `tdd: true` — RED tests first in every slice.

## Threat Matrix

N/A — no routing-table, shell, subprocess, VCS/PR-automation, executable-file-classification, or
process-integration boundary. The substantive risk surface here is the authentication hot path,
covered by D4's failure table and by the integration test asserting that an active user is never
rejected.

## Migration / Rollout

No database migration. Rollout is per slice.

| Slice | Rollback |
| --- | --- |
| PR1 | Revert. Badge returns to plain text; no API, data, or contract touched |
| PR2 | Revert. Guard is in-memory validation with no persisted state; the endpoint is read-only and additive |
| PR3 | Set `AccountStatus__EnforcePerRequest=false` and restart — behaviour returns to today's ≤60-minute window without a code deploy. Full revert only if the cache itself misbehaves |

### Mobile blast radius

PR1 and PR2 are backoffice-only: the mobile app has no users screen, no user DTO, and never
calls the Admin-only pre-check endpoint. **PR3 changes runtime behaviour mobile will feel with
no mobile code change**: mobile holds the same JWT, so a Capturista disabled mid-shift now 401s
on their next request (previously up to 60 minutes later), which drives refresh → refusal →
logout in `TokenAuthenticator.kt`. No DTO, enum, or auth-shape change, so no cross-client
contract change — but the *timing* change must be validated against
`/home/user/NicaRunner` before apply, per `openspec/config.yaml:35-36`.

## What would make this design wrong

- **`context.Fail()` did not produce 401.** Everything about "no client changes" rests on it.
  The integration test asserting `401` on a disabled user is the load-bearing check — if it
  returns 403, D1 collapses back to a custom middleware.
- **The deployment is already multi-instance.** D2's "immediate" revocation is only immediate at
  N = 1. If Render is scaled or `ConnectionStrings:Redis` is set, revocation latency becomes 30s
  per stale instance and the Redis-backed implementation must ship with PR3, not later.
- **`RaceJudge` rows are not actually populated in practice.** Rows come only from
  `JoinByCodeAsync`; if judges are onboarded some other way in the field, the pre-check returns
  empty and the dialog never fires — a silent no-op, not a visible failure.
- **Mobile capture is offline-first.** If unsynced Room captures exist when PR3 forces a logout
  mid-race, data could be stranded. Unverified from this repository; must be checked in
  `/home/user/NicaRunner` before PR3 is applied.
- **An installation seeded without the three protected admins.** The two-admin guard is
  effectively dormant otherwise (`ProtectedSeedUsers.cs`, `AdminUserSeeder.cs:44` seeds them
  `IsActive = true`); in a small non-standard installation it will block legitimate operations
  with no override. Deliberate, and it belongs in the release note.

## Open Questions

None blocking. All five proposal questions are settled in
`proposal.md` `## Decisions (resolved with the user)`; TTL (30s), config key name, fail-open
matrix, guard ordering, endpoint ownership, and PR-separability are decided above.
