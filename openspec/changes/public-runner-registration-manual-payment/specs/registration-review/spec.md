# Registration Review Specification

## Purpose

Give authenticated admins the ability to configure per-race-category capacity and price, open and revoke public `RegistrationLink`s, list and review submitted `Registration`s, and confirm or reject them — individually or in bulk via Excel. Confirming a `Registration` is the sole trigger that creates the authoritative `Runner`, enforces race capacity, and validates the admin-supplied `Dorsal`; the system never generates a `Dorsal` on its own.

## Requirements

### Requirement: RaceCategory Capacity and Price Configuration

An authenticated admin MUST be able to configure `Capacidad` and `Precio` on a `RaceCategory` before a `RegistrationLink` for that race can be opened.

#### Scenario: Configuration required before opening link
- GIVEN a `RaceCategory` without `Capacidad` or `Precio` configured
- WHEN an admin attempts to open a `RegistrationLink` for its race
- THEN the system MUST reject the operation until capacity and price are configured

### Requirement: Registration Link Administration

An authenticated admin MUST be able to create/open and revoke a `RegistrationLink` for a race. A revoked or expired link MUST reject subsequent anonymous access.

#### Scenario: Admin opens a link
- GIVEN a race with fully configured `RaceCategory` capacity and price
- WHEN an admin opens a `RegistrationLink`
- THEN the system MUST create an active, non-expired link usable for anonymous access

#### Scenario: Admin revokes a link
- GIVEN an active `RegistrationLink`
- WHEN an admin revokes it
- THEN the system MUST mark it revoked and reject further anonymous access through it

### Requirement: Registration Listing for Review

An authenticated admin MUST be able to list `Registration` records filtered by race and by state.

#### Scenario: List pending review items
- GIVEN registrations in states `Pendiente`, `ComprobanteSubido`, `Confirmada`, and `Rechazada` for a race
- WHEN an admin lists registrations filtered by state `ComprobanteSubido`
- THEN the system MUST return only registrations in that state for that race

### Requirement: Confirm Registration

An authenticated admin MUST be able to confirm a `Registration` that is in state `ComprobanteSubido` by supplying a `Dorsal` value as part of the confirm action. The system MUST NOT generate, probe for, or otherwise auto-assign a `Dorsal` — the admin-supplied value is the only source. Confirmation MUST atomically verify remaining capacity for the registration's `RaceCategory`, validate the supplied `Dorsal` (see "Admin-Supplied Dorsal Validation"), create exactly one `Runner` with that `Dorsal`, set `RunnerId` on the `Registration`, transition it to `Confirmada`, and write an `AuditLog` entry.

#### Scenario: Successful confirmation
- GIVEN a `Registration` in state `ComprobanteSubido` for a `RaceCategory` with remaining capacity, and an admin-supplied `Dorsal` that is valid, unique, and not reserved
- WHEN an admin confirms the registration with that `Dorsal`
- THEN the system MUST create one `Runner` with the supplied `Dorsal`, set `RunnerId`, transition to `Confirmada`, and record an `AuditLog` entry

#### Scenario: Confirmation rejected without a supplied dorsal
- GIVEN a `Registration` in state `ComprobanteSubido`
- WHEN an admin attempts to confirm it without supplying a `Dorsal`
- THEN the system MUST reject the confirmation and MUST NOT create a `Runner`

#### Scenario: Confirmation rejected at capacity
- GIVEN a `RaceCategory` already at its configured `Capacidad` of confirmed registrations
- WHEN an admin confirms another `Registration` in state `ComprobanteSubido` for that category
- THEN the system MUST reject the confirmation and MUST NOT create a `Runner`

#### Scenario: Concurrent confirmations at the capacity boundary
- GIVEN a `RaceCategory` with exactly one remaining capacity slot
- WHEN two confirmations for that category are submitted concurrently
- THEN the system MUST confirm exactly one of them and reject the other, never exceeding `Capacidad`

#### Scenario: Confirmation rejected outside eligible state
- GIVEN a `Registration` in state `Pendiente`, `Confirmada`, or `Rechazada`
- WHEN an admin attempts to confirm it
- THEN the system MUST reject the confirmation

### Requirement: Reject Registration

An authenticated admin MUST be able to reject a `Registration` in state `Pendiente` or `ComprobanteSubido`, transitioning it to `Rechazada` with a recorded reason and an `AuditLog` entry, without creating a `Runner`.

#### Scenario: Successful rejection
- GIVEN a `Registration` in state `ComprobanteSubido`
- WHEN an admin rejects it with a reason
- THEN the system MUST transition it to `Rechazada`, store the reason, and record an `AuditLog` entry

#### Scenario: Rejection does not create a Runner
- GIVEN a `Registration` in state `Pendiente`
- WHEN an admin rejects it
- THEN the system MUST NOT create a `Runner` and MUST leave `RunnerId` null

### Requirement: Admin-Supplied Dorsal Validation

The `Dorsal` used to create a `Runner` at confirm time is always admin-supplied, individually or via bulk-Excel confirm — the system's role is validation, not generation. The system MUST reject a supplied `Dorsal` that is empty/blank, already held by another `Runner` in the same race, or currently held in `ReservedDorsal` for that race. Uniqueness and reserved-status comparisons MUST be numeric on the digit portion, ignoring leading zeros (e.g. `21K7` and `21K007` are the same `Dorsal`), while the caller-supplied digit padding MUST be preserved in the stored/displayed value.

#### Scenario: Unique dorsal per race
- GIVEN multiple confirmations occurring for the same race, each with a distinct admin-supplied `Dorsal`
- WHEN each confirmation creates a `Runner`
- THEN the system MUST persist each `Runner` with its distinct `Dorsal` and MUST NOT allow two `Runner`s in the same race to hold the same normalized `Dorsal`

#### Scenario: Confirmation rejected for reserved dorsal
- GIVEN a `Dorsal` value marked reserved for the race, currently held by no `Runner`
- WHEN an admin confirms a `Registration` supplying that `Dorsal`
- THEN the system MUST reject the confirmation and MUST NOT create a `Runner`

#### Scenario: Confirmation rejected for dorsal already taken
- GIVEN a `Dorsal` value already held by an existing `Runner` in the race
- WHEN an admin confirms a `Registration` supplying that `Dorsal`
- THEN the system MUST reject the confirmation and MUST NOT create a `Runner`

#### Scenario: Numeric equivalence ignores leading zeros
- GIVEN a `Runner` holding `Dorsal` "21K007" in a race
- WHEN an admin confirms a different `Registration` supplying `Dorsal` "21K7" for that race
- THEN the system MUST reject the confirmation, because "21K7" is numerically equivalent to "21K007" and therefore already taken

> **Open question (not specified here)**: whether the admin-supplied `Dorsal` must additionally conform to a specific shape (e.g. a category-code prefix plus a digit ceiling) is unresolved after the auto-generation pivot. This spec intentionally requires only non-empty + uniqueness + not-reserved; no shape/format constraint is asserted pending an explicit decision.

### Requirement: Bulk Confirm via Excel Template

An authenticated admin MUST be able to confirm multiple `ComprobanteSubido` registrations for a race in one pass using an Excel template, mirroring the existing `RunnerService.ImportFromExcelAsync` pattern. The admin MUST be able to download a template scoped to that race's pending registrations, pre-populated with reference `RaceCategory` data for lookup only (not editable); the admin fills in `Dorsal` per row and re-uploads. Each row MUST be validated and processed independently, with the same partial-success contract as the existing import (a total/processed-count/per-row-errors result), so one invalid row MUST NOT block confirmation of the other valid rows in the same upload. Each row's confirmation MUST apply the same capacity, uniqueness, not-reserved, and not-empty checks as an individual confirm.

#### Scenario: Template download scoped to pending registrations
- GIVEN a race with registrations in multiple states
- WHEN an admin downloads the bulk-confirm template for that race
- THEN the system MUST include only registrations in state `ComprobanteSubido`, pre-populated with read-only reference `RaceCategory` data for lookup

#### Scenario: Partial success on mixed valid and invalid rows
- GIVEN an uploaded bulk-confirm file with some rows carrying valid, unique, non-reserved dorsals and other rows carrying blank, duplicate, or reserved dorsals
- WHEN an admin uploads the file
- THEN the system MUST confirm every valid row and MUST report a per-row error for every invalid row, without rejecting the entire batch

#### Scenario: Capacity exhausted mid-batch
- GIVEN a `RaceCategory` with remaining capacity smaller than the number of valid rows targeting it in the uploaded batch
- WHEN an admin uploads the file
- THEN the system MUST confirm rows only up to the remaining capacity for that category and MUST report a capacity-exhausted error for the remaining rows targeting it, without rejecting the entire batch

### Requirement: Manual Dorsal Assignment Guards Reserved Dorsals

When an admin manually creates a `Runner` via `RunnerService.CreateAsync` with an explicit `Dorsal`, or updates a `Runner`'s `Dorsal` via `RunnerService.UpdateAsync`, the system MUST reject the operation if the submitted `Dorsal` is currently held in `ReservedDorsal` for that race, using the same conflict-error contract as an already-taken `Dorsal`. On update, this guard MUST apply only when the submitted `Dorsal` differs (numerically, ignoring leading zeros) from the `Runner`'s current `Dorsal`; updating unrelated fields on a `Runner` who already legitimately holds a `Dorsal` that has since become reserved MUST NOT fail.

#### Scenario: Manual creation rejected for reserved dorsal
- GIVEN a `Dorsal` value marked reserved for the race
- WHEN an admin manually creates a `Runner` with that `Dorsal` via `RunnerService.CreateAsync`
- THEN the system MUST reject the creation with a clear conflict error and MUST NOT create the `Runner`

#### Scenario: Manual update rejected when changing to a reserved dorsal
- GIVEN a `Runner` whose current `Dorsal` is not reserved, and a different `Dorsal` value marked reserved for the race
- WHEN an admin updates the `Runner`'s `Dorsal` to the reserved value via `RunnerService.UpdateAsync`
- THEN the system MUST reject the update with a clear conflict error and MUST NOT change the `Runner`'s `Dorsal`

#### Scenario: Update to unrelated fields unaffected by a now-reserved current dorsal
- GIVEN a `Runner` who already holds a `Dorsal` that has since been marked reserved
- WHEN an admin updates the `Runner` via `RunnerService.UpdateAsync` without changing its `Dorsal`
- THEN the system MUST NOT reject the update on account of the reserved-dorsal guard

### Requirement: Reserved Dorsal Release

An authenticated admin MUST be able to delete/release a `ReservedDorsal` entry for a race via `ReservedDorsalsController`, making that `Dorsal` value available again for confirm-time and manual assignment.

#### Scenario: Admin releases a reserved dorsal
- GIVEN a `Dorsal` value marked reserved for a race
- WHEN an admin deletes the `ReservedDorsal` entry
- THEN the system MUST make that `Dorsal` value available for future confirm-time and manual assignment

### Requirement: Audit Logging of Review Actions

Every confirm and reject action MUST write an `AuditLog` entry capturing the acting admin, the action, the affected `Registration`, and a timestamp.

#### Scenario: Audit entry on confirm
- GIVEN an admin confirms a `Registration`
- WHEN the confirmation completes
- THEN the system MUST record an `AuditLog` entry identifying the admin, the action, and the `Registration`

### Requirement: Notification on Review Outcome

The system SHOULD notify the registrant of the confirm or reject outcome using a notification service without retry or audit guarantees for this slice.

#### Scenario: Notification attempted on confirm
- GIVEN a `Registration` is confirmed
- WHEN the confirmation completes
- THEN the system SHOULD attempt to send a notification to the registrant, without blocking or retrying confirmation on notification failure

## Key Learnings

1. Confirmation is the single gate for both capacity enforcement and `Runner` creation; submission itself is unbounded per the proposal.
2. Capacity concurrency safety must hold under simultaneous confirmations at the exact capacity boundary, not just under sequential access.
3. `RegistrationLink` administration (open/revoke) sits under admin review, while anonymous validation of an already-issued link sits under public registration.
4. Notifications for this slice intentionally drop retry/audit guarantees because `NotificationLog` foreign keys are non-nullable and no `Runner` exists until confirmation.
5. Dorsal auto-generation was fully rejected by the user; the system now only validates an admin-supplied `Dorsal` (non-empty, unique, not reserved), never generates or probes for one.
6. The `ReservedDorsal` guard applies to both confirm-time (individual and bulk) and the pre-existing manual `RunnerService.CreateAsync`/`UpdateAsync` path, with an `excludeRunnerId`-style exemption on update so unrelated-field edits never fail on a runner's own now-reserved dorsal.
7. Dorsal comparisons for both uniqueness and reserved-status are numeric on the digit portion (ignoring leading zeros), while the caller-supplied padding is preserved in the stored/displayed value — a fixed-width or literal-string comparison assumption would be incorrect.
8. Bulk confirm reuses the existing `ImportFromExcelAsync` partial-success contract (per-row validation, total/processed/errors), scoped to a race's pending registrations instead of blank runner rows.
