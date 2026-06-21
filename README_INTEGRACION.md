# Archivos generados — NicaRunner-API (Fase 1, Core API)

Este ZIP contiene los archivos nuevos para integrar a tu solución `NicaRunner-API`
ya compilada con `dotnet build`. **Copia cada archivo a la ruta indicada dentro de
tu repo**, respetando la estructura de carpetas que ya tienes.

## Cómo integrar

### 1. Entidades de Domain (copiar directo, son archivos nuevos)

```
src/NicaRunner.Domain/Entities/Race.cs
src/NicaRunner.Domain/Entities/RaceCategory.cs
src/NicaRunner.Domain/Entities/Runner.cs
src/NicaRunner.Domain/Entities/Result.cs
src/NicaRunner.Domain/Entities/ResultAudit.cs
src/NicaRunner.Domain/Entities/User.cs
src/NicaRunner.Domain/Entities/PublicResultToken.cs
```

Crea la carpeta `Entities` dentro de `src/NicaRunner.Domain/` si no existe, y copia
estos 7 archivos ahí.

### 2. DbContext en Infrastructure (copiar directo, archivo nuevo)

```
src/NicaRunner.Infrastructure/Data/NicaRunnerDbContext.cs
```

Crea la carpeta `Data` dentro de `src/NicaRunner.Infrastructure/` si no existe.

### 3. appsettings (reemplazar o fusionar con los que ya generó `dotnet new webapi`)

```
src/NicaRunner.Api/appsettings.Development.json
src/NicaRunner.Api/appsettings.json
```

Si ya tienes contenido en estos archivos (por ejemplo, configuración de Swagger o
logging adicional), **fusiona** el bloque `ConnectionStrings` en vez de sobreescribir
todo el archivo.

### 4. Program.cs — NO sobreescribir, integrar manualmente

```
src/NicaRunner.Api/Program.cs.snippet.txt
```

Este NO es un `Program.cs` completo — es un fragmento. Abre tu `Program.cs` real
(generado por `dotnet new webapi`) en VS Code y:

1. Agrega los dos `using` indicados al inicio del archivo.
2. Pega el bloque `builder.Services.AddDbContext<...>` justo antes de la línea
   `var app = builder.Build();`.

## Después de copiar todo

Desde la raíz de tu solución:

```powershell
dotnet build
```

Si compila sin errores, el siguiente paso es generar la primera migración de EF Core:

```powershell
cd src/NicaRunner.Api
dotnet ef migrations add InitialCreate --project ../NicaRunner.Infrastructure --startup-project .
dotnet ef database update --project ../NicaRunner.Infrastructure --startup-project .
```

(Si `dotnet ef` no se reconoce, instala la herramienta global una vez:
`dotnet tool install --global dotnet-ef`)

Esto crea `nicarunner.dev.db` (SQLite) en la carpeta de `NicaRunner.Api` con las
7 tablas del modelo: Users, Races, RaceCategories, Runners, Results, ResultAudits,
PublicResultTokens.

## Notas de diseño

- **Dorsal único por carrera** (no global): índice compuesto `(RaceId, Dorsal)` en `Runner`.
- **Email único** en `User`.
- **Token público único** en `PublicResultToken`.
- `Result.Runner` usa `DeleteBehavior.Restrict` para evitar que borrar un corredor
  borre en cascada sus resultados sin querer.
- `ResultAudit` sí usa cascade delete desde `Result`, porque la auditoría no tiene
  sentido sin el resultado al que pertenece.
- Roles ya reflejan la decisión tomada: `Capturista / Administrador / Lector`
  (sin "Organizador").
