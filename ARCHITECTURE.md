# Architecture

## Layers and dependency rule

```
Web.UI  ──►  Facade  ──►  Entities (domain + ports)  ◄──  DataAccess
   │            │                 │                          │
   └────────────┴──────►  Framework (Result, clock, rules) ◄─┘
Web.UI references DataAccess ONLY in Program.cs to call AddDataAccess().
```
Enforced by `tests/RiseUp.Architecture.Tests` (Facade has no EF Core/DataAccess/ASP.NET dependency; controllers
never touch DataAccess or EF; controllers never bind entities; repositories expose no SaveChanges).

## Request pipeline

`HTTPS → SecurityHeaders/CorrelationId → RateLimiter → Auth (cookie + policies) → thin MVC controller →
view model / request DTO → Facade use case (FluentValidation + role & object authorisation) → repositories →
IUnitOfWork (single commit, serializable transaction where race-sensitive) → SQL Server → Result<T> →
redirect + flash, or JSON with mapped status (400/401/403/404/409/429/503).`

## Corrections to the legacy CRM sample (src.zip)

| Legacy pattern | RiseUp |
|---|---|
| `Repository.AddAsync` calls `SaveChangesAsync` | Repositories never save; `IUnitOfWork` commits once per use case |
| `GetUserByEmailPasswordAsync(email, passwordHash)` predicate | PBKDF2 `PasswordHasher` verify + lockout (5 tries / 15 min) |
| Hard-coded `ClaimTypes.Role = "manager"` | Roles from the database → claims → named policies; admins need an MFA claim |
| `Login(User user)` binds an entity | View models / request DTOs only (architecture test) |
| `AddDbContext` + `UseSqlServer` inside `AddCrmFacade` | `AddDataAccess()` in DataAccess; `AddFacade()` registers use cases only |
| jQuery AJAX | Vanilla ES modules: `wwwroot/js/core/http.js` (CSRF header), `ui.js` |
| Duplicate `CRM.API` / `CRM.Web.API` WeatherForecast projects | Removed; optional API later must reuse the Facade |

## Integrity guarantees (database + transaction, not just UI)

* One startup per founder — unique `Startups.OwnerAccountId`.
* One pending request per pair regardless of type — filtered unique index `UX_ConnectionRequest_PendingPair`.
* One connection and one conversation per pair — unique `(StartupId, InvestorId)` on both tables.
* Non-negative wallet buckets — CHECK constraints + optimistic concurrency token on `StartupWallet`.
* Welcome grant once, webhook credit once, pitch debit once — unique `TokenLedger.IdempotencyKey`.
* Daily invitation quota — unique `(InvestorId, DhakaDate)` counter with concurrency token, CHECK 0–10.
* One like per account — unique `(PostId, ActorAccountId)`.

## AI boundaries

* **Model 1** (`IContentSafetyModel`): only posts and founder pitches; returns validated Approve/Reject; timeout or
  outage = fail closed (draft kept, no debit). Never called for comments, replies or chats (test T25).
* **Model 2** (`IAiMatchingService`): receives permission-filtered, sanitised profiles; output filtered to the allowed
  candidate set; any failure → deterministic `RuleBasedMatcher`; promotions keep running and counting (test T26).
* Verification decisions are always human (Verification Administrator).

## Background jobs

`ScheduledJobs` (BackgroundService) runs idempotent Facade operations: request expiry, promotion clocks, malware-scan
queue. For multiple instances, move to a persistent scheduler (e.g. Quartz with SQL job store) — Facade calls stay.

## v3 / v4 additions

* **Media pipeline.** `UploadSaver` (Web) streams the upload to private storage and checks magic bytes →
  `MediaIntake` (Facade) validates kind/size/duration and runs the malware scanner → `MediaAsset` row.
  Every file is served through `/media/{id}` → `IMediaFacade.OpenAsync`, which applies the access rules
  (owner, pitch recipients, public teasers/posts, document download permission) and returns a stream with HTTP
  Range support for seeking. Nothing in pitch videos or documents is ever sent to an AI model.
* **Pitch package.** One `PitchPackage` per startup, versioned on every change; each sent pitch snapshots
  `PackageVersion` and the chosen document ids. Engagement (`PitchEngagement`) is written only for the receiving
  investor and never goes backwards.
* **Profiles.** `MemberProfile` belongs to a user account or a team account. Name changes run through one rule set
  (`ProfileFacade.ValidateName`, reserved words, 30-day cooldown); verified accounts create a `NameChangeRequest`
  (one pending per account, enforced by a filtered unique index) that a Verification/Super admin decides.
  Approval renames everywhere the name is denormalised (account, investor profile, comments, messages, questions,
  meetings) inside one unit of work; `NameDirectory` resolves current names for new content.
* **Shell.** `ShellViewComponent` builds the top bar (Home on every page), sidebar and avatar once per request
  (cached in `HttpContext.Items`) from `IProfileFacade.GetShellAsync`.
