# RiseUp v3 — System analysis: what a serious pitching platform still needs

Scope reviewed: the live prototype (v2) and the .NET solution against the way founders actually raise money
(Shark Tank / Demo Day / angel-syndicate practice) and the way investors actually screen deals.

## 1. Findings — the pitch itself was the weakest part of the product

| # | Gap | Why it matters | Priority |
|---|---|---|---|
| G1 | A pitch is a 3,000-character text box. | Investors decide on video, deck and numbers, not prose. Founders cannot show the product. | **Critical** |
| G2 | No pitch video or product demo video anywhere (pitch, profile, posts). | Video is the primary medium for early-stage screening; it's how Shark Tank, YC Demo Day and most syndicates work. | **Critical** |
| G3 | Documents live only in the Confidential Library, disconnected from the pitch. | The investor receiving a pitch has nothing to read with it; the founder has to grant files one by one later. | **Critical** |
| G4 | No structured "ask" (amount, instrument, valuation, use of funds, runway). | Every investor asks these first. Free text makes pitches incomparable. | High |
| G5 | Investor can only Accept / Decline. No "ask a question", "request more info" or "request a meeting". | Real deal flow is a conversation before a connection. Binary decisions kill good-but-incomplete pitches. | High |
| G6 | No pitch analytics for founders (was it watched? how far? which slides?). | Founders pay a token per pitch and get zero feedback. | High |
| G7 | No reusable pitch package — founders rewrite the pitch per investor. | Wastes tokens and time; quality drops. | High |
| G8 | Pitch deck has no viewer; PDF/PPTX is download-only. | Downloads leak, viewers log; investors want to flip slides in place. | Medium |
| G9 | No public teaser format to create demand (investors discover through the feed only). | A 60-second public teaser with "full pitch for verified investors" converts browsing into invitations. | Medium |
| G10 | No shortlist / notes for investors; no deal-flow stages. | Investors screen dozens of startups; without a pipeline they churn. | Medium |
| G11 | No meeting scheduling after connection. | Every accepted pitch ends in "let's find a time" by email. | Medium |
| G12 | No pitch-quality guidance (not AI-written — a structure checklist). | Spec forbids AI writing pitches, but a human checklist raises quality legally. | Medium |
| G13 | Chat attachments, post images and profile videos exist only as stubs. | Media is where trust is built. | Medium |
| G14 | Spec gaps: media limits, transcoding, captions, view-logging and watermarking for videos were never specified. | Needed before the .NET build can implement media. | High (spec) |

## 2. Design decisions (kept inside the binding rules)

* **Pitch Package** (per startup, reusable): pitch video ≤ 3 min, product demo video ≤ 2 min, public teaser ≤ 60 s,
  deck (PDF/PPTX ≤ 20 MB), up to 6 pitch documents (financial model, one-pager, cap table, traction report…),
  structured Ask (amount USD, instrument: equity / SAFE / convertible / revenue-share, pre-money valuation or
  discount, use of funds %, runway months), highlights, team, FAQ. A pitch *send* still costs **exactly 1 token**
  and still carries a founder-written personal note that is the only thing AI Model 1 checks (FR-066, FR-110).
  Videos and documents are **never** sent to AI; they are malware-scanned and report-moderated by humans.
* **Pitch Room** (what the investor opens from a pitch): video player with chapters, deck viewer (no download by
  default), documents (view-logged; downloads only when the founder enables it), Ask summary, Q&A thread
  (max 5 questions while pending, founder answers inside the room), decisions: Accept · Decline ·
  Request more info (pauses the 14-day clock? **No** — the clock is binding; instead it notifies the founder and the
  pitch stays pending) · Propose meeting (3 slots) — a meeting can be proposed only after acceptance (connection).
* **Analytics**: per pitch — opened, watch completion %, deck slides viewed, documents viewed, questions asked;
  aggregate per package. Shown to the founder only; investors are never told they are being measured beyond the
  standard privacy notice.
* **Reels** (public): the teaser videos of verified startups in a vertical/auto-advancing player; "Watch the full
  pitch" appears for verified investors and routes to a free invitation (investor) — never to a token charge.
* **Video posts**: posts may carry one video ≤ 90 s or one image; feed plays muted, tap for sound.
* **Shortlist**: investor-private list with stages (Watching → Pitch received → In conversation → Passed),
  private notes, never visible to founders.
* **Meetings**: after connection, either side proposes up to 3 slots (Asia/Dhaka display); accepting creates a
  meeting card in the chat and an .ics; video call happens outside RiseUp (no escrow, no calls hosted).
* **Media rules (new spec)**: MP4/WebM/MOV up to 200 MB per video; server transcodes to H.264 720p + poster frame;
  captions optional (VTT); malware scan + quarantine like documents; streaming via signed, expiring URLs;
  dynamic viewer watermark ("Shared with <investor> · RiseUp") on deck pages; view/watch events logged.

## 3. Out of scope / deliberately not added
* No AI-generated pitch text, scripts or video summaries (forbidden by the requirements).
* No in-app video calls, e-signature, escrow or term-sheet tooling (RiseUp is not a broker).
* No investor team members (forbidden).
