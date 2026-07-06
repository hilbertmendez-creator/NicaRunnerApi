# SPARC Fase 2 — Pseudocódigo: Plantillas HTML para correos

**Feature:** Estilizar correos del sistema con plantilla HTML moderna  
**Slug:** `email-html-templates`  
**Fase:** 2 — Pseudocode  
**Prerequisito:** Gate Fase 1 aprobado (`email-html-templates-spec.md`)  
**Fecha:** 2026-07-06

---

## 1. Objetivo de esta fase

Definir algoritmos, contratos y flujos de error antes de implementar. Cada bloque mapea a criterios de aceptación (AC) y casos borde (EC) de la spec.

---

## 2. Tipos y contratos

### 2.1 `RenderedEmail` (Application)

```
RECORD RenderedEmail:
    Html    : string    // HTML completo listo para Resend
    Text    : string    // fallback plano (auditoría + clientes sin HTML)
    Subject : string    // asunto del correo
```

### 2.2 `EmailTemplateType` (Application)

```
ENUM EmailTemplateType:
    RaceResult      // EM-01
    PasswordReset   // EM-02
    WelcomeAccount  // EM-03
```

### 2.3 Modelos por variante (Application)

```
RECORD RaceResultEmailModel:
    RecipientName : string
    RaceName      : string
    Position      : int
    ArrivalTime     : TimeSpan    // formatear HH:mm:ss en renderer

RECORD PasswordResetEmailModel:
    RecipientName : string
    ResetUrl      : string        // URL ya construida y validada
    ExpiresMinutes: int = 30

RECORD WelcomeAccountEmailModel:
    RecipientName : string
    TempPassword  : string
    ProductName   : string = "NicaRunner Backoffice"
```

### 2.4 `IEmailTemplateRenderer` (Application.Common.Interfaces)

```
INTERFACE IEmailTemplateRenderer:
    Render(template: EmailTemplateType, model: object) → RenderedEmail
        THROWS TemplateRenderException si plantilla no existe o Scriban falla
```

> **Nota:** overloads tipados en C# (`Render(RaceResultEmailModel)`, etc.) son azúcar sobre el mismo algoritmo; la interfaz expone al menos un método genérico + extensiones tipadas.

### 2.5 Extensión de `INotificationSender`

```
INTERFACE INotificationSender:
    Channel : NotificationChannel

    // Firma extendida — html es opcional para retrocompat (AC-05)
    SendAsync(
        destino : string,
        text    : string,
        subject : string? = null,
        html    : string? = null,
        ct      : CancellationToken = default
    ) → NotificationSendResult
```

**WhatsApp (`StubWhatsAppSender`):** ignora `html`, envía solo `text`.  
**Email (`ResendEmailSender`):** incluye `html` en payload si no es null/empty.

---

## 3. Algoritmo central — `ScribanEmailTemplateRenderer`

**Complejidad:** O(1) lookup de plantilla + O(n) render donde n = tamaño del template (~5–15 KB).  
**Target:** < 5 ms por render en CPU moderna, sin I/O (EC-05).  
**Cubre:** AC-01, AC-02, AC-04, RF-07, RF-08

```
CLASS ScribanEmailTemplateRenderer IMPLEMENTS IEmailTemplateRenderer:

    STATIC templates : Dictionary<EmailTemplateType, Template>  // cargados en ctor, cacheados
    STATIC layout    : Template                                   // _Layout.html

    CONSTRUCTOR():
        layout = LoadEmbedded("Templates/_Layout.html")
        FOR EACH type IN EmailTemplateType:
            templates[type] = LoadEmbedded($"Templates/{type}.html")

    FUNCTION LoadEmbedded(path) → Template:
        stream = Assembly.GetManifestResourceStream(path)
        IF stream IS NULL:
            THROW TemplateRenderException("Plantilla no encontrada: {path}")
        content = ReadAllText(stream)
        parsed = Template.Parse(content)
        IF parsed.HasErrors:
            THROW TemplateRenderException(parsed.Messages)
        RETURN parsed
        // Complejidad: O(1) amortizado — solo en startup / primera instancia (singleton)

    FUNCTION Render(template, model) → RenderedEmail:
        // --- validación de entrada ---
        IF model IS NULL:
            THROW ArgumentNullException

        IF NOT templates.ContainsKey(template):
            THROW TemplateRenderException("Tipo de plantilla desconocido")

        // --- preparar contexto Scriban con escape automático ---
        scriptModel = BuildScriptModel(template, model)
        // BuildScriptModel aplica HtmlEncode a strings dinámicos (AC-04, EC-02, EC-09)

        // --- render parcial ---
        TRY:
            bodyHtml = templates[template].Render(scriptModel)
        CATCH ScribanException ex:
            THROW TemplateRenderException("Error renderizando {template}", ex)

        // --- composición con layout ---
        layoutModel = {
            content      : bodyHtml,           // ya escapado vía scriptModel
            year         : DateTime.UtcNow.Year,
            product_name : scriptModel.product_name,
            logo_svg     : EmailDesignTokens.LogoSvgInline  // constante estática
        }
        html = layout.Render(layoutModel)

        // --- generar text fallback ---
        text = BuildPlainText(template, scriptModel)

        subject = ResolveSubject(template, scriptModel)

        RETURN new RenderedEmail(html, text, subject)
        // Complejidad total: O(n), sin allocaciones grandes (< 100 KB, RNF-02)
```

### 3.1 `BuildScriptModel` — mapeo por tipo

```
FUNCTION BuildScriptModel(template, model) → ScriptObject:

    base = { product_name: "NicaRunner" }

    SWITCH template:
        CASE RaceResult:
            m = CAST model AS RaceResultEmailModel
            RETURN {
                ...base,
                recipient_name : HtmlEncode(m.RecipientName),
                race_name      : HtmlEncode(m.RaceName),
                position       : m.Position,                    // int, no encode
                arrival_time   : FormatTime(m.ArrivalTime),     // "HH:mm:ss" (EC-06)
            }

        CASE PasswordReset:
            m = CAST model AS PasswordResetEmailModel
            IF NOT IsValidHttpUrl(m.ResetUrl):
                THROW TemplateRenderException("ResetUrl inválida")
            RETURN {
                ...base,
                recipient_name : HtmlEncode(m.RecipientName),
                reset_url      : HtmlEncode(m.ResetUrl),        // encode & en query
                expires_minutes: m.ExpiresMinutes,
            }

        CASE WelcomeAccount:
            m = CAST model AS WelcomeAccountEmailModel
            RETURN {
                ...base,
                product_name   : HtmlEncode(m.ProductName),
                recipient_name : HtmlEncode(m.RecipientName),
                temp_password  : HtmlEncode(m.TempPassword),
            }
```

### 3.2 `BuildPlainText` — fallback (RF-05, AC-01, AC-06)

```
FUNCTION BuildPlainText(template, scriptModel) → string:

    SWITCH template:
        CASE RaceResult:
            RETURN $"Hola {scriptModel.recipient_name}, tu resultado en {scriptModel.race_name} fue: " +
                   $"posición {scriptModel.position}, tiempo {scriptModel.arrival_time}. ¡Gracias por participar!"
            // Nota: recipient_name ya está HtmlEncoded; para text usar versión raw del model
            // IMPLEMENTACIÓN: BuildPlainText recibe model original + scriptModel, o
            //                 mantener campos raw en ScriptObject (raw_name, display_name)

        CASE PasswordReset:
            RETURN $"Hola {rawName}, recibimos una solicitud para restablecer tu contraseña.\n\n" +
                   $"Este link es válido por {expires} minutos:\n{rawUrl}\n\n" +
                   "Si no solicitaste esto, ignora este correo."

        CASE WelcomeAccount:
            RETURN $"Hola {rawName}, se creó tu cuenta en {product}.\n" +
                   $"Tu contraseña temporal es: {rawPassword}\n\n" +
                   "Deberás cambiarla al iniciar sesión por primera vez."
```

> **Decisión:** `ScriptObject` lleva pares `{ raw, encoded }` para campos de texto:
> `recipient_name` (encoded, para HTML) y `recipient_name_raw` (para text). Evita doble encode y preserva acentos en text/plain.

### 3.3 `ResolveSubject`

```
FUNCTION ResolveSubject(template, scriptModel) → string:

    SWITCH template:
        CASE RaceResult:     RETURN "Tu resultado en NicaRunner"
        CASE PasswordReset:  RETURN "Restablece tu contraseña de NicaRunner"
        CASE WelcomeAccount: RETURN "Tu cuenta en NicaRunner Backoffice"
```

---

## 4. Algoritmo — escape y URLs seguras

**Cubre:** AC-04, AC-07, EC-02, EC-03, EC-04, EC-09, RF-09, RF-10

### 4.1 `HtmlEncode`

```
FUNCTION HtmlEncode(value) → string:
    IF value IS NULL OR EMPTY: RETURN ""
    RETURN WebUtility.HtmlEncode(value)
    // O(1) por carácter, ~O(n) total
    // "<script>" → "&lt;script&gt;" (EC-02)
```

### 4.2 `BuildResetUrl`

```
FUNCTION BuildResetUrl(baseUrl, token) → string:
    IF string.IsNullOrWhiteSpace(baseUrl):
        THROW InvalidOperationException("Frontend:BaseUrl no configurado")  // AC-07

    normalized = baseUrl.TrimEnd('/')                     // EC-04
    url = $"{normalized}/reset-password?token={Uri.EscapeDataString(token)}"

    IF NOT IsValidHttpUrl(url):
        THROW InvalidOperationException("URL de reset inválida")

    RETURN url
    // O(1)
```

### 4.3 `IsValidHttpUrl`

```
FUNCTION IsValidHttpUrl(url) → bool:
    RETURN Uri.TryCreate(url, UriKind.Absolute, out uri)
       AND (uri.Scheme == "http" OR uri.Scheme == "https")
```

---

## 5. Algoritmo — `ResendEmailSender` (payload dual)

**Cubre:** AC-05, AC-01, RF-06

```
FUNCTION SendAsync(destino, text, subject?, html?, ct) → NotificationSendResult:

    IF ApiKey IS NULL OR WHITESPACE:
        RETURN Failure("Falta configurar Resend:ApiKey...")
        // ERROR PATH: config faltante — no retry útil

    payload = {
        from    : FromEmail,
        to      : [destino],
        subject : subject ?? DefaultSubject,
        text    : text,
    }

    IF html IS NOT NULL AND html.Length > 0:
        payload.html = html
    // ELSE: solo text — retrocompat (AC-05)

    TRY:
        response = POST "https://api.resend.com/emails" payload
        IF response.IsSuccessStatusCode:
            RETURN Success
        ELSE:
            detail = ExtractErrorMessage(response.Body) ?? statusCode
            RETURN Failure(detail)
            // ERROR PATH: 4xx/5xx Resend — NotificationLog queda Fallida, reintento vía cron
    CATCH HttpRequestException ex:
        RETURN Failure("No se pudo contactar a Resend: {ex.Message}")
        // ERROR PATH: red — reintento vía cron (EC-10)
```

**Complejidad:** O(1) local + O(network) I/O dominante.

---

## 6. Migración de servicios emisores

### 6.1 `AuthService.ForgotPasswordAsync` (EM-02)

**Cubre:** AC-01, AC-07, EC-03, EC-04

```
FUNCTION ForgotPasswordAsync(request):
    user = GetByEmail(request.Email)
    IF user IS NULL: RETURN silently                    // anti-enumeración
    IF user.Provider != Local: RETURN silently

    user.PasswordResetToken = GenerateToken()
    user.PasswordResetTokenExpiry = UtcNow + 30min
    SaveChanges()

    emailSender = senders.First(Email)
    IF emailSender IS NULL: RETURN                      // sin sender registrado

    TRY:
        resetUrl = BuildResetUrl(frontend.BaseUrl, user.PasswordResetToken)
        model = PasswordResetEmailModel(user.Nombre, resetUrl, 30)
        rendered = templateRenderer.Render(PasswordReset, model)
        result = emailSender.SendAsync(user.Email, rendered.Text, rendered.Subject, rendered.Html)
        // No propagar fallo al usuario — token ya guardado; log opcional futuro
    CATCH InvalidOperationException ex WHEN ex.Message contains "BaseUrl":
        // AC-07: no enviar correo roto; token queda válido pero sin notificación
        RETURN
    CATCH TemplateRenderException:
        RETURN  // fallo interno — no exponer al caller
```

### 6.2 `UserManagementService.CreateAsync` (EM-03)

**Cubre:** AC-01, EC-09

```
FUNCTION CreateAsync(request):
    tempPassword = GenerateTempPassword()
    user = new User { ..., PasswordHash = Hash(tempPassword), MustChangePassword = true }
    Add(user); SaveChanges()

    emailSender = senders.First(Email)
    IF emailSender IS NULL: RETURN ToDto(user)

    model = WelcomeAccountEmailModel(user.Nombre, tempPassword)
    rendered = templateRenderer.Render(WelcomeAccount, model)
    emailSender.SendAsync(user.Email, rendered.Text, rendered.Subject, rendered.Html)

    RETURN ToDto(user)
    // ERROR PATH Resend fallido: usuario ya creado — mismo comportamiento que hoy (fire-and-forget)
```

### 6.3 `NotificationService` (EM-01)

**Cubre:** AC-01, AC-06, EC-05, EC-07, EC-10

```
// Reemplaza BuildMessage() por BuildRenderedEmail()

FUNCTION BuildRenderedEmail(race, runner, result) → RenderedEmail:
    model = RaceResultEmailModel(runner.Nombre, race.Nombre, result.Posicion, result.TiempoLlegada)
    RETURN templateRenderer.Render(RaceResult, model)

FUNCTION CreatePendingLogsAsync(race, runner, result):
    rendered = BuildRenderedEmail(race, runner, result)
    // Mensaje en log = rendered.Text (AC-06, auditoría legible)
    log.Mensaje = rendered.Text
    ...

FUNCTION AttemptSendAsync(log):
    ...
    IF channel == Email:
        // Re-render en cada intento (EC-10) — datos del corredor pueden haber cambiado
        rendered = RebuildFromLog(log)  // ver abajo
        sendResult = sender.SendAsync(destino, rendered.Text, rendered.Subject, rendered.Html)
    ELSE IF channel == WhatsApp:
        sendResult = sender.SendAsync(destino, log.Mensaje)  // sin html

FUNCTION RebuildFromLog(log) → RenderedEmail:
    // Opción A (preferida): guardar ResultId y re-fetch race/runner/result al enviar
    (race, runner, result) = LoadContextFromLog(log)
    RETURN BuildRenderedEmail(race, runner, result)
    // Complejidad: O(1) DB reads — aceptable en cron (EC-05, EC-10)
    // Alternativa descartada: guardar HTML en log — infla DB, stale design
```

---

## 7. Estructura Scriban de plantillas

### 7.1 `_Layout.html` (slot composition)

```html
<!-- Pseudocódigo Scriban -->
<!DOCTYPE html>
<html>
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width">
  <style>/* tokens inline de EmailDesignTokens */</style>
</head>
<body style="background: {{ bg_app }};">
  <table width="600" align="center">
    <tr><!-- HEADER: gradient navy, {{ logo_svg }}, {{ product_name }} --></tr>
    <tr><!-- BODY: {{ content }} — HTML ya renderizado del partial --></tr>
    <tr><!-- FOOTER: © {{ year }} --></tr>
  </table>
</body>
</html>
```

### 7.2 `RaceResult.html` (partial)

```html
<p>Hola, {{ recipient_name }}</p>
<p>Tu resultado en <strong>{{ race_name }}</strong>:</p>
<table><!-- badge posición + monospace tiempo --></table>
<p>¡Gracias por participar!</p>
```

### 7.3 `PasswordReset.html`

```html
<p>Hola, {{ recipient_name }}</p>
<p>Recibimos una solicitud para restablecer tu contraseña.</p>
<a href="{{ reset_url }}" style="/* gradient CTA */">Restablecer contraseña</a>
<p class="alert">Este enlace expira en {{ expires_minutes }} minutos.</p>
<p>Si no solicitaste esto, ignora este correo.</p>
```

### 7.4 `WelcomeAccount.html`

```html
<p>Hola, {{ recipient_name }}</p>
<p>Se creó tu cuenta en {{ product_name }}.</p>
<p>Contraseña temporal:</p>
<code>{{ temp_password }}</code>
<p>Deberás cambiarla al iniciar sesión por primera vez.</p>
```

---

## 8. Registro DI (`Program.cs`)

```
builder.Services.AddSingleton<IEmailTemplateRenderer, ScribanEmailTemplateRenderer>()
// Singleton: templates parseados una vez (cache en memoria)

// ResendEmailSender sin cambio de registro
builder.Services.AddHttpClient<ResendEmailSender>(...)
builder.Services.AddScoped<INotificationSender>(sp => sp.GetRequiredService<ResendEmailSender>())
```

**Inyección en servicios:**

```
AuthService(..., IEmailTemplateRenderer templateRenderer, ...)
UserManagementService(..., IEmailTemplateRenderer templateRenderer, ...)
NotificationService(..., IEmailTemplateRenderer templateRenderer, ...)
```

---

## 9. Diagrama de flujo completo

```
┌─────────────┐     model      ┌──────────────────────────┐
│ AuthService │───────────────►│ IEmailTemplateRenderer   │
│ UserMgmt    │                │  1. BuildScriptModel     │
│ NotifSvc    │                │  2. Render partial       │
└─────────────┘                │  3. Render layout        │
                               │  4. BuildPlainText       │
                               └───────────┬──────────────┘
                                           │ RenderedEmail
                                           ▼
                               ┌──────────────────────────┐
                               │ INotificationSender      │
                               │  ResendEmailSender       │
                               │  POST { html, text }     │
                               └───────────┬──────────────┘
                                           │
                                           ▼
                                    Resend API
```

---

## 10. Rutas de error explícitas

| # | Punto de fallo | Comportamiento | Reintento | AC/EC |
|---|----------------|----------------|-----------|-------|
| E-01 | `Frontend:BaseUrl` vacío | No envía EM-02; token guardado | N/A | AC-07 |
| E-02 | Plantilla embedded no encontrada | `TemplateRenderException` en startup (ctor) | No | — |
| E-03 | Scriban syntax error en template | Fallo en startup al parsear | No | — |
| E-04 | Scriban render runtime error | Catch → no envía; log Fallida si en NotificationService | Sí (cron) | EC-10 |
| E-05 | `Resend:ApiKey` faltante | `NotificationSendResult(false, ...)` | Sí | — |
| E-06 | Resend HTTP 4xx/5xx | Fallida + Error en NotificationLog | Sí (≤5 intentos) | EC-10 |
| E-07 | Resend network error | Fallida + mensaje red | Sí | EC-10 |
| E-08 | XSS en nombre (`<script>`) | HtmlEncode neutraliza en HTML | N/A | AC-04, EC-02 |
| E-09 | Sin email sender registrado | Skip envío silencioso (Auth/UserMgmt) | N/A | existente |
| E-10 | Corredor sin email al reintento | Fallida "destino inválido" | No (MaxIntentos) | existente |
| E-11 | HTML > 100 KB | Unlikely; assert en test si crece | N/A | RNF-02 |

---

## 11. Anotaciones de complejidad

| Operación | Complejidad | Notas |
|-----------|-------------|-------|
| LoadEmbedded (startup) | O(t) por template | t = tamaño template; una vez |
| Render HTML | O(n) | n ≈ 10–50 KB output |
| BuildPlainText | O(1) | string concat fija |
| HtmlEncode por campo | O(m) | m = longitud campo |
| BuildResetUrl | O(1) | |
| SendAsync Resend | O(1) + network | dominado por I/O |
| RebuildFromLog | O(1) DB | 3 reads por notificación |
| ProcessPendingAsync (N logs) | O(N) × render | EC-05: N=100 → ~500ms render total |

---

## 12. Plan de tests (input para Fase 4)

```
DESCRIBE ScribanEmailTemplateRenderer:

    TEST RaceResult_RendersHtmlWithBrandColors:
        model = { Name: "María", Race: "10K", Pos: 3, Time: 01:23:45 }
        result = Render(RaceResult, model)
        ASSERT result.Html CONTAINS "#0D47A1"      // header navy (AC-02)
        ASSERT result.Html CONTAINS "María"         // acento preservado
        ASSERT result.Text CONTAINS "posición 3"

    TEST RaceResult_EscapesXss:
        model = { Name: "<script>alert(1)</script>", ... }
        result = Render(RaceResult, model)
        ASSERT result.Html NOT CONTAINS "<script>"
        ASSERT result.Html CONTAINS "&lt;script&gt;"   // AC-04

    TEST PasswordReset_CtaContainsUrl:
        ...

    TEST BuildResetUrl_TrailingSlash_Normalized:
        url = BuildResetUrl("https://x.com/", "tok")
        ASSERT url == "https://x.com/reset-password?token=tok"  // EC-04

    TEST BuildResetUrl_EmptyBase_Throws:
        ASSERT THROWS BuildResetUrl("", "tok")  // AC-07

DESCRIBE ResendEmailSender:

    TEST Send_WithHtml_IncludesBothFieldsInPayload:   // AC-05
    TEST Send_WithoutHtml_SendsTextOnly:              // AC-05 retrocompat

DESCRIBE NotificationService:

    TEST NotifyResult_LogMensajeIsPlainText:          // AC-06
    TEST ProcessPending_ReRendersOnRetry:             // EC-10
```

---

## 13. Trazabilidad AC ↔ pseudocódigo

| AC | Secciones que lo cubren |
|----|-------------------------|
| AC-01 | §3, §6.1–6.3, §7 |
| AC-02 | §3, §7.1 (tokens inline) |
| AC-03 | §7 (tablas HTML, inline styles) — verificación manual Fase 4 |
| AC-04 | §3.1, §4.1, §10 E-08 |
| AC-05 | §2.5, §5 |
| AC-06 | §6.3 CreatePendingLogsAsync |
| AC-07 | §4.2, §6.1 E-01 |

---

## 14. Gate Fase 2 — autochequeo

| Criterio del gate | Requerido | Encontrado | Estado |
|-------------------|-----------|------------|--------|
| Pseudocódigo cubre todos los AC | 7 AC | §13 mapea 7/7 | ✓ |
| Rutas de error explícitas | Sí | §10 (11 rutas) | ✓ |
| Complejidad anotada | Sí | §3, §11 | ✓ |

**Blockers:** ninguno.

---

## 15. Próximo paso

Fase 3 — Architecture: diagrama de componentes final, contratos C# exactos, EmbeddedResource setup en `.csproj`, y decisión de registro DI singleton vs scoped.
