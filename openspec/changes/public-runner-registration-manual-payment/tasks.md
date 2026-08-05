# Tasks: Public Runner Registration with Manual Payment

File paths below are bare filenames; full paths match `design.md`'s File Changes table.

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | ~1800-2400 (entities/services/repos/controllers ~1000; migration+Designer+snapshot ~400; tests ~600) |
| 400-line budget risk | High |
| Chained PRs recommended | Yes |
| Suggested split | PR1 → PR2 → PR3 → PR4 |
| Delivery strategy | ask-on-risk |
| Chain strategy | pending |

Decision needed before apply: Yes
Chained PRs recommended: Yes
Chain strategy: pending
400-line budget risk: High

Session review budget is 800 lines (overrides the skill's default 400); the estimate still exceeds it. `ReservedDorsal` entity/repo/controller grew beyond the proposal's original Affected Areas during design — a scope-growth risk. Ask the user which chain strategy to use before `sdd-apply`.

### Suggested Work Units

| Unit | Goal | PR | Focused test | Harness | Rollback boundary |
|---|---|---|---|---|---|
| 1 | Domain + staged migration | PR1 | `dotnet test --filter DorsalNormalizer` | `dotnet ef database update` (Sqlite) | drop migration; entities unused until PR2 |
| 2 | RegistrationService + controllers + reserved-dorsal guard | PR2 | `dotnet test --filter RegistrationService` | `WebApplicationFactory` submit→confirm | revert controllers/service; PR1 schema stays inert |
| 3 | Bulk-Excel confirm | PR3 | `dotnet test --filter ExcelRegistrationParser` | manual xlsx round-trip via `WebApplicationFactory` | revert bulk endpoints only; single confirm unaffected |
| 4 | Notifications + rate limiting | PR4 | `dotnet test --filter RegistrationNotifier` | `WebApplicationFactory` 429 test | revert notifier call site |

## Phase 1: Domain & Migration (PR 1)
- [x] 1.1 Create `Registration.cs` (+`RegistrationStatus` enum, `FechaNacimiento`, reviewer/`RunnerId`, `PrecioAplicado`).
- [x] 1.2 Create `RegistrationLink.cs` mirroring `PublicResultToken`.
- [x] 1.3 Create `ReservedDorsal.cs` (`Dorsal`, `DorsalNormalizado`, `Motivo`, `CreatedBy`, unique `RaceId+DorsalNormalizado`).
- [x] 1.4 Modify `RaceCategory.cs`: add `Capacidad`, `Precio`, `ConfirmedCount`.
- [x] 1.5 Modify `Runner.cs`: add `DorsalNormalizado`.
- [x] 1.6 Modify `Race.cs`: add `InscripcionesAbiertas`, `FechaLimiteInscripcion`.
- [x] 1.7 Create `DorsalNormalizer.cs` (pure `Normalize`, D11 regex).
- [x] 1.8 Create `EdadCalculator.cs` (`AtRaceDate`), extracted from `RunnerService.CalculateEdad`.
- [x] 1.9 Create `IReservedDorsalRepository.cs` (`IsReservedAsync` + CRUD).
- [x] 1.10 Modify `NicaRunnerDbContext.cs`: new DbSets, unique `RegistrationLinks.Token`, `IX_Registrations_RaceId_Estado`, `Restrict` FKs, additive unique `IX_Runners_RaceId_DorsalNormalizado`.
- [x] 1.11 Write pre-flight `GROUP BY RaceId, normalized HAVING COUNT(*)>1` duplicate check; document manual re-numbering. **Must run before prod deploy.**
- [x] 1.12 Create staged migration + `.Designer.cs` + snapshot: add-nullable → backfill → create-index; append provider-guarded Postgres `ALTER TABLE` for `Precio`/`InscripcionesAbiertas`/new `DateTime` cols.
- [x] 1.13 Tests: `DorsalNormalizer` equivalence cases, `EdadCalculator.AtRaceDate`; integration: migrated `0101` next to existing `101` rejected by index (D11).

## Phase 2: Core Registration & Review Flow (PR 2)
- [x] 2.1 Create `RegistrationRepository.cs` + `RegistrationLinkRepository.cs`: `TryClaimAsync`, `TryReserveSlotAsync`, `ReleaseSlotAsync` via `ExecuteUpdateAsync`.
- [x] 2.2 Create `ReservedDorsalRepository.cs` implementing `IReservedDorsalRepository`.
- [x] 2.3 Create `RegistrationService.cs`: submit, receipt upload, list, confirm (claim→reserve→promote→notify), reject.
- [x] 2.4 Modify `RunnerService.cs`: add `CreateFromRegistrationAsync(dorsal)` overload; `CreateAsync`/`UpdateAsync` reject a reserved dorsal (checked only when it changes, mirrors `excludeRunnerId`); set `DorsalNormalizado` on writes.
- [x] 2.5 Create `PublicRegistrationController.cs`: `[AllowAnonymous]` + rate limit, link/submit/receipt routes under `api/public`.
- [x] 2.6 Create `RegistrationsController.cs`: admin list/confirm/reject.
- [x] 2.7 Create `ReservedDorsalsController.cs`: admin `GET`/`POST`/`DELETE` under `api/races/{raceId}/reserved-dorsals`.
- [x] 2.8 Wire `AuditLog` writes on confirm/reject.
- [x] 2.9 Unit tests: state machine, closed/past-deadline rejection, confirm-without-dorsal, reject-no-runner.
- [x] 2.10 Integration + E2E tests: parallel confirms at capacity boundary, double-confirm idempotency, manual create/update reserved-dorsal guard + D10 unrelated-edit exemption, reservation delete-then-reuse, full anonymous submit→confirm flow, unauthenticated admin-route rejection.

### Phase 2 gap found during apply — required for end-to-end usability, not in any original phase
- [x] 2.11 Add admin endpoint(s) to configure `RaceCategory.Capacidad`/`Precio` for a race (spec requirement "RaceCategory Capacity/Price Configuration" had no assigned task).
- [x] 2.12 Add admin endpoint(s) to create/revoke a `RegistrationLink` for a race (spec requirement "Registration Link Administration" had no assigned task) — without this there is no way to generate the public link at all.
- [x] 2.13 Tests for 2.11/2.12: reject opening registration without capacity/price configured on at least one category; revoke immediately invalidates the public link.

## Phase 3: Bulk-Excel Confirm (PR 3)
- [ ] 3.1 Create `IExcelRegistrationParser.cs`: `ParsedConfirmRow`, `Parse`, `GenerateTemplate`.
- [ ] 3.2 Create `ExcelRegistrationParser.cs` (ClosedXML, reuse `ParseFecha`/`NullIfEmpty`); visible reference sheet, reads only id+dorsal columns.
- [ ] 3.3 Create `BulkConfirmResultDto.cs` + `ConfirmRowError.cs` mirroring `ImportRunnersResultDto`/`ImportRunnerError`.
- [ ] 3.4 Add `GET confirm-template` + `POST confirm-bulk` to `RegistrationsController` (`.xlsx`, 5MB guard, per-row errors).
- [ ] 3.5 Implement per-row validation + PR2 confirm pipeline call in `RegistrationService`, no batched save (D2/D13).
- [ ] 3.6 Tests: row validation, in-file dedup, capacity-exhausted-mid-batch (D13); integration: template round-trip, one bad row doesn't abort batch, mid-batch exhaustion keeps earlier successes.

## Phase 4: Notifications, Rate Limiting & Rollout (PR 4)
- [ ] 4.1 Create `RegistrationNotifier.cs` over `IEnumerable<INotificationSender>`, best-effort.
- [ ] 4.2 Modify `Program.cs` + `appsettings*.json`: `public-registration` rate-limit policy, `RegistrationOptions:EdadMayoriaEdad` (default 18).
- [ ] 4.3 Call `RegistrationNotifier` from confirm/reject (non-blocking, no retry).
- [ ] 4.4 Tests: notifier failure doesn't block confirm; 429 on rate-limit exceed; `ConfirmedCount` reconciles vs `COUNT(Runners)`; default `InscripcionesAbiertas=false` verified end-to-end.
