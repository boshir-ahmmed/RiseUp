# RiseUp v3 — Video Pitching, Pitch Studio, Pitch Room, Reels (system-analyst spec)

Prototype: https://claude.ai/artifact/XfDbdQkwVkcZE4t7E2EBBT (role switcher in the top bar; demo clock "+1 day" completes uploads/scans).

## Why v3
Gap analysis of v2 (see GAP_ANALYSIS.md) found pitching was text-only. Investors decide on **what they can see**: product in use, the founder on camera, a deck, and a few documents. v3 adds a reusable **Pitch Package** and an investor-facing **Pitch Room**, without changing the token or AI rules.

## Unchanged business rules
- 1 token per *delivered* pitch (Welcome bucket first); AI reject / outage → draft, no charge.
- AI Model 1 checks **only the founder-written note**. Videos, deck and documents are never sent to any AI model; they are malware-scanned and human-moderated on report (FR-066: RiseUp never writes or scores pitches).
- One pending request per startup–investor pair; 14-day decision window; cooldown counted from the original send; 10 investor invitations/day (Asia/Dhaka).
- Accept → exactly one connection + one group chat. "Request more information" does **not** pause the 14-day clock.

## New domain objects
| Object | Fields | Rules |
|---|---|---|
| PitchPackage (1 per startup) | version, pitchVideo (≤3 min), demoVideo (≤2 min), teaserVideo (≤60 s, public flag), captions, deckFile (PDF/PPTX ≤20 MB) + rendered slides, docs[] (≤6, PDF/XLSX/PPTX ≤10 MB, downloadable flag), ask {amount, instrument, valuation/cap, discount, runway, minTicket, useOfFunds[] = 100 %}, highlights[≤5], team[], faq[] | Media uploaded to quarantine → scan → transcode 720p → signed streaming URL (no download). Package version increments on every save; sent pitches keep the version they were sent with. |
| PitchRequest (extends ConnectionRequest) | pkgVersion, docIds[], questions[≤5] {text, answer}, infoRequested {at,msg}, events {openedAt, watchPct per video, slidesViewed, docsViewed} | Only receiving investor / owning founder can open the room (403 otherwise). Engagement logged only inside the room. |
| Shortlist (investor-private) | sid, stage ∈ Watching · Pitch received · In conversation · Passed, note | Never visible to founders. |
| Meeting | conv, proposer, ≤3 slots (Asia/Dhaka), status Proposed/Confirmed, acceptedSlot, agenda | Only after connection; the call happens outside RiseUp; calendar details copied as text. |
| Post.video | mp4 ≤90 s + poster | Muted autoplay at ≥60 % visibility; sound toggle; grid tiles show poster + duration. |

## Screens
**Founder → Pitch Studio** (replaces "Pitches"): Package editor (3 video cards, deck import + slide editor with watermark preview, documents with download toggle, structured Ask with 100 % validation, highlights/team/FAQ), Send a pitch (investor + note → Model 1; pick which documents travel), Sent pitches & analytics (open rate, watch %, slides, docs, questions), Pitch checklist (human guidance, not AI), "Preview as investor".

**Investor → Pitch Room** (`#pitchroom-<id>`): player with chapters & CC (pitch ⇄ demo), watermarked in-app deck viewer, view-logged documents (download only if founder allowed), Ask card, founder note, highlights, team, FAQ, Q&A (max 5 while pending), decision panel: Accept / Request more information / Decline; after accept: open conversation + propose meeting. Shortlist toggle in header.

**Everyone → Reels** (feed tab + `#reels`): public 60-s teasers from verified startups, no funding figures; verified investors get "Request the full pitch (free invitation)"; visitors see Join.

**Investor → Shortlist**: pipeline stages + private notes, jump to pitch room / chat / invite.

**Conversations**: "📅 Propose meeting" after connection; meeting cards with slot acceptance.

## Media & security requirements
MP4/WebM/MOV ≤200 MB → ClamAV scan → ffmpeg transcode 720p/H.264+AAC, HLS or progressive with **HTTP Range** support (required for seeking), signed URLs (15 min), per-viewer watermark on deck/doc viewer, no download by default, view/download audit log, VTT captions, poster frames. Quarantine bucket separate from the public bucket; teaser only reaches the public bucket when `publicTeaser=true` and the startup is Verified.

## Out of scope (deliberate)
AI-written or AI-scored pitches; in-app video calls; escrow/payments between parties; investor team members; public funding figures.

## .NET solution impact (follow-up)
Entities: PitchPackage, PitchMedia, PitchDocument, PitchQuestion, PitchEvent, ShortlistEntry, Meeting. Facades: PitchPackageFacade, PitchRoomFacade (access + events + Q&A), ShortlistFacade, MeetingFacade; ConnectionRequestFacade.SendPitch snapshots package version + doc ids; MediaProcessingService (scan/transcode queue). Controllers: PitchStudio, PitchRoom, Reels, Shortlist. Migration + 20 facade tests.
