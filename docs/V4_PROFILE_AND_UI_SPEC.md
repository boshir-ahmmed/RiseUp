# RiseUp v4 — Modern UI, navigation and profile editing (system-analyst spec)

Prototype: https://claude.ai/artifact/XfDbdQkwVkcZE4t7E2EBBT (use "View as" in the top demo bar to switch roles).

## 1. Navigation
- **Top bar on every signed-in page** (LinkedIn-style): logo → Home feed, search (startups/industries), icon tabs with badges, and a **Me** menu (view/edit profile, dashboard, startup profile, wallet, shortlist, settings, sign out).
  - Founder: Home · Investors · Pitch Studio · Messages · Alerts
  - Investor: Home · Startups · Pitches · Messages · Alerts
  - Team member: Home · Messages · Alerts
  - Admin: Home · Admin
- Visitors keep the public header, whose first item is **Home**.
- **Left sidebar on the newsfeed and every app page**: profile card (cover, photo, name, headline, quick stats, Edit profile) + grouped navigation starting with "Home feed".
- Mobile (<760 px): tabs move to a fixed bottom bar; sidebar becomes a slide-out menu.

## 2. Newsfeed
Three columns: sidebar | feed | right rail (Trending pitches, Recommended/Popular startups, Verified investors, saved count). Visitors get an Explore rail + Join card instead of the sidebar.
- **Composer** (verified founders): Photo · Video · Milestone · Pitch shortcuts.
- **Post card**: startup header (logo, ✓, stage, tagline, time), coloured category chip, title + body with Read more, media (4:3 graphics, 16:9 video with muted autoplay), stats line, action bar **Like · Comment · Share · Save**, last two comments as bubbles with avatars, comment box with your photo.
- **Saved** tab: private bookmarks per account.
- **Pitch spotlight** card after posts 1, 4 and 7: teaser poster with play button, startup identity, chips, product-demo button, highlights. Verified investors also see the **ask (amount, instrument, min ticket, runway) and 3 metrics** and can Request full pitch / Open pitch room / Message founder / Shortlist. Everyone else sees a lock line — funding figures stay private (unchanged rule).
- Pitch room has a branded hero (logo, chips, ask, status, shortlist).

## 3. Profile model
`UserProfile { accountKey, displayName, headline(≤120), location(≤80), bio(≤600), link(https URL), photo, cover, nameChangedAt, pendingName }`
`NameChangeRequest { id, accountKey, from, to, requestedAt, status Pending|Approved|Rejected|Cancelled, reviewer, reason }`
Startup gains `logoImage`, `coverImage`.

## 4. Who can edit what
| Field | Founder (owner) | Investor | Team member | Admin |
|---|---|---|---|---|
| Profile photo | ✓ | ✓ | ✓ (only field) | ✓ |
| Cover photo | ✓ | ✓ | – | – |
| Display name | ✓ (rules below) | ✓ (rules below) | – (owner manages) | ✓ immediate |
| Headline / location / bio / link | ✓ | ✓ | – | ✓ |
| Startup logo / cover / tagline / description | ✓ | – | – | – |
| Startup legal name | re-verification (existing rule) | – | – | – |

## 5. Rules
- **Photos**: JPG/PNG/WebP, ≤5 MB, ≥200×200 px; cropped client-side (zoom + drag) to 400×400 JPEG; server re-encodes and strips EXIF; applies instantly; reported photos go to moderators. Remove → initials.
- **Cover**: ≤8 MB, ≥800×200, cropped to 1200×300. **Startup logo**: ≤2 MB, square 400×400.
- **Display name**: 2–60 characters; letters, spaces, `. ' -` only; blocked words: admin, administrator, official, RiseUp, verified, support, moderator, staff (impersonation guard); **one change per 30 days**.
- **Verified accounts** (founder of a verified startup, verified investor): new name is submitted to a **Verification Administrator / Super Admin** ("Profile name changes" queue) and must match the ID on file. The old name stays live until approval; user can cancel; rejection carries a reason and a notification.
- Unverified accounts and admins: name changes apply immediately.
- On approval the name is updated everywhere it is displayed (comments, chat messages, Q&A, meetings, startup owner field).
- Every change is written to the audit log.

## 6. Acceptance tests (automated, all passing)
34 v4 checks incl.: Home in top bar on 61 pages across all roles; photo too small / wrong type rejected; crop output 400×400 & cover 1200×300; photo appears in top bar, sidebar and on old comments; impersonation and digit names blocked; verified name → pending, old name kept, field locked; Moderator gets 403 on the queue; approval renames everywhere; 30-day cooldown; unverified changes immediately; rejection keeps name + notifies; bad link rejected; team = photo only; startup logo appears on feed posts; no horizontal overflow at 360 px; bottom tab bar on mobile. Earlier suites (v3 pitching 34, 23 business rules, smoke, feed, full role×route crawl) still pass.

## 7. .NET impact (follow-up)
Entities `UserProfile`, `NameChangeRequest`; `ProfileFacade` (UpdateProfile, UploadPhoto/Cover with ImageSharp re-encode, RequestNameChange, Approve/Reject), `StartupFacade.UpdateBranding`; layout `_AppLayout` with top bar + sidebar partials; `ProfileController`, `Admin/ProfileChangesController`; blob storage for images.
