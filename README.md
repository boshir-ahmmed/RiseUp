> **Structure update (18 Nov 2026):** This project has been reorganized into CRM-style
> `Interface` / `Implementation` folders under both `RiseUp.Facade` and `RiseUp.DataAccess`,
> with dedicated Facade `DTOs` and `Validators`. See [ARCHITECTURE_REFACTOR.md](ARCHITECTURE_REFACTOR.md)
> for the actual paths, cross-project compile-link explanation, and Visual Studio build instructions.

# RiseUp — Verified Startup–Investor Network (Bangladesh)

ASP.NET Core 10 MVC + Razor + C# + EF Core 10 + SQL Server, organised in the same layered style as the legacy
`CRM` solution (`Framework / Entities / DataAccess / Facade / Web.UI`) but with the architecture corrections required
by the RiseUp master prompt (see `ARCHITECTURE.md`).

> All organisations and people in the demo data are fictional. Content screening and matching run locally in the
> `RiseUp.AI` sandbox. The demo payment gateway also runs in sandbox mode.

## Quick start (no SQL Server needed)

```bash
dotnet run --project src/RiseUp.Web.UI          # Development profile: SQLite demo DB + demo data
```
Open https://localhost:7180. Demo password for every account: `Demo#2026pass`.

| Role | Sign in with |
|---|---|
| Founder (verified) | `farhan@nodifreight.example` |
| Founder (pending verification) | `ayesha@krishicloud.example` |
| Investor | `sadia@padmavc.example` (also `imran@angel.example`, `nusrat@meghnacap.example`) |
| Team member (chat + own photo only) | `rafi.ops` |
| Super / Verification / Moderator / Finance admin | `super@`, `verify@`, `moderator@`, `finance@riseup.example` — then any 6-digit code (Development only) |

## What's in v3 / v4

| Area | Where |
|---|---|
| Pitch Studio (investor pitch / demo / teaser video, deck, ≤6 documents, the ask, highlights, team, FAQ) | `/pitches` (founders) |
| Pitch Room (videos with range streaming, deck viewer, documents, 5-question Q&A, more-info, decide) | `/pitch-room/{id}` |
| Reels (public teasers), Shortlist (investor-private pipeline), Meetings (+ .ics) | `/reels`, `/shortlist`, conversations |
| Profiles for everyone: name, headline, location, bio, link, photo, cover; startup logo/cover | `/me`, `/me/edit`, startup settings |
| Name changes: rules + 30-day cooldown; verified accounts go to admin review | `/admin/profile-changes` |
| New shell: top bar with **Home** on every page + left sidebar + right rail; video posts; saved posts | `_AppLayout`, `ShellViewComponent` |

Specs: `docs/V3_PITCHING_SPEC.md`, `docs/V4_PROFILE_AND_UI_SPEC.md`. Demo videos ship in `src/RiseUp.Web.UI/DemoMedia`
and are imported into private storage by the demo seeder.

## Run against SQL Server

To install the fictional accounts, portraits, photo posts and portrait reels from Visual Studio, follow the [SQL Server demo setup](DATABASE_SETUP.md).

```bash
# LocalDB (Windows / Visual Studio): choose the "RiseUp (SQL Server)" launch profile, or:
dotnet run --project src/RiseUp.Web.UI --launch-profile "RiseUp (SQL Server)"

# Docker (SQL Server 2022 + app):
docker compose up --build        # http://localhost:8080
```
Migrations: `src/RiseUp.DataAccess/Migrations` (applied automatically when `Database:InitializeOnStartup=true`).
Idempotent scripts for DBAs (SSMS 2022): `database/RiseUp_Schema_v4_idempotent.sql` (full schema) and
`database/RiseUp_Upgrade_v2_to_v4.sql` (upgrade an existing v2 database). `RiseUp_InitialCreate.sql` is the original v2 script.

```bash
dotnet tool install --global dotnet-ef
dotnet ef migrations add <Name> --project src/RiseUp.DataAccess --startup-project src/RiseUp.DataAccess
dotnet ef database update      --project src/RiseUp.DataAccess --startup-project src/RiseUp.DataAccess
```

## Build and test

```bash
dotnet build RiseUp.slnx
dotnet test  RiseUp.slnx        # business rules, architecture rules, HTTP smoke tests
```

## Solution layout

```
RiseUp/
  RiseUp.slnx
  src/
    RiseUp.Framework/    Result<T>, error codes, IClock, Asia/Dhaka time, business limits, roles/policies, routes
    RiseUp.Entities/     Domain entities + repository ports (IRepository, IUnitOfWork) + external-service ports
    RiseUp.DataAccess/   EF Core DbContext, one configuration per entity, repositories, UnitOfWork, migrations,
                         payment gateway, malware scanner, private file storage, demo seed
    RiseUp.AI/           Local content screening and profile matching adapters (API solution folder)
    RiseUp.Facade/       Use cases by feature (Identity, Startups, Investors, Verification, ConnectionRequests,
                         Conversations, TeamAccounts, ConfidentialDocuments, Posts, Wallets, Payments, Promotions,
                         Discovery, Moderation, Notifications, Media, Profiles, Pitching) with FluentValidation and
                         object-level authorisation
    RiseUp.Web.UI/       Thin MVC controllers, Razor views (Instagram-style feed home), Admin area, SignalR chat hub,
                         security headers, rate limiting, background jobs, vanilla JS modules (no jQuery)
  tests/
    RiseUp.Facade.Tests/        61 executable acceptance tests (§17 + v3/v4 rules) on real EF Core + SQLite
    RiseUp.Architecture.Tests/  9 dependency and legacy-fix rules (NetArchTest)
    RiseUp.Web.SmokeTests/      34 HTTP tests via WebApplicationFactory
  database/              Idempotent SQL Server scripts (full v4 schema + v2→v4 upgrade)
  docs/prototype/        The interactive HTML prototype v4 (non-production) + specs in docs/
```

See `QA_REPORT.md` for the test campaign and every defect found and fixed, and `REQUIREMENTS_TRACEABILITY.md`
for the status of all 253 requirements (including what is still partial).

## Database in Visual Studio
See [DATABASE_SETUP.md](DATABASE_SETUP.md) for Package Manager Console commands (Update-Database, Add-Migration, Remove-Migration).
