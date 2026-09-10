# Tasks: Backoffice User Status — close the real gaps

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | PR1 ~190, PR2 ~270, PR3 ~200 (total ~660) |
| 400-line budget risk | Low per slice / High if delivered as one PR |
| Chained PRs recommended | Yes |
| Suggested split | PR1 (frontend) -> PR2 (API+frontend) -> PR3 (API) |
| Delivery strategy | ask-on-risk (resolved: three chained PRs, stacked-to-main) |
| Chain strategy | stacked-to-main (resolved) |

Decision needed before apply: No — resolved at session preflight (stacked-to-main, PR1 -> main)
Chained PRs recommended: Yes
Chain strategy: stacked-to-main
400-line budget risk: Low (per slice, given the 3-way split below)

### Suggested Work Units

| Unit | Goal | PR | Focused test command | Runtime harness | Rollback boundary |
|------|------|-----|----------------------|-----------------|-------------------|
| 1 | Generalize `StatusBadge`, add `UserStatusBadge`, wire Estado column, fix `handleToggleActive` try/catch, add test file | PR1 | `cd frontend && npm test` | `cd frontend && npm run build` (tsc -b) | Revert commit; `StatusBadge` returns to `RaceStatus`-only form, Estado column to plain text |
| 2 | Two-active-admin guard + `GET /{id}/active-races` pre-check + confirm dialog | PR2 | `dotnet test tests/NicaRunner.Tests/NicaRunner.Tests.csproj --configuration Release` and `cd frontend && npm test` | `dotnet build NicaRunner.sln --configuration Release` and `cd frontend && npm run build` | Revert guard block in `UpdateAsync`; endpoint is read-only additive; dialog is client-side only |
| 3 | Per-request `IsActive` check via `OnTokenValidated`, `IAccountStatusCache`, config flag, fail-open | PR3 | `dotnet test tests/NicaRunner.Tests/NicaRunner.Tests.csproj --configuration Release` | `dotnet build NicaRunner.sln --configuration Release`; manual: deactivate a user, retry their next request, confirm 401 | Set `AccountStatus__EnforcePerRequest=false` + restart; no code revert needed |

PR2 and PR3 both edit `UserManagementService.UpdateAsync` (design D8): PR2's guard sits at the
top of the method (after line 87, before line 89), PR3's cache invalidation sits at the bottom
(after line 132) and adds a constructor parameter. Land PR2 first; PR3 rebases onto PR2 without
conflict because the hunks are disjoint.

---

## Phase 0: Pre-PR3 verification (blocking, not optional)

- [x] 0.1 Confirm in `/home/user/NicaRunner` whether unsynced offline-first Room captures could
      be stranded by a forced mid-race logout when a Capturista is disabled (design "What would
      make this design wrong" / proposal Cross-client impact). Read-only check against the sibling
      repo; do not modify it.
- [x] 0.2 Confirm the deployment is still single-instance (no `ConnectionStrings:Redis` set, no
      horizontal scaling). If multi-instance or Redis is active, D2's "immediate" revocation claim
      is false and a Redis-backed `IAccountStatusCache` must ship with PR3, not later.
- [x] 0.3 Record both findings before starting Phase 5 (PR3). If either finding contradicts the
      design's assumption, stop and flag it instead of implementing PR3 as specced.

## Phase 1: PR1 — Status badge, frontend tests, try/catch fix (frontend only, ~190 lines)

- [x] 1.1 RED: extend `frontend/src/__tests__/users-page.status-toggle.test.tsx` (new file) with
      failing assertions for Estado badge label per `isActive` (Activo/Inactivo), using
      `renderWithProviders` per `users-page.pagination.under-load.test.tsx`.
- [x] 1.2 RED: add failing assertion that clicking Desactivar/Activar calls
      `updateUser(id, { isActive: !previousValue })`.
- [x] 1.3 RED: add failing assertion that the signed-in admin's own row renders the toggle button
      `disabled`.
- [x] 1.4 RED: add failing assertion that a rejected `updateUser` call from `handleToggleActive`
      shows an error toast and does not optimistically update the row.
- [x] 1.5 GREEN: generalize `frontend/src/components/StatusBadge.tsx` to a tone-based primitive
      (`tone: 'ok' | 'neutral' | 'muted'`, optional `live` dot) while preserving the exact existing
      `export function StatusBadge({ status }: { status: RaceStatus })` signature (line 28) as a
      thin wrapper so `RacesPage.tsx:77` is untouched.
- [x] 1.6 GREEN: add sibling `UserStatusBadge({ isActive }: { isActive: boolean })` in the same
      file, rendering Activo/Inactivo via the tone primitive.
- [x] 1.7 GREEN: wire `UserStatusBadge` into the Estado column in
      `frontend/src/features/users/UsersPage.tsx:98-99`, replacing the plain-text render.
- [x] 1.8 GREEN: add `try/catch` to `handleToggleActive` (`UsersPage.tsx:51-54`), mirroring the
      pattern already used by `handleRoleChange` (lines 41-49) — catch, raise an error toast, skip
      the optimistic update on failure.
- [x] 1.9 REFACTOR: confirm `RacesPage.tsx:77` renders unchanged (manual/visual check or existing
      `RacesPage` test if one asserts badge output).
- [x] 1.10 Verify PR1 green: `cd frontend && npm test`. Verify build: `cd frontend && npm run build`.

## Phase 2: PR2 — Two-active-admin guard (API, targets PR1 branch)

- [x] 2.1 RED: add failing test in `tests/NicaRunner.Tests/UserManagementServiceTests.cs` — with
      exactly two active `Administrador` users, deactivating one throws `ForbiddenException` with
      detail "No se puede dejar el sistema con menos de dos administradores activos." (spec
      scenario "Deactivation blocked at the floor").
- [x] 2.2 RED: add failing test for the same floor triggered by changing `Role` away from
      `Administrador` (spec scenario "Role change blocked at the floor").
- [x] 2.3 RED: add failing test that with three or more active admins, deactivation/re-role
      succeeds (spec scenario "Above the floor, allowed").
- [x] 2.4 RED: add failing test that inactive `Administrador` rows are excluded from the count
      (spec scenario "Only active admins count").
- [x] 2.5 RED: add failing test confirming self-deactivation and protected-seed-admin guards still
      win and are evaluated before the new guard (existing guards unchanged, new guard ordered
      after them).
- [x] 2.6 GREEN: add `Task<int> CountActiveByRoleAsync(UserRole role, CancellationToken ct)` to
      `src/NicaRunner.Application/Common/Interfaces/IUserRepository.cs`.
- [x] 2.7 GREEN: implement `CountActiveByRoleAsync` in
      `src/NicaRunner.Infrastructure/Repositories/UserRepository.cs` as
      `context.Users.CountAsync(u => u.Role == role && u.IsActive, ct)`.
- [x] 2.8 GREEN: insert the guard block in
      `src/NicaRunner.Application/Users/UserManagementService.cs`, between the seed-admin guard
      (line 87) and the diff block (line 89). Trigger: target is currently `Administrador` and
      currently `IsActive`, and (`request.IsActive is false` or `request.Role` changes away from
      `Administrador`). Throw when `n < 3` (post-mutation floor `n - 1 >= 2`).
- [x] 2.9 Verify: `dotnet test tests/NicaRunner.Tests/NicaRunner.Tests.csproj --configuration Release`.

## Phase 3: PR2 — In-flight Capturista pre-check endpoint (API)

- [x] 3.1 RED: add a repository/service-level test asserting the active-races query returns races
      where the target user is `RaceJudge` **and** races where the target user is `Race.AdminId`
      (union), excluding `Planeada`/`Terminada` races. This is load-bearing: `RaceService.JoinByCodeAsync`
      (`src/NicaRunner.Application/Races/RaceService.cs:152-153`) returns early when
      `race.AdminId == userId`, so a race's own admin never gets a `RaceJudge` row — a judges-only
      query would silently miss a running race operated by its admin.
- [x] 3.2 GREEN: create `src/NicaRunner.Application/Races/Dtos/ActiveRaceSummaryDto.cs` with
      `(int Id, string Nombre, DateTime FechaCarrera)`.
- [x] 3.3 GREEN: add `Task<List<ActiveRaceSummaryDto>> GetActiveForUserAsync(int userId, CancellationToken ct = default)`
      to `IRaceRepository`/`RaceRepository.cs`, implementing the union query:
      `context.Races.Where(r => r.Estado == RaceStatus.EnCurso && (r.AdminId == userId || r.Judges.Any(j => j.UserId == userId)))`.
- [x] 3.4 GREEN: add `GetActiveForUserAsync` to `IRaceService`/`RaceService.cs`, mapping to
      `ActiveRaceSummaryDto`.
- [x] 3.5 GREEN: add `GET /api/users/{id}/active-races` to
      `src/NicaRunner.Api/Controllers/UsersController.cs`, injecting `IRaceService` as a third
      constructor dependency (does not touch `UserManagementService`'s constructor — keeps this
      slice separable from PR3 per design D8).
- [x] 3.6 Verify: `dotnet test tests/NicaRunner.Tests/NicaRunner.Tests.csproj --configuration Release`.

## Phase 4: PR2 — Frontend pre-check + confirm dialog (frontend, targets PR1 branch)

- [x] 4.1 RED: add failing frontend test — pre-check returns one active race, admin clicks
      Desactivar, a confirmation `Modal` appears naming that race.
- [x] 4.2 RED: add failing frontend test — pre-check returns no active races, `PATCH` fires
      directly with no dialog.
- [x] 4.3 RED: add failing frontend test — pre-check returns multiple active races, the dialog
      names every one.
- [x] 4.4 RED: add failing frontend test — admin confirms the dialog, `PATCH` proceeds and
      deactivation succeeds.
- [x] 4.5 RED: add failing frontend test — admin cancels the dialog, no `PATCH` request is sent
      and the user remains active.
- [x] 4.6 GREEN: add `getUserActiveRaces(id)` to `frontend/src/api/endpoints.ts`, after
      `getUserAudit` (lines 351-354).
- [x] 4.7 GREEN: add `ActiveRaceSummary` type to `frontend/src/api/types.ts`.
- [x] 4.8 GREEN: wire the pre-check call and confirm `Modal` into `handleToggleActive`'s
      deactivation path in `UsersPage.tsx`, following the `RestartRaceDialog.tsx` naming-affected-
      items precedent.
- [x] 4.9 Verify PR2 green (combined with Phase 2/3): `dotnet test tests/NicaRunner.Tests/NicaRunner.Tests.csproj --configuration Release`
      and `cd frontend && npm test`. Verify build: `dotnet build NicaRunner.sln --configuration Release`
      and `cd frontend && npm run build`.

## Phase 5: PR3 — Session revocation infrastructure (API only, targets PR2 branch)

Do not start this phase until Phase 0 (0.1-0.3) is recorded.

- [x] 5.1 RED: add a load-bearing integration test (per `AliasIntegrationTests.cs` pattern in
      `tests/NicaRunner.Tests/`) asserting a disabled user's next authenticated request returns
      **401**, not 403. This is the check the whole "no client changes needed" argument (design
      D1) depends on — if it returns 403 the design falls back to custom middleware and Phase 5
      must be redesigned before continuing.
- [x] 5.2 RED: add an integration test asserting an active, valid user is never rejected by the
      new check (spec "Active user unaffected").
- [x] 5.3 RED: add unit tests on `AccountStatusJwtEvents` for the fail-open matrix (design D4):
      cache/DB throw -> allow + `LogWarning`; user row not found -> allow + `LogWarning`; missing/
      unparseable `NameIdentifier` -> allow + `LogWarning`; `IsActive == false` -> `context.Fail()`
      + `LogInformation`.
- [x] 5.4 RED: add a unit test asserting that with `AccountStatus:EnforcePerRequest` off, no
      cache/DB call happens at all.
- [x] 5.5 RED: add a unit test on `UserManagementService.UpdateAsync` asserting
      `IAccountStatusCache.Invalidate(userId)` is called after `SaveChangesAsync`, not at the
      `IsActive` assignment (Moq `MockSequence`/callback ordering).
- [x] 5.6 GREEN: create `src/NicaRunner.Application/Common/Interfaces/IAccountStatusCache.cs`
      (`Task<bool> IsActiveAsync(int userId, CancellationToken ct = default)`, `void Invalidate(int userId)`).
- [x] 5.7 GREEN: create `src/NicaRunner.Infrastructure/Security/AccountStatusCache.cs`
      implementing `IAccountStatusCache` via `IMemoryCache` + `IUserRepository.GetByIdAsync`, key
      `account-status:{userId}`, absolute 30s TTL.
- [x] 5.8 GREEN: create `src/NicaRunner.Infrastructure/Security/AccountStatusOptions.cs`
      (`EnforcePerRequest`, `CacheSeconds`), following the `JwtSettings.cs` pattern.
- [x] 5.9 GREEN: create `src/NicaRunner.Api/Auth/AccountStatusJwtEvents.cs` with the
      `OnTokenValidated` body implementing the fail-open matrix from design D4.
- [x] 5.10 GREEN: wire `builder.Services.AddMemoryCache()`, `Configure<AccountStatusOptions>` (near
      `Program.cs:171-175`), DI for `IAccountStatusCache`, and `OnTokenValidated` into the existing
      `JwtBearerEvents` block (`Program.cs:246-260`).
- [x] 5.11 GREEN: add `AccountStatus: { EnforcePerRequest: true, CacheSeconds: 30 }` to
      `src/NicaRunner.Api/appsettings.json`.
- [x] 5.12 GREEN: add `IAccountStatusCache` as a new constructor parameter on
      `UserManagementService` and call `accountStatusCache.Invalidate(user.Id)` after
      `SaveChangesAsync` (`UserManagementService.cs:132`).
- [x] 5.13 Verify PR3 green: `dotnet test tests/NicaRunner.Tests/NicaRunner.Tests.csproj --configuration Release`.
      Verify build: `dotnet build NicaRunner.sln --configuration Release`.

## Phase 6: Final cross-PR checks

- [ ] 6.1 Confirm `IsActive` remains the only status vocabulary across all three slices — no new
      enum, no new DTO field beyond `ActiveRaceSummaryDto` and `ActiveRaceSummary` (frontend type).
- [ ] 6.2 Confirm no `src/NicaRunner.Infrastructure/Migrations/` file was added in any slice.
- [ ] 6.3 Confirm each PR independently satisfies its own build+test command from the forecast
      table above before requesting review.
