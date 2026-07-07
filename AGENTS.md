# AGENTS.md

## Cursor Cloud specific instructions

NicaRunner is a race-timing/results system: a **.NET 8 ASP.NET Core API** (`src/NicaRunner.Api`, clean-architecture solution `NicaRunner.sln` + xUnit tests in `tests/`) and a **React 19 + Vite frontend** (`frontend/`, npm workspaces; shared UI lib `frontend/packages/ui` = `@nicarunner/ui`). The canonical build/test recipe is `.github/workflows/api-ci.yml`; run commands live in `frontend/package.json`.

### Toolchain / environment
- The **.NET 8 SDK (8.0.100)** is installed at `~/.dotnet` (not via apt). Interactive shells pick it up through `~/.bashrc`; non-interactive contexts (e.g. the startup update script) should use the full path `"$HOME/.dotnet/dotnet"`.
- `dotnet-ef` is installed as a global tool at `~/.dotnet/tools` (needed for EF migrations).
- The update script only refreshes deps (`dotnet restore NicaRunner.sln`, `npm --prefix frontend install`); it does NOT start services or apply migrations.

### Database & migrations (non-obvious)
- Development uses **SQLite** automatically (`src/NicaRunner.Api/nicarunner.dev.db`); no external DB needed. Production uses PostgreSQL.
- Migrations are **auto-applied only in non-Development** (`Program.cs`). In Development you MUST apply them manually before first run:
  `dotnet ef database update --project src/NicaRunner.Infrastructure --startup-project src/NicaRunner.Api` (set `ASPNETCORE_ENVIRONMENT=Development`).

### Getting an admin login for local testing (non-obvious)
- The admin seeder only runs when `Seed:DefaultAdminPassword` is set. Start the API with `Seed__DefaultAdminPassword=<pwd>` to seed the protected admin accounts: `hilbert.mendez@gmail.com`, `evr86.skip@gmail.com`, `edufisica@ymail.com`.
- Seeded accounts have `MustChangePassword=true`, so the first login forces a password change at `/change-password` before reaching the dashboard.

### Running the services (dev)
- **API**: `ASPNETCORE_ENVIRONMENT=Development dotnet run --project src/NicaRunner.Api` → http://localhost:5190 (Swagger + email-preview endpoints enabled in dev; health at `/health`, not `/api/health`).
- **Frontend**: `npm run dev` in `frontend/` → http://localhost:5173. `dev` runs `build:ui` first, so `@nicarunner/ui` is always rebuilt — the SPA import fails without it. Vite proxies `/api` → `http://localhost:5190` (override with `VITE_API_PROXY_TARGET`).

### Lint / test
- Backend tests: `dotnet test tests/NicaRunner.Tests/NicaRunner.Tests.csproj`.
- Frontend lint: `npm run lint` in `frontend/`. Note: the current repo has pre-existing lint errors (mostly `no-explicit-any`); a non-clean lint result is not an environment problem. There is no frontend test runner configured.
