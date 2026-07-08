# Render — setup manual post-deploy

El blueprint `render.yaml` aprovisiona solo el servicio web. La base de datos
PostgreSQL vive en [Neon](https://neon.tech) (tier gratuito permanente). Hay
cinco variables marcadas como `sync: false` que **vos** tenés que setear desde
el dashboard de Render una vez después del primer deploy. Sin estas, la API no
arranca o arranca con seguridad rota.

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

### 3. `Resend__ApiKey`

Llave de [Resend](https://resend.com/api-keys) para el envío de emails de
resultados. Crear una llave con scope `Send only` (no necesita más permisos).

Si todavía no usás Resend en producción y querés posponer el envío real, podés
dejar esta variable vacía — la API arranca igual, pero `POST /api/notifications/notify`
va a fallar con error explícito.

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

## Después de setear las 5

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
