# Branch protection en `main`

Hoy `main` está desprotegida: cualquier `git push origin main` (o un merge de
PR sin CI verde) llega directo a producción vía el auto-deploy de Render. Esto
necesita protegerse antes de seguir agregando features.

Yo no puedo activar esto vía API en esta sesión — tenés que hacerlo desde la
UI de GitHub.

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
