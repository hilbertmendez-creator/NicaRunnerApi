# SPARC Fase 1 — Especificación: Plantillas HTML para correos

**Feature:** Estilizar correos del sistema con plantilla HTML moderna  
**Slug:** `email-html-templates`  
**Fase:** 1 — Specification  
**Decisión arquitectónica:** Opción A — plantillas renderizadas en el API, Resend solo como transporte  
**Fecha:** 2026-07-06

---

## 1. Contexto

Hoy los correos se generan en la capa **Application** como cadenas de texto plano y se envían vía `ResendEmailSender`, que solo incluye el campo `text` en el payload de la API de Resend. No existen plantillas HTML ni integración con plantillas hosted de Resend.

### Correos identificados

| ID | Tipo | Origen | Subject actual |
|----|------|--------|-----------------|
| EM-01 | Resultado de carrera | `NotificationService.BuildMessage()` | `Tu resultado en NicaRunner` (default ResendOptions) |
| EM-02 | Reset de contraseña | `AuthService` | `Restablece tu contraseña de NicaRunner` |
| EM-03 | Cuenta nueva backoffice | `UserManagementService` | `Tu cuenta en NicaRunner Backoffice` |

### Flujo actual

```
Application (texto plano) → INotificationSender → ResendEmailSender → POST /emails { text }
```

### Flujo objetivo

```
Application (modelo de datos) → IEmailTemplateRenderer → HTML + text fallback
    → INotificationSender (extendido) → ResendEmailSender → POST /emails { html, text }
```

---

## 2. Referencia de diseño — Backoffice (`@nicarunner/ui`)

Los correos deben alinearse visualmente con el **tema `brand`** del backoffice (`frontend/src/index.css`), que es el tema de marca pública de NicaRunner.

### 2.1 Paleta de colores (tema brand)

| Token | Hex | Uso en correo |
|-------|-----|---------------|
| `--bg-app` | `#F5F9FF` | Fondo externo del body |
| `--bg-card` | `#FFFFFF` | Tarjeta de contenido |
| `--bg-sidebar` | `#0D47A1` | Header / barra de marca |
| Sidebar gradient | `#0D47A1` → `#08306B` | Header con gradiente (160deg) |
| `--bd-card` | `#D6EAFF` | Borde de tarjeta |
| `--text-hi` | `#11243F` | Títulos y texto principal |
| `--text-lo` | `#5B6B82` | Texto secundario |
| `--text-xs` | `#9AA5B1` | Footer, disclaimers |
| `--accent` | `#1565FF` | Links y CTAs |
| `--pr-bg` / `--pr-text` | `#1565FF` / `#FFFFFF` | Botón primario |
| Botón gradient | `#42A5F5` → `#1565FF` | CTA principal (90deg) |
| `--badge-ok-text` | `#059669` | Éxito / confirmación |
| `--badge-er-text` | `#DC2626` | Alertas / errores |
| Logo mark | `#863bff` | Isotipo NicaRunner (`favicon.svg`) |

> **Nota de marca:** El logo usa púrpura (`#863bff`) mientras la UI operativa usa azul marino (`#0D47A1`). Los correos combinan ambos: isotipo púrpura en header navy, CTAs en azul gradiente.

### 2.2 Tipografía

| Elemento | Fuente | Tamaño | Peso |
|----------|--------|--------|------|
| Título principal | Inter | 20px | 600 |
| Subtítulo / sección | Inter | 14px | 600 |
| Cuerpo | Inter | 14px | 400 |
| Datos numéricos (posición, tiempo) | JetBrains Mono | 16–20px | 600 |
| Footer / legal | Inter | 11px | 400 |
| Botón CTA | Inter | 14px | 500 |

**Fallback email-safe:** `'Inter', system-ui, 'Segoe UI', Arial, sans-serif` y `'JetBrains Mono', ui-monospace, 'Courier New', monospace`.

Google Fonts no es fiable en clientes de correo; Inter y JetBrains Mono deben declararse con `@import` en `<style>` y fallback a fuentes del sistema.

### 2.3 Espaciado y radios

| Token | Valor brand | Uso |
|-------|-------------|-----|
| `--radius-card` | 14px | Contenedor principal |
| `--radius-btn` | 10px | Botones CTA |
| `--radius-badge` | 20px | Badges de estado |
| Padding tarjeta | 24–32px | Contenido interno |
| Ancho máximo | 600px | Estándar email responsive |

### 2.4 Patrones de UI a replicar

Basados en `AppLayout.tsx`, `theme/styles.ts` y `@nicarunner/ui`:

1. **Header:** barra navy con gradiente, logo inline SVG, texto "NicaRunner" en blanco, subtítulo "Gestión de competencias de atletismo" en blanco al 60% opacidad.
2. **Tarjeta de contenido:** fondo blanco, borde `#D6EAFF`, border-radius 14px, padding 32px.
3. **Botón primario:** gradiente `#42A5F5` → `#1565FF`, texto blanco, border-radius 10px, padding 12px 24px, ancho auto (full-width en móvil para CTA principal).
4. **Badge de resultado:** pill con fondo `#EEF3FF`, texto `#1D4ED8` para datos destacados (posición).
5. **Alerta informativa:** fondo `#EEF3FF`, borde `#BFDBFE`, texto `#1D4ED8` (reset password disclaimer).
6. **Footer:** texto `#9AA5B1`, 11px, link a sitio web si aplica.

### 2.5 Assets

| Asset | Ruta | Estrategia en correo |
|-------|------|---------------------|
| Logo SVG | `frontend/public/favicon.svg` | Inline SVG simplificado en plantilla base (sin filtros complejos) |
| Logo component | `frontend/src/routes/NicaRunnerLogo.tsx` | Referencia para versión simplificada email-safe |

---

## 3. Requisitos funcionales

### RF-01 — Plantilla base reutilizable
Un layout HTML compartido (`_Layout.html`) con slots para: título, cuerpo, CTA opcional, footer. Compatible con tablas HTML inline (no flexbox/grid moderno).

### RF-02 — Variante EM-01: Resultado de carrera
Mostrar: saludo personalizado, nombre de carrera, posición (badge), tiempo de llegada (monospace), mensaje de agradecimiento. Sin CTA obligatorio.

### RF-03 — Variante EM-02: Reset de contraseña
Mostrar: saludo, explicación, botón CTA "Restablecer contraseña" con link, alerta de expiración (30 min), disclaimer "Si no solicitaste esto, ignora este correo."

### RF-04 — Variante EM-03: Cuenta nueva backoffice
Mostrar: saludo, confirmación de cuenta creada, contraseña temporal en bloque monospace destacado, instrucción de cambio obligatorio al primer login. Sin CTA (el usuario va al login manualmente).

### RF-05 — Fallback texto plano
Cada variante genera automáticamente la versión `text` con el mismo contenido semántico que hoy (para clientes sin HTML y accesibilidad).

### RF-06 — Envío dual html + text
`ResendEmailSender` envía ambos campos en el payload. Resend elige la mejor versión según el cliente.

### RF-07 — Motor de plantillas en Infrastructure
Plantillas como archivos `.html` embebidos (EmbeddedResource) en `NicaRunner.Infrastructure`, renderizados con **Scriban** (ligero, sin Razor runtime). Application define modelos tipados por tipo de correo.

### RF-08 — Interfaz de renderizado desacoplada
Nueva interfaz `IEmailTemplateRenderer` en Application.Common.Interfaces. Los servicios (`AuthService`, `UserManagementService`, `NotificationService`) construyen modelos, no HTML.

### RF-09 — Escape de HTML
Todos los valores dinámicos (nombres, emails, tokens en texto) deben escaparse. Links se construyen con URLs validadas, no concatenación cruda de input de usuario.

### RF-10 — Configuración de URL base
Links en EM-02 usan `Frontend:BaseUrl` (ya existente). Validar que no esté vacío antes de renderizar; si falta, fallar el envío con error descriptivo.

---

## 4. Requisitos no funcionales

### RNF-01 — Compatibilidad email clients
Probar en Gmail (web + móvil), Outlook (desktop + web), Apple Mail. Usar tablas anidadas, estilos inline, `@media` para responsive.

### RNF-02 — Tamaño del payload
HTML inline SVG simplificado; sin imágenes externas hosteadas (evita bloqueo de imágenes). HTML total < 100 KB por correo.

### RNF-03 — Sin regresión en flujo de notificaciones
El cron `ProcessPendingAsync` y envío masivo `NotifyAllAsync` siguen funcionando. El campo `Mensaje` en `NotificationLog` almacena la versión text (para auditoría/debug).

### RNF-04 — Testabilidad
Tests unitarios del renderer con snapshots de HTML por variante. Tests de escape XSS en campos dinámicos.

### RNF-05 — Mantenibilidad
Tokens de color como constantes C# (`EmailDesignTokens.cs`) que mapean 1:1 al tema brand del frontend, documentados con referencia a `index.css`.

---

## 5. Criterios de aceptación

| ID | Criterio | Verificación |
|----|----------|--------------|
| AC-01 | Los 3 tipos de correo (EM-01, EM-02, EM-03) se envían con HTML estilizado y texto plano equivalente | Test unitario por variante + envío manual en staging |
| AC-02 | El diseño visual usa la paleta del tema `brand` del backoffice (header navy, card blanca, CTA gradiente, tipografía Inter) | Revisión visual contra captura del backoffice en tema brand |
| AC-03 | Los correos se renderizan correctamente en Gmail, Outlook web y cliente móvil iOS | Checklist manual con capturas |
| AC-04 | Valores dinámicos escapados; intento de XSS en nombre de corredor no ejecuta script | Test unitario con input malicioso |
| AC-5 | `ResendEmailSender` envía payload con campos `html` y `text`; sin `html` el envío sigue funcionando (retrocompat) | Test de integración con mock HTTP |
| AC-06 | `NotificationLog.Mensaje` conserva versión text para auditoría | Test en NotificationService |
| AC-07 | Si `Frontend:BaseUrl` está vacío, EM-02 falla con error claro sin enviar correo roto | Test en AuthService |

> Gate Fase 1: ≥ 3 AC ✓ (7 definidos), restricciones explícitas ✓, edge cases ✓

---

## 6. Restricciones

1. **Resend es solo transporte** — no usar plantillas hosted de Resend ni React Email en su dashboard.
2. **Plantillas viven en el repo** — versionadas en git, embebidas como EmbeddedResource.
3. **No cambiar contratos públicos de API** — los endpoints de notificación/auth/users no cambian su request/response.
4. **Español neutro** — todo el copy de correos en español, coherente con el backoffice.
5. **Sin dependencias pesadas** — Scriban (~200 KB), no RazorLight ni servicios de renderizado externos.
6. **Sin imágenes CDN** — logo inline SVG; no depender de URLs externas que puedan bloquearse.
7. **Application no referencia Infrastructure** — modelos y `IEmailTemplateRenderer` en Application; implementación en Infrastructure.

---

## 7. Casos borde

| ID | Caso | Comportamiento esperado |
|----|------|------------------------|
| EC-01 | Cliente de correo bloquea imágenes/SVG | Logo inline SVG se muestra como vector nativo; si falla, el texto "NicaRunner" en header es suficiente |
| EC-02 | Nombre con caracteres especiales (`María José`, `O'Brien`, `<script>`) | Escapado HTML correcto; acentos preservados (UTF-8) |
| EC-03 | Link de reset muy largo | Botón CTA con URL completa; versión text incluye URL en línea separada |
| EC-04 | `Frontend:BaseUrl` con trailing slash | Normalizar URL antes de concatenar (`/reset-password?token=...`) |
| EC-05 | Envío masivo (100+ correos) | Renderizado por correo es stateless y rápido (< 5ms); no bloquea cron |
| EC-06 | Tiempo de llegada midnight edge (`00:00:00`) | Formato `HH:mm:ss` consistente con hoy |
| EC-07 | Carrera/corredor con nombre muy largo | Text wrap en card; no rompe layout de 600px |
| EC-08 | Outlook ignora `@media` queries | Layout degrada gracefully: contenido sigue legible en ancho fijo 600px |
| EC-09 | Contraseña temporal con caracteres especiales | Mostrada en `<code>` con escape; monospace |
| EC-10 | Reintento de notificación fallida | Re-renderiza plantilla con datos actuales del corredor (contacto resuelto en envío) |

---

## 8. Diseño preliminar de componentes (input para Fase 3)

```
NicaRunner.Application/
  Common/Interfaces/
    IEmailTemplateRenderer.cs
    IEmailSender.cs                    ← extiende INotificationSender o wrapper
  Notifications/EmailTemplates/
    EmailTemplateType.cs               ← enum: RaceResult, PasswordReset, WelcomeAccount
    RaceResultEmailModel.cs
    PasswordResetEmailModel.cs
    WelcomeAccountEmailModel.cs
    RenderedEmail.cs                   ← record { string Html, string Text, string Subject }

NicaRunner.Infrastructure/
  Notifications/
    EmailDesignTokens.cs               ← colores del tema brand
    ScribanEmailTemplateRenderer.cs
    Templates/
      _Layout.html
      RaceResult.html
      PasswordReset.html
      WelcomeAccount.html
    ResendEmailSender.cs               ← modificado: html + text
```

---

## 9. Wireframes textuales

### EM-01 — Resultado de carrera

```
┌─────────────────────────────────────────┐
│ ▓▓▓ HEADER NAVY GRADIENT ▓▓▓▓▓▓▓▓▓▓▓▓▓ │
│  [logo] NicaRunner                      │
│  Gestión de competencias de atletismo   │
├─────────────────────────────────────────┤
│                                         │
│  Hola, {Nombre}                         │
│                                         │
│  Tu resultado en {Carrera}:             │
│                                         │
│  ┌─────────────┐  ┌──────────────────┐  │
│  │ Posición    │  │ Tiempo           │  │
│  │   #{Pos}    │  │  {HH:mm:ss}      │  │
│  └─────────────┘  └──────────────────┘  │
│                                         │
│  ¡Gracias por participar!               │
│                                         │
├─────────────────────────────────────────┤
│  Footer: NicaRunner © 2026              │
└─────────────────────────────────────────┘
```

### EM-02 — Reset de contraseña

```
┌─────────────────────────────────────────┐
│ ▓▓▓ HEADER ▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓ │
│  [logo] NicaRunner                      │
├─────────────────────────────────────────┤
│  Hola, {Nombre}                         │
│  Recibimos una solicitud para           │
│  restablecer tu contraseña.             │
│                                         │
│  ┌─────────────────────────────────┐    │
│  │   Restablecer contraseña  →     │    │  ← CTA gradient
│  └─────────────────────────────────┘    │
│                                         │
│  ⓘ Este enlace expira en 30 minutos.   │  ← alert info
│                                         │
│  Si no solicitaste esto, ignora         │
│  este correo.                           │
├─────────────────────────────────────────┤
│  Footer                                 │
└─────────────────────────────────────────┘
```

### EM-03 — Cuenta nueva

```
┌─────────────────────────────────────────┐
│ ▓▓▓ HEADER ▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓ │
│  [logo] NicaRunner Backoffice           │
├─────────────────────────────────────────┤
│  Hola, {Nombre}                         │
│  Se creó tu cuenta en NicaRunner        │
│  Backoffice.                            │
│                                         │
│  Contraseña temporal:                   │
│  ┌─────────────────────────────────┐    │
│  │  {tempPassword}                 │    │  ← monospace block
│  └─────────────────────────────────┘    │
│                                         │
│  Deberás cambiarla al iniciar sesión    │
│  por primera vez.                       │
├─────────────────────────────────────────┤
│  Footer                                 │
└─────────────────────────────────────────┘
```

---

## 10. Trazabilidad

| Requisito | AC | Edge cases |
|-----------|-----|------------|
| RF-01..04 | AC-01, AC-02 | EC-01, EC-07 |
| RF-05..06 | AC-01, AC-05 | EC-05 |
| RF-07..08 | AC-01 | — |
| RF-09 | AC-04 | EC-02, EC-09 |
| RF-10 | AC-07 | EC-03, EC-04 |
| RNF-01 | AC-03 | EC-01, EC-08 |
| RNF-03 | AC-06 | EC-10 |

---

## 11. Fuera de alcance (Fase 1)

- WhatsApp con formato rich (canal separado, stub actual)
- Editor visual de plantillas en backoffice
- Internacionalización (i18n) — solo español
- Dark mode en correos
- A/B testing de diseños

---

## 12. Próximo paso

Ejecutar `/sparc advance` para validar gate de Fase 1, luego Fase 2 (Pseudocode) con algoritmos de renderizado y estructura Scriban detallada.
