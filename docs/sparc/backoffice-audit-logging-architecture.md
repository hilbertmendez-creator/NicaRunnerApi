# SPARC · Fase 3 — Arquitectura (contratos implementables)

## Feature: BackOffice — Edición de nombre + Bitácora `AuditLog` transversal

- **Slug:** `backoffice-audit-logging`
- **Rol:** DBA + full-stack Senior (optimización de queries)
- **Decisiones firmes:** `AuditLog` genérico + `IAuditService`; **solo modificaciones**; usuarios auditan **Nombre + Rol + Estado**.
- Esta fase entrega los **artefactos exactos** (firmas, tipos, EF config, migración, DI, endpoints, componentes) listos para codificar en Fase 4.

---

## 1. Mapa de cambios por capa (Clean Architecture)

```
Domain
  + Entities/AuditLog.cs                         (NUEVO)

Application
  + Common/Interfaces/IAuditLogRepository.cs      (NUEVO)
  + Common/Interfaces/IAuditService.cs            (NUEVO)
  + Auditing/AuditService.cs                      (NUEVO)
  + Auditing/FieldChange.cs                       (NUEVO)
  + Auditing/Dtos/AuditLogDto.cs                  (NUEVO)
  ~ Users/Dtos/UpdateUserRequest.cs               (+ Nombre)
  ~ Users/UserManagementService.cs                (audita Nombre/Rol/Estado)
  ~ Users/IUserManagementService.cs               (firma ya lleva currentUserId ✔)
  ~ Races/RaceService.cs + IRaceService.cs         (+ currentUserId, audita)
  ~ Categories/CategoryService.cs + ICategoryService.cs (+ currentUserId, audita)

Infrastructure
  + Repositories/AuditLogRepository.cs            (NUEVO)
  ~ Data/NicaRunnerDbContext.cs                    (+ DbSet + índices + FK)
  + Migrations/<ts>_AddAuditLog.cs                 (NUEVO, EF)

Api
  ~ Controllers/UsersController.cs                 (+ GET {id}/audit)
  ~ Controllers/RacesController.cs                 (+ GET {raceId}/audit, pasa userId a Update)
  ~ Controllers/CategoriesController.cs            (+ GET {categoryId}/audit, + GetUserId helper, pasa userId)
  ~ Program.cs                                      (+ 2 registros DI)

Frontend
  ~ features/users/UsersPage.tsx + EditUserModal.tsx   (editar nombre)
  ~ components/AuditHistory.tsx (generalizado) + api/endpoints.ts + api/types.ts
```

---

## 2. Domain — `AuditLog`

```csharp
namespace NicaRunner.Domain.Entities;

public class AuditLog
{
    public int Id { get; set; }
    public string EntityType { get; set; } = string.Empty;   // "User" | "Race" | "Category"
    public int EntityId { get; set; }
    public string Campo { get; set; } = string.Empty;         // "Nombre", "Role", "IsActive", ...
    public string? ValorAnterior { get; set; }                // null = campo vacío real
    public string? ValorNuevo { get; set; }
    public int AutorId { get; set; }                          // usuario que modificó (claim)
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // UTC

    public User Autor { get; set; } = null!;                  // solo para proyección de lectura
}
```

> **Discriminador como constantes** (no enum) para no acoplar Domain a nombres de pantalla y permitir SQL legible:
```csharp
namespace NicaRunner.Domain.Constants;
public static class AuditEntityTypes
{
    public const string User = "User";
    public const string Race = "Race";
    public const string Category = "Category";
}
```

---

## 3. Infrastructure — EF config + índices (rendimiento)

En `NicaRunnerDbContext`:
```csharp
public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

// --- en OnModelCreating ---
modelBuilder.Entity<AuditLog>(e =>
{
    e.Property(a => a.EntityType).HasMaxLength(40).IsRequired();
    e.Property(a => a.Campo).HasMaxLength(60).IsRequired();
    e.Property(a => a.ValorAnterior).HasMaxLength(1024);
    e.Property(a => a.ValorNuevo).HasMaxLength(1024);

    // Índice que cubre WHERE (EntityType,EntityId) + ORDER BY CreatedAt DESC → sin sort
    e.HasIndex(a => new { a.EntityType, a.EntityId, a.CreatedAt })
        .IsDescending(false, false, true)
        .HasDatabaseName("IX_AuditLog_Entity_Created");

    // FK autor: Restrict para no perder historial (patrón NotificationLog)
    e.HasOne(a => a.Autor)
        .WithMany()
        .HasForeignKey(a => a.AutorId)
        .OnDelete(DeleteBehavior.Restrict);
    // (el índice de FK sobre AutorId lo crea EF automáticamente)
});
```
> Migración: `dotnet ef migrations add AddAuditLog` → aplica en Sqlite (dev) y Postgres (prod). `IsDescending` es soportado por ambos proveedores en EF Core 8+; si el proveedor lo ignora, el índice ascendente sirve el `ORDER BY ... DESC` en reversa sin penalización real.

```csharp
public class AuditLogRepository(NicaRunnerDbContext context) : IAuditLogRepository
{
    public void AddRange(IEnumerable<AuditLog> entries) => context.AuditLogs.AddRange(entries);

    public Task<List<AuditLogDto>> GetHistoryAsync(
        string entityType, int entityId, int limit, DateTime? beforeUtc, CancellationToken ct)
    {
        var query = context.AuditLogs
            .AsNoTracking()
            .Where(a => a.EntityType == entityType && a.EntityId == entityId);

        if (beforeUtc is { } before)
            query = query.Where(a => a.CreatedAt < before);

        return query
            .OrderByDescending(a => a.CreatedAt)
            .Take(Math.Clamp(limit, 1, 200))
            .Select(a => new AuditLogDto(
                a.Id, a.EntityType, a.EntityId, a.Campo,
                a.ValorAnterior, a.ValorNuevo,
                a.AutorId, a.Autor.Nombre, a.CreatedAt))
            .ToListAsync(ct);
    }
}
```
> `AddRange` **no** persiste (sin `SaveChanges`): las filas se commitean en el `SaveChangesAsync` que el servicio dueño ya ejecuta → misma transacción, cero round-trips extra.

---

## 4. Application — contratos

```csharp
// Auditing/FieldChange.cs
public readonly record struct FieldChange(string Campo, string? ValorAnterior, string? ValorNuevo);

// Auditing/Dtos/AuditLogDto.cs
public record AuditLogDto(
    int Id, string EntityType, int EntityId, string Campo,
    string? ValorAnterior, string? ValorNuevo,
    int AutorId, string AutorNombre, DateTime CreatedAt);

// Common/Interfaces/IAuditLogRepository.cs
public interface IAuditLogRepository
{
    void AddRange(IEnumerable<AuditLog> entries);
    Task<List<AuditLogDto>> GetHistoryAsync(string entityType, int entityId, int limit, DateTime? beforeUtc, CancellationToken ct = default);
}

// Common/Interfaces/IAuditService.cs
public interface IAuditService
{
    // Encola SOLO campos cambiados en el ChangeTracker; NO persiste.
    void TrackChanges(string entityType, int entityId, int autorId, IEnumerable<FieldChange> changes);
    Task<List<AuditLogDto>> GetHistoryAsync(string entityType, int entityId, int limit = 50, DateTime? beforeUtc = null, CancellationToken ct = default);
}
```

```csharp
// Auditing/AuditService.cs
public class AuditService(IAuditLogRepository repository) : IAuditService
{
    private const int MaxValueLength = 1024;

    public void TrackChanges(string entityType, int entityId, int autorId, IEnumerable<FieldChange> changes)
    {
        var rows = changes
            .Where(c => c.ValorAnterior != c.ValorNuevo)     // solo-si-cambió (P-8)
            .Select(c => new AuditLog
            {
                EntityType = entityType,
                EntityId = entityId,
                Campo = c.Campo,
                ValorAnterior = Truncate(c.ValorAnterior),
                ValorNuevo = Truncate(c.ValorNuevo),
                AutorId = autorId,
                CreatedAt = DateTime.UtcNow
            })
            .ToList();

        if (rows.Count > 0) repository.AddRange(rows);
    }

    public Task<List<AuditLogDto>> GetHistoryAsync(string entityType, int entityId, int limit = 50, DateTime? beforeUtc = null, CancellationToken ct = default)
        => repository.GetHistoryAsync(entityType, entityId, limit, beforeUtc, ct);

    private static string? Truncate(string? v) =>
        v is { Length: > MaxValueLength } ? v[..MaxValueLength] : v;
}

// Normalizador de valores — helper estático reutilizable (cultura invariante)
public static class AuditValue
{
    public static string? Of(string? v) => v;
    public static string? Of(decimal v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
    public static string? Of(DateTime v) => v.ToUniversalTime().ToString("O");
    public static string Of(bool v) => v ? "true" : "false";
    public static string Of<T>(T v) where T : struct, Enum => v.ToString();
    public static string Of(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
}
```

### 4.1 `UpdateUserRequest` (+ Nombre, parche parcial)
```csharp
public record UpdateUserRequest(
    [MaxLength(120)] string? Nombre,
    UserRole? Role,
    bool? IsActive);
```

### 4.2 `UserManagementService.UpdateAsync` (audita Nombre/Rol/Estado)
```csharp
public async Task<UserDto> UpdateAsync(int currentUserId, int targetUserId, UpdateUserRequest request, CancellationToken ct = default)
{
    var user = await userRepository.GetByIdAsync(targetUserId, ct)
        ?? throw new NotFoundException($"No existe el usuario con id {targetUserId}.");

    // (validaciones self/seed existentes se conservan tal cual)
    ...

    var changes = new List<FieldChange>();

    if (request.Nombre is { } nombreRaw)
    {
        var nombre = nombreRaw.Trim();
        if (nombre.Length == 0) throw new ValidationException("El nombre no puede estar vacío.");
        changes.Add(new("Nombre", user.Nombre, nombre));
        user.Nombre = nombre;
    }
    if (request.Role is { } role)
    {
        changes.Add(new("Role", user.Role.ToString(), role.ToString()));
        user.Role = role;
    }
    if (request.IsActive is { } isActive)
    {
        changes.Add(new("IsActive", AuditValue.Of(user.IsActive), AuditValue.Of(isActive)));
        user.IsActive = isActive;
    }

    auditService.TrackChanges(AuditEntityTypes.User, user.Id, currentUserId, changes);
    await userRepository.SaveChangesAsync(ct);   // 1 transacción: user + audit
    return ToDto(user);
}
```
> `AuditService` se inyecta en el constructor primario de `UserManagementService`. Las restricciones self/seed (no cambiar propio rol, no desactivar semilla) se evalúan **antes** de encolar cambios: si lanzan, no se audita nada.

### 4.3 Races / Categories
- `IRaceService.UpdateAsync` y `ICategoryService.UpdateAsync` añaden parámetro `int currentUserId`.
- Cuerpo: capturar viejos → `changes` (usando `AuditValue.Of` para `Descripcion` null, `FechaCarrera`, `Estado`, `Distancia`, ints) → mutar → `auditService.TrackChanges(AuditEntityTypes.Race/Category, id, currentUserId, changes)` → el `SaveChangesAsync` existente commitea. (Detalle de campos en §4.2/4.3 del pseudocódigo.)

---

## 5. Api — endpoints (OpenAPI documentado, C-8)

```csharp
// UsersController (clase ya es [Authorize(Roles=Administrador)])
[HttpGet("{id:int}/audit")]
public async Task<ActionResult<List<AuditLogDto>>> GetAudit(int id, [FromQuery] int limit = 50, [FromQuery] DateTime? before = null, CancellationToken ct = default)
    => Ok(await auditService.GetHistoryAsync(AuditEntityTypes.User, id, limit, before, ct));

// PATCH existente ya pasa GetUserId() como currentUserId ✔
```
```csharp
// RacesController — Update pasa el userId; nuevo GET audit (solo Admin)
[HttpPut("{raceId:int}")]
[Authorize(Roles = nameof(UserRole.Administrador))]
public async Task<ActionResult<RaceDto>> Update(int raceId, UpdateRaceRequest request, CancellationToken ct)
    => Ok(await raceService.UpdateAsync(raceId, request, GetUserId(), ct));

[HttpGet("{raceId:int}/audit")]
[Authorize(Roles = nameof(UserRole.Administrador))]
public async Task<ActionResult<List<AuditLogDto>>> GetAudit(int raceId, [FromQuery] int limit = 50, [FromQuery] DateTime? before = null, CancellationToken ct = default)
    => Ok(await auditService.GetHistoryAsync(AuditEntityTypes.Race, raceId, limit, before, ct));
```
```csharp
// CategoriesController — añade GetUserId() helper + claim; Update pasa userId; nuevo GET audit
[HttpPut("{categoryId:int}")]
[Authorize(Roles = nameof(UserRole.Administrador))]
public async Task<ActionResult<CategoryDto>> Update(int categoryId, UpdateCategoryRequest request, CancellationToken ct)
    => Ok(await categoryService.UpdateAsync(categoryId, request, GetUserId(), ct));

[HttpGet("{categoryId:int}/audit")]
[Authorize(Roles = nameof(UserRole.Administrador))]
public async Task<ActionResult<List<AuditLogDto>>> GetAudit(int categoryId, [FromQuery] int limit = 50, [FromQuery] DateTime? before = null, CancellationToken ct = default)
    => Ok(await auditService.GetHistoryAsync(AuditEntityTypes.Category, categoryId, limit, before, ct));

private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
```
> Los tres controllers reciben `IAuditService` por constructor. `CategoriesController` pasa de `[Authorize]` a inyectar también el claim; la lectura de audit queda **solo Admin** (dato sensible).

### DI (Program.cs, junto a las líneas 143/167)
```csharp
builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();
builder.Services.AddScoped<IAuditService, AuditService>();
```

---

## 6. Frontend

```ts
// api/types.ts
export interface AuditLogDto {
  id: number; entityType: string; entityId: number; campo: string;
  valorAnterior: string | null; valorNuevo: string | null;
  autorId: number; autorNombre: string; createdAt: string;
}
// api/endpoints.ts
export const getUserAudit     = (id: number) => http.get<AuditLogDto[]>(`/api/users/${id}/audit`)
export const getRaceAudit     = (id: number) => http.get<AuditLogDto[]>(`/api/races/${id}/audit`)
export const getCategoryAudit = (id: number) => http.get<AuditLogDto[]>(`/api/categories/${id}/audit`)
export const updateUser = (id: number, body: { nombre?: string; role?: UserRole; isActive?: boolean }) =>
  http.patch<UserDto>(`/api/users/${id}`, body)
```
- **`EditUserModal`**: campo Nombre editable → `PATCH`. Reutiliza estilos de `UserFormModal`.
- **`AuditHistory`** generalizado: props `{ title, load: () => Promise<AuditLogDto[]>, onClose }`. Render muestra `campo`, `valorAnterior → valorNuevo`, `autorNombre`, y `new Date(createdAt).toLocaleString('es-NI')` → **hora local Nicaragua** (AC-8). Botón "Historial" en filas de Usuarios/Carreras/Categorías (solo visible para Admin).

---

## 7. Verificación de restricciones de la spec

| Constraint | Cómo se cumple |
|---|---|
| C-1 Clean Arch | Entidad en Domain, servicio/interfaces en Application (sin EF), repo/EF en Infrastructure |
| C-2 Reutiliza patrón | Mismo idioma que `ResultAudit`/`RegisterAuditIfChanged`; mejora N+1 y tracking |
| C-3 Autor del claim | `GetUserId()` → `currentUserId`; nunca del body |
| C-4 Hora local | UTC en BD; `toLocaleString('es-NI')` en UI |
| C-5 Autorización | `[Authorize(Roles=Administrador)]` intacto en escrituras y en GET audit |
| C-6 Multi-motor | Sin SQL específico; migración EF; `IsDescending` portable |
| C-7 Inmutable | Sin endpoints update/delete de `AuditLog` |
| C-8 Estándares | Conventional Commits; endpoints en Swagger |
| C-9 Semilla | Nombre editable+auditable; rol/estado siguen bloqueados para semilla |
| C-10 Tamaño/validación | Archivos pequeños; `Trim`+no-vacío; `MaxLength` en DTO y columnas |

**Sin dependencias circulares:** `Api → Application → Domain`; `Infrastructure → Application/Domain`. `AuditService` depende solo de `IAuditLogRepository`. ✔

---

## 8. Gate de Fase 3 (Architecture)

- [x] Todas las restricciones (C-1…C-10) direccionadas (§7).
- [x] Contratos de API tipados: entidad, interfaces, DTOs, requests, firmas de servicio, endpoints (§2–§6).
- [x] Sin dependencias circulares; capas respetadas.
- [x] Estrategia de índices + migración + DI especificadas.

**Listo para gate.** Siguiente: Fase 4 (Refinement) — implementación con pruebas (TDD: authorization 403, audit-on-change, no-audit-when-unchanged, order+author name, local-time) hasta cobertura ≥ 80% de los AC.
