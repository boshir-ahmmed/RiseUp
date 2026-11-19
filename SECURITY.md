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
* Payments: HMAC-verified server-to-server confirmations; browser redirects never credit; idempotent credits.
* Privacy: restricted fields are never placed in DTOs/HTML for unauthorised viewers (tests T23, smoke tests);
  private-message inspection is report-scoped, reason-required and audited (test T34).
* Dependencies: `dotnet list package --vulnerable` clean (patched `System.Security.Cryptography.Pkcs` pinned).

## Known gaps / hardening backlog
* CSP allows inline **styles** (`'unsafe-inline'` in `style-src` only) for per-startup colours; move to classes or nonces.
* TOTP verification is implemented, but the authenticator **enrolment UI** is not; seeded admins rely on
  `Mfa:AllowDemoCodes` which must be `false` outside Development.
* Sandbox adapters (AI, payments, malware scanner) must be replaced before production; the sandbox payment gateway
  refuses to start in Production by design.
* Email delivery: notifications are written to the outbox; an SMTP/API sender worker is not yet implemented.
* Data-protection keys should be persisted to a shared store for multi-instance deployments.
* Legal templates are drafts for review by Bangladeshi counsel.

Report vulnerabilities privately to the maintainers; do not open public issues.
