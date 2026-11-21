namespace RiseUp.Entities.Common;

public enum PrimaryRole { Founder = 1, Investor = 2, TeamMember = 3, Admin = 4 }
public enum AdminRole { None = 0, SuperAdmin = 1, VerificationAdmin = 2, Moderator = 3, FinanceAdmin = 4 }
public enum VerificationStatus { Unverified = 0, Pending = 1, Verified = 2, Rejected = 3, Expired = 4, Revoked = 5 }
public enum FundingStage { PreSeed = 1, Seed = 2, SeriesA = 3, SeriesB = 4 }
public enum RequestType { Pitch = 1, Invitation = 2 }
public enum RequestStatus { Pending = 0, Accepted = 1, Rejected = 2, Expired = 3, Cancelled = 4 }
public enum TokenBucket { Welcome = 1, Purchased = 2 }
public enum TeamAccountStatus { Active = 1, Deactivated = 2, Deleted = 3 }
public enum MemberKind { Owner = 1, Investor = 2, Team = 3 }
public enum PostCategory { Announcements = 1, Milestones = 2, Products = 3, Achievements = 4, Funding = 5, Hiring = 6, Reel = 7 }
public enum ModerationState { None = 0, PendingCheck = 1, Approved = 2, Rejected = 3, AiUnavailable = 4 }
public enum PaymentStatus { Pending = 0, Paid = 1, Failed = 2, Refunded = 3, RequiresReview = 4 }
public enum PromotionStatus { Active = 1, Paused = 2, Completed = 3 }
public enum DocumentKind { PitchDeck = 1, Supporting = 2 }
public enum ScanStatus { Pending = 0, Clean = 1, Infected = 2 }
public enum ReportStatus { Open = 0, Dismissed = 1, Actioned = 2 }
public enum ReportKind { Post = 1, Comment = 2, Profile = 3, GroupMessage = 4 }
public enum VerificationSubject { Startup = 1, Investor = 2 }
public enum LicenceHolderType { Individual = 1, Company = 2 }
public enum TransferStatus { AwaitingIncomingConsent = 0, AwaitingReview = 1, Approved = 2, Rejected = 3 }

// ---- v3/v4 ----
public enum MediaKind { ProfilePhoto = 1, ProfileCover = 2, StartupLogo = 3, StartupCover = 4, ProfileLogo = 5, PitchVideo = 10, DemoVideo = 11, TeaserVideo = 12, VideoPoster = 13, PitchDeck = 14, PitchDocument = 15, PostVideo = 20, PostImage = 21 }
public enum PitchVideoSlot { Pitch = 1, Demo = 2, Teaser = 3 }
public enum FundingInstrument { Equity = 1, Safe = 2, ConvertibleNote = 3, RevenueShare = 4 }
public enum NameChangeStatus { Pending = 0, Approved = 1, Rejected = 2, Cancelled = 3 }
public enum ShortlistStage { Watching = 1, PitchReceived = 2, InConversation = 3, Passed = 4 }
public enum MeetingStatus { Proposed = 0, Confirmed = 1, Cancelled = 2 }
