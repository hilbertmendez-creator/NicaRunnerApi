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

## Remaining Tasks

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

## Status

10/10 Phase 1 tasks complete (1.1–1.10). Ready for verify (Phase 1 scope only) or for the
next apply batch (Phase 0 / Phase 2) — not started here.
