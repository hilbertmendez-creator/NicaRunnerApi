# Branch protection en `main`

Hoy `main` está desprotegida: cualquier `git push origin main` (o un merge de
PR sin CI verde) llega directo a producción vía el auto-deploy de Render. Esto
necesita protegerse antes de seguir agregando features.

Yo no puedo activar esto vía API en esta sesión — tenés que hacerlo desde la
UI de GitHub.

## Pasos

`Settings` → `Branches` → `Branch protection rules` → `Add rule`.

**Branch name pattern**: `main`

Activar exactamente estas opciones (marcadas con `[x]`):

```
[x] Require a pull request before merging
    [x] Require approvals — minimum: 1
        (Si trabajás solo, esto te obliga a auto-aprobar tus PRs desde la UI;
         es fricción intencional para que pares y leas el diff antes de mergear.)
    [x] Dismiss stale pull request approvals when new commits are pushed
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
     un push directo. Activar incluso si sos el único admin.)

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

## Cuando agregues más checks de CI

Cada nuevo workflow que querés que sea bloqueante (ej. el futuro
`contract-validation`, lint del frontend, etc.) hay que agregarlo a mano a la
lista de "Status checks required" en la misma página. GitHub no los toma
automáticamente.

## Si sos el único maintainer

La regla de "1 approval" te va a obligar a abrir cada PR desde una rama,
esperar que CI corra, y aprobar tu propio PR antes de mergear. Eso **es bueno**
— es el momento en que releés el diff con cabeza fría antes de que entre a
producción. Si te resulta insoportable cuando tengas que pushear un hotfix
genuino:

- Opción A (recomendada): mantenés la regla, y para hotfix abrís PR igual con
  label `hotfix` para autoidentificación. Te toma 2 minutos extra y vale.
- Opción B: bajás `approvals` a `0` pero mantenés `Require status checks`.
  Perdés el review pero seguís protegido contra CI roto.
