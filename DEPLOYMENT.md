# Deployment

## Environments
| Setting | Development | Staging | Production |
|---|---|---|---|
| `Database:Provider` | `SqliteDemo` (refused in Production) | `SqlServer` | `SqlServer` |
| `Database:InitializeOnStartup` | true (EnsureCreated + demo seed) | true (migrations) | **false** — run migrations in the release pipeline |
| `Payments:Gateway` | `Sandbox` simulator | `SSLCOMMERZ` test store | `Disabled` or `SSLCOMMERZ` live store |
| `Registration:Enabled` | true by default | false by default | false by default |
| `Scanning:Host` | optional with demo signatures | internal clamd hostname | internal clamd hostname |
| `Mfa:AllowDemoCodes` | true | optional | **false** |

Secrets (`ConnectionStrings:RiseUp`, `Payments:SslCommerz:StorePassword`, `Email:Smtp:Password`) come from environment variables or
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

## Enable credit purchases

1. Obtain an SSLCOMMERZ merchant store and ask the provider to enable the desired Bangladesh channels, including bKash and Nagad. The hosted page displays the channels activated for that store. Confirm fees, settlement terms and wallet availability with the provider. [Payment channels and pricing](https://sslcommerz.com/pricing/)
2. Apply the RiseUp migrations. On a new database, review and run `database/RiseUp_CreditPackages.sql` to install the existing BDT credit packages. It preserves an existing catalogue.
3. In staging, configure `Site__PublicOrigin` with the public HTTPS origin, `Payments__Gateway=SSLCOMMERZ`, `Payments__SslCommerz__TestMode=true`, and the test store ID and password. Initiation supplies `/payments/sslcommerz/ipn` as the notification URL and `/payments/return` as the browser return. Keep both reachable through the reverse proxy. Keep query strings out of outbound HTTP tracing because the validation API uses merchant credentials in its query. [Provider integration guide](https://developer.sslcommerz.com/doc/v4/index.html)
4. From a verified founder’s wallet, check the merchant’s enabled payment methods, receipt and BDT amount. Check a completed payment, cancellation, delayed notification and replay. Each bank transaction should produce exactly one Purchased ledger entry. Use the live store credentials and `TestMode=false` when ready. Keep the public origin and database separate for each environment.
5. The worker checks uncredited pending orders and the previous 30 days of orders against the provider. Finance can rerun a lookup. `RequiresReview` flags mismatched amounts, currencies, high-risk results, missing or inconsistent credit entries and conflicting provider references. Investigate these and reconcile bank deposits, fees, refunds and chargebacks against the merchant settlement statement. Record any authorized compensating adjustment in Finance.

## Open registration and enable email

Set `Email__Enabled=true` and configure `Email__Smtp__Host`, `Port` (normally 587 with STARTTLS), `From`, and the relay credentials. Configure the public HTTPS origin used in email links. Verify delivery with a mailbox you control, then enable `Registration__Enabled=true`. Registration, email verification, resend and reset can be exercised while purchases are disabled. Confirm the published terms and privacy and retention notices before accepting public sign-ups.

SMTP workers claim individual outbox rows, retry after delivery failures and redact payloads after processing. A crash between relay acceptance and the database acknowledgement can cause a repeated email. Verification and reset links are single use. Monitor unprocessed mail, worker warnings and failed deliveries alongside the finance exception queue. The service can run with existing users while public registration is closed.

The ownership-transfer prototype is available in Development. Public releases keep existing cases readable and close initiation and decision routes until incoming-owner identity, consent, provisioning and access revocation are implemented together.
