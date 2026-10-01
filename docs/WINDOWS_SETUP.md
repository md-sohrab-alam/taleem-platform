# Windows development notes

1. Install **Docker Desktop** (WSL2 backend recommended) — required for PostgreSQL + pgvector.
2. Install **.NET 10 SDK**, **Node 20+**, **Android Studio**.
3. From repo root:

```powershell
docker compose up -d db
cd backend
dotnet run --project src/Taleem.Api
```

4. Admin: `cd admin-web; npm install; npm run dev` → http://localhost:3000  
5. Web: `cd web; npm install; npm run dev` → http://localhost:3001  
6. Android Studio → open `android/` → run `developmentDebug`

If NuGet downloads fail behind a corporate proxy, configure `HTTP_PROXY`/`HTTPS_PROXY` or a local NuGet cache.

Generate EF migrations when `dotnet-ef` is available:

```powershell
dotnet tool install -g dotnet-ef
dotnet ef migrations add InitialCreate --project src/Taleem.Infrastructure --startup-project src/Taleem.Api --output-dir Persistence/Migrations
```

Until then, the API uses `EnsureCreated` on first boot.
