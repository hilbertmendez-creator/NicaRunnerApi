# Design: Public Runner Registration with Manual Payment

## Technical Approach

Additive vertical slice over the existing Clean layering: two aggregates (`Registration`, `RegistrationLink`) in `Domain`, `RegistrationService` in `Application`, repositories + one migration in `Infrastructure`, two controllers in `Api`. `RunnerService` stays the sole runner-creation authority and gains an internal overload that takes the admin-supplied `Dorsal` (D7 — nothing is generated). Confirm is the only capacity gate; submission is unbounded. Admins confirm either one registration at a time or in bulk by round-tripping an Excel sheet, extending the existing `ImportFromExcelAsync`/`ClosedXML` pattern rather than adding new file-handling machinery.

## Architecture Decisions

| # | Question | Options | Decision & rationale |
|---|----------|---------|----------------------|
| D1 | Capacity/price home | `Category` (global) vs `RaceCategory` | **`RaceCategory`** — `Category` is a shared catalog; 5K/10K differ per race. Adds `Capacidad int?` (null = unlimited), `Precio decimal?`, `ConfirmedCount int` (default 0). |
| D2 | Capacity concurrency | SERIALIZABLE tx vs `RowVersion` vs conditional `UPDATE` | **Conditional `UPDATE` + rows-affected** via `ExecuteUpdateAsync` (EF 8, both providers). No isolation-level or optimistic-concurrency infra exists; Sqlite/Postgres semantics differ materially. |
| D3 | Composing confirm | Ambient `BeginTransactionAsync` vs reserve-then-compensate | **Reserve-then-compensate**. An ambient transaction would be the codebase's first, spanning 3 repos, and the dorsal retry loop would then depend on EF savepoint semantics under Npgsql. Compensation biases residual drift to **undersell**, never oversell. |
| D4 | Public link | `Race.JoinCode` vs new entity | **`RegistrationLink`**, field-for-field mirror of `PublicResultToken` (`Token` unique, `FechaExpiracion`, `IsExpired`, `CreatedBy`) — revocable/expirable; `JoinCode` is always-live. |
| D5 | Notifications | Extend `NotificationLog` vs new sender facade | **`RegistrationNotifier`** over `IEnumerable<INotificationSender>`. `NotificationLog.RunnerId`/`ResultId` are non-nullable and no `Runner` exists pre-confirm. Loses retry/audit until a later slice. |
| D6 | Minor threshold | `Category.EdadMinima` vs real DOB | **`Registration.FechaNacimiento` (required)**, age computed **at `Race.FechaCarrera`**, never `DateTime.Now`, so classification cannot shift after submission. Threshold from `RegistrationOptions:EdadMayoriaEdad` (default 18). |
| D7 | Where does a dorsal come from? | System auto-assignment vs always admin-supplied | **Always admin-supplied at confirm.** No generation, so there is no prefix scheme, no numeric range, and no format rule anywhere — `Dorsal` stays the free string it is today, constrained only by uniqueness (D11) and reservations (D9). Supersedes the earlier auto-assignment design: `DorsalAssigner`, `RaceCategory.DorsalPrefijo`/`DorsalDigitos` are **dropped**. `Category.Codigo` still appears in the bulk template as read-only reference (D12), which is all the prefix concept was ever buying. |
| D8 | Reserved dorsals | Per-race-category vs per-race rows vs ranges | **`ReservedDorsal` rows keyed `(RaceId, Dorsal)` unique.** Dorsal uniqueness is already per race (`IX Runners RaceId+Dorsal`), so reservations must share that grain or they could not block a collision. Individual rows, not ranges — smallest model that satisfies the requirement. |
| D9 | Does the format bind the manual path? | Validate everywhere vs auto-assign only | **Format: auto-assign only. Reservations: everywhere.** `CreateRunnerRequest.Dorsal` stays a free string — a format regex would reject shapes admins use today and would make `UpdateAsync` fail on unrelated edits to legacy runners (config rule: never break existing signatures). But `ReservedDorsal` is a hard block on **every** write path, manual included, so a held-back number cannot be consumed by accident. Releasing a reservation is an explicit `DELETE` (see D10). |
| D10 | Editing a runner that already holds a reserved dorsal | Always check vs check on change | **Check only when the dorsal actually changes.** `UpdateAsync` re-validates the dorsal on every edit, so an unconditional check would 409 an unrelated phone-number edit if the runner's existing dorsal were reserved afterwards. Rule: skip the reserved check when `request.Dorsal` equals the runner's current dorsal — same exemption grain as the existing `excludeRunnerId` argument on `DorsalExistsAsync`. |
| D11 | `21K7` vs `21K007` must conflict | Service-side numeric compare vs persisted normalized column | **Persisted `DorsalNormalizado` + a second, additive unique index.** A service-only numeric compare cannot be the guarantee: today's `IX Runners RaceId+Dorsal` is textual, so it would happily accept `0101` next to `101`. With every dorsal now typed by a human (D7), a padding-variant duplicate is *more* likely, not less. Both `Runner` and `ReservedDorsal` store the raw string for display **and** a normalized key; uniqueness and every reserved check run on the normalized key. |
| D12 | Bulk confirm input | Blank import sheet vs pre-populated worksheet | **Export-then-fill.** The admin downloads a sheet already holding this race's `ComprobanteSubido` registrations (one row each, identified by `RegistrationId`), fills only the `Dorsal` column, and re-uploads. Runners are not re-typed — name/category/DOB already exist from public submission. A second **visible** `Categorías (referencia)` sheet carries `Codigo`/`Nombre`/`Distancia`/age range purely to look at while choosing a dorsal; unlike `ExcelRunnerParser.GenerateTemplate` it is *not* hidden and *not* a data-validation source, because the category is already fixed per row. |
| D13 | Capacity exhausted mid-batch | Abort batch vs skip that category vs skip that row | **Fail the row, short-circuit that race-category, keep going.** The first `rows=0` from the conditional reserve marks that race-category exhausted in an in-memory set; its remaining rows fail fast with "cupo lleno" without re-hitting the DB, while other categories continue. A full 5K must not block confirming 10K rows, and this preserves `ImportFromExcelAsync`'s established contract that one bad row never aborts the batch. Already-confirmed rows are **not** rolled back — same partial-success semantics the Excel import has today. |

`RunnerService.CalculateEdad` (private, anchored on `race.FechaCarrera`) is extracted to `Application/Common/EdadCalculator.AtRaceDate` and reused by both services, so submission validates the same category age-range rule that confirm would otherwise fail on late.

## Data Flow

    anon POST /api/public/registrations/{token}
      → RegistrationLink lookup (token, !IsExpired, FechaExpiracion)
      → Race.InscripcionesAbiertas && FechaLimiteInscripcion
      → EdadCalculator.AtRaceDate → category range + minor/emergency-contact
      → Registration(Pendiente) ─ PUT receipt ─→ ComprobanteSubido

    admin POST /api/races/{id}/registrations/{rid}/confirm
      1 claim   UPDATE Registrations SET Estado=Confirmada,Revisado* 
                WHERE Id=@r AND Estado=ComprobanteSubido      rows=0 → 409
      2 reserve UPDATE RaceCategories SET ConfirmedCount=ConfirmedCount+1
                WHERE Id=@rc AND (Capacidad IS NULL
                                  OR ConfirmedCount < Capacidad) rows=0 → revert 1, 409
      3 promote RunnerService.CreateFromRegistrationAsync(dorsal admin-supplied)
                + Registration.RunnerId + AuditLog  — one SaveChangesAsync
                failure → decrement 2, revert 1, rethrow
      4 notify  RegistrationNotifier (best effort, never rolls back)

    admin GET  .../registrations/confirm-template  → xlsx (pendientes + referencia)
    admin POST .../registrations/confirm-bulk      → per row: validate, then 1-4 above
                                                     row error or cupo lleno → collect,
                                                     continue; never abort the batch

Step 1 is the idempotency latch: concurrent double-confirm loses the rows-affected race and never reaches step 2.

## File Changes

| File | Action | Description |
|------|--------|-------------|
| `Domain/Entities/Registration.cs` | Create | + `RegistrationStatus` enum; `FechaNacimiento` required, `RunnerId`/`RevisadoPorUserId`/`RevisadoAt`/`MotivoRechazo` nullable, `PrecioAplicado` snapshot |
| `Domain/Entities/RegistrationLink.cs` | Create | Mirrors `PublicResultToken` |
| `Domain/Entities/RaceCategory.cs` | Modify | `Capacidad`, `Precio`, `ConfirmedCount` |
| `Domain/Entities/ReservedDorsal.cs` | Create | **New vs proposal** — raw `Dorsal` + `DorsalNormalizado`, unique `(RaceId, DorsalNormalizado)`, `Motivo`, `CreatedBy` |
| `Domain/Entities/Runner.cs` | Modify | **New vs proposal** — `DorsalNormalizado` (D11); raw `Dorsal` unchanged and still what is displayed |
| `Application/Common/DorsalNormalizer.cs` | Create | **New vs proposal** — pure `Normalize`, shared by both write paths |
| `Infrastructure/Repositories/ReservedDorsalRepository.cs` | Create | **New vs proposal** — set load + admin CRUD |
| `Api/Controllers/ReservedDorsalsController.cs` | Create | **New vs proposal** — `api/races/{raceId}/reserved-dorsals`, admin-only. `GET`/`POST`/`DELETE`; **`DELETE` is mandatory, not optional** — since D9 blocks the manual path too, without a release endpoint a reserved number is permanently unusable by anyone |
| `Domain/Entities/Race.cs` | Modify | `InscripcionesAbiertas`, `FechaLimiteInscripcion` |
| `Application/Registrations/RegistrationService.cs` | Create | Submit / receipt / list / confirm / reject |
| `Application/Common/EdadCalculator.cs` | Create | Pure age-at-race-date |
| `Application/Common/Interfaces/IExcelRegistrationParser.cs` | Create | `ParsedConfirmRow` + parse/template contract (D12) |
| `Infrastructure/Excel/ExcelRegistrationParser.cs` | Create | ClosedXML impl parallel to `ExcelRunnerParser`; reuses its `ParseFecha`/`NullIfEmpty` idioms |
| `Application/Registrations/Dtos/BulkConfirmResultDto.cs`, `ConfirmRowError.cs` | Create | Mirror `ImportRunnersResultDto`/`ImportRunnerError` |
| `Application/Runners/RunnerService.cs` | Modify | `CreateFromRegistrationAsync(dorsal)` overload; age math delegated; sets `DorsalNormalizado` on every write; **`CreateAsync` + `UpdateAsync` reject a dorsal held in `ReservedDorsal`** (D9/D10) — alongside the existing `DorsalExistsAsync` check, no format validation added |
| `Application/Common/Interfaces/IReservedDorsalRepository.cs` | Create | **New vs proposal** — `IsReservedAsync(raceId, dorsal)` + set load |
| `Infrastructure/Repositories/Registration*Repository.cs` | Create | `TryClaimAsync`/`TryReserveSlotAsync`/`ReleaseSlotAsync` via `ExecuteUpdateAsync` |
| `Infrastructure/Data/NicaRunnerDbContext.cs` | Modify | DbSets, unique `RegistrationLinks.Token`, `IX_Registrations_RaceId_Estado`, `Restrict` on `Runner`/reviewer FKs, **new unique `IX_Runners_RaceId_DorsalNormalizado`** (added alongside the existing textual index, not replacing it) |
| `Infrastructure/Migrations/*_AddRegistrations.cs` (+`.Designer.cs`, snapshot) | Create | See Migration |
| `Api/Controllers/PublicRegistrationController.cs` | Create | `[AllowAnonymous]` + `[EnableRateLimiting("public-registration")]`, `api/public` route pair |
| `Api/Controllers/RegistrationsController.cs` | Create | `[Authorize(Roles = nameof(UserRole.Administrador))]`; list/confirm/reject **+ `GET confirm-template` and `POST confirm-bulk`** (`[Consumes("multipart/form-data")]`, .xlsx + 5 MB guards copied from `RunnersController.ImportExcel`) |
| `Api/Program.cs`, `appsettings*.json` | Modify | `public-registration` IP fixed-window policy; `RegistrationOptions` |

## Interfaces / Contracts

```csharp
// Atomic capacity gate — no transaction, rows-affected is the verdict.
public Task<int> TryReserveSlotAsync(int raceCategoryId, CancellationToken ct) =>
    context.RaceCategories
        .Where(rc => rc.Id == raceCategoryId &&
                     (rc.Capacidad == null || rc.ConfirmedCount < rc.Capacidad))
        .ExecuteUpdateAsync(s => s.SetProperty(rc => rc.ConfirmedCount,
                                               rc => rc.ConfirmedCount + 1), ct);
```

**Bulk confirm (D12)** — parallel to `IExcelRunnerParser`, reusing ClosedXML and the same row/error shapes:

```csharp
public record ParsedConfirmRow(int Fila, int? RegistrationId, string Dorsal);

public interface IExcelRegistrationParser
{
    List<ParsedConfirmRow> Parse(Stream excelStream);          // reads ONLY cols 1 and 7
    byte[] GenerateTemplate(IReadOnlyList<Registration> pendientes,
                            IReadOnlyList<Category> referencia);
}
```

Sheet `Inscripciones`: `RegistrationId | Nombre | Apellidos | Categoría | F. Nacimiento | Referencia | **Dorsal**`. Only the first and last columns are read back — every other column is decoration, so an admin who edits a name by accident cannot corrupt anything. `RegistrationId` is the join key; a blank or unparseable one is a row error, never a silent skip.

Per-row validation, mirroring `ImportFromExcelAsync`'s reason-collecting loop: id resolves within this race → `Estado == ComprobanteSubido` → `Dorsal` non-empty → not duplicated **within the file** (normalized `HashSet`, exactly like the existing `seenDorsals`) → not taken by a `Runner` → not in `ReservedDorsal`. Rows that pass then run the **same** confirm pipeline as the single-registration endpoint (claim → reserve → promote), one row at a time.

This deliberately does *not* batch into a single `SaveChangesAsync` the way `ImportFromExcelAsync` does: the capacity gate is a per-row atomic conditional UPDATE (D2), and batching would dissolve exactly the guarantee it exists to provide. Cost is N round-trips, bounded by the race's pending count.

Result `BulkConfirmResultDto(int Total, int Confirmadas, List<ConfirmRowError> Errores)` mirrors `ImportRunnersResultDto(total, added, errors)`; `ConfirmRowError(int Fila, string Motivo)` mirrors `ImportRunnerError`. Upload guards (`.xlsx`, 5 MB, generic parse-failure → 400 `ValidationException`) are copied from `RunnersController.ImportExcel` and `ImportFromExcelAsync`.

**Normalization (D11)** — a pure function of the final dorsal string, applied on every write to `Runner` and `ReservedDorsal`:

```csharp
// "21K007" -> "21K7";  "0101" -> "101";  "5K1234" -> "5K1234";  "VIP-1" -> "VIP-1"
public static string Normalize(string dorsal)
{
    var raw = dorsal.Trim().ToUpperInvariant();
    var m = Regex.Match(raw, @"^([A-Z0-9]*?)(\d+)$");
    return m.Success ? m.Groups[1].Value + long.Parse(m.Groups[2].Value) : raw;
}
```

Dorsals with no numeric tail (`VIP-1`) fall through to the trimmed uppercase string, so they are still covered by uniqueness without gaining a format rule (D7/D9).

`DorsalExistsAsync(raceId, dorsal, excludeRunnerId)` and `IsReservedAsync(raceId, dorsal)` both compare `DorsalNormalizado`, not the raw column, on every write path — single confirm, bulk confirm, and manual `CreateAsync`/`UpdateAsync` alike. The in-file dedup `HashSet` is likewise built from normalized values, so `101` and `0101` in two rows of the same upload collide as a row error rather than reaching the database.

## Testing Strategy

| Layer | What | How |
|-------|------|-----|
| Unit | Age at race date, minor/emergency rule, state machine, closed/past-deadline rejection, bulk row validation + in-file dedup, `DorsalNormalizer` (`21K007`≡`21K7`, `0101`≡`101`, non-conforming passthrough, prefix-boundary equivalence) | xUnit, pure |
| Integration | Parallel confirms at `Capacidad`; concurrent double-confirm; dorsal collision retry; creating `0101` when `101` exists is rejected by the DB, not just the service (D11); template round-trip (download → fill `Dorsal` → upload → runners created); bulk batch with one bad row still confirms the rest; bulk hitting `Capacidad` mid-batch fails only that category's remaining rows and keeps the earlier successes (D13); manual `CreateAsync` rejects a reserved dorsal but still accepts a free-form non-conforming one (regression); `UpdateAsync` on an unrelated field succeeds when the runner's *unchanged* dorsal is reserved (D10); reservation `DELETE` then re-use succeeds; `ConfirmedCount` reconciliation vs `COUNT(Runners)` | Real Sqlite connection (existing `AliasIntegrationTests` harness) |
| E2E | Anonymous submit → receipt → confirm → `Runner` + `AuditLog`; 429 on rate limit | `WebApplicationFactory` |

## Threat Matrix

N/A — no routing, shell, subprocess, VCS/PR automation, executable-file classification, or process-integration boundary. The anonymous HTTP surface is covered by rate limiting, token validation, and input validation above.

## Migration / Rollout

One migration + paired `.Designer.cs` + updated `NicaRunnerDbContextModelSnapshot`, creating `Registrations`, `RegistrationLinks`, and `ReservedDorsals`, and adding `Runners.DorsalNormalizado` + its unique index.

**D11 backfill is the one deploy-blocking step.** Existing runners keep their raw dorsals (D9), but `DorsalNormalizado` must be populated for every existing row, and the new unique index will then **refuse to build** if any race already contains two runners whose dorsals normalize equally (`101` and `0101` coexist legally today). The migration MUST therefore: (1) add the column nullable, (2) backfill, (3) create the unique index. Ordering matters because step 3 is the collision detector. Before deploying, run the pre-flight — `GROUP BY RaceId, normalized HAVING COUNT(*) > 1` — and have an operator re-number the duplicates; there is no safe automatic winner. The index change is **additive**: the existing textual `IX Runners RaceId+Dorsal` stays, since normalized-uniqueness is strictly stronger and never fires independently, so rollback is still a plain drop. Migrations here are scaffolded with Sqlite active, so literal `type: "TEXT"`/`"INTEGER"` annotations reach Postgres as `text`/`integer` and break `decimal`/`bool` reads and writes (see `FixPostgresDecimalColumns`, `FixPostgresBooleanColumns`). The same migration MUST therefore append provider-guarded `ALTER TABLE ... TYPE numeric|boolean` statements (`ActiveProvider == "Npgsql.EntityFrameworkCore.PostgreSQL"`) for `RaceCategories.Precio`, `Races.InscripcionesAbiertas`, and every new `DateTime` column. Rollout: deploy with `InscripcionesAbiertas = false`; races opt in individually. Rollback = down-migration dropping both tables and the new columns.

## Open Questions

**None blocking — design is ready for tasks.**

Resolved during review: padding is caller-preserved but compared numerically (D11); dorsals are always admin-supplied, so the whole prefix/format/auto-generation branch is dropped (D7) and the earlier prefix-boundary ambiguity disappears with it; bulk capacity exhaustion is defined per race-category (D13).

- [ ] Non-blocking: should `Precio` be required before `InscripcionesAbiertas` can be set true, or only warned? Either choice is a one-line guard in `RaceService`.
- [ ] `20260802171650_AddTimingDisputes` declares `DateTime`/`int` columns as `"TEXT"`/`"INTEGER"` with no companion Postgres fix — pre-existing, out of scope here, but worth a separate change.
