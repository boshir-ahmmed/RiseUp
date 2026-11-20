# Requirements Traceability

Status of all 253 binding requirements (master prompt Appendix A). **Tested** = covered by an automated test
(test names in `tests/`); **Implemented** = built and exercised manually/E2E; **Partial / Blocked / Not verified** = honest gaps.
Summary: **Tested** 146, **Implemented** 67, **Partial** 30, **Supported** 3, **Not verified** 3, **Blocked** 2, **Deployment concern** 1, **Documented** 1


## 1.1 User requirements

| ID | Requirement | Status | Evidence / notes |
|---|---|---|---|
| UR-01 | Visitor — Browse publicly available startup profiles, approved public posts and visible comments without registering. | Tested | smoke public pages |
| UR-02 | Founder / Investor — Register with one primary role, Founder or Investor; verify email and securely manage account credentials. | Tested | Identity tests |
| UR-03 | Founder — Create and maintain one startup profile, with a limited basic public profile available before business verification. | Tested | T02 |
| UR-04 | Founder — Submit identity, business and ownership evidence for manual review and correct rejected applications. | Tested | T03_T04 |
| UR-05 | Verified Startup — Publish startup updates, manage profile and engagement, use a token wallet and send manually written pitches to eligible verifie… | Tested | T06 |
| UR-06 | Main Startup Owner — Create and manage up to five additional chat-only team logins and assign them to investor conversations. | Tested | T19 |
| UR-07 | Startup Team Member — Access only assigned group conversations and exchange permitted messages and files with their participants. | Tested | T20 |
| UR-08 | Investor — Create and maintain an investor profile, submit professional verification evidence and manage the Receive Pitches setting. | Implemented |  |
| UR-09 | Verified Investor — Search startups, view eligible funding information, receive pitches and send a limited number of free startup invitations. | Tested | T14, T23 |
| UR-10 | Founder / Investor — Discover relevant potential counterparts through AI-assisted matching and advanced manual search and filters. | Implemented | sandbox Model 2 + rule fallback |
| UR-11 | Founder / Investor — Accept or reject received pitch/invitation requests and communicate in the single authorized group chat created upon acceptance. | Tested | T15 |
| UR-12 | Verified Founder / Investor — Like, comment, reply and share links to published startup posts; report abusive content or accounts. | Tested | T25 |
| UR-13 | Startup Owner — Control access to confidential startup documents separately from group-chat participation. | Tested | T22 |
| UR-14 | Founder — Receive 20 one-time welcome pitch tokens after first successful startup verification and optionally buy additional tokens. | Tested | T05 |
| UR-15 | Verified Startup — Purchase fixed-duration promotions for eligible published posts and view promotion performance. | Tested | T06_T28 |
| UR-16 | Investor / Founder — See authorized contact details only after a startup-investor connection has been established. | Tested | T23 |
| UR-17 | Account Holder — Receive appropriate notifications and view role-specific dashboards, histories and verification status. | Partial | dashboards + in-app notifications; email delivery pending |
| UR-18 | Administrator — Perform only role-authorized verification, moderation, financial operations, configuration and reporting. | Tested | T29 |
| UR-19 | Verification Administrator — Manually decide startup and investor verification, including business ownership and re-verification. | Tested | T03_T04 |
| UR-20 | Moderator — Investigate reports, take proportionate actions and inspect private-chat content only when authorized by a report. | Tested | T34 |
| UR-21 | Finance Administrator — Investigate payment exceptions, refunds, chargebacks and audited token adjustments. | Tested | T29 |
| UR-22 | All Parties — Use RiseUp for discovery and communication only; investment agreements and transfers take place outside the platform. | Implemented |  |

## 1.2 System requirements

| ID | Requirement | Status | Evidence / notes |
|---|---|---|---|
| SR-01 | Architecture — The application shall use ASP.NET Core Model-View-Controller (MVC) with modular C# application services and server-enforced policies. | Tested | Architecture tests |
| SR-02 | Frontend — Razor Views, HTML, CSS, JavaScript and Bootstrap shall render responsive desktop, tablet and mobile-browser interfaces. | Implemented |  |
| SR-03 | Database — Microsoft SQL Server shall store accounts, startup and investor profiles, requests, group memberships, posts, tokens, payments, reports … | Implemented |  |
| SR-04 | Development environment — Microsoft Visual Studio shall support application development and debugging. | Supported | Visual Studio solution; tooling, not code |
| SR-05 | Database administration — SQL Server Management Studio (SSMS) 2022 shall support authorized database administration. | Supported | SQL script for SSMS 2022 |
| SR-06 | Source control — Git and GitHub shall maintain source control and change history. | Supported | CI workflow; repository hosting is the team's |
| SR-07 | Account services — Authentication shall support verified email, secure password reset, session management and founder/investor role separation. | Tested | Identity tests |
| SR-08 | Authorization — Server-side role- and resource-based authorization shall restrict every endpoint, dashboard operation and file download. | Tested | T32, smoke |
| SR-09 | Administrator security — Separate Super Admin, Verification Administrator, Moderator and Finance Administrator permissions; require strong two-fact… | Tested | T29 |
| SR-10 | Verification service — A document workflow shall route identity, trade license, company registration, ownership and investor credential evidence to… | Tested | T03_T04 |
| SR-11 | Email and notifications — A configured provider shall send email verification, password resets, payment receipts and other relevant notices; in-app… | Partial | in-app notifications, email outbox and SMTP delivery worker |
| SR-12 | Document storage — Private object/file storage shall protect identity documents, confidential library files and chat attachments; application endpo… | Partial | private local storage with opaque keys; production object storage pending |
| SR-13 | File security — Uploads shall be validated by size, extension and file signature, scanned for malware, and blocked or quarantined if unsafe. | Partial | signature/size checks + quarantine + DEMO scanner; real AV engine pending |
| SR-14 | Payment integration — A suitable Bangladesh payment gateway with secure hosted checkout and verified server-to-server confirmations shall process R… | Blocked | sandbox gateway only; needs merchant onboarding & credentials |
| SR-15 | Wallet and ledger — Transactional wallet services shall maintain separate Welcome and Purchased balances, append-only ledger entries and idempotent… | Tested | T06, T27 |
| SR-16 | Request workflow — Persistent state and scheduled jobs shall manage pitches, invitations, 14-day deadlines, eligibility changes and reminder/notifi… | Partial | idempotent BackgroundService jobs; persistent scheduler for multi-instance pending |
| SR-17 | Messaging — An access-controlled real-time or near-real-time group messaging service shall maintain one conversation per startup-investor pair. | Tested | T15, T20 |
| SR-18 | Search and matching — Search services shall provide indexed, filtered and sortable discovery, with authorized inputs for AI and deterministic match… | Tested | T26 |
| SR-19 | AI Model 1 — A dedicated content-safety model shall automatically approve or reject startup post and founder-authored pitch submissions before publ… | Partial | Model 1 contract + fail-closed flow tested with sandbox model; Gemini adapter blocked by credentials |
| SR-20 | AI Model 1 outage — The submission workflow shall preserve drafts and fail closed without charging tokens when content-safety inference is unavaila… | Tested | T08 |
| SR-21 | AI Model 2 — A separate model shall provide two-sided investor-startup matching and paid-promotion audience targeting. | Partial | Model 2 contract + fallback tested with sandbox model; Gemini adapter blocked by credentials |
| SR-22 | AI Model 2 fallback — Deterministic matching and rule-based promotion targeting shall operate when the matching/targeting model is unavailable. | Tested | T26 |
| SR-23 | Post and promotion service — The content system shall support verified-startup posting, fixed-duration paid promotion, Promoted labels, expiry/paus… | Tested | T06_T28 |
| SR-24 | Background processing — Scheduled processing shall expire pending requests, maintain promotion durations, reconcile payment exceptions and perform … | Partial | expiry, promotion clock, scan queue; payment reconciliation job & retention purge pending |
| SR-25 | Operational records — Restricted audit logs shall record privileged actions, verification decisions, financial events and private-content inspections. | Tested | T34 |
| SR-26 | Hosting and availability — The website and administrative functions shall operate as a responsive web platform; native mobile apps and installable … | Implemented | responsive web; no native apps/PWA by design |
| SR-27 | Localization — Public pages, dashboards, forms, notification templates and administration interfaces shall be in English; free-form user messages m… | Implemented |  |
| SR-28 | Time consistency — Invitation daily limits shall reset at midnight in Asia/Dhaka; server records shall retain unambiguous timestamps. | Tested | T14 |
| SR-29 | Financial boundary — RiseUp shall process only payments for its own platform services, not startup investments, securities agreements or investor f… | Implemented |  |

## 1.3 Functional requirements

| ID | Requirement | Status | Evidence / notes |
|---|---|---|---|
| FR-001 | Visitors shall access public startup profiles and public posts without creating an account. | Implemented |  |
| FR-002 | Primary registration shall require a choice of Founder OR Investor; one account shall not hold both primary roles. | Tested | Identity_Registration… |
| FR-003 | Registration shall collect the required identity/contact fields, password, and accepted Terms and Privacy Policy versions/timestamps. | Tested | Identity_Registration… |
| FR-004 | Users shall verify their email address before using authenticated platform functions. | Tested | Identity_Registration… |
| FR-005 | Secure sign-in, sign-out and password reset shall be supported; reset links shall be single-use and expire after 60 minutes. | Tested | Identity_LockoutAfterFive… |
| FR-006 | An account shall be temporarily locked for 15 minutes after five consecutive unsuccessful login attempts. | Tested | Identity_LockoutAfterFive… |
| FR-007 | Primary users shall be able to manage permitted profile details and request deactivation or permanent account deletion. | Partial | startup profile edit + deletion done; investor profile editing UI pending |
| FR-008 | Public administrator sign-up shall be prohibited; administrators shall be privately provisioned by an authorized administrator. | Tested | Identity_Registration… / smoke /register/admin |
| FR-009 | Founders and Investors may enable optional two-factor authentication; all administrators must use strong two-factor authentication. | Partial | admin 2FA enforced (TOTP verifier); optional user 2FA enrolment UI pending |
| FR-010 | Only a Founder designated as the Main Startup Owner shall manage the startup, its teams, pitches, wallet, posts and document grants. | Implemented |  |
| FR-011 | Founders may make one basic startup profile public before verification, but may not pitch, publish posts, buy promotions or receive investor invita… | Tested | T02 |
| FR-012 | Each Main Startup Owner shall submit a government NID or passport and the startup trade license for manual verification. | Implemented | typed identity and trade licence uploads, company authority evidence, audited manual review |
| FR-013 | Where the trade license names an individual, the Main Startup Owner shall be the named legal holder of that license. | Tested | T03_T04 |
| FR-014 | Where the trade license names a company, accepted registration evidence shall identify the Main Startup Owner as a registered director or authorize… | Tested | T03_T04 (company path: manual review) |
| FR-015 | Verification Administrators shall manually inspect identity, business documents, ownership evidence and related records; AI shall not decide docume… | Tested | T03_T04 |
| FR-016 | Investors shall provide identity and professional/company credentials and supporting evidence for manual Verification Administrator review. | Implemented | upload form + manual review |
| FR-017 | Only a successfully verified startup shall display a Verified Startup badge, and only a successfully verified investor shall display a Verified Inv… | Implemented |  |
| FR-018 | Verification badges shall identify completed identity/business checks, without implying investment performance, solvency or an investment guarantee. | Implemented |  |
| FR-019 | Administrators shall give a clear rejection reason; rejected applicants may correct information or documents and resubmit for manual review. | Tested | T03_T04 |
| FR-020 | Changes to legal identity, ownership or business registration details shall trigger re-verification and corresponding temporary restrictions; ordin… | Implemented | legal-name change; ; other legal fields follow the same path when added |
| FR-021 | Verification validity shall follow relevant evidence expiration and new risk signals; the dashboard shall display current status and dates without … | Tested | T05 |
| FR-022 | The system shall record verification reviewer, decision time, status, evidence reference and reason for rejection, expiry or revocation. | Tested | T03_T04 |
| FR-023 | Each Founder shall own at most one startup profile; duplicate legal startup identities shall be detected and reviewed. | Tested | T01 |
| FR-024 | Startup profiles shall support name, tagline, description, industry, stage, location, founding year, business model, website, tags and permitted me… | Implemented | media fields pending, see FR-028 |
| FR-025 | Startup fields shall retain the original field limits of 120 characters for tagline and 2,000 characters for description, with funding goal recorde… | Implemented |  |
| FR-026 | Startup profiles shall capture funding requirements and permitted business metrics separately from basic public information. | Implemented |  |
| FR-027 | Visitors shall see only the public startup profile; restricted funding requirements, investment information and business metrics shall be visible t… | Tested | T23 + smoke VisitorHtml… |
| FR-028 | Startup owners may maintain logo, cover, gallery and appropriate pitch-deck and support-document metadata, subject to file security controls. | Partial | logo/cover/gallery uploads not built in .NET UI (limits defined in BusinessRules) |
| FR-029 | The startup profile shall support JPG/PNG/WebP logos up to 2 MB, covers up to 5 MB, and at most 10 gallery images of up to 5 MB each. | Partial | see FR-028 |
| FR-030 | The confidential library shall support one PDF/PPTX pitch deck of up to 20 MB and up to 10 supporting PDF documents of up to 10 MB each. | Implemented |  |
| FR-031 | Owners may record prior investor claims with investor, year, round and supporting evidence where available. | Partial | claims displayed; owner claim-entry form pending |
| FR-032 | Prior investor claims may be confirmed through a participating Verified Investor, suitable official firm email or supporting investment records. | Partial | confirmation workflow pending |
| FR-033 | Unconfirmed prior-investor claims shall be visibly distinguished from independently confirmed claims. | Implemented | confirmed/unconfirmed labels |
| FR-034 | Ownership transfer shall require both the current owner and incoming owner to consent and a Verification Administrator to approve the evidence and … | Tested | T30 |
| FR-035 | A claimant shall not replace the Main Startup Owner solely on submitted documents when the existing owner refuses or cannot be reached, except as r… | Tested | T30 |
| FR-036 | An ownership dispute shall not by itself suspend the existing owner; independent evidence of fraud or another policy violation may trigger restrict… | Tested | T30 |
| FR-037 | The system shall maintain an audit record of approved ownership transfers and legal ownership changes. | Tested | T30 |
| FR-038 | Investor profiles shall contain professional identity, affiliation, preferred industries, preferred investment stages, interests and investment range. | Implemented |  |
| FR-039 | Verified startups shall be able to view investor preferences and investment ranges before purchasing a pitch. | Tested | T23 |
| FR-040 | Public visitors shall not access private investor investment ranges, personal email, phone number or direct social contact links. | Tested | T23 + smoke |
| FR-041 | Investor direct contact details and startup contact details shall remain hidden from unconnected parties and become available only to authorized pa… | Tested | T23, T20 |
| FR-042 | Investors shall independently conduct due diligence; the system shall not treat a verification badge or matching score as an investment recommendat… | Implemented | disclaimers on badges, matches and footer |
| FR-043 | The Main Startup Owner shall be able to create at most five additional active or deactivated startup team logins. | Tested | T19 |
| FR-044 | Each team account shall store a unique username, email, owner-assigned password, name, role/designation and optional social links. | Implemented |  |
| FR-045 | Only the Main Startup Owner shall create and reset team passwords; team members shall not change or reset their passwords themselves. | Tested | T21 |
| FR-046 | Team passwords shall be securely hashed and shall not be recoverable or displayed in plaintext after being assigned. | Tested | T19 (one-time credential) |
| FR-047 | Team member accounts shall have no independent Founder or Investor profile and no post, wallet, verification, pitch, invitation or administrator pe… | Tested | T21 |
| FR-048 | Only the Main Startup Owner shall add or remove individual team members from each startup-investor group conversation. | Tested | T20 |
| FR-049 | Team members shall exchange messages and approved files only in conversations to which the Main Startup Owner assigned them. | Implemented |  |
| FR-050 | Newly assigned team members shall be able to view the full authorized group history and prior attachments. | Tested | T20 |
| FR-051 | The investor shall receive an automatic notification when a new team member joins the shared group. | Tested | T20 |
| FR-052 | Removing a team member from a conversation shall immediately remove access to that conversation and its history. | Tested | T20 |
| FR-053 | The Main Startup Owner may deactivate or permanently delete team accounts; deactivated accounts shall continue occupying a team slot, while deleted… | Tested | T19 |
| FR-054 | When reactivating a team account, the Main Startup Owner shall select whether previous group memberships are restored or a different set of groups … | Implemented |  |
| FR-055 | Eligible users shall be able to search startups and investors using keyword search, filters and sorting, including industry, stage, location and co… | Implemented |  |
| FR-056 | Startup search shall support name, tagline and description matching, and offer sorting by newest, most viewed and most reacted where the data is pu… | Implemented |  |
| FR-057 | AI Model 2 shall rank eligible startup-investor matches in both directions using permitted structured details and profile relevance. | Implemented |  |
| FR-058 | Recommendations shall provide understandable relevance reasons and shall not claim that funding or investment success is guaranteed. | Implemented |  |
| FR-059 | AI ranking shall not read or expose unauthorized personal details, private files, unpublished startup metrics or private messages. | Tested | T33 |
| FR-060 | AI recommendations shall never automatically send pitches, send invitations, spend tokens, start group chats or grant file access. | Implemented |  |
| FR-061 | If AI Model 2 is unavailable, rule-based matching using industry, stage, funding range and geography shall continue automatically. | Tested | T26 |
| FR-062 | Manual searching and filtering shall remain functional regardless of AI Model 2 availability. | Tested | T26 |
| FR-063 | Eligible results shall respect active verification, role, startup publication and blocking status before display or recommendation. | Implemented |  |
| FR-064 | Only a verified Main Startup Owner shall be permitted to compose and send a manually authored pitch to a Verified Investor. | Tested | T21 |
| FR-065 | The system shall not generate, rewrite, polish or otherwise author founder pitch text with AI. | Implemented |  |
| FR-066 | AI Model 1 shall approve or reject a pitch for content safety before delivery; rejected content shall not be delivered or charged. | Tested | T07 |
| FR-067 | If AI Model 1 is unavailable, the pitch shall remain an editable saved draft and delivery shall be blocked until a successful retry. | Tested | T08 |
| FR-068 | An investor may set Receive Pitches to ON or OFF; new pitches shall be prohibited without charge while OFF. | Tested | T09 |
| FR-069 | Changing Receive Pitches to OFF shall not cancel previously pending pitches or interfere with existing group chats. | Tested | T09 |
| FR-070 | The system shall block pitch attempts to an unverified, blocked, ineligible or already-connected investor, without debiting a token. | Tested | T09, T12 |
| FR-071 | Each successfully delivered founder pitch shall cost exactly one token, with Welcome tokens consumed before Purchased tokens. | Tested | T06_T10 |
| FR-072 | Founders shall not be subject to a fixed daily pitch limit, although spam, duplicate-request and abuse controls shall apply. | Implemented |  |
| FR-073 | A startup-investor pair shall have at most one pending request in total; a pending investor invitation shall block a new founder pitch. | Tested | T12 |
| FR-074 | An unanswered pitch shall expire 14 days after its original successful delivery. | Tested | T13 |
| FR-075 | After a pitch is rejected early, a founder may pitch that investor again only when 14 days have passed since the original send time. | Tested | T13 |
| FR-076 | After the 14-day expiry of an unanswered pitch, the founder may submit another pitch immediately if otherwise eligible. | Tested | T13 |
| FR-077 | A successfully delivered pitch shall not be refunded due to acceptance, rejection, lack of response or expiry. | Tested | T06_T10, T17 |
| FR-078 | If a technical failure prevents successful pitch delivery, the token shall be restored to the same balance category from which it was debited. | Implemented |  |
| FR-079 | Every re-pitch shall require a new AI safety decision and another token if it is successfully delivered. | Tested | T13 |
| FR-080 | Only Verified Investors shall be permitted to send free invitations to verified startups. | Tested | T02 |
| FR-081 | Investor invitations shall not cost platform tokens and shall not be affected by the investor's Receive Pitches ON/OFF setting. | Implemented |  |
| FR-082 | Each Verified Investor shall be limited to 10 successfully submitted invitations per calendar day, measured in Asia/Dhaka time. | Tested | T14 |
| FR-083 | The invitation allowance shall reset at 00:00 Asia/Dhaka, without carrying unused allowance to the next day. | Tested | T14 |
| FR-084 | The Main Startup Owner, not a team member, shall accept or reject incoming investor invitations. | Tested | T32 |
| FR-085 | An unanswered invitation shall expire automatically 14 days after submission. | Implemented |  |
| FR-086 | After an invitation is rejected early, the investor may re-invite the startup only after 14 days from the original submission timestamp. | Tested | T13 (same mechanism) |
| FR-087 | A blocked or duplicate invitation attempt shall not consume the daily invitation allowance. | Tested | T12, T14 |
| FR-088 | While either a pitch or invitation is pending for a pair, a second request in either direction shall be prevented. | Tested | T12 |
| FR-089 | Accepting a pitch or invitation shall establish one connection and one private group conversation for the startup-investor pair. | Tested | T15 |
| FR-090 | No duplicate connection or second group chat shall be created for a pair already connected. | Tested | T15 |
| FR-091 | Neither party shall voluntarily close a connection, but authorized users may archive or mute its conversation. | Partial | mute implemented; archive pending |
| FR-092 | Blocking shall prevent new pitches/invitations and temporarily disable messages in the affected existing conversation. | Tested | T16 |
| FR-093 | Unblocking shall restore an existing connection and messaging access if all other eligibility rules allow it. | Tested | T16 |
| FR-094 | The chat list shall display the investor's name on the startup side and the startup's name on the investor side. | Tested | E2E chat title |
| FR-095 | Each group conversation shall include one verified investor account, the Main Startup Owner and only owner-assigned startup team accounts. | Implemented |  |
| FR-096 | Investor sub-team logins and unauthorized guests shall not participate in a group conversation. | Implemented |  |
| FR-097 | Authorized participants shall exchange text messages and allowed image, PDF and presentation file attachments. | Partial | text messaging implemented; chat file attachments pending in .NET UI |
| FR-098 | Chat text messages shall accept up to 2,000 characters, while longer materials may be shared as approved attachments. | Tested | T24 |
| FR-099 | Chat attachments shall be stored privately, malware-scanned and downloadable only by participants authorized for that group. | Partial | see FR-097 |
| FR-100 | Message authors shall be able to edit their own text messages for 15 minutes after sending, with an edited indicator. | Tested | T24 |
| FR-101 | Message authors shall be able to delete their own messages for everyone, with a deletion marker and authorized audit record. | Tested | T24 |
| FR-102 | Archiving, muting or inactivity shall not automatically erase existing conversation history or attachments. | Implemented |  |
| FR-103 | Main Startup Owners shall maintain a confidential startup document library separate from chat attachments. | Implemented |  |
| FR-104 | Verified Investors shall access confidential library files only when the Main Startup Owner explicitly grants access to that investor. | Tested | T22 |
| FR-105 | Owners shall be able to revoke an investor's library permissions; access shall not arise automatically from verification or group membership. | Tested | T22 |
| FR-106 | Authorized administrators may inspect private-chat content only in response to a report and with an auditable reason, not through routine browsing. | Tested | T34 |
| FR-107 | Comments, replies and private-chat messages shall use user reports and administrator moderation rather than routine AI content review. | Tested | T25 |
| FR-108 | Changes in verification validity shall not automatically end already established group chats, except when an independent suspension or blocking res… | Tested | T16 |
| FR-109 | Only verified Main Startup Owners shall publish, edit or remove startup posts, including announcements, milestones, product updates, achievements, … | Implemented |  |
| FR-110 | AI Model 1 shall automatically approve or reject each proposed new or edited startup post before it becomes public. | Tested | T18 |
| FR-111 | AI-rejected content shall not be published; the founder may edit it and request a fresh AI check without an administrator appeal. | Tested | T18 |
| FR-112 | If AI Model 1 fails, post content shall remain a draft rather than bypass moderation or receive routine administrator pre-approval. | Tested | T08 |
| FR-113 | Public visitors shall read approved public posts and comments without being able to perform social interactions. | Tested | E2E visitor like blocked |
| FR-114 | Only Verified Founders and Verified Investors shall like, unlike, comment, reply and share a link to a public post. | Tested | E2E |
| FR-115 | The platform shall not provide public Member accounts, general-user social engagement or post bookmarking. | Implemented |  |
| FR-116 | Authors may remove their own comments/replies, and authorized Moderators may act on reported content in line with policy. | Implemented |  |
| FR-117 | The startup dashboard shall display permitted views, likes, comments, shares and post analytics. | Partial | likes/comments/shares/views + promotion analytics; dedicated post analytics page pending |
| FR-118 | Posts shall support a title of up to 150 characters, body of up to 5,000 characters and an optional image of up to 5 MB. | Partial | title/body limits enforced; post image upload UI pending |
| FR-119 | Public post comments shall support up to 1,000 characters and replies up to three nested levels. | Tested | T25b |
| FR-120 | Startup profile views shall be deduplicated to at most one per user or anonymous session per startup in a 24-hour period. | Implemented |  |
| FR-121 | Exactly 20 one-time Welcome tokens shall be credited only when a startup first completes successful verification. | Tested | T05 |
| FR-122 | Welcome tokens shall not expire and shall be usable only for founder pitches; re-verification shall not grant additional Welcome tokens. | Tested | T05 |
| FR-123 | Purchased tokens shall not expire and shall be usable for founder pitches and eligible post promotions. | Implemented |  |
| FR-124 | Wallets shall maintain two separate non-negative balances and an append-only ledger of credits, debits and reversals. | Tested | T06_T10 |
| FR-125 | Founders shall use Welcome tokens first for pitching and Purchased tokens only after Welcome tokens are exhausted. | Tested | T06_T10 |
| FR-126 | Promotions shall be payable only from Purchased tokens, never Welcome tokens. | Tested | T06_T28 |
| FR-127 | Administrators shall configure fixed token packages and corresponding purchase prices in Bangladeshi taka. | Implemented |  |
| FR-128 | Token purchases shall use the configured Bangladesh gateway with available electronic payment methods, including mobile financial services or cards… | Implemented | SSLCOMMERZ hosted checkout exposes merchant-enabled Bangladesh channels |
| FR-129 | Purchased tokens shall be credited only upon verified and authenticated successful payment, with duplicate callbacks unable to generate duplicate c… | Tested | T27 |
| FR-130 | Every token purchase and adjustment shall appear in the authorized owner wallet history and corresponding financial audit trail. | Implemented |  |
| FR-131 | Finance Administrators shall manually investigate payment failures, missing credits, refund/chargeback disputes and exceptions; normal purchases sh… | Tested | T29 |
| FR-132 | Account deletion shall forfeit unused Welcome and Purchased token balances, subject to mandatory applicable law and a clear user warning. | Implemented |  |
| FR-133 | RiseUp shall process platform-token purchases only, not investment commitments, escrow accounts or investor-to-startup payment transfers. | Implemented |  |
| FR-134 | Verified startups shall be able to promote eligible approved posts using purchased tokens. | Tested | T06_T28 |
| FR-135 | The promotion service shall offer administrator-configurable fixed-duration packages of 1, 3 and 7 days. | Implemented |  |
| FR-136 | Promotion prices shall be specified in Purchased tokens and debited when the purchase is completed. | Tested | T06_T28 |
| FR-137 | Promoted posts shall be clearly labelled Promoted and shown in authorized promotional placements and relevant discovery feeds. | Implemented |  |
| FR-138 | AI Model 2 shall select relevant audiences using industry, investment stage and investor interests while observing data visibility rules. | Partial | Model 2 relevance used in feed ordering; explicit audience-targeting records pending |
| FR-139 | If AI Model 2 is unavailable, rule-based targeting shall continue active promotions using industry, stage and interests without pausing time or cha… | Tested | T26 |
| FR-140 | AI audience targeting shall resume automatically after model recovery, and promotion impressions, clicks and engagement shall continue to be counted. | Implemented | counting continues; switches back automatically when the model call succeeds |
| FR-141 | Promotion purchases shall normally be non-refundable if cancelled by a founder or later removed for a policy violation. | Implemented |  |
| FR-142 | A refund of promotion cost shall be considered only for a confirmed technical failure by RiseUp to activate the purchased promotion. | Partial | manual Finance remediation via ledger adjustment; no automated refund flow |
| FR-143 | Existing paid promotions shall pause on temporary account suspension or verification lapse and resume with remaining paid duration upon eligible re… | Tested | T06_T28 |
| FR-144 | Founders shall see promotion status, remaining duration and authorized performance analytics. | Implemented |  |
| FR-145 | On ordinary verification expiry, the system shall prevent new pitches, invitations, post publishing and promotion purchases until re-verified. | Tested | T17 |
| FR-146 | When a startup or investor loses ordinary verified status, all related still-pending pitches and invitations shall be cancelled; previously deliver… | Tested | T17 |
| FR-147 | Ordinary verification lapse shall not by itself disable messaging in previously created group chats. | Tested | T16 |
| FR-148 | Paid promotions active at verification lapse shall pause and preserve unused time for resumption after successful re-verification. | Tested | T17 |
| FR-149 | Confirmed fraud or policy-based suspension shall block new requests, postings and existing group messaging until reinstatement. | Tested | T16 |
| FR-150 | On suspension or verification revocation for misconduct, pending pitches and invitations shall be cancelled without refund of previously delivered … | Tested | T16 |
| FR-151 | Active promotions shall pause during suspension and resume with remaining duration if the affected startup becomes eligible again. | Tested | T06_T28 |
| FR-152 | Suspension shall preserve permitted wallet balances, group history and administrative evidence while the account is restricted. | Tested | T06_T28 |
| FR-153 | An account suspension may be appealed or resolved through authorized re-verification/investigation; content-specific AI rejections and ordinary ver… | Partial | moderators can lift suspension; formal appeal form pending |
| FR-154 | Users shall be able to report prohibited or abusive public content, profiles, comments and private group content through designated reporting funct… | Implemented | report buttons on posts, comments, profiles, messages |
| FR-155 | Moderators shall review user reports and take authorized steps including dismissing, hiding/removing, warning or suspending, with reasons recorded. | Partial | dismiss/actioned + suspension; hide/remove of the reported object pending |
| FR-156 | Private message review shall be limited to the content necessary for an authorized report investigation and shall be recorded in audit logs. | Tested | T34 |
| FR-157 | Verification Administrators shall make all identity, license, ownership and investor credential decisions; no AI verification approval service shal… | Tested | T03_T04 |
| FR-158 | Finance Administrators shall manage payment anomalies and authorized financial corrections with traceable ledger references. | Tested | T29 |
| FR-159 | Super Admins shall administer authorized platform settings and permissions without automatically granting every administrator unlimited routine pri… | Tested | T29 |
| FR-160 | Administrative actions shall record actor, target, action, reason and timestamp; privileged actions shall enforce least-privilege authorization. | Tested | T29, T34 |
| FR-161 | When an account is permanently deleted, future access shall be revoked and identity fields anonymized or removed subject to legal retention rules. | Tested | T31 |
| FR-162 | When a Founder deletes the account, the corresponding public startup and posts shall be removed, team accounts deactivated and outstanding requests… | Tested | T31 |
| FR-163 | Historical group conversations may remain accessible to the remaining authorized party with the deleted party anonymized, where lawful. | Implemented |  |
| FR-164 | Identity and verification documents shall be securely erased following permanent deletion unless an investigation, legal hold or applicable law req… | Tested | T31 |
| FR-165 | Payment/audit records shall be retained only as necessary for lawful financial and regulatory obligations. | Partial | financial records retained; retention purge job pending |
| FR-166 | Account deletion shall clearly warn the account holder about permanent startup visibility changes and forfeiture of unused tokens. | Tested | T31 |
| FR-167 | The Founder dashboard shall show verification, startup performance, posts, pitches, received invitations, group chats, team members, promotions and… | Implemented |  |
| FR-168 | The Investor dashboard shall show verification, relevant startup recommendations, received pitches, sent invitations, pending requests and group co… | Implemented |  |
| FR-169 | The platform shall provide relevant in-app and email notifications for request decisions, new messages, verification outcomes, team membership and … | Partial | in-app notifications; email pending |
| FR-170 | The system shall display pending request age, expiry and invitation quota status using the approved Bangladesh calendar rules. | Implemented |  |
| FR-171 | The public website, user dashboards, forms, system notifications and administration panel shall use English for initial release. | Implemented |  |
| FR-172 | The interface shall work on desktop, tablet and mobile browsers; dedicated native mobile apps and installable PWA behavior are excluded from the in… | Implemented | responsive; no native/PWA by design |

## 1.4 Non-functional requirements

| ID | Requirement | Status | Evidence / notes |
|---|---|---|---|
| NFR-01 | Usability — At least four of five first-time test participants shall complete registration, discovery and an eligible connection-request workflow w… | Not verified | needs a usability study |
| NFR-02 | Responsive design — Pages shall have no horizontal overflow or clipped interactive content at viewport widths of 360, 768 and 1280 pixels. | Tested | E2E 360/768/1280 |
| NFR-03 | Core performance — With 100 concurrent test users, at least 95% of public page requests shall complete in 2 seconds and dashboard requests in 3 sec… | Not verified | load test pending |
| NFR-04 | Search performance — Search/filter requests over 10,000 startup records shall complete within 2 seconds under the agreed benchmark environment. | Not verified | benchmark pending (indexes in place) |
| NFR-05 | Availability — The production service shall target at least 99% monthly availability, excluding announced maintenance windows. | Deployment concern | not verifiable in code |
| NFR-06 | Scalability — Data structures, indices and pagination shall accommodate at least 10,000 startups and 100,000 registered users without redesign of c… | Implemented | indexes + bounded paging; not load-tested |
| NFR-07 | Accessibility — User-facing interfaces shall target WCAG 2.1 AA, including keyboard navigation, clear focus indicators, readable forms and sufficie… | Partial | labels/focus/keyboard spot-checked; no full audit |
| NFR-08 | Compatibility — Supported experiences shall function in current Chrome, Edge, Firefox and Safari desktop/mobile browsers. | Partial | Chromium tested only |
| NFR-09 | Maintainability — Business rules shall be isolated in tested application services rather than implemented solely in controllers or Razor views. | Tested | Architecture tests |
| NFR-10 | Data integrity — Database constraints and atomic transactions shall enforce one startup per owner, one pending request per pair, one chat per pair … | Tested | T01, T12, T15, T06 |
| NFR-11 | Backup and recovery — The service shall perform daily backups with documented retention and tested restoration procedures. | Documented | restore drill not executed |
| NFR-12 | Privacy — No private funding details, direct contact information, verification evidence, confidential documents or group history shall be exposed t… | Tested | T23 + smoke |
| NFR-13 | Logging — Operational errors shall include timestamps and correlation IDs without logging passwords, credentials, complete private messages or sens… | Implemented |  |
| NFR-14 | Financial reconciliation — Gateway settlement and successful payments shall be checked against wallet credits; discrepancies shall be escalated for… | Partial | automatic provider order and ledger comparison, Finance exceptions; bank settlement statements require operator review |
| NFR-15 | Access control — Every restricted route, action and file request shall validate both the user role and object-level relationship before returning d… | Tested | T32 + smoke |
| NFR-16 | Authentication security — Passwords shall be one-way hashed, sensitive sessions protected, all administrators required to use strong two-factor aut… | Tested | Identity tests + T29 |
| NFR-17 | Transport security — Authenticated and payment-related traffic shall be protected by HTTPS/TLS; secrets shall not be committed to the source reposi… | Implemented | HTTPS redirect + HSTS outside Development; secrets via env/vault |
| NFR-18 | Sensitive-data storage — Identity proofs and private attachments shall use protected storage, restricted access and appropriate encryption controls. | Partial | private storage + access control; at-rest encryption is an infrastructure setting |
| NFR-19 | Secure uploads — Untrusted uploads shall be checked for file type, actual signature, size and malware and shall not be executed as active server co… | Tested | Uploads_StayQuarantined… + E2E fake PDF |
| NFR-20 | AI moderation integrity — No post or pitch shall bypass mandatory AI Model 1 approval; failure or timeout shall preserve the draft and produce no p… | Tested | T07, T08 |
| NFR-21 | AI matching resilience — If AI Model 2 fails, matching and promotion targeting shall continue by deterministic, permitted business-rule fallback. | Tested | T26 |
| NFR-22 | AI explainability — Investor-startup matching recommendations shall show intelligible evidence-based reasons and shall not be represented as invest… | Implemented |  |
| NFR-23 | AI privacy — Only data permitted to the requesting user shall be passed to AI Model 2, and sensitive identifiers shall not be included in model logs. | Tested | T33 |
| NFR-24 | Concurrent processing — Payment webhooks, pitch send attempts, invitation limits and wallet operations shall be idempotent and concurrency-safe. | Tested | T11, T15, T27 |
| NFR-25 | Auditability — All privileged verification, moderation, suspension, ownership, wallet and private-content access decisions shall be attributable to… | Tested | T34 |
| NFR-26 | Retention — Deletion and retention mechanisms shall remove identity evidence when no longer legally needed and preserve only lawful financial and c… | Partial | evidence erased on deletion; scheduled retention purge pending |
| NFR-27 | Content safety — Comments and private-chat messages shall be report-moderated; files shall be security-scanned, without routine AI reading of priva… | Tested | T25 |
| NFR-28 | Time handling — Daily invitation limits shall use Asia/Dhaka civil dates; 14-day windows and message edit deadlines shall be calculated consistentl… | Tested | T13, T14, T24 |
| NFR-29 | Localization — All system-controlled interface labels, dashboard content, form copy and notification templates shall be available in English for in… | Implemented | English UI |
| NFR-30 | Legal scope — Product wording shall avoid claiming that RiseUp guarantees funding or performs investment brokerage, escrow, underwriting or advice. | Implemented |  |


## 2. v3 / v4 requirements (added 18 Nov 2026)

| ID | Requirement | Status | Evidence |
|---|---|---|---|
| V3-01 | Founders build one reusable pitch package (pitch ≤3 min, demo ≤2 min, teaser ≤60 s, deck, ≤6 documents, ask, highlights, team, FAQ) | Tested | K01–K03 |
| V3-02 | The ask's use of funds totals 100%; every package change bumps the version | Tested | K01 |
| V3-03 | A sent pitch snapshots package version + chosen documents; only the written note goes to AI Model 1; 1 token | Tested | K04, T-series |
| V3-04 | Pitch room visible only to the receiving investor and the founding startup; founder sees analytics | Tested | R01 |
| V3-05 | Watch/slide/document analytics recorded for the investor only, monotonic | Tested | R02 |
| V3-06 | Up to 5 questions while pending; only the startup answers; no AI | Tested | R03 |
| V3-07 | "Request more info" does not pause the 14-day window | Tested | R04 |
| V3-08 | Private media only for owner and recipients; public teaser only when opted in; view-only documents not downloadable | Tested | M01, smoke |
| V3-09 | Spotlight cards show the ask only to verified investors | Tested | M02 |
| V3-10 | Shortlist is investor-private | Tested | S01 |
| V3-11 | Meetings only between connected owner and investor; other side confirms; ≤3 future slots within 90 days; .ics | Tested | S02 |
| V3-12 | Reels of public teasers; video posts in the feed | Implemented | E2E screenshots |
| V4-01 | Everyone edits their profile (name, headline, location, bio, link, photo, cover); team members photo only | Tested | P06, E2E |
| V4-02 | Names: 2–60 letters, no reserved/impersonation words | Tested | P04 |
| V4-03 | Verified accounts' name changes are reviewed by Verification/Super admins; one pending at a time | Tested | P01, P08 |
| V4-04 | Approval renames everywhere; rejection needs a reason and notifies; 30-day cooldown | Tested | P02, P03, P05 |
| V4-05 | Photos/covers/logos: JPG/PNG/WebP, size limits, malware-scanned, client-side crop | Tested | P07, E2E |
| V4-06 | Home button in the top bar on every signed-in page; sidebar on the news feed | Implemented | E2E screenshots |
| V4-07 | Saved posts are private bookmarks | Tested | S03 |
| V4-08 | Responsive layout to 360 px with bottom tab bar | Implemented | E2E 360 px check |
