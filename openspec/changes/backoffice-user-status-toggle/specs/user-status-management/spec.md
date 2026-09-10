# User Status Management Specification

## Purpose

Admin-driven activation/deactivation of backoffice user accounts: the guardrails that
keep the operation safe, and the backoffice presentation of current status. `IsActive`
(`User.cs:27`) remains the single status vocabulary; this spec does not introduce a
parallel field or enum.

## Existing Behavior (context — not a deliverable of this change)

- Admin-only `PATCH /api/users/{id}` toggles `IsActive`
  (`UsersController.cs:19,35`; `UserManagementService.UpdateAsync:68-134`).
- Self-deactivation and self-role-change are rejected with `ForbiddenException`
  (`UpdateAsync:73-79`).
- Protected seed admins (`ProtectedSeedUsers.cs`) can never be deactivated or re-roled
  (`UpdateAsync:81-87`).
- Every `IsActive`/`Role` change is recorded via `auditService.TrackChanges`
  (`UpdateAsync:130`).
- The Estado column and the Activar/Desactivar toggle already exist in `UsersPage.tsx`
  (98-99, 51-54, 126-133), backed by `updateUser(id, { isActive })`.

## Requirements

### Requirement: Status badge presentation

The Estado column SHOULD render user status as a visual badge consistent with the
app's existing status presentation (the `RaceStatus` badge pattern), instead of plain
text.

#### Scenario: Active user

- GIVEN a user row with `isActive: true`
- WHEN the Estado column renders
- THEN it shows a badge labeled "Activo"

#### Scenario: Inactive user

- GIVEN a user row with `isActive: false`
- WHEN the Estado column renders
- THEN it shows a badge labeled "Inactivo"

#### Scenario: RacesPage unaffected

- GIVEN the existing `RaceStatus` badge consumer at `RacesPage.tsx:77`
- WHEN the badge primitive is generalized to support a user-status tone
- THEN `RacesPage` rendering and its existing props contract are unchanged

### Requirement: Test coverage for status column and toggle

The frontend test suite MUST assert the Estado label per status, the toggle button's
call payload, and the self-row disabled guard.

#### Scenario: Estado label assertion

- GIVEN a rendered `UsersPage` with active and inactive users
- WHEN each row is inspected
- THEN the Estado badge label matches `isActive` for that row

#### Scenario: Toggle payload assertion

- GIVEN an admin clicks Desactivar/Activar on another user's row
- WHEN the click handler resolves
- THEN `updateUser` is called with `{ isActive: !previousValue }`

#### Scenario: Self-row guard assertion

- GIVEN the row belongs to the signed-in admin
- WHEN the row renders
- THEN the toggle button is `disabled`

### Requirement: Toggle failure feedback

`handleToggleActive` MUST catch a rejected `updateUser` call and surface an error
message to the user; it MUST NOT leave an unhandled promise rejection.

#### Scenario: Rejected toggle

- GIVEN `updateUser` rejects (e.g. a guard violation)
- WHEN `handleToggleActive` runs
- THEN an error toast is shown
- AND the row is not optimistically updated

### Requirement: Minimum active administrators

The system MUST reject any `UpdateAsync` call that would leave fewer than two active
users with role `Administrador`, whether triggered by deactivating an `Administrador`
or by changing their `Role` away from `Administrador`.

#### Scenario: Deactivation blocked at the floor

- GIVEN exactly two active `Administrador` users
- WHEN an admin sets `IsActive: false` on one of them
- THEN the request is rejected with 403 and detail
  "No se puede dejar el sistema con menos de dos administradores activos."

#### Scenario: Role change blocked at the floor

- GIVEN exactly two active `Administrador` users
- WHEN an admin changes one of their `Role` away from `Administrador`
- THEN the request is rejected the same way as deactivation

#### Scenario: Above the floor, allowed

- GIVEN three or more active `Administrador` users
- WHEN one is deactivated or re-roled
- THEN the request succeeds (existing self/seed guards still apply first)

#### Scenario: Only active admins count

- GIVEN inactive `Administrador` rows also exist
- WHEN the floor is evaluated
- THEN only rows with `IsActive: true` and `Role: Administrador` count
  (`GetByRoleAsync`, `UserRepository.cs:50`)

### Requirement: In-flight Capturista deactivation warning

Deactivating a Capturista who is judge (`RaceJudge`) of a race with
`Estado == EnCurso` MUST NOT be blocked. The backoffice MUST warn which race(s) are
affected and require explicit confirmation before the deactivation is submitted.

#### Scenario: Pre-check finds one active race

- GIVEN a Capturista is `RaceJudge` on exactly one `EnCurso` race
- WHEN the admin clicks Desactivar
- THEN `GET /api/users/{id}/active-races` returns that race
- AND the UI shows a confirmation dialog naming it

#### Scenario: Pre-check finds no active race

- GIVEN a Capturista is not judge of any `EnCurso` race
- WHEN the admin clicks Desactivar
- THEN no dialog appears and the `PATCH` proceeds directly

#### Scenario: Pre-check finds multiple active races

- GIVEN a Capturista judges more than one `EnCurso` race
- WHEN the admin clicks Desactivar
- THEN the dialog names every affected race

#### Scenario: Admin confirms

- GIVEN the warning dialog is shown
- WHEN the admin confirms
- THEN the `PATCH` proceeds and deactivation succeeds (never blocked by this check)

#### Scenario: Admin cancels

- GIVEN the warning dialog is shown
- WHEN the admin cancels
- THEN no `PATCH` request is sent and the user remains active

## Not Guaranteed

- The pre-check and the `PATCH` are not atomic (TOCTOU): a race could start between
  the two calls. Accepted — the warning is advisory, not an invariant.
