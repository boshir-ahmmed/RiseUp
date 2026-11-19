# Deployment

## Environments
| Setting | Development | Staging | Production |
|---|---|---|---|
| `Database:Provider` | `SqliteDemo` (refused in Production) | `SqlServer` | `SqlServer` |
| `Database:InitializeOnStartup` | true (EnsureCreated + demo seed) | true (migrations) | **false** — run migrations in the release pipeline |
| `Payments:Gateway` | `Sandbox` | `Sandbox` | real adapter (Sandbox throws at startup) |
| `Scanning:Host` | optional with demo signatures | internal clamd hostname | internal clamd hostname |
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

* Configure an internal ClamAV daemon with current definitions and an INSTREAM limit large enough for permitted uploads. The default port is 3310. Keep this port private. A scanner outage blocks identity intake and review. Development enables a signature-only scanner for fictional files.
* Keep scheduled jobs enabled for verification expiry and physical erasure from primary storage. Super Admins can place or release a hold per document in the verification queue. Restore procedures must reapply account deletions and holds before serving restored files. Backup retention and the legal notice need owner approval before collecting real evidence.
* Apply the ProtectVerificationEvidence migration before deployment. Older applications recorded with file extensions as document types need a corrected submission. Records already marked erased by older releases may have lost their storage keys. Reconcile these with backup records under controlled access before making erasure claims.
