namespace RiseUp.Framework.Constants;

/// <summary>
/// Business limits from the binding RiseUp requirements. Values marked "implementation default"
/// are not stakeholder decisions and should be moved to PlatformSetting configuration.
/// </summary>
public static class BusinessRules
{
    public const int WelcomeTokens = 20;                      // FR-121
    public static readonly TimeSpan RequestWindow = TimeSpan.FromDays(14);  // FR-074/085
    public const int DailyInvitationLimit = 10;               // FR-082
    public const int MaxTeamAccounts = 5;                     // FR-043
    public static readonly TimeSpan MessageEditWindow = TimeSpan.FromMinutes(15); // FR-100
    public static readonly TimeSpan PasswordResetLifetime = TimeSpan.FromMinutes(60); // FR-005
    public const int MaxFailedLogins = 5;                     // FR-006
    public static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15); // FR-006
    public static readonly TimeSpan ProfileViewDedupe = TimeSpan.FromHours(24); // FR-120

    public const int TaglineMax = 120;                        // FR-025
    public const int DescriptionMax = 2000;                   // FR-025
    public const int PostTitleMax = 150;                      // FR-118
    public const int PostBodyMax = 5000;                      // FR-118
    public const int CommentMax = 1000;                       // FR-119
    public const int MaxCommentDepth = 3;                     // FR-119
    public const int MessageMax = 2000;                       // FR-098
    public const int PitchMax = 3000;                         // implementation default

    public const long LogoMaxBytes = 2 * 1024 * 1024;         // FR-029
    public const long CoverMaxBytes = 5 * 1024 * 1024;
    public const long PostImageMaxBytes = 5 * 1024 * 1024;
    public const int GalleryMaxImages = 10;
    public const long PitchDeckMaxBytes = 20 * 1024 * 1024;   // FR-030
    public const long SupportingDocMaxBytes = 10 * 1024 * 1024;
    public const int SupportingDocMaxCount = 10;

    public static readonly int[] PromotionDays = [1, 3, 7];   // FR-135
    public static readonly TimeSpan VerificationValidity = TimeSpan.FromDays(365); // implementation default

    // ---- v3: video pitching (see docs/V3_PITCHING_SPEC.md) ----
    public const int PitchVideoMaxSeconds = 180;
    public const int DemoVideoMaxSeconds = 120;
    public const int TeaserMaxSeconds = 60;
    public const int PostVideoMaxSeconds = 90;
    public const long VideoMaxBytes = 200L * 1024 * 1024;
    public const long PostVideoMaxBytes = 100L * 1024 * 1024;
    public const int PitchDocumentsMax = 6;
    public const long PitchDocumentMaxBytes = 10 * 1024 * 1024;
    public const int DeckSlidesMax = 20;
    public const int HighlightsMax = 5;
    public const int PitchTeamMax = 8;
    public const int PitchFaqMax = 8;
    public const int UseOfFundsMax = 6;
    public const int PitchQuestionsMax = 5;
    public const int PitchQuestionMax = 300;
    public const int PitchAnswerMax = 600;
    public const int MoreInfoBatchMax = 20;
    public const int MoreInfoReplyMax = 2000;
    public const int MoreInfoMax = 500;
    public const int ShortlistNoteMax = 300;
    public const int MeetingSlotsMax = 3;
    public static readonly TimeSpan MeetingHorizon = TimeSpan.FromDays(90);

    // ---- v4: personal profiles (see docs/V4_PROFILE_AND_UI_SPEC.md) ----
    public const long ProfilePhotoMaxBytes = 5 * 1024 * 1024;
    public const long ProfileCoverMaxBytes = 8 * 1024 * 1024;
    public const int DisplayNameMin = 2;
    public const int DisplayNameMax = 60;
    public const int HeadlineMax = 120;
    public const int ProfileLocationMax = 80;
    public const int ProfileBioMax = 600;
    public const int ProfileLinkMax = 200;
    public static readonly TimeSpan NameChangeCooldown = TimeSpan.FromDays(30);
    /// <summary>Impersonation guard for display names.</summary>
    public static readonly string[] ReservedNameWords = ["admin", "administrator", "official", "riseup", "verified", "support", "moderator", "staff"];

    public const string TermsVersion = "0.1";
    public const string PrivacyVersion = "0.1";
}
