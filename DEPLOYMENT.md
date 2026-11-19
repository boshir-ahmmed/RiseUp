# Deployment

## Environments
| Setting | Development | Staging | Production |
|---|---|---|---|
| `Database:Provider` | `SqliteDemo` (refused in Production) | `SqlServer` | `SqlServer` |
| `Database:InitializeOnStartup` | true (EnsureCreated + demo seed) | true (migrations) | **false** — run migrations in the release pipeline |
| `Payments:Gateway` | `Sandbox` | `Sandbox` | real adapter (Sandbox throws at startup) |
| `Mfa:AllowDemoCodes` | true | optional | **false** |

Secrets (`ConnectionStrings:RiseUp`, `Payments:WebhookSecret`) come from environment variables or
a secret vault — see `.env.example`. Never commit them.

## Release steps
1. `dotnet test RiseUp.slnx` (CI: `.github/workflows/ci.yml` also checks vulnerable packages and pending migrations).
2. `dotnet ef migrations script --idempotent` → review → apply with a migration-only account.
3. Build the container (`Dockerfile`, non-root user, `/health` check) and deploy behind HTTPS.
4. Verify `/health`, sign-in, a sandbox-free payment test transaction, and admin 2FA.

## Operations
* Health: `GET /health` (includes database check).
* Logs: structured `ILogger` with `X-Correlation-ID`; no passwords, tokens, message bodies or ID documents are logged.
* Backups: daily SQL Server full + transaction-log backups with documented retention; **test restores quarterly** (NFR-11).
* Private files: mount `Storage:RootPath` on encrypted, backed-up storage (or replace with private object storage).
