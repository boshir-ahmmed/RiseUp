# Run the RiseUp demo on Microsoft SQL Server

The solution includes versioned migrations and a development demo. One Package Manager Console command applies the schema, creates the fictional data, and imports the bundled media. You can run it again after pulling updates.

## Connect Visual Studio Community to your instance

1. In SSMS, connect to the instance you intend to use. Copy the **Server name** and note the authentication method. The steps below use Windows Authentication and a dedicated database named **RiseUpDemo**. Your Windows account needs permission to create the database, or an administrator can create an empty database and grant you permission to apply its schema and write data.
2. Pull the updated main branch and open **RiseUp.slnx** in Visual Studio Community 2026 with the **ASP.NET and web development** workload and .NET 10 SDK. In Solution Explorer, right-click **RiseUp.Web.UI** and choose **Set as Startup Project**.
3. Right-click **RiseUp.Web.UI → Manage User Secrets**. Add the following to the JSON object, keeping any settings already there. Replace the server value with the one from SSMS. The double backslash in JSON represents a single backslash in the instance name.

   ```json
   {
     "Database:Provider": "SqlServer",
     "ConnectionStrings:RiseUp": "Server=.\\SQLEXPRESS;Database=RiseUpDemo;Integrated Security=True;Encrypt=True;TrustServerCertificate=True"
   }
   ```

   For a default local instance, use `Server=localhost`. For LocalDB, use `Server=(localdb)\\MSSQLLocalDB` in JSON. For SQL Server Authentication, replace `Integrated Security=True` with `User ID=your_login;Password=your_password` in User Secrets. The certificate setting shown is for local development. The site and the EF tools both read these user secrets. If you previously set `RISEUP_MIGRATIONS_CONNECTION` or `ConnectionStrings__RiseUp`, remove the override or point it to the same database.
4. Build the solution. To browse tables inside Visual Studio, open **View → SQL Server Object Explorer → Add SQL Server** and connect with the same instance name and authentication. You can also use SSMS.

## Install the demo

Open **Tools → NuGet Package Manager → Package Manager Console** and run these lines. Allow the command to finish before starting the web app.

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"
Update-Database -Project RiseUp.DataAccess -StartupProject RiseUp.Web.UI -Context RiseUpDbContext -Args '--seed-demo'
```

The existing migrations already describe the schema. Skip `Remove-Migration` and `Add-Migration` for demo setup. The import supports an empty database or an existing RiseUp demo. If you point it at an unrelated populated RiseUp installation, select a separate demo database. The demo uses shared test credentials.

The database holds the records and media references. Media bytes live in **src/RiseUp.Web.UI/App_Data/private** by default. Keep this folder with the database when moving or backing up the demo. You can set `Storage:RootPath` to an absolute directory in the same User Secrets to keep media outside the checkout. Keep that location consistent between the import and the site.

## Run and verify

Choose the **RiseUp (SQL Server)** launch profile beside the green Run button and press **F5**. The development banner displays the provider. Sign in as founder **farhan@nodifreight.example** or investor **sadia@padmavc.example** using **Demo#2026pass**.

- Open **Post & Reels** as the founder to see Nodi Freight's photo post and vertical reel. Visit **Reels** to see clips from the other demo startups.
- Open the startup and investor directories for fictional accounts, and Pitch Studio for existing pitch and demo videos. Portraits and field photographs are synthetic. The four nine-second portrait reels are animated photo stories.
- In SSMS or SQL Server Object Explorer, refresh **Databases → RiseUpDemo → Tables**. Run this query against the demo database to check the new media.

```sql
SELECT DB_NAME() AS CurrentDatabase;
SELECT COUNT(*) AS ProfilePhotos FROM dbo.MemberProfiles WHERE PhotoMediaId IS NOT NULL;
SELECT p.LiveTitle, m.FileName, m.DurationSeconds
FROM dbo.Posts AS p
JOIN dbo.MediaAssets AS m ON m.Id = p.VideoMediaId
WHERE m.FileName LIKE 'reel-%.mp4';
```

A fresh demo has 19 member portraits, eight startups with logos and covers, and four gallery reels. Repeating the import preserves existing demo edits and balances.

## Find a newly registered profile

Choose **RiseUp (SQL Server)** before registering. In Visual Studio, check the application's startup output for **RiseUp registration database**. It reports the SQL Server instance and database or the SQLite file used by that process. Run this query in the same SQL Server database, replacing the name with the one used at registration:

```sql
SELECT DB_NAME() AS DatabaseName, SERVERPROPERTY('ServerName') AS ServerName;
SELECT u.Id, u.Email, u.FullName, s.Name AS Startup
FROM dbo.UserAccounts u
LEFT JOIN dbo.Startups s ON s.OwnerAccountId = u.Id
WHERE u.FullName = N'Farhan Kabir';
```

The default **RiseUp (configured database)** profile selects SQL Server when a development SQL connection is configured and otherwise uses the local SQLite demo. Each provider stores its own accounts. An account created while using the SQLite demo stays in that file when you switch to SQL Server. To make SQL Server your normal development target, set `"Database:Provider": "SqlServer"` alongside `ConnectionStrings:RiseUp` in the web project's User Secrets and choose **RiseUp (configured database)** or **RiseUp (SQL Server)**.

## Other migration commands

For schema-only setup, omit `-Args '--seed-demo'`. The default Development launch profiles also initialize the demo on startup. To run a schema-only Development site, set `Database:InitializeOnStartup` to `false` in User Secrets.

Use `Get-Migration -Project RiseUp.DataAccess -StartupProject RiseUp.Web.UI` to see the migration list. Use `Add-Migration YourSchemaChange -Project RiseUp.DataAccess -StartupProject RiseUp.Web.UI` after making a new entity or configuration change. `Remove-Migration` is for undoing a newly scaffolded, unapplied migration during development.

The equivalent demo command from the solution directory, with .NET EF tools 10.x installed and the environment set to Development, is:

```powershell
dotnet ef database update --project src/RiseUp.DataAccess --startup-project src/RiseUp.Web.UI --context RiseUpDbContext -- --seed-demo
```

## Troubleshooting

| Symptom | What to check |
|---|---|
| PMC does not recognize Update-Database | Restore NuGet packages, build the solution and reopen Package Manager Console. |
| Cannot connect or login fails | Try the same server name and authentication in SSMS. Check database creation and schema permissions. |
| Site shows SQLite or a different data set | Select the SQL Server launch profile. Check the startup project’s User Secrets and environment overrides. |
| Database already contains objects and EF reports a conflict | Check the selected database and its `__EFMigrationsHistory`. A new, empty demo database avoids conflicts with another schema. |
| Videos or photos return 404 after moving the database | Restore the matching private media directory and configure its path. Bundled source files are in `DemoMedia`. |
