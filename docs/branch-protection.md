# Branch protection en `main`

`main` está protegida. Sin protección, cualquier `git push origin main` (o un
merge de PR sin CI verde) llegaría directo a producción vía el auto-deploy de
Render.

Este documento describe la configuración objetivo y cómo llegar a ella. La
protección se administra desde la UI de GitHub (`Settings` → `Branches`), no por
API.

> El estado real de la regla —qué checks están marcados como requeridos, si
> "Require approvals" está activo— solo se ve en esa pantalla. Si editás la
> regla, actualizá también este documento: es la única forma de que no vuelva a
> quedar describiendo una configuración que ya no existe.

## ⚠️ Si sos el único maintainer (caso actual)

GitHub **no permite que el autor de un PR apruebe su propio PR**. Si activás
"Require approvals" estando solo, todos tus PRs van a quedar bloqueados para
siempre (Merging is blocked → "Review required") porque no hay nadie más con
write access para aprobarlos.

**Solución correcta para 1 persona**: activar la protección de branch **sin**
marcar "Require approvals". Seguís protegido contra:

- Push directo a `main` sin pasar por PR.
- Merge con CI roja (gracias a "Require status checks").
- Force push y deletion del branch.

Lo que perdés (innecesariamente, si sos solo) es la doble lectura humana del
diff — que cuando trabajás solo es teatro de seguridad de todas formas.

Cuando sumes a una segunda persona con write access, marcás el checkbox
"Require approvals" y volvés a tener gate de review real.

## Pasos

`Settings` → `Branches` → `Branch protection rules` → `Add rule`.

**Branch name pattern**: `main`

Activar exactamente estas opciones (marcadas con `[x]`):

```
[x] Require a pull request before merging
    [ ] Require approvals             ← DESMARCADO mientras seas solo;
                                        marcar cuando entre otro dev con
                                        write access (default GitHub: 1).
                                        Nota: NO es un selector numérico,
                                        es un checkbox — para "0 approvals"
                                        hay que desmarcarlo entero.
    [ ] Dismiss stale pull request approvals — (queda inerte si approvals está off)
    [ ] Require review from Code Owners — (saltar hasta tener CODEOWNERS)

[x] Require status checks to pass before merging
    [x] Require branches to be up to date before merging
    Status checks required:
        - build-and-test           (job de .github/workflows/api-ci.yml)
        - frontend-build-and-test  (job de .github/workflows/frontend-ci.yml)

[x] Require conversation resolution before merging

[x] Require linear history
    (Evita merges con commits de merge sucios — fuerza rebase o squash.)

[ ] Require signed commits — (opcional, agregar cuando tengas GPG configurado)
[ ] Require deployments to succeed — (no aplica hasta tener staging)

[x] Do not allow bypassing the above settings
    (CRÍTICO: sin esto, un admin de la org/repo puede saltarse las reglas con
     un push directo. Activar incluso si sos el único admin — vos también
     deberías pasar por el PR + CI verde.)

[ ] Allow force pushes — DEJAR DESACTIVADO
[ ] Allow deletions — DEJAR DESACTIVADO
```

Guardar con `Create` / `Save changes`.

## Verificación

Intentar pushear directo a main:

```bash
git checkout main
echo "test" >> README.md
git commit -am "test"
git push origin main
# → ! [remote rejected] main -> main (protected branch hook declined)
```

Si el push se rechaza, la protección está activa.

Abrir un PR de prueba con CI verde y confirmar que se puede mergear sin
approval. Si dice "Review required", quedó marcado "Require approvals" —
volver y desmarcarlo.

## Cuando agregues más checks de CI

Cada nuevo workflow que querés que sea bloqueante (ej. el futuro
`contract-validation`, lint del frontend, etc.) hay que agregarlo a mano a la
lista de "Status checks required" en la misma página. GitHub no los toma
automáticamente.

Tres reglas que se aprendieron a los golpes:

**1. Un check requerido no puede tener filtros de `paths` / `paths-ignore`.**
Si el filtro descarta el PR, el workflow no corre, el check nunca reporta y
GitHub deja el PR esperando un status que no va a llegar — sin timeout, hasta
que alguien lo destrabe a mano. `api-ci.yml` tenía `paths-ignore: frontend/**`
y por eso se lo sacamos: cualquier PR con el diff entero bajo `frontend/**`
habría quedado colgado. Si un workflow necesita filtros de path, no lo marques
como requerido.

**2. El check tiene que haber corrido al menos una vez** para aparecer en el
buscador de la pantalla (lista los checks vistos en los últimos ~7 días). O sea:
primero mergeá el workflow, después marcalo como requerido.

**3. Dos jobs no pueden compartir nombre.** La lista de required checks se arma
por nombre de check, no por workflow. Por eso el job del frontend se llama
`frontend-build-and-test` y no `build-and-test`.

## Cuando entre una segunda persona al repo

1. Volver a Settings → Branches → editar la regla.
2. Marcar `[x] Require approvals` y dejar el default de `1`.
3. Marcar `[x] Dismiss stale pull request approvals when new commits are pushed`
   (refuerza que si rebaseás o agregás commits post-approval, hay que volver
   a aprobar — evita merges sobre código que el reviewer no vio).
4. Considerar agregar un archivo `CODEOWNERS` y marcar
   `[x] Require review from Code Owners` para que ciertas áreas (ej.
   migraciones EF) requieran review de gente puntual.

## Workarounds para hotfix cuando ya tengas approvals activos

Si una urgencia requiere mergear sin esperar review:

- **Opción A (limpia)**: el reviewer aprueba rápido por mobile/Slack.
- **Opción B (bypass)**: si tenés permiso de admin, desmarcás temporalmente
  "Do not allow bypassing the above settings" → mergeás → reactivás. Dejá
  rastro en el commit/PR de por qué fue urgente.
- **NUNCA**: agregar tu PR a "Bypass list" permanentemente — anula la
  protección para esa identidad de forma silenciosa.
