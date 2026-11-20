# Security

## Controls implemented
* Passwords: ASP.NET Core Identity PBKDF2 hashing; lockout after 5 failures for 15 minutes; generic error messages.
* Reset/verification tokens: random 256-bit, stored as SHA-256 hashes, single use; reset expires after 60 minutes.
* Sessions: HttpOnly, SameSite=Lax, Secure outside Development; 8h sliding expiry.
* Authorisation: named policies per role; **every admin policy requires an MFA claim**; object-level checks in every
  Facade use case (IDOR tests T32); least-privilege admin areas (test T29).
* CSRF: global `AutoValidateAntiforgeryToken`; tokens on every POST form; `X-CSRF-TOKEN` header for `fetch`.
* Headers: CSP (`script-src 'self'`), `X-Content-Type-Options`, `X-Frame-Options: DENY`, `Referrer-Policy`,
  `Permissions-Policy`, HSTS outside Development, `noindex` on private routes, correlation IDs.
* Rate limiting: sign-in/registration and request-sending endpoints.
* Uploads: extension allow-list + magic-byte signature check + size limits + private storage with opaque keys +
  quarantine until a malware scan passes; infected files deleted.
* Hosted payments use an authenticated SSLCOMMERZ lookup with order, BDT amount, currency, bank reference and risk checks. The simulator uses signed callbacks. Browser returns open the wallet. Unique ledger and bank references prevent duplicate credits.
* Privacy: restricted fields are never placed in DTOs/HTML for unauthorised viewers (tests T23, smoke tests);
  private-message inspection is report-scoped, reason-required and audited (test T34).
* Dependencies: `dotnet list package --vulnerable` clean (patched `System.Security.Cryptography.Pkcs` pinned).

## Known gaps / hardening backlog
* CSP allows inline **styles** (`'unsafe-inline'` in `style-src` only) for per-startup colours; move to classes or nonces.
* TOTP verification is implemented, but the authenticator **enrolment UI** is not; seeded admins rely on
  `Mfa:AllowDemoCodes` which must be `false` outside Development.
* Production rejects the payment simulator and provider test stores. Configure a live merchant store for sales. The AI adapters remain a separate deployment decision.
* The email worker uses STARTTLS and a configured public link origin. Keep SMTP credentials and unprocessed outbox payloads private.
* Data-protection keys should be persisted to a shared store for multi-instance deployments.
* Legal templates are drafts for review by Bangladeshi counsel.

Report vulnerabilities privately to the maintainers; do not open public issues.
