# Database setup in Visual Studio (Package Manager Console)

Projects: migrations live in **RiseUp.DataAccess**, the startup project is **RiseUp.Web.UI**, the DbContext is **RiseUpDbContext**.
Default database: `Server=(localdb)\MSSQLLocalDB;Database=RiseUp` (from `src/RiseUp.Web.UI/appsettings.json` → `ConnectionStrings:RiseUp`).

## 1. One-time
1. Open `RiseUp.slnx` in Visual Studio 2022 (17.14+) / 2026.
2. Right-click **RiseUp.Web.UI** → *Set as Startup Project*.
3. Tools → NuGet Package Manager → **Package Manager Console**. Set *Default project* to **RiseUp.DataAccess**.
4. Build the solution (Ctrl+Shift+B).

## 2. Create the database (first time)
The `InitialCreate` migration is already included, so you only need:
```powershell
Update-Database -Project RiseUp.DataAccess -StartupProject RiseUp.Web.UI
```

## 3. Everyday commands
```powershell
# After changing an entity / configuration
Add-Migration AddPitchPackages -Project RiseUp.DataAccess -StartupProject RiseUp.Web.UI
Update-Database -Project RiseUp.DataAccess -StartupProject RiseUp.Web.UI

# Undo the last migration (only if it has NOT been applied yet)
Remove-Migration -Project RiseUp.DataAccess -StartupProject RiseUp.Web.UI

# Already applied? Roll the database back first, then remove
Update-Database InitialCreate -Project RiseUp.DataAccess -StartupProject RiseUp.Web.UI
Remove-Migration -Project RiseUp.DataAccess -StartupProject RiseUp.Web.UI

# Start over completely (deletes the RiseUp database!)
Drop-Database -Project RiseUp.DataAccess -StartupProject RiseUp.Web.UI
Update-Database -Project RiseUp.DataAccess -StartupProject RiseUp.Web.UI

# Useful
Get-Migration    -Project RiseUp.DataAccess -StartupProject RiseUp.Web.UI
Script-Migration -Idempotent -Project RiseUp.DataAccess -StartupProject RiseUp.Web.UI -Output database\RiseUp.sql
```
Tip: `-Context RiseUpDbContext` can be added to any command; it is optional because there is only one DbContext.

## 4. Seed (demo) data
Demo users, startups, investors, posts and pitches are inserted by `DemoDataSeeder` on first run (not by a migration),
so after `Update-Database` just run the site with the **"RiseUp (SQL Server LocalDB)"** launch profile (dropdown next to the ▶ button).
The seeder only inserts when tables are empty and never runs in Production.

## 5. Same thing from a terminal (dotnet CLI)
```bash
dotnet tool install --global dotnet-ef --version 10.0.14
dotnet ef database update     --project src/RiseUp.DataAccess --startup-project src/RiseUp.Web.UI
dotnet ef migrations add Name --project src/RiseUp.DataAccess --startup-project src/RiseUp.Web.UI
dotnet ef migrations remove   --project src/RiseUp.DataAccess --startup-project src/RiseUp.Web.UI
```

## 6. Using a different SQL Server
Edit `ConnectionStrings:RiseUp` in `src/RiseUp.Web.UI/appsettings.json`, e.g.
`Server=.\SQLEXPRESS;Database=RiseUp;Trusted_Connection=True;TrustServerCertificate=True`
or `Server=localhost;Database=RiseUp;User Id=sa;Password=...;TrustServerCertificate=True`.
The migration tools read the same setting (or the env var `RISEUP_MIGRATIONS_CONNECTION`, which wins).

## Troubleshooting
| Error | Fix |
|---|---|
| *The term 'Add-Migration' is not recognized* | Build once, then restart VS so the Microsoft.EntityFrameworkCore.Tools package loads in PMC. |
| *Your startup project doesn't reference Microsoft.EntityFrameworkCore.Design* | Make sure **RiseUp.Web.UI** is the startup project (it now references EF Tools). |
| *Build failed* | PMC builds first; fix compile errors in the Error List, then retry. |
| *A network-related or instance-specific error… (localdb)* | Install "SQL Server Express LocalDB" via the VS Installer, or run `sqllocaldb start MSSQLLocalDB`. |
| *There is already an object named … in the database* | The DB was created another way. `Drop-Database`, then `Update-Database`. |
| Site still shows the SQLite demo | Pick the **RiseUp (SQL Server LocalDB)** launch profile; the default profile uses SQLite. |
