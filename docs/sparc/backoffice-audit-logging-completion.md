# SPARC · Fase 5 — Completion

## Feature: BackOffice — Edición de nombre de usuario + Bitácora de auditoría transversal

- **Slug:** `backoffice-audit-logging`
- **Rama:** `claude/backoffice-audit-logging-972b9e`
- **Estado:** ✅ Completo — todos los gates de fase pasados

---

## 1. Matriz de trazabilidad (criterios de aceptación → prueba → estado)

| Criterio | Verificación | Estado |
|---|---|---|
| **AC-1** — `PATCH /api/users/{id}` con `Nombre` nuevo → `200` + `UserDto` actualizado | `UserManagementServiceTests.UpdateAsync_CambiaNombre_...` (unit) + verificado en navegador contra backend real (`PATCH /api/users/1` → `200`) | ✅ Pass |
| **AC-2** — 1 entrada de bitácora con campo/valores/autor/`createdAt` UTC | `UserManagementServiceTests.UpdateAsync_CambiaNombre_RegistraUnaEntradaDeAuditoriaConAutorYValores` + confirmado en navegador (`GET /api/users/1/audit`) | ✅ Pass |
| **AC-3** — Nombre idéntico → sin entrada de bitácora | `UserManagementServiceTests.UpdateAsync_NombreIdenticoAlActual_NoRegistraAuditoria` | ✅ Pass |
| **AC-4** — Editar Carrera (Nombre+Estado) → 2 entradas | `RaceServiceTests.UpdateAsync_CambiaNombreYEstado_RegistraDosEntradasDeAuditoria` + confirmado en navegador (3 campos cambiados → 3 entradas, orden desc correcto) | ✅ Pass |
| **AC-5** — Editar Categoría (Distancia) → 1 entrada | `CategoryServiceTests.UpdateAsync_CambiaDistanciaYNombre_RegistraSoloLosCamposQueCambiaron` + confirmado en navegador | ✅ Pass |
| **AC-6** — No-Administrador → `403` sin persistir ni auditar | Verificado en navegador contra backend real: `PATCH /users`, `PUT /races`, `PUT /categories` y los 3 `GET .../audit` → `403` con rol Capturista | ✅ Pass |
| **AC-7** — Historial ordenado desc + nombre del autor | `AuditServiceTests` + `AuditLogRepository.GetHistoryAsync` (proyección con JOIN) + confirmado en navegador (`autorNombre` presente, orden desc real) | ✅ Pass |
| **AC-8** — `createdAt` UTC mostrado en hora local Nicaragua | Confirmado en navegador tras el fix de `DateTimeKind` (bug real encontrado y corregido — ver §3) | ✅ Pass |
| **AC-9** — Bitácora inmutable (sin PATCH/DELETE) | Revisión de código: `UsersController`, `RacesController`, `CategoriesController` solo exponen `GET .../audit` | ✅ Pass |

**9/9 criterios de aceptación verificados**, cada uno con prueba unitaria y/o verificación end-to-end contra el backend real en navegador.

---

## 2. Resumen de pruebas

```
Build:  0 advertencias, 0 errores (backend .NET 8 + frontend tsc -b + vite build)
Tests:  126/126 aprobadas (NicaRunner.Tests)
```

Pruebas nuevas añadidas en esta feature:
- `AuditServiceTests.cs` — 5 pruebas (diff solo-si-cambió, null-safe, truncado a 1024, delegación de lectura)
- `RaceServiceTests.cs` — 4 pruebas (nuevo archivo; no existía cobertura directa de `RaceService.UpdateAsync`)
- `CategoryServiceTests.cs` — +1 prueba de auditoría (sobre las 6 existentes, actualizadas a la nueva firma)
- `UserManagementServiceTests.cs` — +4 pruebas (nombre/rol/estado auditados, no-cambio, validación vacío)
- `FakeAuditLogRepository.cs` — fake en memoria compartido por los tests anteriores

---

## 3. Bugs reales encontrados y corregidos durante la verificación (Fase 4)

La verificación no se limitó a tests unitarios — se ejecutó contra el backend real en navegador, lo que expuso 2 fallas que ningún mock hubiera atrapado:

1. **500 Internal Server Error al editar nombre** — `[property: MaxLength(120)]` en un record con constructor primario dispara `InvalidOperationException` en el pipeline de validación de ASP.NET Core (el atributo debe apuntar al parámetro del constructor, no a la propiedad generada). Corregido en [UpdateUserRequest.cs](src/NicaRunner.Application/Users/Dtos/UpdateUserRequest.cs) quitando el target `property:`, alineado con el patrón ya usado en `UpdateCategoryRequest`.
2. **Desfase de 6 horas en la hora mostrada** — SQLite/Postgres devuelven `DateTimeKind.Unspecified` en lectura; `System.Text.Json` serializa eso sin sufijo `Z`, y el navegador interpreta la hora UTC como si ya fuera local. Es un bug **preexistente y sistémico** en toda la app (afecta también `UserDto.CreatedAt` y potencialmente `ResultAudit`), fuera del alcance de esta feature arreglar globalmente. Se corrigió de forma acotada en [AuditLogRepository.cs](src/NicaRunner.Infrastructure/Repositories/AuditLogRepository.cs) (`DateTime.SpecifyKind(..., Utc)` tras la materialización), sin tocar configuración global de serialización.

> **Deuda técnica anotada (fuera de alcance):** aplicar el mismo fix de `DateTimeKind` a `UserDto.CreatedAt`, `RaceDto`, `ResultAuditDto` y demás DTOs con `DateTime` de solo-lectura, o configurar un `JsonConverter` global de UTC en `Program.cs`. Se recomienda una tarea separada.

---

## 4. Checklist de despliegue

- [x] **Migración EF Core** generada: `20260713032954_AddAuditLog`. Aplicada y verificada contra Sqlite (dev). Sin sintaxis específica de motor — compatible con Postgres (prod) por diseño (`IsDescending`, tipos estándar, `HasMaxLength`).
- [ ] **Aplicar la migración en producción** (Postgres/Render): el `Program.cs` ya auto-aplica migraciones pendientes al arrancar en no-Development (`db.Database.Migrate()` en `!app.Environment.IsDevelopment()`), por lo que el próximo deploy la aplica automáticamente. **No requiere acción manual**, pero se recomienda confirmar en logs del primer deploy que `AddAuditLog` corrió sin errores.
- [x] **DI registrado**: `IAuditLogRepository` + `IAuditService` en `Program.cs`.
- [x] **Autorización**: endpoints de escritura y de lectura de auditoría protegidos con `[Authorize(Roles = Administrador)]`; verificado con `403` real para rol Capturista.
- [x] **OpenAPI/Swagger**: los 3 endpoints nuevos (`GET .../audit`) quedan documentados automáticamente vía Swashbuckle (mismo patrón que el resto de la API; sin configuración adicional necesaria).
- [x] **Sin migraciones de datos**: tabla nueva (`AuditLogs`), no toca datos existentes.
- [x] **Reversible**: migración `Down()` implementada (`DropTable`).
- [ ] **Deuda técnica documentada** (no bloqueante): unificar el fix de `DateTimeKind` a nivel global (ver §3).

---

## 5. Resumen de archivos

**Nuevos (8 backend + 1 frontend + 2 test helpers):**
`AuditLog.cs`, `AuditEntityTypes.cs`, `IAuditLogRepository.cs`, `IAuditService.cs`, `AuditService.cs`, `FieldChange.cs` (dentro de `AuditService.cs`/`IAuditService.cs`), `AuditValue.cs`, `AuditLogDto.cs`, `AuditLogRepository.cs`, migración `AddAuditLog` (+Designer), `EntityAuditHistory.tsx`, `AuditServiceTests.cs`, `RaceServiceTests.cs`, `FakeAuditLogRepository.cs`.

**Modificados (19):** controllers (Users/Races/Categories), `Program.cs`, servicios de aplicación (Users/Races/Categories + interfaces), `UpdateUserRequest.cs`, `NicaRunnerDbContext.cs` + snapshot, frontend (`endpoints.ts`, `types.ts`, `UserFormModal.tsx`, `UsersPage.tsx`, `RacesPage.tsx`, `CategoryCatalogPage.tsx`), tests existentes actualizados a las nuevas firmas.

---

## 6. Gate de Fase 5

- [x] Todos los tests en verde (126/126)
- [x] Documentación completa (spec, pseudocode, architecture, completion)
- [x] Checklist de despliegue verificado (1 ítem no bloqueante: aplicación automática en el próximo deploy)
- [x] Matriz de trazabilidad completa (9/9 criterios)

**SPARC workflow completo para `backoffice-audit-logging`.** Listo para commit y PR.
