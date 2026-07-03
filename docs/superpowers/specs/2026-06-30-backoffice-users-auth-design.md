# Backoffice: Seed de administradores, cambio forzado de password, recuperación de password y mantenimiento de usuarios

**Fecha**: 2026-06-30
**Estado**: Aprobado para plan de implementación

## Contexto

El backoffice (`/src/NicaRunner.Api`, `/frontend`) no tiene ningún usuario administrador con el que hacer login por primera vez, ni forma de resetear una contraseña olvidada, ni una pantalla para gestionar usuarios/roles, ni una pantalla de login que refleje la marca. Este documento cubre las seis piezas necesarias para resolverlo.

Estado actual relevante (ver exploración del repo):
- `User` (`/src/NicaRunner.Domain/Entities/User.cs`): `Id, Email, PasswordHash, GoogleId, Provider, Nombre, Role (UserRole: Capturista/Administrador/Lector), CreatedAt, IsActive`.
- Auth JWT custom en `AuthService`/`AuthController`, hashing con `PasswordHasher` (PBKDF2 vía `Microsoft.AspNetCore.Identity.PasswordHasher<User>`).
- Email transaccional ya integrado vía Resend (`INotificationSender` / `ResendEmailSender`).
- Frontend React con `LoginPage.tsx` y `AuthContext.tsx`; no hay flujo de forgot/change password ni pantalla de usuarios.
- Prod hace auto-migración de EF Core al iniciar (`Program.cs`), la BD ya existe con datos — no se puede depender de `HasData()` en una migración fresca.

## 1. Modelo de datos

En `User.cs` (Domain) se agregan tres columnas nullable (migración EF Core):
- `bool MustChangePassword` (default `true`)
- `string? PasswordResetToken`
- `DateTime? PasswordResetTokenExpiry`

Se decide **no** crear tabla separada de tokens de reset: solo hay un puñado de usuarios de backoffice, un token activo por usuario es suficiente.

## 2. Seed de usuarios administradores

- Seeder idempotente ejecutado en el arranque de la app (junto al bloque de migración automática en `Program.cs`), **no** vía `HasData()` de migración (la BD de prod ya existe).
- Correos del seed: `hilbert.mendez@gmail.com`, `evr86.skip@gmail.com`, `edufisica@ymail.com`.
- Por cada correo: si no existe un `User` con ese email, se crea con `Role = Administrador`, `Provider = Local`, `IsActive = true`, `MustChangePassword = true`, y password temporal fija (misma para los tres) hasheada con el `PasswordHasher` existente.
- La password temporal fija se lee de configuración (`Seed:DefaultAdminPassword`, env var / appsettings / user-secrets) — **nunca hardcodeada en el código ni committeada**. Se comunica a los 3 administradores por un canal seguro fuera del repo.
- El seeder es seguro de re-ejecutar en cada deploy: solo crea lo que falta, nunca sobreescribe usuarios existentes.

## 3. Forzar cambio de password en primer login

- `AuthResponse` (DTO de login) gana el campo `MustChangePassword: bool`, poblado desde `User.MustChangePassword`.
- Nuevo endpoint `POST /api/auth/change-password` (requiere JWT válido vía `[Authorize]`):
  - Body: `{ currentPassword, newPassword }`.
  - Valida `currentPassword` contra el hash almacenado; si es válido, hashea `newPassword`, actualiza `PasswordHash`, pone `MustChangePassword = false`.
- Frontend: `AuthContext` guarda `mustChangePassword` del response de login. Si es `true`, la app redirige forzosamente a una página `ChangePasswordPage.tsx` y bloquea el acceso a cualquier otra ruta del backoffice hasta que se complete el cambio (patrón similar al `<ProtectedRoute>` existente, pero invertido: "forzar" en vez de "proteger").

## 4. Recuperar contraseña desde el login

- `POST /api/auth/forgot-password` (público). Body: `{ email }`.
  - Si existe un `User` con ese email y `Provider` incluye `Local`: genera un token aleatorio criptográficamente seguro + expiry (30 minutos), lo guarda en `PasswordResetToken`/`PasswordResetTokenExpiry`, envía email vía `INotificationSender` (Resend) con link `https://<frontend>/reset-password?token=...`.
  - Responde `200 OK` siempre, exista o no el email, para no filtrar qué correos están registrados (mitiga enumeración de usuarios).
- `POST /api/auth/reset-password` (público). Body: `{ token, newPassword }`.
  - Busca usuario por `PasswordResetToken`; valida que no esté expirado. Si es válido: hashea `newPassword`, limpia `PasswordResetToken`/`PasswordResetTokenExpiry`, pone `MustChangePassword = false`.
  - Si el token es inválido o expiró: `400 Bad Request` con mensaje genérico.
- Frontend:
  - `LoginPage.tsx` agrega link "¿Olvidaste tu contraseña?".
  - Nueva `ForgotPasswordPage.tsx`: pide email, llama a `/api/auth/forgot-password`, muestra mensaje genérico de confirmación (independiente de si el email existe).
  - Nueva `ResetPasswordPage.tsx`: lee `token` de la query string, pide nueva password (con confirmación), llama a `/api/auth/reset-password`, redirige a login al éxito.

## 5. Pantalla de mantenimiento de usuarios de backoffice

**Backend** — nuevo `UsersController`, todos los endpoints con `[Authorize(Roles = "Administrador")]`:
- `GET /api/users` → lista `{ id, email, nombre, role, isActive, createdAt }` de todos los usuarios (sin exponer `PasswordHash` ni tokens).
- `POST /api/users` → body `{ email, nombre, role }`. Genera password temporal aleatoria por usuario, crea el `User` con `MustChangePassword = true`, `Provider = Local`, `IsActive = true`, y envía la password temporal por email vía Resend (reutiliza `INotificationSender`).
- `PATCH /api/users/{id}` → body `{ role?, isActive? }`. Reglas de negocio:
  - Un admin no puede desactivarse a sí mismo (`IsActive = false` sobre su propio `id` → `400`).
  - Un admin no puede cambiar su propio rol (evita quedar sin admins).
- No hay `DELETE` físico: desactivar es siempre soft-delete vía `IsActive = false` (campo que ya existe en el modelo).

**Frontend** — nueva ruta `/backoffice/usuarios`, visible solo para `Role = Administrador` (usa el `<ProtectedRoute>` existente con chequeo de rol):
- Tabla: email, nombre, rol, estado (activo/inactivo), fecha de creación.
- Botón "Nuevo usuario" → modal/formulario: email, nombre, rol (select de `Capturista`/`Administrador`/`Lector`).
- Por fila: selector de rol inline y toggle activar/desactivar (deshabilitados sobre la fila del usuario actualmente logueado, según reglas de negocio del backend).
- Entrada de navegación "Usuarios" visible solo si el usuario logueado tiene `Role = Administrador`.

## 6. Rediseño de la pantalla de login (con logo)

- **Layout**: pantalla dividida en desktop (≥1024px) — panel izquierdo de marca (fondo `slate-blue-900`, igual que el header del `AppLayout`) con el logo de NicaRunner (`frontend/public/favicon.svg`, el blob morado/azul) a tamaño grande (~140px) centrado, más el nombre "nicaRunner" y un halo radial sutil en `#7e14ff` al 10-15% de opacidad detrás del logo; panel derecho blanco con el formulario. En mobile (<1024px) se colapsa a una sola columna: el logo (más pequeño, ~64px) centrado arriba del formulario, sin el panel oscuro completo.
- **Paleta**: se reutiliza la que ya existe en el backoffice — `slate-blue-900/800/700` para el panel de marca, `amber-400` como acento (coherente con el nav activo de `AppLayout`), blanco/`zinc` para el panel del formulario. No se introduce una paleta nueva.
- **Tipografía**: se mantiene la fuente actual del proyecto, solo se ajusta jerarquía (tamaños/pesos) del título y del link de recuperar contraseña.
- **Componentes**: se sigue usando `Button`, `Label`, `Input` de `@nicarunner/ui` sin cambios de API — solo cambia el layout/contenedor alrededor del `<form>` existente.
- **Nuevo elemento**: link "¿Olvidaste tu contraseña?" bajo el campo de password, alineado a la derecha, que navega a `/forgot-password` (la página de la sección 4).
- **Accesibilidad**: contraste verificado (texto blanco sobre `slate-blue-900`, texto oscuro sobre blanco), estados de foco visibles (ya provistos por `@nicarunner/ui`), responsive en 375/768/1024/1440px.

## Fuera de alcance

- No se agregan permisos granulares por acción — se mantienen los 3 roles existentes como enum.
- No hay recuperación de password para usuarios `Provider = Google` (no tienen password local que resetear).
- No se implementa rate-limiting específico en `/forgot-password` en esta iteración (se documenta como mejora futura si se observa abuso).
