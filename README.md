# Performance Hub - Backend

.NET 10 Web API (Controllers) + EF Core 10 + SQL Server LocalDB.
The frontend lives in the `performance-hub-frontend` repo.

## First-time setup (each teammate, on her own machine)
1. Clone the repo and open `PerformanceApi.csproj` / the folder in Visual Studio.
2. Copy `appsettings.Development.json.example` to `appsettings.Development.json`
   (that file is never committed). Each machine has its own local database.
3. Package Manager Console (Default project = PerformanceApi): `Update-Database`
4. Run with the **https** profile. Check the port in `Properties/launchSettings.json`
   and put it in the frontend `.env` as `VITE_API_URL`.

## Project structure
```
Controllers/   one controller per feature (ObjectivesController, AuthController, ...)
Models/        entities = database tables
Dtos/          the shapes of request/response JSON
Data/          AppDbContext (add your DbSet lines here)
Migrations/    created by EF - never edit a migration that was already applied
```
Package versions: EF Core must be **10.0.15** (version 11 does not work with net10.0).

## Migrations (important)
- Only ONE person creates a migration at a time. Say it in the group first.
- Pull `main` before creating one, and merge your PR right after.
- After pulling someone else's migration, run `Update-Database`.

## Git rules
- Never push to `main`. Work on `feature/<name>-<topic>`, open a Pull Request,
  the other teammate reviews it.
- Shared files (`Program.cs`, `AppDbContext.cs`): tell the team before changing them.
- API contract: see `docs/contract.md`.
