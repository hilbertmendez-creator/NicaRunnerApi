# SPARC · Fase 2 — Pseudocódigo & Diseño de rendimiento

## Feature: BackOffice — Edición de nombre + Bitácora `AuditLog` transversal

- **Slug:** `backoffice-audit-logging`
- **Rol asumido:** DBA + Programador full-stack Senior, especialista en optimización de queries
- **Objetivo transversal:** que la bitácora **no degrade el rendimiento** ni de las escrituras (crear audit) ni de las lecturas (consultar historial), hoy y a escala (cientos de miles de filas).
- **Decisiones heredadas de Fase 1:** `AuditLog` genérico + `IAuditService`; **solo modificaciones**.

---

## 0. Principios de optimización que gobiernan este diseño

| # | Principio | Impacto |
|---|---|---|
| **P-1** | **Cero queries extra para leer el valor anterior.** La entidad ya está cargada y *tracked*; el valor viejo se captura en memoria antes de mutar. No hay `SELECT` adicional. | Elimina 1 round-trip por edición |
| **P-2** | **Una sola transacción / un solo `SaveChanges`.** Las filas de auditoría se `Add`-ean al mismo `DbContext`; el `SaveChangesAsync` que la entidad ya ejecutaba persiste cambio + auditoría **atómicamente**. | Cero round-trips extra; atomicidad garantizada |
| **P-3** | **Índice compuesto que cubre WHERE + ORDER BY.** `(EntityType, EntityId, CreatedAt DESC)` sirve el filtro y el orden sin *sort* en memoria ni *table scan*. | Lectura O(log n) + range scan |
| **P-4** | **Proyección directa a DTO con JOIN al autor.** La consulta hace `Select` con `Autor.Nombre` en **una sola query** — sin N+1, sin cargar entidades completas. Corrige la carencia de `ResultAuditDto` (que solo trae `AdminId`). | 1 query en vez de 1+N |
| **P-5** | **`AsNoTracking` en todas las lecturas** de bitácora (son solo-lectura). | Sin overhead del change-tracker |
| **P-6** | **Paginación keyset** por `CreatedAt` (no `OFFSET`). Historial acotado por defecto (`Take`), escalable a "cargar más" sin degradarse en páginas profundas. | Constante en cualquier página |
| **P-7** | **Filas estrechas + columnas acotadas.** `HasMaxLength` en discriminadores; nada de `text` ilimitado en columnas indexadas. | Índices densos, menos I/O |
| **P-8** | **Solo se escribe si cambió** (diff en memoria). Un no-cambio = 0 filas, 0 I/O. | Evita ruido y escrituras inútiles |
| **P-9** | **FK `Restrict` sobre el autor.** El historial nunca se pierde por cascada; y el índice de FK acelera "auditoría por autor". | Integridad + consulta rápida |

---

## 1. Modelo de datos — `AuditLog`

```
ENTITY AuditLog
    Id             : int, PK, identity
    EntityType     : string(40)   NOT NULL   // "User" | "Race" | "Category"  (discriminador)
    EntityId       : int          NOT NULL   // id del registro modificado
    Campo          : string(60)   NOT NULL   // "Nombre", "FechaCarrera", "Distancia", ...
    ValorAnterior  : string(1024) NULL       // representación textual estable; NULL real = campo vacío
    ValorNuevo     : string(1024) NULL
    AutorId        : int          NOT NULL   // FK -> User (quién modificó; tomado del claim, no del body)
    CreatedAt      : DateTime     NOT NULL   // UTC (DateTime.UtcNow). Se muestra en hora local Nicaragua.

    NAV Autor : User   // solo para proyección de nombre en lectura
```

### Decisión de tipos (DBA)
- **`EntityType` como `string(40)`** en vez de `int`/enum en BD: legible, extensible a nuevas pantallas sin migración de datos, y con `HasMaxLength(40)` la columna es angosta → índice denso. El costo vs. un `smallint` es marginal frente a la claridad operativa (consultas SQL directas legibles para el DBA).
- **`ValorAnterior/ValorNuevo` `string(1024)` NULL**: acota el ancho (evita `text`/`nvarchar(max)`), suficiente para nombres/descripciones. `NULL` distingue "campo vacío real" de cadena vacía. Truncado defensivo a 1024 en el serializador.
- **`CreatedAt` UTC**: consistente con todo el sistema (`DateTime.UtcNow`). La hora local se resuelve en presentación (C-4 de la spec).

### Índices (el corazón del rendimiento)
```
IX_AuditLog_Entity_Created  (EntityType, EntityId, CreatedAt DESC)   -- consulta principal: cubre WHERE + ORDER BY
IX_AuditLog_AutorId         (AutorId)                                -- FK + "qué cambió este usuario"
FK  AuditLog.AutorId -> User.Id   ON DELETE RESTRICT                 -- nunca perder historial
```

> **Por qué el índice compuesto en ese orden:** la selectividad va de menor a mayor cardinalidad de filtro y termina en la columna de orden. `EntityType` + `EntityId` fijan un rango contiguo; `CreatedAt DESC` entrega las filas **ya ordenadas** → el planificador hace un *index range scan* y devuelve el `Take(N)` sin *sort*. Válido en Postgres (prod) y SQLite (dev).

### Compatibilidad multi-motor (C-6)
- Sin sintaxis específica de motor (mismo criterio que `NicaRunnerDbContextModelSnapshot`).
- El `DESC` en el índice se declara vía `HasIndex(...).IsDescending(false, false, true)` (EF Core lo traduce a ambos motores). Si el proveedor no lo soporta, el índice ascendente + `ORDER BY CreatedAt DESC` sigue usando el índice en reversa — degradación nula en la práctica.

---

## 2. Contrato del servicio de auditoría — `IAuditService`

Vive en `Application` (**sin dependencia de EF** — Clean Architecture, C-1). Solo **acumula** filas en el repositorio; **no** llama a `SaveChanges` (lo hace el servicio dueño de la transacción → P-2).

```
INTERFACE IAuditService
    // Registra en memoria SOLO los campos que cambiaron. No persiste.
    method TrackChanges(entityType, entityId, autorId, changes: List<FieldChange>)

    // Lectura paginada, proyectada, sin tracking.
    method GetHistoryAsync(entityType, entityId, limit=50, beforeUtc=null) -> List<AuditLogDto>

VALUE FieldChange { Campo, ValorAnterior:string?, ValorNuevo:string? }
```

```
INTERFACE IAuditLogRepository   // en Application; impl en Infrastructure
    method AddRange(entries: List<AuditLog>)                 // NO async: solo marca en el ChangeTracker
    method GetHistoryAsync(entityType, entityId, limit, beforeUtc, ct) -> List<AuditLogDto>
```

### Serialización estable de valores (evita falsos diffs y valores no comparables — E-7)
```
FUNCTION Normalize(value) -> string?:
    IF value == null: RETURN null
    SWITCH type:
        decimal  -> value.ToString(CultureInfo.Invariant)      // "10.5" siempre, sin coma decimal local
        DateTime -> value.ToUniversalTime().ToString("O")       // ISO-8601, comparable
        enum     -> value.ToString()                            // "EnCurso", "Administrador"
        bool     -> value ? "true" : "false"
        else     -> value.ToString()
    RETURN Truncate(result, 1024)
```

### Diff (solo-si-cambió — P-8)
```
FUNCTION TrackChanges(entityType, entityId, autorId, changes):
    rows = []
    FOR each c in changes:
        IF c.ValorAnterior == c.ValorNuevo:   CONTINUE        // no-op, cero I/O
        rows.add(new AuditLog {
            EntityType=entityType, EntityId=entityId,
            Campo=c.Campo, ValorAnterior=c.ValorAnterior, ValorNuevo=c.ValorNuevo,
            AutorId=autorId, CreatedAt = DateTime.UtcNow
        })
    IF rows not empty:  repository.AddRange(rows)             // un solo AddRange → un round-trip al hacer SaveChanges
```

---

## 3. Lectura del historial — query única, proyectada, paginada (P-3..P-6)

```
FUNCTION GetHistoryAsync(entityType, entityId, limit=50, beforeUtc=null, ct):
    query = context.AuditLogs
                .AsNoTracking()                               // P-5
                .Where(a => a.EntityType == entityType
                         && a.EntityId   == entityId)          // usa IX_AuditLog_Entity_Created
    IF beforeUtc != null:
        query = query.Where(a => a.CreatedAt < beforeUtc)      // keyset pagination (P-6) — NO OFFSET
    RETURN query
                .OrderByDescending(a => a.CreatedAt)           // servido por el índice, sin sort
                .Take(Clamp(limit, 1, 200))                    // cota dura anti-abuso
                .Select(a => new AuditLogDto(                  // P-4: proyección + JOIN autor en 1 query
                    a.Id, a.EntityType, a.EntityId, a.Campo,
                    a.ValorAnterior, a.ValorNuevo,
                    a.AutorId, a.Autor.Nombre,                 // JOIN implícito, sin N+1
                    a.CreatedAt))
                .ToListAsync(ct)
```

**Plan de ejecución esperado (Postgres):** `Index Scan using ix_auditlog_entity_created` → `Limit` → `Nested Loop` contra `users` por `AutorId` (PK). Sin `Sort`, sin `Seq Scan`. Coste ~O(log n + limit).

---

## 4. Instrumentación de los servicios de dominio (patrón idéntico en los 3)

Regla de oro (P-1 + P-2): **capturar viejo → mutar → TrackChanges → el `SaveChanges` existente commitea todo**.

### 4.1 Usuarios — añadir edición de `Nombre` + auditoría
```
// UpdateUserRequest: (Nombre?, Role?, IsActive?)   <-- se AÑADE Nombre (nullable = parche parcial)

FUNCTION UserManagementService.UpdateAsync(currentUserId, targetUserId, request, ct):
    user = repo.GetByIdAsync(targetUserId) OR throw NotFound      // ya estaba; entidad tracked
    ... (validaciones self/seed existentes: rol, estado) ...

    changes = []
    IF request.Nombre is provided:
        newName = request.Nombre.Trim()                          // E-3
        IF newName is empty: throw ValidationException           // E-2
        changes.add(FieldChange("Nombre", user.Nombre, newName)) // viejo desde memoria (P-1)
        user.Nombre = newName

    IF request.Role provided and changed:   ... user.Role = role      // (comportamiento existente; auditable opcional)
    IF request.IsActive provided and changed: ... user.IsActive = ...

    auditService.TrackChanges("User", user.Id, currentUserId, changes)   // NO persiste aún
    repo.SaveChangesAsync(ct)                                            // P-2: 1 transacción = user + audit
    RETURN ToDto(user)
```
> Nota: el requerimiento explícito es **el nombre**. Rol/estado ya existen; se pueden auditar en la misma pasada sumándolos a `changes` (recomendado, costo cero). Semilla protegida: el **nombre sí** es editable (solo rol/estado están bloqueados) — se audita igual (C-9/E-9).

### 4.2 Carreras — auditar campos de `UpdateAsync`
```
FUNCTION RaceService.UpdateAsync(raceId, request, currentUserId, ct):     // <-- se añade currentUserId
    race = GetRaceOrThrowAsync(raceId)                                     // tracked
    changes = [
        FieldChange("Nombre",       race.Nombre,               request.Nombre),
        FieldChange("Descripcion",  Normalize(race.Descripcion),Normalize(request.Descripcion)),  // E-4 null
        FieldChange("FechaCarrera", Normalize(race.FechaCarrera),Normalize(request.FechaCarrera)),
        FieldChange("Estado",       race.Estado.ToString(),     request.Estado.ToString())
    ]
    race.Nombre=...; race.Descripcion=...; race.FechaCarrera=...; race.Estado=...; race.UpdatedAt=UtcNow
    auditService.TrackChanges("Race", race.Id, currentUserId, changes)
    repo.SaveChangesAsync(ct)                                              // 1 transacción
    RETURN ToDto(race)
```

### 4.3 Categorías — auditar campos de `UpdateAsync`
```
FUNCTION CategoryService.UpdateAsync(categoryId, request, currentUserId, ct):   // <-- se añade currentUserId
    category = GetCategoryOrThrowAsync(categoryId)                               // tracked
    EnsureValidAgeRange(...); EnsureCodigoIsUniqueAsync(...)                     // validaciones existentes
    changes = [
        FieldChange("Codigo",         category.Codigo,          request.Codigo.Trim().ToUpperInvariant()),
        FieldChange("NombreCategoria",category.NombreCategoria, request.NombreCategoria),
        FieldChange("Descripcion",    Normalize(category.Descripcion), Normalize(request.Descripcion)),
        FieldChange("Distancia",      Normalize(category.Distancia),   Normalize(request.Distancia)),  // decimal invariante
        FieldChange("EdadMinima",     category.EdadMinima.ToString(),  request.EdadMinima.ToString()),
        FieldChange("EdadMaxima",     category.EdadMaxima.ToString(),  request.EdadMaxima.ToString()),
        FieldChange("Orden",          category.Orden.ToString(),       request.Orden.ToString())
    ]
    ... aplicar cambios ...
    auditService.TrackChanges("Category", category.Id, currentUserId, changes)
    repo.SaveChangesAsync(ct)
    RETURN ToDto(category)
```

> **Impacto de firma:** `RaceService.UpdateAsync` y `CategoryService.UpdateAsync` reciben `currentUserId`. Los controllers ya tienen `GetUserId()` (Races) — Categories añade el mismo helper con `[Authorize]` + claim. Cambio localizado.

---

## 5. Endpoints de lectura (Api) — OpenAPI documentado (C-8)

```
GET /api/users/{id}/audit           [Authorize(Roles=Administrador)]  -> List<AuditLogDto>
GET /api/races/{raceId}/audit       [Authorize(Roles=Administrador)]  -> List<AuditLogDto>
GET /api/categories/{categoryId}/audit [Authorize(Roles=Administrador)] -> List<AuditLogDto>
    query params: ?limit=50 & before=<ISO-UTC>   (keyset paging)
```
Cada uno delega en `auditService.GetHistoryAsync("<Tipo>", id, limit, before, ct)`.

`AuditLogDto` (record): `(Id, EntityType, EntityId, Campo, ValorAnterior?, ValorNuevo?, AutorId, AutorNombre, CreatedAt)`.

---

## 6. Frontend (full-stack)

- **Editar nombre:** convertir `UserFormModal` (hoy solo-alta) o añadir `EditUserModal` que hace `PATCH /api/users/{id}` con `{ nombre }`. Validación cliente: no vacío, trim.
- **Historial:** generalizar `AuditHistory.tsx` (hoy atado a result) a un `<AuditHistory entityType entityId />` que llama al endpoint correspondiente. **Reutiliza** el render existente `new Date(createdAt).toLocaleString('es-NI')` → **hora local Nicaragua** (AC-8), y `AutorNombre` ahora disponible (P-4).
- Paginación "cargar más" opcional usando `before = última fila.createdAt`.

---

## 7. Complejidad y presupuesto de rendimiento

| Operación | Queries BD | Round-trips | Notas |
|---|---|---|---|
| Editar nombre usuario (con audit) | 1 SELECT (ya existía) + 1 SaveChanges | **+0** vs. hoy | audit va en el mismo SaveChanges (P-2) |
| Editar carrera (3 campos cambian) | 1 SELECT + 1 SaveChanges | **+0** | 3 filas audit en 1 INSERT batch |
| Consultar historial (50 filas) | **1** query proyectada | 1 | sin N+1 (P-4), sin sort (P-3), sin tracking (P-5) |
| Historial paginado profundo | 1 query keyset | 1 | constante en cualquier página (P-6) |

**Escala:** con `IX_AuditLog_Entity_Created`, una tabla de 1–10 M filas responde el historial de un registro en *index range scan* acotado por `Take` — independiente del tamaño total de la tabla.

---

## 8. Nota de diseño (alternativa considerada y descartada para Fase 1/2)

**Interceptor de `SaveChanges` (auditoría automática vía `ChangeTracker`)** — capturaría *todo* cambio de entidades marcadas `IAuditable` leyendo `Entry.OriginalValues`/`CurrentValues`, sin código por servicio. Es más "mágico" y potente, pero: (a) mete infraestructura EF en el flujo de forma implícita, (b) requiere un `ICurrentUser` accesible en el interceptor, (c) es más difícil de testear y de controlar qué campos se auditan.
**Se descarta por ahora** a favor del `IAuditService` explícito: mismo idioma que el `RegisterAuditIfChangedAsync` que el equipo ya conoce, Clean Architecture pura (sin EF en `Application`), 100% testeable, y con **idéntico costo de round-trips** (P-2). Se deja anotado como evolución futura si el número de entidades auditadas crece mucho.

---

## 9. Gate de Fase 2 (Pseudocode)

- [x] Pseudocódigo cubre los 9 criterios de aceptación de la spec (edición nombre, audit por campo, solo-si-cambió, 403, orden desc + nombre autor, hora local, inmutabilidad).
- [x] Rutas de error explícitas: `NotFound`, `ValidationException` (nombre vacío), `403` autorización, `null`→marcador.
- [x] Complejidad anotada por operación (§7) con presupuesto de round-trips y comportamiento a escala.
- [x] Estrategia de índices y plan de ejecución documentados (§1, §3).

**Listo para gate.** Bloqueante menor a confirmar en Fase 3 (Architecture): ¿auditamos también rol/estado del usuario en la misma pasada (costo cero) o estrictamente solo `Nombre`?
