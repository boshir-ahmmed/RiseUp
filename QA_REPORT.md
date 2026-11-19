# QA Report — RiseUp (tester / system-analyst review)

Date: 8 Nov 2026, updated 18 Nov 2026 for v3 (video pitching) and v4 (profiles, new shell) · Scope: (1) the interactive HTML prototype, (2) the ASP.NET Core 10 solution in this repository.

## 1. Results summary

| Suite | Result |
|---|---|
| Facade acceptance tests (`RiseUp.Facade.Tests`, real EF Core on SQLite) | **61 / 61 passed** (36 original + 25 v3/v4) |
| Architecture rules (`RiseUp.Architecture.Tests`) | **8 / 8 passed** |
| HTTP smoke tests (`RiseUp.Web.SmokeTests`, WebApplicationFactory) | **34 / 34 passed** |
| Browser end-to-end on the running .NET app (Playwright, all roles) | **40 / 40 passed**, 0 console errors, 0 CSP violations |
| v4 browser flows (name review → approve, cropped photo upload, saved posts, pitch-room Q&A, meetings, team photo-only) | **7 / 7 passed**, 0 page errors |
| Responsive check at 360 / 768 / 1280 px | No horizontal overflow on tested public and workspace pages |
| Vulnerable NuGet packages | None |
| Prototype: automated crawl of every role × page × button (light + forced-dark colour scheme) | 0 issues after fixes |
| Prototype: business-rule edge cases | 23 / 23 passed |

## 2. Defects found and fixed — prototype

| # | Sev. | Defect | Root cause | Fix |
|---|---|---|---|---|
| P1 | High | Comment box text and caret invisible (reported by user) | Page did not declare a colour scheme; when the host applied dark mode, inputs with transparent background got white text | `color-scheme: light`, explicit text/caret colours on all form controls |
| P2 | High | Register role cards and chat list unreadable in dark scheme | Same root cause (buttons inherit UA dark text colour) | Same fix + explicit colours |
| P3 | Med | Founder dashboard overflowed to 513 px on a 360 px phone | Grid children could not shrink below table width | `.grid > * { min-width: 0 }` |
| P4 | Med | Toast notifications blocked clicks on buttons beneath them | Toast container captured pointer events | `pointer-events: none` |
| P5 | Med | Account deletion was a placeholder | Not implemented | Full flow: cancel requests, forfeit tokens (ledger), deactivate team, anonymise chats, erase evidence |
| P6 | Med | Ownership transfer had no workflow | Not implemented | Two consents + Verification Administrator approval, audited |
| P7 | Med | Legal-name change did not trigger re-verification (FR-020) | Only a message was shown | Status → Pending, promotions paused, application created |
| P8 | Med | Admin overview showed every role's counts | Missing least-privilege filter | KPIs per admin role |
| P9 | Low | Invite/Pitch buttons offered where they must fail | UI not reflecting rules | Buttons replaced by explanatory badges |
| P10 | Low | Re-verification message claimed 20 Welcome tokens were credited | Message not tied to the actual grant | Message reflects whether a grant happened |
| P11 | Low | Sticky header could hide focused elements; registration form not reset; notification badge stale | — | `scroll-padding-top`, form reset, badge cleared |

## 3. Defects found while testing the .NET application (all fixed)

| # | Sev. | Defect | Fix |
|---|---|---|---|
| N1 | Critical | Every POST form failed with 400 → 405 | Forms with an explicit `action` do not get the anti-forgery token automatically; `/error` only accepted GET | `asp-antiforgery="true"` on all 47 POST forms; error route accepts any verb |
| N2 | High | Demo DB seeding crashed (`CK_Investor_TicketRange`) | SQLite stores `decimal` as TEXT, so CHECK constraints compared strings | Decimals stored as REAL on SQLite only (SQL Server unaffected) |
| N3 | High | Uploaded documents stayed "Pending scan" forever | No scanner was wired | `IMalwareScanner` port, demo signature scanner, scan job, infected-file deletion + test |
| N4 | High | Vulnerable transitive package `System.Security.Cryptography.Pkcs` 9.0.11 | Pulled in by SqlClient | Pinned patched 10.0.14 |
| N5 | Med | Buttons and logos squashed | Bootstrap grid `.row` collided with RiseUp `.row` | Load Bootstrap Reboot + Utilities layers only |
| N6 | Med | CSP blocked inline colour styles | `style-src` too strict for dynamic colours | `'unsafe-inline'` for styles only (scripts stay `'self'`) — logged in SECURITY.md |
| N7 | Med | A controller depended on DataAccess (sandbox signing) | Layer leak | Moved to `IPaymentGateway.CreateSandboxSignature` (architecture test) |
| N8 | Low | Accept-flow system message was saved before the conversation had an id | Ordering | Message added after the first save |
| N9 | Low | Anonymous view de-duplication used `HttpContext.Session` (not configured) | Would throw | Random 24-hour HttpOnly cookie |

## 3a. v3/v4 defects found while testing the .NET application (all fixed)
| # | Defect | Fix |
|---|---|---|
| V1 | `PackageDto` name collided with the payments DTO | Renamed to `PitchPackageDto` |
| V2 | Calendar export escaped `;` incorrectly | `\;` per RFC 5545 |
| V3 | Seeded extra pending pitch changed the Receive-Pitches-OFF test expectation | T09 asserts "count unchanged" instead of a fixed number |
| V4 | Test host could not override the storage root (duplicate config key) | Overrides now replace defaults |
| V5 | Recommendations showed raw enum `PreSeed`/`SeriesA` | Human stage labels |
| V6 | Top bar lost its active tab inside a pitch room | Pitch room highlights Pitches/Requests |
| V7 | Initials for "Rashed (Verification)" rendered as `R(` | Initials use letters only |
| V8 | Reels duration chip: dark text on dark chip | White text |
| V9 | Mobile top bar hid search and showed an empty header | Compact search, no overflow at 360 px |

## 4. Acceptance matrix coverage (master prompt §17)
Executable tests exist for items 1–34 except 35 (accessibility audit, see below). Test names carry the item numbers
(`T01_…` to `T34_…`). Items 33/34 cover AI input sanitisation and audited private-message inspection.
v3/v4 rules are in `V3V4FeatureTests` (`P01–P08` profiles & names, `K01–K04` pitch package, `R01–R04` pitch room,
`M01–M02` media access & spotlight privacy, `S01–S03` shortlist, meetings, saved posts).

## 5. Not yet verified (honest limitations)
* Load/performance targets (NFR-03/04), availability (NFR-05) and backup-restore drills (NFR-11) need an agreed
  environment — not executed here.
* Full WCAG 2.1 AA audit and usability study (NFR-01/07) not performed; labels, focus styles and keyboard paths were
  spot-checked only. Browsers: Chromium tested; Firefox/Safari/Edge not yet.
* Real Gemini models, a live Bangladesh payment gateway, a production malware engine and an email sender are not
  connected (sandbox adapters by design).
* Video transcoding/thumbnailing is not server-side: duration is read from the MP4 header and the poster is captured
  in the browser. Large-file upload resumption and CDN delivery are deployment work.
