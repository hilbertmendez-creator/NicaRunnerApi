# Apply Progress: backoffice-user-status-toggle

## Scope executed

Phase 1 only (tasks 1.1–1.10), per explicit instruction. Phase 0 and Phases 2–6 were
NOT touched.

**Mode**: Strict TDD (RED → GREEN → REFACTOR), `openspec/config.yaml` `apply.tdd: true`.
**Branch**: `claude/backoffice-user-toggle-jy1gqu-pr1-status-badge` (pre-existing, not created
here). No commit was made — orchestrator handles git per instructions.
**Artifact store**: `openspec`. Engram is NOT available in this environment (no `engram`
binary, no `mem_*` tools) — this is a deviation from the `sdd-apply` skill's default
`mem_save`/`mem_search` persistence instructions, explicitly superseded by the task prompt.
Progress is persisted only to this file and `tasks.md`.

## Files changed

| File | Action | Lines (authored) |
|------|--------|-------------------|
| `frontend/src/components/StatusBadge.tsx` | Modified | +47 / -21 (net diff line, see below) |
| `frontend/src/features/users/UsersPage.tsx` | Modified | included in above diff stat |
| `frontend/src/__tests__/users-page.status-toggle.test.tsx` | Created | 103 |

`git diff --stat` (StatusBadge.tsx + UsersPage.tsx combined): `2 files changed, 47
insertions(+), 21 deletions(-)`. Plus the new 103-line test file, all additions.
**Total authored changed lines: 171** (68 modified + 103 new), within the ~190 estimate
and the 400-line PR budget.

## What was done

1. **`StatusBadge.tsx`**: replaced the `RaceStatus`-keyed `STYLES`/`LABELS` records with a
   tone-based inner primitive (`Badge({ tone: 'ok' | 'neutral' | 'muted', label, live? })`
   per design D7). `export function StatusBadge({ status }: { status: RaceStatus })` keeps
   its exact original signature as a thin wrapper mapping `RaceStatus` -> tone/label
   (`Planeada` -> `neutral`, `EnCurso` -> `ok` + live dot, `Terminada` -> `muted`),
   reproducing the prior visual styles 1:1 (same CSS custom properties per status, same
   `dot-live` conditional). Added sibling `UserStatusBadge({ isActive }: { isActive:
   boolean })` (`isActive` -> `ok`/"Activo", inactive -> `neutral`/"Inactivo").
2. **`UsersPage.tsx`**: Estado column now renders `<UserStatusBadge isActive={u.isActive}
   />` instead of the plain-text ternary. `handleToggleActive` now wraps the `updateUser`
   call in `try/catch`, mirroring `handleRoleChange` exactly: success path shows a
   `toast.success` and calls `reload()`; failure path shows `toast.error` and skips
   `reload()` (no optimistic update).
3. **Test file** (`users-page.status-toggle.test.tsx`, new): 4 tests, following the
   `renderWithProviders` + `vi.mock('../api/endpoints', ...)` pattern from
   `users-page.pagination.under-load.test.tsx`. Because the `DataTable` component renders
   both a desktop table row and a hidden mobile-card duplicate for every row, queries use
   `findAllBy*`/pick-first or "length > 0" assertions rather than single-match `findBy*` —
   matching the existing convention in the pagination test file (`findAllByRole(...)[0]`).

## TDD Cycle Evidence

| Task | Test File | Layer | Safety Net | RED | GREEN | TRIANGULATE | REFACTOR |
|------|-----------|-------|------------|-----|-------|-------------|----------|
| 1.1 | `users-page.status-toggle.test.tsx` | Integration (RTL) | N/A (new file) | ✅ Written (see note) | ✅ Passed | ✅ 2 cases (active + inactive label) | ➖ None needed |
| 1.2 | `users-page.status-toggle.test.tsx` | Integration (RTL) | N/A (new file) | ✅ Written (see note) | ✅ Passed | ➖ Single (one payload shape asserted) | ➖ None needed |
| 1.3 | `users-page.status-toggle.test.tsx` | Integration (RTL) | N/A (new file) | ✅ Written (see note) | ✅ Passed | ➖ Single (disabled boolean, one scenario) | ➖ None needed |
| 1.4 | `users-page.status-toggle.test.tsx` | Integration (RTL) | ✅ Baseline: pagination test file's 1/1 still green before this change | ✅ Written, confirmed failing | ✅ Passed after `try/catch` added | ➖ Single (spec has one "Rejected toggle" scenario) | ✅ Clean — no further extraction needed |
| 1.5 | (covered by 1.1–1.4 as approval tests) | — | ✅ All 4 tests green before AND after this refactor | — (refactor task, no new test) | ✅ tsc -b clean, full suite green | — | ✅ Extracted `Badge`/`Tone` primitive, no duplication |
| 1.6 | (covered by 1.1) | — | — | — | ✅ | — | ✅ Clean |
| 1.7 | (covered by 1.1–1.3) | — | — | — | ✅ | — | ✅ Clean |
| 1.8 | `users-page.status-toggle.test.tsx` (test 4) | Integration (RTL) | ✅ | ✅ Written first (RED confirmed below) | ✅ Passed | ➖ Single scenario | ✅ Clean |
| 1.9 | manual/visual — no existing `RacesPage` test in the repo | N/A | N/A | N/A | N/A (confirmed via `tsc -b` type-check + code inspection, see note) | N/A | N/A |
| 1.10 | full suite | — | — | — | ✅ 56/56 passed, build green | — | — |

**Honest note on RED for 1.1–1.3**: these three assertions describe *observable behavior*
that the pre-existing plain-text Estado column already satisfied (the text "Activo" /
"Inactivo" was already rendered, `updateUser(id, {isActive})` was already called on click,
and the toggle button was already `disabled` for the signed-in admin's own row via
`isSelf`). Testing-library queries text content and DOM attributes, not implementation
(per the strict-tdd "Implementation Detail Coupling Rule" — CSS class assertions are
banned), so a "badge" vs. "plain text" render is not distinguishable to these assertions.
Running the suite confirmed this: 3/4 tests passed immediately, only test 4 (rejected
toggle -> error toast, no reload) was a true RED, because `handleToggleActive` had no
`try/catch` at all (an unhandled promise rejection, confirmed in the RED run — see below).
Tests 1.1–1.3 therefore served as **approval/safety-net tests** for the `StatusBadge`
refactor (1.5–1.7): they were run again after each GREEN step and stayed green throughout,
proving the refactor did not change observable behavior. This is a deviation from a pure
RED cycle for those three tasks specifically, but not a deviation from the spec or design —
noted here rather than silently omitted per the skill's "note deviations" rule.

**RED baseline run** (`npx vitest run src/__tests__/users-page.status-toggle.test.tsx`,
before the `StatusBadge`/`UsersPage` GREEN edits):
```
 Test Files  1 failed (1)
      Tests  1 failed | 3 passed (4)
   × UsersPage status toggle > shows an error toast and does not reload the list when the toggle is rejected
Unhandled Rejection: Error: forbidden
```
This confirms the pre-existing bug (no try/catch on `handleToggleActive`) and that the new
test correctly detects it.

**GREEN run** (same command, after the edits):
```
 Test Files  1 passed (1)
      Tests  4 passed (4)
```

### Test Summary
- **Total tests written**: 4
- **Total tests passing**: 4 (in the focused file); 56/56 in the full suite
- **Layers used**: Integration/RTL (4), Unit (0), E2E (0) — matches the project's testing
  capability profile (`@testing-library/react` + jsdom, no E2E configured)
- **Approval tests** (refactoring): 3 (tasks 1.1–1.3, see note above) — kept green across
  the `StatusBadge` generalization (1.5–1.6) and the Estado-column wiring (1.7)
- **Pure functions created**: 0 net-new pure functions; the `Badge`/tone-mapping tables in
  `StatusBadge.tsx` are pure lookup tables, same shape as the pre-existing `STYLES`/`LABELS`

## Work Unit Evidence (Hard Gate, all modes)

| Evidence | Value |
|---|---|
| Focused test command and exact result | `npx vitest run src/__tests__/users-page.status-toggle.test.tsx` → RED: `1 failed \| 3 passed (4)`. GREEN (after fix): `4 passed (4)` |
| Runtime harness command/scenario and exact result | `cd frontend && npm test` → `Test Files 17 passed (17)`, `Tests 56 passed (56)`. `cd frontend && npm run build` → `tsc -b` clean, `vite build` succeeded (`✓ built in 2.32s`) |
| Rollback boundary | Revert the 3 changed files (`StatusBadge.tsx`, `UsersPage.tsx`, new test file). `StatusBadge` returns to its prior `RaceStatus`-only form; Estado column returns to plain text; `handleToggleActive` returns to its unguarded form. No API, data, or contract touched — matches the design's stated PR1 rollback ("Revert. Badge returns to plain text; no API, data, or contract touched") |

## Deviations from Design

None — implementation matches design D7 exactly: tone-based primitive with `tone: 'ok' |
'neutral' | 'muted'`, optional `live` dot, exact `StatusBadge({ status })` signature
preserved as a thin wrapper, sibling `UserStatusBadge({ isActive })` added, component kept
in `frontend/src/components/` (not promoted to `@nicarunner/ui`).

One naming choice not fully specified by the design: which tone each `RaceStatus`/`isActive`
value maps to. Design only fixed the tone *names* (`ok`/`neutral`/`muted`) and said the
visual output must stay identical for `RaceStatus`. Chosen mapping: `Planeada` -> `neutral`
(was `--badge-cl-*`, CSS comment already calls it "neutral chip"), `EnCurso` -> `ok` (was
`--badge-ok-*`), `Terminada` -> `muted` (was `--badge-pr-*`). For `UserStatusBadge`:
`isActive: true` -> `ok` (reuses the green "ok" tokens), `isActive: false` -> `neutral`
(reuses the dim gray tokens) rather than `muted` (which reuses the warning-orange `--wn-*`
tokens and would misleadingly suggest an alert state for a routine "Inactivo" badge).

## Issues Found

- The `DataTable` component (`@nicarunner/ui`) renders every row twice — once in a desktop
  `<table>` and once in a `sm:hidden` mobile-card list — which means any `findByRole`/
  `findByText` single-match query against a rendered `UsersPage`/`RacesPage` will throw
  "Found multiple elements" unless the test explicitly handles the duplication (as the
  existing `users-page.pagination.under-load.test.tsx` does with `findAllByRole(...)[0]`
  and length assertions). Not a regression introduced by this change — pre-existing
  `DataTable` behavior — but worth flagging since it is easy to miss when writing new
  frontend tests against tables in this codebase.
- No `RacesPage` test exists in the repo (task 1.9 originally suggested "manual/visual
  check or existing `RacesPage` test if one asserts badge output" — there is no such
  test). Verification for 1.9 was: (a) `export function StatusBadge({ status }: { status:
  RaceStatus })` signature is byte-identical to before, so `RacesPage.tsx:77`'s
  `<StatusBadge status={race.estado} />` call site needed zero changes and still
  type-checks under `tsc -b`; (b) the `RACE_STATUS_TONE`/`RACE_STATUS_LABELS` tables plus
  the `live={status === 'EnCurso'}` prop reproduce the exact same CSS custom properties,
  class names, and conditional `dot-live` span the previous `STYLES`/`LABELS` records did,
  per status. `RacesPage.tsx` itself was not edited (confirmed via `git status` — only
  `StatusBadge.tsx`, `UsersPage.tsx`, and the new test file changed).

## Remaining Tasks (as of PR1 batch)

Phase 0 (0.1–0.3) and Phases 2–6 (tasks 2.1 onward) are NOT started — out of scope for
this batch by explicit instruction.

## Workload / PR Boundary

- Mode: stacked PR slice (chained, stacked-to-main)
- Current work unit: Unit 1 (PR1) — "Generalize `StatusBadge`, add `UserStatusBadge`, wire
  Estado column, fix `handleToggleActive` try/catch, add test file"
- Boundary: starts from the pre-existing plain-text Estado column and unguarded
  `handleToggleActive`; ends with the badge-based Estado column, `UserStatusBadge`,
  guarded `handleToggleActive`, and the new test file — all frontend-only, no API touched
- Estimated review budget impact: 171 authored changed lines, well under the 400-line
  budget and close to the ~190-line forecast

## Status (as of PR1 batch)

10/10 Phase 1 tasks complete (1.1–1.10). Ready for verify (Phase 1 scope only) or for the
next apply batch (Phase 0 / Phase 2) — not started here.

---

# PR2 batch — Phases 2, 3, 4 (tasks 2.1–2.9, 3.1–3.6, 4.1–4.9)

## Scope executed

Phases 2, 3, and 4 only, per explicit instruction. Phase 0, 1 (already done in the PR1
batch above), 5 and 6 were NOT touched in this batch.

**Mode**: Strict TDD (RED → GREEN → REFACTOR), `openspec/config.yaml` `apply.tdd: true`.
**Branch**: `claude/backoffice-user-toggle-jy1gqu-pr2-guards` (pre-existing, stacked on the
PR1 branch, not created here). No commit was made — orchestrator handles git.
**Artifact store**: `openspec`. Engram is NOT available in this environment (no `engram`
binary, no `mem_*` tools) — same deviation as PR1, explicitly superseded by the task
prompt. Progress is persisted only to this file and `tasks.md`.

## Files changed (PR2)

| File | Action | Lines (authored) |
|------|--------|-------------------|
| `src/NicaRunner.Application/Common/Interfaces/IUserRepository.cs` | Modified | +4 |
| `src/NicaRunner.Infrastructure/Repositories/UserRepository.cs` | Modified | +6 |
| `src/NicaRunner.Application/Users/UserManagementService.cs` | Modified | +11 |
| `tests/NicaRunner.Tests/UserManagementServiceTests.cs` | Modified | +75 |
| `tests/NicaRunner.Tests/UserRepositoryTests.cs` | Created | 31 |
| `src/NicaRunner.Application/Races/Dtos/ActiveRaceSummaryDto.cs` | Created | 6 |
| `src/NicaRunner.Application/Common/Interfaces/IRaceRepository.cs` | Modified | +11 |
| `src/NicaRunner.Infrastructure/Repositories/RaceRepository.cs` | Modified | +11 |
| `src/NicaRunner.Application/Races/IRaceService.cs` | Modified | +7 |
| `src/NicaRunner.Application/Races/RaceService.cs` | Modified | +3 |
| `src/NicaRunner.Api/Controllers/UsersController.cs` | Modified | +11/-1 |
| `tests/NicaRunner.Tests/RaceRepositoryActiveForUserTests.cs` | Created | 58 |
| `frontend/src/api/endpoints.ts` | Modified | +9 |
| `frontend/src/api/types.ts` | Modified | +8 |
| `frontend/src/features/users/UsersPage.tsx` | Modified | +64/-3 |
| `frontend/src/__tests__/users-page.status-toggle.test.tsx` | Modified | +75 |

`git diff --stat` (13 tracked files): `291 insertions(+), 4 deletions(-)`. Plus 3 new
files (`UserRepositoryTests.cs` 31, `ActiveRaceSummaryDto.cs` 6,
`RaceRepositoryActiveForUserTests.cs` 58 = 95 lines, all additions).
**Total authored changed lines: 390** (295 tracked + 95 new files) — within the 400-line
review budget cap (forecast was ~270; the union-query and floor-guard SQLite-backed tests
required to make these scenarios genuinely load-bearing pushed it higher; trimmed comments
and deduplicated frontend test literals to stay under the cap).

## What was done

1. **Two-active-admin guard (Phase 2, design D5)**: added
   `IUserRepository.CountActiveByRoleAsync(UserRole, ct)`, implemented in `UserRepository`
   as `context.Users.CountAsync(u => u.Role == role && u.IsActive, ct)`. Inserted the guard
   in `UserManagementService.UpdateAsync` between the seed-admin guard (ends line 87) and
   the diff block (line 89) — exactly where design D8 requires, leaving the constructor
   untouched for PR3's later cache-invalidation hunk. Trigger: target is currently
   `Administrador` and `IsActive`, and the request would deactivate them or move them away
   from `Administrador`; throws `ForbiddenException` when the post-mutation count would be
   `< 2` (i.e. pre-mutation `n < 3`). Exact message:
   "No se puede dejar el sistema con menos de dos administradores activos."
2. **In-flight Capturista pre-check (Phase 3, design D6)**: created
   `ActiveRaceSummaryDto(Id, Nombre, FechaCarrera)` (no `JoinCode` — narrower than
   `RaceDto` by design). Added `IRaceRepository.GetActiveForUserAsync(userId, ct)`,
   implemented as the union query
   `r.Estado == EnCurso && (r.AdminId == userId || r.Judges.Any(j => j.UserId == userId))`,
   projected directly to the DTO (same precedent as
   `IResultRepository.GetPlacingCountsAsync` returning an Application-layer projection
   type from the repository). Delegated from `RaceService.GetActiveForUserAsync`. Added
   `GET /api/users/{id}/active-races` to `UsersController`, injecting `IRaceService` as a
   third constructor parameter — `UserManagementService`'s constructor was not touched,
   keeping this slice separable from PR3 per design D8.
3. **Frontend pre-check + confirm dialog (Phase 4)**: added `getUserActiveRaces(id)` to
   `endpoints.ts` and `ActiveRaceSummary` to `types.ts`. `handleToggleActive` in
   `UsersPage.tsx` now calls the pre-check only when deactivating (`target.isActive`); if
   it returns races, it opens an inline confirm `Modal` (from `@nicarunner/ui`, same
   component `RestartRaceDialog.tsx` uses) naming every affected race, with "Cancelar" and
   "Desactivar de todos modos" actions; if it returns none, the `PATCH` fires directly as
   before. The actual `PATCH` call was extracted into `applyToggle` so both the direct path
   and the dialog-confirm path share it (`handleConfirmDeactivation` calls it after
   closing the dialog). Per design's File Changes table, the dialog was kept inline in
   `UsersPage.tsx` rather than extracted to a new component file — no new frontend
   component file is listed there for PR2.

## TDD Cycle Evidence

| Task | Test File | Layer | Safety Net | RED | GREEN | TRIANGULATE | REFACTOR |
|------|-----------|-------|------------|-----|-------|-------------|----------|
| 2.1 | `UserManagementServiceTests.cs` | Unit (Moq) | ✅ 34/34 pre-batch | ✅ Written, confirmed failing (compile RED: `CountActiveByRoleAsync` missing, then behavior RED: no exception thrown) | ✅ Passed | ✅ (2.2/2.3 vary Role/count) | ➖ None needed |
| 2.2 | `UserManagementServiceTests.cs` | Unit (Moq) | (same batch) | ✅ Written, confirmed failing | ✅ Passed | — | ➖ None needed |
| 2.3 | `UserManagementServiceTests.cs` | Unit (Moq) | (same batch) | ➖ Passed trivially pre-guard (see honest note below) | ✅ Passed | ✅ n=3 vs n=2 | ➖ None needed |
| 2.4 | `UserRepositoryTests.cs` (new) | Repository (Sqlite real) | N/A (new file) | ✅ Written, confirmed failing (compile RED: method missing) | ✅ Passed | ✅ 2 active + 1 inactive admin + 1 active non-admin in one seed | ➖ None needed |
| 2.5 | `UserManagementServiceTests.cs` (2 tests) | Unit (Moq) | (same batch) | ➖ Passed trivially pre-guard (ordering safety-net, see note) | ✅ Passed | ✅ self-guard + seed-guard cases | ➖ None needed |
| 2.6–2.7 | — | — | — | (interface + impl, no test of their own — covered by 2.1–2.4) | ✅ compiles + 2.1–2.4 green | — | — |
| 2.8 | `UserManagementServiceTests.cs` | Unit (Moq) | ✅ full suite 434/434 after | ✅ (drives 2.1/2.2) | ✅ | ✅ | ➖ None needed |
| 2.9 | full suite | — | — | — | ✅ 434/434 | — | — |
| 3.1 | `RaceRepositoryActiveForUserTests.cs` (new) | Repository (Sqlite real) | N/A (new file) | ✅ Written, confirmed failing (compile RED: `GetActiveForUserAsync`/`ActiveRaceSummaryDto` missing) | ✅ Passed | ✅ admin-owned race (no RaceJudge row) vs judge-owned race, both EnCurso, in the same seed; Planeada/Terminada siblings prove exclusion | ➖ None needed |
| 3.2–3.5 | — | — | — | (DTO + interface + impl + controller, no test of their own — covered by 3.1) | ✅ compiles + 3.1 green | — | — |
| 3.6 | full suite | — | — | — | ✅ 434/434 | — | — |
| 4.1 | `users-page.status-toggle.test.tsx` | Integration (RTL) | ✅ 4/4 pre-batch | ✅ Written, confirmed failing (no dialog existed) | ✅ Passed | ✅ (4.3 covers 2-race case) | ✅ Clean |
| 4.2 | `users-page.status-toggle.test.tsx` | Integration (RTL) | (same batch) | ➖ Passed trivially pre-wiring (see honest note below) | ✅ Passed (now genuinely exercises the wired pre-check) | — | ➖ None needed |
| 4.3 | `users-page.status-toggle.test.tsx` | Integration (RTL) | (same batch) | ✅ Written, confirmed failing | ✅ Passed | ✅ 1-race vs 2-race dialogs | ➖ None needed |
| 4.4 | `users-page.status-toggle.test.tsx` | Integration (RTL) | (same batch) | ✅ Written, confirmed failing | ✅ Passed | ✅ (4.5 covers cancel path) | ➖ None needed |
| 4.5 | `users-page.status-toggle.test.tsx` | Integration (RTL) | (same batch) | ✅ Written, confirmed failing | ✅ Passed | ✅ confirm vs cancel | ✅ Extracted `clickDeactivate` helper, shared race fixtures |
| 4.6–4.8 | — | — | — | (endpoint + type + wiring, no test of their own — covered by 4.1–4.5) | ✅ compiles + 4.1–4.5 green | — | — |
| 4.9 | full suites | — | — | — | ✅ backend 434/434, frontend 61/61, both builds clean | — | — |

**Honest note on task 2.3 and both 2.5 tests**: these three assertions describe behavior
that already held true *before* the new guard existed (three admins was always allowed;
the self- and seed-admin guards already won and already short-circuited before reaching
any admin-count logic, since that logic didn't exist yet). Running them before adding the
guard confirmed they passed trivially — not a genuine RED. They function as
approval/ordering safety-net tests: 2.3 proves the new guard doesn't misfire above the
floor, and both 2.5 tests prove (via `_users.Verify(..., Times.Never)` on
`CountActiveByRoleAsync`) that the pre-existing guards still short-circuit before the new
one runs, which is exactly what task 2.5 asks to confirm. This mirrors the same honest
disclosure pattern used in the PR1 batch above for tasks 1.1–1.3.

**Honest note on task 4.2**: "pre-check returns no active races → PATCH fires directly
with no dialog" was already the *only* possible behavior before the pre-check existed
(there was no dialog to suppress), so the test passed immediately when first written —
not a genuine RED at that point. It was kept because after wiring `getUserActiveRaces`
into `handleToggleActive` (tasks 4.6–4.8), this same test became the one proving the
empty-array path still resolves to a direct `PATCH` without regressing into an
always-shown dialog; the full suite run after wiring confirms it still passes for the
right (now real) reason.

**RED baseline runs**:
- 2.1/2.2/2.4 (compile RED): `dotnet build tests/NicaRunner.Tests/NicaRunner.Tests.csproj -c Release`
  → `error CS1061: ... does not contain a definition for 'CountActiveByRoleAsync'` (6 errors, one
  per new call site) before `IUserRepository`/`UserRepository` were touched.
- 2.1/2.2 (behavior RED, after adding the interface/impl but before the guard):
  `dotnet test ... --filter "FullyQualifiedName~UserManagementServiceTests|FullyQualifiedName~UserRepositoryTests"`
  → `Failed: 2, Passed: 32` — both floor tests failed with `Assert.Throws() Failure: No
  exception was thrown`, confirming the guard did not exist yet, for the right reason.
- 3.1 (compile RED): same build command →
  `error CS1061: 'RaceRepository' does not contain a definition for 'GetActiveForUserAsync'`
  (2 errors) before `IRaceRepository`/`RaceRepository`/`ActiveRaceSummaryDto` existed.
- 4.1/4.3/4.4/4.5 (behavior RED): `npx vitest run src/__tests__/users-page.status-toggle.test.tsx`
  → `Test Files 1 failed (1)`, `Tests 4 failed | 5 passed (9)` — the 4 new dialog-behavior
  tests failed (no dialog existed to find/click), while 4.2 and the 4 pre-existing PR1
  tests passed (see honest note above for 4.2).

**GREEN runs**: focused `UserManagementServiceTests`+`UserRepositoryTests` filter → `34/34`
after the guard; `RaceRepositoryActiveForUserTests` filter → `1/1` after the repository/
service/controller wiring; `npx vitest run src/__tests__/users-page.status-toggle.test.tsx`
→ `9/9` after wiring the dialog.

### Test Summary
- **Total tests written this batch**: 12 (6 backend unit/repository in Phase 2, 1 backend
  repository in Phase 3, 5 frontend integration in Phase 4)
- **Total tests passing**: backend 434/434 full suite (426 baseline + 8 new); frontend
  61/61 full suite (56 baseline + 5 new — task 4.2's test already existed as a passing
  case before wiring, see honest note, so it does not add to the file's test count but its
  assertion coverage changed meaning)
- **Layers used**: Unit/Moq (5: 2.1, 2.2, 2.3, 2.5×2), Repository/Sqlite-real (2: 2.4,
  3.1), Integration/RTL (5: 4.1, 4.3, 4.4, 4.5, plus 4.2 re-verified)
- **Approval/safety-net tests**: 3 (2.3, both 2.5 cases) — proved guard ordering and
  above-floor behavior without ever failing first, per the honest note above
- **Pure functions created**: 0 net-new pure functions; `CountActiveByRoleAsync` and
  `GetActiveForUserAsync` are thin, intent-stating repository query methods (no business
  logic to extract), matching the existing `GetPlacingCountsAsync`/`GetCloseBlockerCountsAsync`
  precedent in `ResultRepository`

## Work Unit Evidence (Hard Gate, all modes)

| Evidence | Value |
|---|---|
| Focused test command and exact result | Backend: `dotnet test ... --filter "FullyQualifiedName~UserManagementServiceTests\|FullyQualifiedName~UserRepositoryTests"` → `34/34`; `--filter "FullyQualifiedName~RaceRepositoryActiveForUserTests"` → `1/1`. Frontend: `npx vitest run src/__tests__/users-page.status-toggle.test.tsx` → `9/9` |
| Runtime harness command/scenario and exact result | `dotnet build NicaRunner.sln --configuration Release --no-incremental` → `Build succeeded`, `3 Warning(s)` (identical pre-existing warnings in `RaceDashboardHub.cs`/`UtcDateTimeConverter.cs`, unrelated to this change), `0 Error(s)`. `dotnet test tests/NicaRunner.Tests/NicaRunner.Tests.csproj -c Release` → `Passed: 434, Failed: 0`. `cd frontend && npm test` → `Test Files 17 passed (17)`, `Tests 61 passed (61)`. `cd frontend && npm run build` → `tsup` (packages/ui) clean, `tsc`/`vite build` succeeded (`✓ built in 2.40s`) |
| Rollback boundary | Revert the 13 modified files plus the 3 new files listed above. `UserManagementService.UpdateAsync` returns to its pre-guard form (guard is a pure in-memory + one COUNT(*) check with no persisted state); `GET /api/users/{id}/active-races` is a new, read-only, additive endpoint with no consumers outside this PR's own dialog; the frontend confirm dialog is client-side only. Matches the design's stated PR2 rollback ("Revert. Guard is in-memory validation with no persisted state; the endpoint is read-only and additive") |

## Deviations from Design

None — implementation matches design D5, D6, and D8 exactly: guard placement (between
line 87 and line 89, `UserManagementService` constructor untouched), exact exception
message, `n < 3` threshold, the mandatory admin-OR-judge union query, `ActiveRaceSummaryDto`
narrower than `RaceDto` (no `JoinCode`), `IRaceService` injected into `UsersController`
rather than `UserManagementService`, and the confirm dialog kept inline in `UsersPage.tsx`
(no new frontend component file, matching design's File Changes table for PR2).

One scope note: the assigned tasks (2.1–2.9, 3.1–3.6, 4.1–4.9) do not include a
`UsersController`-level test for the new `GET /active-races` route — no
`UsersControllerTests.cs` file exists in the repo and none was requested by the task
list, so none was added. The endpoint is exercised indirectly by the frontend integration
tests via the mocked `getUserActiveRaces` client function, and directly by the repository/
service test proving the underlying query is correct.

## Issues Found

- The forecast in this file's Review Workload Forecast table estimated PR2 at ~270 lines;
  the actual authored total is 390. The gap comes from the two SQLite-real repository
  tests (2.4's floor-count exclusion and 3.1's load-bearing union query) needed to make
  those specific spec scenarios genuine rather than mock-asserted, plus the ordering
  safety-net tests in 2.5. Comments were trimmed and frontend test fixtures deduplicated
  to bring the total back under the 400-line cap (initial draft was 462 lines before
  trimming) — still worth flagging for future PR-size estimates involving Sqlite-backed
  query-shape tests.
- Engram is not available in this environment (no `engram` binary, no `mem_*` tools) —
  same deviation as the PR1 batch, explicitly superseded by the task prompt. This is noted
  again here rather than assumed carried over silently.

## Remaining Tasks

Phase 0 (0.1–0.3) and Phase 5 (session revocation, PR3) and Phase 6 (final cross-PR
checks) are NOT started — out of scope for this batch by explicit instruction (PR3 is a
separate slice).

## Status

Phases 1 (10/10), 2 (9/9), 3 (6/6), and 4 (9/9) tasks complete — 34/34 tasks across those
four phases. Phase 0 and Phases 5–6 remain (13 tasks: 0.1–0.3, 5.1–5.13, 6.1–6.3). Ready
for `sdd-verify` on the PR2 scope (Phases 2–4), or for the next apply batch (Phase 0 /
Phase 5) — not started here.

## Phase 0 — Pre-PR3 blocking verification (COMPLETE)

Both checks were run before starting PR3, as Phase 0 requires. One passes, one does not.

### 0.2 Single-instance deployment — CONFIRMED, design assumption holds

`render.yaml` declares one `type: web` service on `plan: free`, and no `ConnectionStrings__Redis`
is set there or in any `appsettings*.json`. `Program.cs:107-117` only registers the SignalR Redis
backplane when that connection string is present, so it is inactive. D2's per-process
`IMemoryCache` therefore gives effectively immediate revocation today.

Incidental finding in `render.yaml`: the `Jwt__Key` entry carries a hand-written warning against
`generateValue: true`, because regenerating it "invalida todos los JWT en uso, tirando las
sesiones de capturistas en medio de una carrera". The repository already treats cutting a
Capturista's session mid-race as dangerous. PR3 does that deliberately, which is what makes 0.1
below decisive rather than academic.

### 0.1 Offline capture safety — FAILED. PR3 is NOT safe to ship as designed.

Verified directly in `/home/user/NicaRunner` (read-only).

Logout does **not** wipe the database. There is no `clearAllTables()` or `deleteDatabase()`
anywhere in the app, and `AuthRepository.signOut` clears only the `competitions` cache. Room and
both outboxes (`pending_arrivals`, `pending_category_starts`) survive logout and re-login. That
was the question Phase 0 asked, and the answer is reassuring — but the loss happens elsewhere.

The real path is an HTTP-status heuristic in the capture queue,
`app/src/main/java/com/nicarunner/app/data/CaptureRepository.kt:290-294`:

```kotlin
// 4xx = el servidor entendió y rechazó (carrera cerrada, tiempo fuera de
// rango). Reintentarlo para siempre deja la cola envenenada [...]
if (response.code() in 400..499) {
    dequeue(idempotencyKey)
    return Result.failure(ApiException(message))
}
```

`dequeue` is a hard `DELETE FROM pending_arrivals WHERE idempotencyKey = :key`, not a state flag.
The identical rule governs the race-start queue at `RaceCategoryRepository.kt:207-210`.

`401` falls inside `400..499`. The rule was written when 4xx could only mean a permanent domain
rejection — a closed race, a time out of range. Once the API returns 401 for a deactivated
account, that same branch destroys a recoverable capture.

Sequence on a single tap of "Registrar llegada" by a Capturista deactivated mid-race:

1. The arrival is written to `pending_arrivals`, then POSTed.
2. The API answers 401.
3. `TokenAuthenticator` (`app/.../data/api/TokenAuthenticator.kt:69-71`) attempts one refresh;
   `RefreshTokenService.cs:40` already refuses it because `IsActive` is false, so it calls
   `tokenStore.clear()` and returns `null`.
4. Returning `null` hands the original 401 back to the caller as an ordinary response, so
   `addVia` deletes the row.
5. `isSignedInFlow` flips and `MainActivity.kt:147` swaps the whole composition to the login
   screen, most likely cancelling the scope before the explanatory snackbar can render.

The runner who just crossed the line has no recorded time on the phone or on the server.

Two aggravating details:
- `SalidaViewModel.start()`'s failure path reports "Salida registrada en el dispositivo —
  pendiente de sincronizar" unconditionally, which is false after a 4xx: the row is already gone.
- `PerfilScreen.kt:105-110` warns before a *manual* logout that pending items will be lost. A
  forced logout gives the judge no such dialog.

Blast radius is one row per failing request, not the whole backlog: `drainPending` breaks on
first failure.

**This is a pre-existing latent defect, not one PR3 introduces.** It can already fire today when
a deactivated user's 60-minute access token finally expires mid-capture. What PR3 changes is the
probability: today the window is a rare coincidence, afterwards it is the normal path, reached
within seconds of deactivation.

**Smallest fix that makes PR3 safe:** exclude 401 (and arguably 403) from the dequeue branch in
`CaptureRepository.addVia` and `RaceCategoryRepository.startCategoriesVia`, treating auth
failures like 5xx — keep the row for retry. Two conditionals, no schema change. The queues
already survive logout and re-login, so recovery follows once the account is reactivated and the
judge reopens that race's screen. Note recovery is not automatic on login: `drainPending` runs
only when the Capture or Salida screen for that race is opened.

**Status: RESOLVED. The blocker was fixed and PR3 is cleared to proceed — see the resolution
note at the end of this file.**

---

# PR3 batch attempt — Phase 5 (tasks 5.1–5.13) — NOT STARTED

## Scope

A follow-up apply session was invoked to implement Phase 5 (tasks 5.1–5.13, session
revocation) on branch `claude/backoffice-user-toggle-jy1gqu-pr3-session-revocation`, stacked
on PR2. Before writing any test or code, this session re-read the required inputs (tasks.md,
design.md, spec.md, apply-progress.md) per the `sdd-apply` skill and found the Phase 0.1
finding above unresolved: `apply-progress.md` explicitly records "PR3 is blocked pending a
user decision" due to a documented data-loss defect in `/home/user/NicaRunner`
(`CaptureRepository.kt`/`RaceCategoryRepository.kt` dequeue pending captures on any 4xx,
including 401 — Phase 5 turns "rare coincidence" into "normal path within seconds").

The task instructions for this batch made no mention of this finding and gave no evidence the
user had explicitly decided to accept the risk or that a mitigating mobile-side fix was
already scheduled. Per the project's STOP discipline (the same standard applied to the
401-vs-403 load-bearing check in task 5.1), this session treated it as a blocking, user-only
decision and **stopped before implementing any of tasks 5.1–5.13**. No source files were
created or modified in this attempt; only this progress note was added.

**Status: still blocked pending a user decision. Phase 5 was not started.**

---

## Phase 0.1 blocker — RESOLVED

The stop above was correct at the time it was written, and the follow-up apply session was
right to refuse to start Phase 5 while this file still read "blocked". The blocker has since
been cleared; this note records how, so no later reader repeats the stop.

The user chose "fix mobile first" when the finding was put to them. The minimum fix identified
in 0.1 was implemented in `/home/user/NicaRunner` on branch
`claude/backoffice-user-toggle-jy1gqu` and is open as
[NicaRunner#61](https://github.com/hilbertmendez-creator/NicaRunner/pull/61):

- `CaptureRepository.addVia` and `RaceCategoryRepository.startCategoriesVia` now let 401 and 403
  fall through **without** dequeuing, exactly as a 5xx already did. Every other 4xx keeps the
  existing poison-pill behaviour, and the tests that pin that behaviour were left untouched.
- Four new tests cover 401 and 403 across both queues. They were a genuine RED, failing on
  `assertFalse(dequeued)` before the fix.
- `./gradlew :app:testDebugUnitTest` → 133 tests, 0 failures (129 baseline + 4).

An arrival that meets a 401 now stays queued and uploads once the account is reactivated and the
judge reopens that race's capture screen, instead of being deleted.

**Remaining constraint, and it is a deployment constraint rather than a code one:** PR3 must not
reach production before NicaRunner#61 is merged and shipped. Landing session revocation against
an app that still discards a 401-rejected capture would reintroduce the exact data loss 0.1
found. The `AccountStatus:EnforcePerRequest` flag is the safety valve if the two ever land out
of order — ship PR3 with it off and turn it on once the app is updated.

Phase 5 is cleared to start.
