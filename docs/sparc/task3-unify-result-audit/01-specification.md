# Especificación — Unificación de ResultAudit en AuditLog

## Contexto

El repositorio mantiene hoy dos sistemas de auditoría paralelos:

1. **`AuditLog`** (genérico, mergeado en PR #50): tabla transversal `EntityType/EntityId/Campo/ValorAnterior/ValorNuevo/AutorId/CreatedAt`, usada actualmente para Usuario, Carrera y Categoría (`AuditEntityTypes.User/Race/Category`).
2. **`ResultAudit`** (preexistente desde la migración `InitialCreate`, 2026-06-21): tabla acoplada 1:1 a `Result` vía FK `ResultId`, con campos `CampoModificado/ValorAnterior/ValorNuevo/Razon/AdminId/CreatedAt`.

La diferencia funcional clave es el campo **`Razon`** (motivo obligatorio de la edición de un resultado de carrera), que no tiene equivalente en `AuditLog`.

## Estado actual (inventario)

### Backend — Domain
- `src/NicaRunner.Domain/Entities/ResultAudit.cs` — entidad con FK `ResultId` (→ `Result`) y `AdminId` (→ `User`).
- `src/NicaRunner.Domain/Entities/AuditLog.cs` — entidad genérica con `EntityType`/`EntityId` discriminador, sin FK física a la entidad auditada (por diseño, para no acoplarse a N tablas).
- `src/NicaRunner.Domain/Constants/AuditEntityTypes.cs` — constantes `User`, `Race`, `Category`. **No incluye `Result`.**
- `src/NicaRunner.Domain/Entities/Result.cs` — tiene navegación `AuditEntries` (colección de `ResultAudit`).

### Backend — Application
- `src/NicaRunner.Application/Common/Interfaces/IResultAuditRepository.cs` — `GetAllByResultAsync`, `AddAsync`.
- `src/NicaRunner.Application/Common/Interfaces/IAuditLogRepository.cs` — `AddRange` (batch, no persiste hasta `SaveChangesAsync` del dueño de la transacción), `GetHistoryAsync` (paginado por keyset, proyecta DTO con nombre del autor).
- `src/NicaRunner.Application/Auditing/AuditService.cs` + `IAuditService.cs` — servicio genérico `TrackChanges(entityType, entityId, autorId, changes)` que graba solo campos que cambiaron y trunca a `MaxValueLength = 1024`.
- `src/NicaRunner.Application/Results/ResultService.cs` — usa `IResultAuditRepository` directamente (no pasa por `IAuditService`): método privado `RegisterAuditIfChangedAsync` (líneas 147-160) construye un `ResultAudit` por campo (`Dorsal`, `TiempoLlegada`) e incluye `request.Razon` en cada fila. `GetAuditAsync` (líneas 136-145) lista el historial de un resultado.
- `src/NicaRunner.Application/Results/Dtos/ResultAuditDto.cs` — DTO expuesto vía API, incluye `Razon`.
- `src/NicaRunner.Application/Auditing/Dtos/AuditLogDto.cs` — DTO del sistema genérico, sin campo `Razon`.

### Backend — Infrastructure
- `src/NicaRunner.Infrastructure/Repositories/ResultAuditRepository.cs` — CRUD trivial (2 métodos, sin lógica adicional).
- `src/NicaRunner.Infrastructure/Repositories/AuditLogRepository.cs` — algo más elaborado: usa `AsNoTracking`, proyección con `JOIN` a `Autor.Nombre`, paginación keyset vía `beforeUtc`, y aprovecha el índice `IX_AuditLog_Entity_Created`.
- `src/NicaRunner.Infrastructure/Data/NicaRunnerDbContext.cs` (líneas 161-186): configuración EF de ambas entidades.
  - `ResultAudit`: `OnDelete(DeleteBehavior.Cascade)` con `Result` **y** con `User` (`AdminId`) — ver migración `InitialCreate.cs` línea 217 (`FK_ResultAudits_Users_AdminId` → `Cascade`).
  - `AuditLog`: `OnDelete(DeleteBehavior.Restrict)` con `User` (`AutorId`) — explícitamente para no perder historial si se borra al autor (comentario línea 180-181).
  - **Esta divergencia es una inconsistencia de integridad de datos ya existente**, no introducida por esta tarea: hoy, si se borra un `User` que es admin, sus filas de `ResultAudit` desaparecen en cascada, mientras que sus filas de `AuditLog` se protegen (`Restrict`).

### Migraciones EF
- `src/NicaRunner.Infrastructure/Migrations/20260621160426_InitialCreate.cs` (líneas 189-254): crea tabla `ResultAudits` con índices `IX_ResultAudits_AdminId` e `IX_ResultAudits_ResultId` (sin índice compuesto por fecha; no está optimizada para listar "últimos N cambios" cross-resultado).
- `src/NicaRunner.Infrastructure/Migrations/20260713032954_AddAuditLog.cs`: crea tabla `AuditLogs` con índice compuesto `IX_AuditLog_Entity_Created (EntityType, EntityId, CreatedAt DESC)`, diseñado para paginación eficiente.
- `src/NicaRunner.Infrastructure/Migrations/NicaRunnerDbContextModelSnapshot.cs`: snapshot vigente, referencia ambas tablas.

### API
- `src/NicaRunner.Api/Controllers/ResultsController.cs` (líneas 54-60): `GET /races/{raceId}/results/{resultId}/audit`, restringido a rol `Administrador` (nota en comentario: a diferencia de dashboard/standings, Lector no tiene acceso aquí).
- `src/NicaRunner.Api/Controllers/UsersController.cs`, `RacesController.cs`, `CategoriesController.cs`: consumen `IAuditService.GetHistoryAsync` para exponer el historial genérico (no localizado en detalle, pero confirmado por grep).
- `src/NicaRunner.Api/Program.cs`: registra ambos repositorios (`IResultAuditRepository` → `ResultAuditRepository`, `IAuditLogRepository` → `AuditLogRepository`) en DI.

### Frontend
- `frontend/src/features/results/AuditHistory.tsx` — componente específico para resultados, consume `getResultAudit(raceId, resultId)`, renderiza `campoModificado`, `valorAnterior → valorNuevo`, y opcionalmente `razon`.
- `frontend/src/components/EntityAuditHistory.tsx` — componente genérico usado para Usuario/Carrera/Categoría, consume el endpoint de `AuditLog`. **Ya existe duplicación de UI**: dos componentes que renderizan esencialmente la misma estructura visual (lista de cambios campo/antes/después/fecha), uno con soporte de `razon` y otro sin.
- `frontend/src/api/types.ts` y `frontend/src/api/endpoints.ts`: definen tipos y llamadas separadas (`ResultAuditDto` vs `AuditLogDto`; `getResultAudit` vs el endpoint genérico).

### Tests
- `tests/NicaRunner.Tests/ResultServiceUpdateTests.cs`, `ResultServiceIdempotencyTests.cs`: cubren la escritura de `ResultAudit` vía `ResultService`.
- `tests/NicaRunner.Tests/AuditServiceTests.cs`, `FakeAuditLogRepository.cs`, `UserManagementServiceTests.cs`, `RaceServiceTests.cs`, `CategoryServiceTests.cs`: cubren `AuditLog`/`AuditService`.

## Alcance del cambio si se aprueba

Si se decide unificar, el cambio tocaría:

1. **Domain**: agregar `AuditEntityTypes.Result = "Result"`; decidir qué hacer con el campo `Razon` (ver sección de opciones).
2. **Application**: reemplazar el uso directo de `IResultAuditRepository` en `ResultService` por `IAuditService.TrackChanges(...)`; adaptar `GetAuditAsync` para usar `IAuditService.GetHistoryAsync("Result", resultId, ...)`; eliminar o dejar obsoleto `ResultAuditDto` en favor de `AuditLogDto` (o extenderlo).
3. **Infrastructure**: eliminar `ResultAuditRepository.cs` y su registro en DI; eliminar la entidad `ResultAudit` del `DbContext` (`DbSet<ResultAudit>`, configuración EF).
4. **Migración de datos**: nueva migración EF que (a) cree cualquier columna adicional necesaria en `AuditLogs` (si se opta por agregar `Razon`/`Contexto`), (b) copie cada fila de `ResultAudits` a `AuditLogs` con `EntityType = "Result"`, `EntityId = ResultId`, `Campo = CampoModificado`, `AutorId = AdminId`, preservando `CreatedAt`, y (c) elimine la tabla `ResultAudits`.
5. **API**: el endpoint `GET /races/{raceId}/results/{resultId}/audit` debe mantenerse (contrato externo, consumido por frontend) pero su implementación pasaría a consultar `AuditLogs` filtrando por `EntityType="Result"` y `EntityId=resultId`. Si el DTO de respuesta cambia de forma (pierde o transforma `Razon`), **es un breaking change de API** para cualquier consumidor externo del endpoint.
6. **Frontend**: `AuditHistory.tsx` podría eliminarse y reemplazarse por `EntityAuditHistory.tsx` (si este último soporta el campo `Razon` de alguna forma) o mantenerse como wrapper delgado sobre el componente genérico.
7. **Tests**: reescribir `ResultServiceUpdateTests.cs`/`ResultServiceIdempotencyTests.cs` para verificar contra `AuditLog` en vez de `ResultAudit`; los fakes/mocks de `IResultAuditRepository` deben eliminarse.

## Riesgos identificados

- **Migración de datos en producción**: `ResultAudits` es una tabla de auditoría con `Cascade` delete ligado a `Result`, en uso desde el commit inicial del proyecto (más antigua que `AuditLog`). Es razonable asumir que ya contiene historial real de ediciones de resultados de carreras pasadas. Cualquier migración que mueva o transforme estas filas debe ser reversible o, como mínimo, no debe ejecutarse sin backup previo — esto es una carga operativa nueva, no solo de código.
- **Pérdida o transformación con pérdida del campo `Razon`**: es el único campo sin equivalente. Cualquier opción que no lo preserve tal cual constituye pérdida de información de auditoría — inaceptable para una bitácora cuyo propósito es justamente la trazabilidad.
- **Breaking change de API**: el DTO `ResultAuditDto` expone `razon` como propiedad de primer nivel; el frontend (`AuditHistory.tsx` línea 45) depende de `entry.razon` directamente. Si la unificación cambia la forma del payload sin coordinar el despliegue de frontend y backend, se rompe la UI de auditoría de resultados en producción.
- **Divergencia de integridad referencial ya existente**: `ResultAudit.AdminId` usa `Cascade` on delete de `User`, mientras `AuditLog.AutorId` usa `Restrict`. Si se migran filas de `ResultAudit` a `AuditLog` tal cual, el comportamiento de borrado de usuarios cambia retroactivamente para esas filas (de "se borran en cascada" a "bloquean el borrado del usuario"). Esto podría, en teoría, impedir borrar un usuario que hoy sí se podría borrar — un efecto colateral no obvio.
- **Downtime / ventana de migración**: dependiendo del volumen de filas y del proveedor de base de datos (el repo migró de Render a Neon recientemente, según memoria del proyecto), una migración de datos con `INSERT ... SELECT` masivo podría bloquear la tabla `AuditLogs` (que ya está en uso activo por Usuario/Carrera/Categoría) durante la ventana de copia. Se recomienda evaluar el tamaño real de `ResultAudits` antes de decidir el enfoque (batch vs. una sola transacción).
- **Acoplamiento de features en curso**: no se pudo verificar en este alcance de especificación si hay trabajo simultáneo o reciente sobre `ResultAudit` en otras ramas activas del repo (hay múltiples worktrees: `backoffice-audit-logging-972b9e`, `captura-roles-invitaciones`, `ruflo-gentle-ia-migration-94ce34`). Una unificación mal coordinada con cambios paralelos sobre resultados podría generar conflictos de migración EF (dos migraciones tocando las mismas tablas en ramas distintas).

## Opciones para el campo "Razon"

**Opción A — Agregar columna nullable `Razon` a `AuditLog`**
- Pros: preserva el dato sin transformación; consulta simple (columna directa); no requiere tocar el DTO genérico más que agregar un campo opcional.
- Contras: acopla el modelo genérico a un caso de uso específico (edición de resultados); el campo queda `null` para el 100% de las filas de Usuario/Carrera/Categoría, ensuciando el esquema genérico con una columna de uso parcial.

**Opción B — Mapear `Razon` dentro de un campo genérico existente (ej. concatenarlo en `ValorNuevo`, o crear un campo `Contexto` genérico reutilizable)**
- Pros: no rompe el principio de "tabla verdaderamente genérica"; un campo `Contexto` (en vez de `Razon`) podría ser útil a futuro para otras entidades (ej. motivo de cambio de rol de usuario).
- Contras: si se concatena en `ValorNuevo`, se corrompe semánticamente ese campo (deja de ser "el valor nuevo puro") y rompe cualquier consumidor que compare `ValorAnterior`/`ValorNuevo` textualmente; si se crea `Contexto` como columna nueva, es funcionalmente idéntico a la Opción A con otro nombre — mismo trade-off de "columna nullable para casi nadie".

**Opción C — Mantener una tabla de extensión (ej. `AuditLogResultDetails` o similar) 1:1 con `AuditLog.Id`, solo para filas de tipo `Result`**
- Pros: no ensucia el esquema genérico; el campo `Razon` vive donde semánticamente pertenece; permite agregar más campos específicos de `Result` en el futuro sin tocar `AuditLog`.
- Contras: reintroduce parte de la complejidad que se buscaba eliminar (dos tablas en vez de una); requiere un `JOIN` adicional en `GetHistoryAsync` cuando `EntityType == "Result"`, lo cual complica el repositorio genérico que hoy es intencionalmente simple y optimizado (comentario en `AuditLogRepository.cs` sobre evitar N+1).

## Recomendación

**El riesgo no se justifica frente al beneficio.** `ResultAudit` es un CRUD simple y aislado (2 métodos en el repositorio, un único consumidor en `ResultService`, sin lógica compleja) — no hay una carga de mantenimiento real que la unificación resuelva. A cambio, se asume el riesgo concreto de migrar datos de auditoría en producción (donde la integridad del historial es el requisito de negocio primario), un posible breaking change de API/frontend, y la corrección retroactiva de una divergencia de `DeleteBehavior` que hoy nadie ha reportado como problema. Si el driver real es "eliminar duplicación de código", el ahorro (un repositorio de ~15 líneas y un DTO) es marginal comparado con el riesgo de tocar historial de auditoría ya persistido. Recomendación: **dejar ambos sistemas coexistiendo** tal como están, y revisar esta decisión solo si en el futuro surge una necesidad funcional concreta (por ejemplo, una vista consolidada de "todos los cambios de todas las entidades" que hoy no existe y que sí justificaría pagar el costo de unificación).
