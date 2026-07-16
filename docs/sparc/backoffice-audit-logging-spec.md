# SPARC · Fase 1 — Especificación

## Feature: BackOffice — Edición de nombre de usuario + Bitácora de auditoría transversal

- **Slug:** `backoffice-audit-logging`
- **Rama:** `claude/backoffice-audit-logging-972b9e`
- **Fase actual:** 1 — Specification
- **Fecha:** 2026-07-12

---

## 1. Resumen del requerimiento (del usuario)

> En la pantalla de Usuarios del BackOffice se debe poder **editar el nombre** de cada usuario y **guardar bitácora** de esos cambios. Aplicar también bitácora a los cambios hechos en pantallas como **Carreras** y **Categorías**, registrando: **usuario que modificó**, **valor original**, **valor nuevo**, y **fecha/hora local**. Toda acción de **modificación de datos solo puede ser realizada por usuarios Administradores**.

---

## 2. Contexto del código existente (hallazgos)

La solución es .NET Clean Architecture (`Api` / `Application` / `Domain` / `Infrastructure`) + frontend React. **Ya existe un patrón de auditoría** que debemos reutilizar, no reinventar:

| Pieza existente | Ubicación | Rol |
|---|---|---|
| Entidad `ResultAudit` | [ResultAudit.cs](src/NicaRunner.Domain/Entities/ResultAudit.cs) | Auditoría a nivel de campo: `CampoModificado`, `ValorAnterior`, `ValorNuevo`, `Razon`, `AdminId`, `CreatedAt` (UTC) |
| Escritura de auditoría | `RegisterAuditIfChangedAsync` en [ResultService.cs:147](src/NicaRunner.Application/Results/ResultService.cs) | Registra una fila **solo si el valor cambió** |
| Repositorio | [IResultAuditRepository.cs](src/NicaRunner.Application/Common/Interfaces/IResultAuditRepository.cs) | `GetAllByResultAsync`, `AddAsync` |
| DTO + endpoint | `ResultAuditDto`, `GetAuditAsync` | Consulta ordenada por fecha desc |
| UI historial | [AuditHistory.tsx](frontend/src/features/results/AuditHistory.tsx) | Modal; renderiza `new Date(createdAt).toLocaleString('es-NI')` → **convierte UTC→hora local del navegador** |

### Estado actual de autorización (importante)
- **Usuarios:** [`UsersController`](src/NicaRunner.Api/Controllers/UsersController.cs) ya es `[Authorize(Roles = Administrador)]` a nivel de clase. ✅
- **Carreras:** en [`RacesController`](src/NicaRunner.Api/Controllers/RacesController.cs), `Create`/`Update`/`Delete`/`Start` ya son `Administrador`. ✅
- **Categorías:** en [`CategoriesController`](src/NicaRunner.Api/Controllers/CategoriesController.cs), `Create`/`Update`/`Delete` ya son `Administrador`. ✅
- Roles disponibles: `Capturista`, `Administrador`, `Lector` ([User.cs](src/NicaRunner.Domain/Entities/User.cs)).

> **Conclusión:** el requisito "solo Administradores modifican" **ya está cubierto** para escrituras de Carreras/Categorías/Usuarios. La Fase 1 lo verifica y lo blinda con pruebas; no requiere reescritura funcional.

### Brechas reales a cubrir
1. **No se puede editar el nombre de usuario.** `UpdateUserRequest` es `(UserRole? Role, bool? IsActive)` — **no incluye `Nombre`** ([UpdateUserRequest.cs](src/NicaRunner.Application/Users/Dtos/UpdateUserRequest.cs)), y `UpdateAsync` no lo toca ([UserManagementService.cs:55](src/NicaRunner.Application/Users/UserManagementService.cs)). El modal de frontend [`UserFormModal`](frontend/src/features/users/UserFormModal.tsx) es **solo de creación** ("Nuevo usuario").
2. **No hay bitácora** en Usuarios, Carreras ni Categorías. Solo Resultados audita.
3. La auditoría existente (`ResultAudit`) está **acoplada a `Result`** (FK `ResultId`). Para cubrir varias entidades hace falta una bitácora **transversal**.

---

## 3. Alcance

### Dentro de alcance
- Editar el campo **`Nombre`** de un usuario (BackOffice), solo Administradores.
- Bitácora de auditoría **transversal** para modificaciones de: **Usuarios**, **Carreras**, **Categorías**.
- Cada entrada registra: **quién** modificó (usuario/Admin), **campo**, **valor anterior**, **valor nuevo**, **fecha/hora** (UTC almacenado, mostrado en hora local Nicaragua).
- UI para **consultar** el historial de auditoría por registro.
- Verificación/blindaje de que las modificaciones son exclusivas de Administradores.

### Fuera de alcance (no se implementa ahora)
- Editar email, rol o estado NO es parte de este requerimiento nuevo (rol/estado ya existen y seguirán funcionando; el foco nuevo es el **nombre**).
- Migrar `ResultAudit` al nuevo modelo transversal (los Resultados ya tienen auditoría propia funcional; unificar es opcional y se propone como deuda técnica, no requisito).
- Auditoría de operaciones de **creación** y **borrado** (el requerimiento dice "cambios"/"modificó"). Se documenta como extensión opcional.
- Exportación de la bitácora (CSV/PDF).
- Retención/purga de bitácora.

---

## 4. Requisitos funcionales

- **RF-1** — Un Administrador puede editar el nombre de un usuario desde la pantalla de Usuarios del BackOffice.
- **RF-2** — Al guardar un cambio de nombre, el sistema registra una entrada de bitácora con: autor (Administrador autenticado), campo `Nombre`, valor anterior, valor nuevo, fecha/hora.
- **RF-3** — Al modificar una **Carrera** (p. ej. `Nombre`, `Descripcion`, `FechaCarrera`, `Estado`), el sistema registra una entrada de bitácora por cada campo que cambió.
- **RF-4** — Al modificar una **Categoría** (p. ej. `Codigo`, `NombreCategoria`, `Descripcion`, `Distancia`, `EdadMinima`, `EdadMaxima`, `Orden`), el sistema registra una entrada de bitácora por cada campo que cambió.
- **RF-5** — La bitácora es consultable por registro (por usuario, por carrera, por categoría), ordenada de más reciente a más antigua.
- **RF-6** — Toda operación de modificación de datos está restringida al rol `Administrador`; un no-Administrador recibe `403 Forbidden`.
- **RF-7** — Solo se registra una entrada cuando el valor **realmente cambia** (mismo patrón que `RegisterAuditIfChangedAsync`).
- **RF-8** — La bitácora es **inmutable**: no existe endpoint para editar ni borrar entradas.

---

## 5. Criterios de aceptación (gate Fase 1: ≥3 — aquí 9)

- **AC-1** — Dado un Administrador autenticado, cuando hace `PATCH /api/users/{id}` con un `Nombre` nuevo, entonces el usuario queda con el nombre nuevo y responde `200` con el `UserDto` actualizado.
- **AC-2** — Dado el cambio de nombre de AC-1, entonces existe **exactamente una** entrada de bitácora con `campo="Nombre"`, `valorAnterior=<nombre viejo>`, `valorNuevo=<nombre nuevo>`, `autorId=<id del Admin>` y `createdAt` en UTC.
- **AC-3** — Dado un usuario cuyo nombre se "actualiza" al **mismo** valor, entonces **no** se crea ninguna entrada de bitácora (RF-7).
- **AC-4** — Dado un Administrador que edita una Carrera cambiando `Nombre` y `Estado`, entonces se crean **dos** entradas de bitácora (una por campo) con sus valores anterior/nuevo.
- **AC-5** — Dado un Administrador que edita una Categoría cambiando `Distancia`, entonces se crea una entrada con `valorAnterior`/`valorNuevo` de la distancia.
- **AC-6** — Dado un usuario con rol `Capturista` o `Lector`, cuando intenta `PATCH /api/users/{id}`, `PUT /api/races/{id}` o `PUT /api/categories/{id}`, entonces responde `403 Forbidden` y **no** se crea entrada de bitácora ni se modifica el dato.
- **AC-7** — Dado cualquier registro auditado, cuando se consulta su historial, entonces las entradas vienen ordenadas por `createdAt` descendente e incluyen el **nombre del autor** (no solo el id).
- **AC-8** — Dada una entrada de bitácora con `createdAt` UTC, cuando se muestra en la UI, entonces se presenta en **hora local de Nicaragua (America/Managua, UTC−6)** vía `toLocaleString('es-NI')`.
- **AC-9** — No existe forma (API ni UI) de editar o eliminar una entrada de bitácora ya registrada (RF-8).

---

## 6. Restricciones (constraints)

- **C-1 (Arquitectura)** — Respetar Clean Architecture: entidad en `Domain`, servicio + interfaz de repo en `Application`, repo + config EF en `Infrastructure`, endpoints en `Api`. Nada de acceso a `DbContext` desde `Application`.
- **C-2 (Patrón)** — Reutilizar el patrón `ResultAudit` (auditoría a nivel de campo, "registrar solo si cambió", DTO + repo + endpoint de consulta, modal `AuditHistory`).
- **C-3 (Autor)** — El autor se toma del claim `ClaimTypes.NameIdentifier` del usuario autenticado (patrón `GetUserId()` ya usado en controllers). Nunca del cuerpo de la petición (evita suplantación).
- **C-4 (Hora local)** — Persistir en **UTC** (`DateTime.UtcNow`, consistente con todo el sistema); la conversión a hora local Nicaragua se hace en presentación. No almacenar hora local en BD.
- **C-5 (Autorización)** — Mantener `[Authorize(Roles = Administrador)]` en toda escritura. No degradar autorización existente.
- **C-6 (BD)** — Cambios de esquema vía **migración EF Core** nueva; compatible con **Sqlite (dev)** y **Postgres (prod)** — sin sintaxis específica de un motor (seguir convención de [NicaRunnerDbContext.cs](src/NicaRunner.Infrastructure/Data/NicaRunnerDbContext.cs)).
- **C-7 (Inmutabilidad)** — La tabla de bitácora es append-only; sin endpoints de update/delete.
- **C-8 (Estándares globales)** — Commits en **Conventional Commits**; endpoints nuevos documentados en **OpenAPI/Swagger** (estándar del proyecto).
- **C-9 (Semillas)** — Respetar `ProtectedSeedUsers`: la edición de nombre de un admin semilla se permite (solo se protegen rol/estado hoy), pero debe auditarse igual. Confirmar en Fase 2.
- **C-10 (Tamaño)** — Archivos < 500 líneas; validar entrada en el borde (nombre no vacío, longitud razonable).

---

## 7. Casos borde (edge cases)

- **E-1** — Nombre nuevo idéntico al actual → sin entrada de bitácora (AC-3).
- **E-2** — Nombre vacío o solo espacios → `400 ValidationException`; no se persiste ni audita.
- **E-3** — Nombre con espacios al inicio/fin → definir en Fase 2 si se hace `Trim` (y auditar el valor ya normalizado). Recomendado: `Trim`.
- **E-4** — Campo nullable que pasa de `null`→valor o valor→`null` (p. ej. `Descripcion` de Carrera/Categoría) → representar `null` como marcador legible (p. ej. `"(sin valor)"`) igual que `ResultService` usa `"(sin asignar)"`.
- **E-5** — Edición concurrente del mismo registro por dos Admins → última escritura gana; ambas ediciones quedan registradas en la bitácora (dos entradas). No se requiere bloqueo optimista en Fase 1.
- **E-6** — Autor eliminado/desactivado luego de auditar → la entrada conserva el `autorId`; FK con `DeleteBehavior.Restrict` para no perder historial (patrón `NotificationLog`).
- **E-7** — Valores decimales (`Distancia`) y enums (`Estado`, `Role`) → serializar de forma estable y legible (`ToString`/cultura invariante) para que anterior/nuevo sean comparables y auditables.
- **E-8** — Registro auditado sin historial todavía → la UI muestra "Sin cambios registrados todavía" (patrón `AuditHistory`).
- **E-9** — Usuario auto-editándose el nombre siendo Admin → permitido (a diferencia de rol/estado, que tienen restricciones self en `UpdateAsync`); debe auditarse.

---

## 8. Decisión de diseño abierta (a resolver antes de Fase 3)

**¿Bitácora transversal genérica vs. auditoría por entidad?**

- **Opción A — `AuditLog` genérico (recomendada):** una entidad `AuditLog { Id, EntityType, EntityId, Campo, ValorAnterior, ValorNuevo, AutorId, CreatedAt }`. Un solo servicio `IAuditService.RegisterChangesAsync(...)` reutilizable por Users/Races/Categories (y futuras pantallas). Menos duplicación; una sola tabla/consulta.
- **Opción B — auditoría por entidad:** `UserAudit`, `RaceAudit`, `CategoryAudit` espejo de `ResultAudit`. Más fiel al patrón actual pero triplica entidad+repo+DTO+migración.

> Recomendación: **Opción A**, dejando `ResultAudit` como está (deuda técnica opcional de unificación). Confirmar con el usuario en el gate.

### ✅ DECISIÓN TOMADA (2026-07-12)
- **Modelo:** **Opción A — `AuditLog` genérico** + `IAuditService` reutilizable. `ResultAudit` permanece sin cambios.
- **Alcance de eventos:** **Solo modificaciones** (cambios de valor). Creación y borrado quedan fuera de esta iteración.

---

## 9. Trazabilidad (se completa en Fases 4–5)

| Criterio | Prueba | Estado |
|---|---|---|
| AC-1 | `UsersController`/`UserManagementService` update-name test | Pendiente |
| AC-2 | audit-written-on-name-change test | Pendiente |
| AC-3 | no-audit-when-unchanged test | Pendiente |
| AC-4 | race-update multi-field audit test | Pendiente |
| AC-5 | category-update audit test | Pendiente |
| AC-6 | authorization 403 tests (users/races/categories) | Pendiente |
| AC-7 | audit query ordering + author name test | Pendiente |
| AC-8 | frontend local-time render test | Pendiente |
| AC-9 | append-only (no update/delete endpoint) test | Pendiente |

---

## 10. Gate de Fase 1

- [x] ≥ 3 criterios de aceptación → **9**
- [x] Restricciones explícitas → **10 (C-1…C-10)**
- [x] Casos borde identificados → **9 (E-1…E-9)**

**Bloqueante para avanzar:** confirmar con el usuario la **Decisión §8** (Opción A vs B) — determina el modelo de datos de la Fase 2/3.
