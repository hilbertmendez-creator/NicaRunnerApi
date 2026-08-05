# Proposal: Public Runner Registration with Manual Payment

## Intent

Every runner is created by an authenticated admin today, so organizers retype entrants from WhatsApp/paper lists and reconcile bank transfers by hand. Nicaraguan races are paid by transfer, not card gateway. Runners should self-register via a public link, attach a transfer reference, and be confirmed or rejected by an admin — confirmation producing the authoritative `Runner`.

## Scope

### In Scope
- `Registration` entity + state machine: `Pendiente -> ComprobanteSubido -> Confirmada | Rechazada`, with `ReferenciaTransferencia`, reviewer fields, and nullable `RunnerId` filled only on confirm.
- Per-race registration window: capacity, price, deadline, and an explicit admin-opened "inscripciones abiertas" flag.
- Anonymous submission endpoints (`[AllowAnonymous]` + new `RateLimiting:PublicRegistration` policy) mirroring `PublicResultsController`.
- Admin review endpoints (list/confirm/reject) writing to `AuditLog`.
- On confirm: capacity check + `Runner` creation with an admin-supplied `Dorsal` (manual, not auto-generated — user's explicit preference; alphanumeric, reserved/uniqueness guards still apply).
- Bulk confirm via Excel: extends the existing `RunnerService.ImportFromExcelAsync` pattern to `Registration` rows — admin downloads a template pre-populated with reference catalog data (race categories) for lookup, fills in `Dorsal` per pending registration, re-uploads to confirm many at once in one pass.
- `Registration.FechaNacimiento` (date of birth) — required at submission; emergency-contact fields required when the runner's actual age at race date falls under the minor threshold.

### Out of Scope
- Any payment gateway, card processing, or automated bank reconciliation.
- Runner-facing self-service edit/cancel after submission (admin rejects instead).
- Waitlist, refunds, transfers between races.
- Auto-generated dorsals — explicitly rejected by the user in favor of manual assignment (individual or bulk-Excel).
- Frontend implementation (separate repo).
- Printable QR / bib artwork — deferred to a follow-up slice.

## Capabilities

### New Capabilities
- `public-registration`: anonymous submission, receipt upload, registration state machine.
- `registration-review`: admin confirm/reject, capacity enforcement, runner promotion.

### Modified Capabilities
- None (no `openspec/specs/` exists yet; this change bootstraps the spec tree).

## Key Decisions (confirmed by user)

| # | Decision | Chosen | Why / tradeoff |
|---|---|---|---|
| 1 | Capacity + price home | `RaceCategory` | `Category` is a global catalog; 5K and 10K sell at different prices/limits. Costs a per-race-category config step. |
| 2 | Concurrency at confirm | Atomic conditional `UPDATE ... WHERE ConfirmedCount < Capacidad` + rows-affected check | No transaction infra exists; Sqlite/Postgres isolation differs materially. Adds a denormalized counter to keep consistent. |
| 3 | Public link shape | New `RegistrationLink` entity, mirroring `PublicResultToken` | Revocable/expirable and inert until opened, unlike `JoinCode`. One more table. |
| 4 | Notifications | New lower-level service over existing `INotificationSender` | `NotificationLog` FKs to `RunnerId`/`ResultId` are non-nullable and no runner exists yet. Loses retry/audit until a later slice. |
| 5 | Minor threshold | Global options-pattern config (`RegistrationOptions:EdadMayoriaEdad`, default 18), resolved against `Registration.FechaNacimiento` at race date | User flipped the sub-agent's default (category-range approximation): exact DOB is required, not an inference from the chosen category. Adds a required date field to `Registration` submission that did not exist in the original sketch. |
| 6 | Dorsal assignment mechanism | Manual only (individual confirm form field, or bulk via Excel) — **no auto-generation** | Superseded decisions 2/3 rounds of `DorsalAssigner` design (probe-insert-retry auto-assign). User's explicit preference. `ReservedDorsal`, numeric-normalized uniqueness, and the reserved-blocks-manual-too guard all still apply to the manually-typed value. |

## Approach

Domain-first: entities in `Domain`, `RegistrationService` in `Application`, EF config + one migration in `Infrastructure`, three controllers in `Api`. Dorsal is always admin-supplied at confirm time — no auto-assignment algorithm — validated against uniqueness (normalized, ignoring leading zeros) and `ReservedDorsal` the same way the existing manual `RunnerService.CreateAsync`/`UpdateAsync` path already is. Bulk confirm reuses the `ImportFromExcelAsync`/`ClosedXML` parsing pattern already proven in this codebase, scoped to pending `Registration` rows instead of blank runner rows. Confirm is the only capacity gate; submission is unbounded.

## Affected Areas

| Area | Impact | Description |
|------|--------|-------------|
| `src/NicaRunner.Domain/Entities/` | New | `Registration`, `RegistrationLink`, `ReservedDorsal`; `RaceCategory` gains `Capacidad`/`Precio`/`ConfirmedCount`; `Runner`/`ReservedDorsal` gain `DorsalNormalizado` |
| `src/NicaRunner.Application/` | New/Modified | `RegistrationService` (individual + bulk-Excel confirm), `DorsalNormalizer`, `RunnerService.CreateAsync`/`UpdateAsync` gain a reserved-dorsal guard (checked only when the dorsal value actually changes, to avoid breaking unrelated edits), notification sender |
| `src/NicaRunner.Infrastructure/Migrations/` | New | One migration creating `Registrations`, `RegistrationLinks`, `ReservedDorsals`, plus `DorsalNormalizado` backfill + normalized unique index on `Runner` (pre-flight duplicate check required — see Risks) + `.Designer.cs` |
| `src/NicaRunner.Api/Controllers/` | New | `PublicRegistrationController`, `RegistrationsController` (individual + bulk-Excel confirm), `ReservedDorsalsController` (admin create/delete — delete is mandatory: a reservation blocking every write path needs a release valve) |
| `src/NicaRunner.Api/appsettings.json` | Modified | `RateLimiting:PublicRegistration`, `RegistrationOptions` |

Note: reserved-dorsal enforcement extends into the pre-existing manual runner-creation path (`RunnerService.CreateAsync`/`UpdateAsync`), which sits outside the two new capabilities below. That invariant is specified under `registration-review` (owner of the `ReservedDorsal` data), not as a separate capability, since it is one guard clause on an already-touched file rather than new admin-facing behavior.

## Risks

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| Overselling capacity under concurrent confirms | Med | Atomic conditional UPDATE; integration test with parallel confirms |
| Duplicate/typo'd dorsal on manual entry (individual or bulk) | Med | Normalized-uniqueness DB index + `ReservedDorsal` guard reject at write time, same as existing manual path |
| **Existing production data may already violate normalized uniqueness** (e.g. `101` and `0101` both present today) | Med-High | Migration ordered add-nullable → backfill → create-index so index creation is the detector, not a blind failure; pre-flight `GROUP BY` duplicate-detection query required before this ships, operator re-numbers any collision manually — **must be run and reviewed before this change deploys to production data** |
| Public endpoint abuse / spam registrations | Med | Per-IP rate limit + capacity only consumed at confirm |
| Denormalized `ConfirmedCount` drifting | Low | Single write path; reconciliation query in verify |
| Scope creep into payments/waitlist | Med | Explicit non-goals above |

## Rollback Plan

Additive and gated: close every race's registration flag and public endpoints reject. Full revert = down-migration dropping `Registrations`, `RegistrationLinks`, and the new `RaceCategory`/`Race` columns. No existing table changes semantics, so `Runner`/`Result` data is untouched.

## Dependencies

- Admin must configure per-race-category capacity and price before a link can be opened.
- Existing `INotificationSender` implementations (Email/WhatsApp) must be configured for review notifications.

## Success Criteria

- [ ] A runner completes registration end-to-end via a public link with no account.
- [ ] Confirming a registration creates exactly one `Runner` with a unique dorsal.
- [ ] Concurrent confirms at the capacity boundary never exceed capacity.
- [ ] Registration submissions are rejected when the race is closed, past deadline, or at capacity-at-confirm.
- [ ] Minor registrations without emergency contact are rejected at submission.
- [ ] Confirm/reject actions appear in `AuditLog`.
