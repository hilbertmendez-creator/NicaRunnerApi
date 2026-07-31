# Render — setup manual post-deploy

El blueprint `render.yaml` aprovisiona solo el servicio web. La base de datos
PostgreSQL vive en [Neon](https://neon.tech) (tier gratuito permanente). Hay
varias variables marcadas como `sync: false` que **vos** tenés que setear
desde el dashboard de Render una vez después del primer deploy. Sin estas, la
API no arranca o arranca con seguridad rota.

## Variables a configurar

Render Dashboard → tu servicio `nicarunner-api` → **Environment** → **Add Environment Variable**.

### 1. `ConnectionStrings__PostgresConnection`

URI de conexión **pooled** de Neon (Dashboard → tu proyecto → Connection
details → **Pooled connection**). Formato:

```
postgresql://user:pass@ep-xxx-pooler.us-west-2.aws.neon.tech/neondb?sslmode=require
```

La API ya normaliza URIs `postgres://` / `postgresql://` a formato Npgsql en
`Program.cs`. Usar siempre el endpoint **pooler** en producción.

Para migrar datos desde Render, ver `scripts/migrate-db-to-neon.ps1`.

### 2. `Jwt__Key`

Llave simétrica HMAC que firma todos los JWT emitidos por la API. Tiene que
ser **larga, aleatoria y estable** — si cambia, todos los tokens activos
quedan inválidos (capturistas en medio de una carrera son deslogueados).

**Generar el valor una sola vez** desde una terminal local:

```bash
openssl rand -base64 64
```

Pegar el resultado entero como valor. Después no se toca más, salvo emergencia
(filtración confirmada de la llave).

### 3. `SendGrid__ApiKey`, `SendGrid__FromEmail`

Envío de emails (resultados, reset de contraseña) vía la API HTTP de
[SendGrid](https://sendgrid.com), usando **Single Sender Verification** — se
verifica una sola dirección de email, sin necesitar un dominio propio ni
configurar DNS.

> **Por qué no SMTP de Gmail ni Resend:** se probaron ambos antes de llegar
> acá. Resend en modo sandbox (sin dominio verificado) solo entrega a la
> dirección dueña de la cuenta — confirmado con un 403 en un envío real.
> SMTP directo con Gmail (puerto 587) se probó después: la conexión saliente
> se cuelga en Render (confirmado: 502 después de exactamente 30s, el
> timeout del proxy) en vez de fallar rápido — parece un puerto SMTP
> filtrado en la red del plan free. SendGrid usa HTTPS normal (mismo
> mecanismo que ya funciona con Resend), así que no tiene ese problema.

**Setup:**

1. Crear cuenta gratis en [sendgrid.com](https://sendgrid.com) (100
   emails/día gratis para siempre).
2. Dashboard → **Settings → Sender Authentication → Single Sender
   Verification** → Create New Sender, con el email que va a aparecer como
   remitente (puede ser tu Gmail personal). SendGrid manda un correo de
   confirmación a esa dirección — hay que abrirlo y confirmar antes de que
   el sender quede verificado.
3. Dashboard → **Settings → API Keys → Create API Key**, con permiso
   **Mail Send** (no hace falta Full Access).
4. En Render:
   - `SendGrid__ApiKey`: la API key del paso 3.
   - `SendGrid__FromEmail`: el email verificado en el paso 2 (tiene que
     coincidir exacto — SendGrid rechaza el envío si el remitente no está
     verificado).

Si todavía no querés habilitar el envío real, podés dejar estas variables
vacías — la API arranca igual, pero `POST /api/notifications/notify` y
"olvidé mi contraseña" van a fallar el envío en silencio (queda logueado,
ver comentario en `AuthService.ForgotPasswordAsync`).

### 4. `Cors__AllowedOrigins__0`

Origen del back office en producción (Vercel). Ejemplo:

```
https://nicarunner-web.vercel.app
```

Sin `/` final. Si necesitás más de un origen (ej. dominio custom + url
`.vercel.app`), agregá `Cors__AllowedOrigins__1`, `__2`, etc.

### 5. `Admin__CleanupSecret`

Autoriza al cron de GitHub Actions
(`.github/workflows/refresh-token-cleanup.yml`) a invocar
`POST /api/admin/refresh-tokens/cleanup`, que borra refresh tokens expirados
o revocados hace más de 7 días. Sin este secret configurado el endpoint
responde 401 a todas las peticiones (safe-by-default).

Generar con:

```bash
openssl rand -base64 32
```

Además del valor en Render, **el mismo valor** hay que ponerlo en
GitHub → Settings → Secrets and variables → Actions → **Secrets** → New
repository secret con nombre `ADMIN_CLEANUP_SECRET`. Si los dos no coinciden
el cron falla con 401 y GitHub avisa por email.

Rotación: cambiá primero el valor en Render (el cron seguirá funcionando
con el valor viejo hasta que Render redeploye), después el secret de GitHub.
Ventana de riesgo breve durante la cual el cron podría fallar una vez — no
crítico, el próximo run del día siguiente ya usa el valor nuevo.

## Variable opcional: `ConnectionStrings__Redis`

Solo hace falta si el plan de Render deja de ser `free` y el servicio escala a
más de una instancia. SignalR necesita un backplane compartido para que un
mensaje enviado desde la instancia A llegue a un cliente conectado a la
instancia B; sin esto, notificaciones en tiempo real se pierden de forma
intermitente al escalar horizontalmente. Con una sola instancia (plan actual)
esta variable no es necesaria — SignalR funciona igual sin ella.

Si escalás: provisionar un Redis (Render Key Value, Upstash, etc.) y setear
`ConnectionStrings__Redis` con la connection string. La API lo detecta solo.

## Después de setearlas

Render hace **redeploy automático** al guardar variables. Esperar a que el
servicio quede en estado `Live` y verificar:

```bash
curl https://<tu-servicio>.onrender.com/health
# → {"status":"ok"}

curl -X POST https://<tu-servicio>.onrender.com/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"test@test","password":"wrong"}'
# → 401 (confirma que JWT y EF están operativos; no llega a 500)
```

## Rotación de `Jwt__Key`

Cuando haya que rotar la llave (filtración o policy interna):

1. Generar nueva llave con `openssl rand -base64 64`.
2. Reemplazar el valor en Render → Environment.
3. Comunicar a los capturistas que **se van a desloguear** y van a tener que
   volver a entrar.

No hay rotación graceful sin agregar soporte de múltiples llaves activas en
`Program.cs` — fuera de scope para v1.
