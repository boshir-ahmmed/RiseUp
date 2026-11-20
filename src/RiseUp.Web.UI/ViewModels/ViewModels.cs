using System.ComponentModel.DataAnnotations;
using RiseUp.Entities.Common;
using RiseUp.Facade.ConfidentialDocuments;
using RiseUp.Facade.ConnectionRequests;
using RiseUp.Facade.Conversations;
using RiseUp.Facade.Discovery;
using RiseUp.Facade.Investors;
using RiseUp.Facade.Moderation;
using RiseUp.Facade.Payments;
using RiseUp.Facade.Pitching;
using RiseUp.Facade.Profiles;
using RiseUp.Facade.Posts;
using RiseUp.Facade.Promotions;
using RiseUp.Facade.Startups;
using RiseUp.Facade.TeamAccounts;
using RiseUp.Facade.Verification;
using RiseUp.Facade.Wallets;
using RiseUp.Framework;

namespace RiseUp.Web.UI.ViewModels;

// Views bind to these view models / Facade DTOs only — never to entities (fixes legacy `Login(User user)` binding).

public sealed class LoginViewModel
{
    [Required, Display(Name = "Email or team username")] public string Identifier { get; set; } = "";
    [Required, DataType(DataType.Password)] public string Password { get; set; } = "";
    public bool RememberMe { get; set; }
    public string? ReturnUrl { get; set; }
}

public sealed class MfaViewModel
{
    [Required, StringLength(6, MinimumLength = 6)] public string? Code { get; set; }
    public string? ReturnUrl { get; set; }
    public bool DemoHint { get; set; }
}

public sealed class RegisterViewModel
{
    public PrimaryRole Role { get; set; }
    [Required, StringLength(150), Display(Name = "Full legal name")] public string FullName { get; set; } = "";
    [Required, EmailAddress, StringLength(256)] public string Email { get; set; } = "";
    [Required, Phone, StringLength(30), Display(Name = "Mobile number")] public string Phone { get; set; } = "";
    [Required, StringLength(100, MinimumLength = 10), DataType(DataType.Password)] public string Password { get; set; } = "";
    [Display(Name = "Startup name"), StringLength(100)] public string? StartupName { get; set; }
    [Display(Name = "Firm / affiliation"), StringLength(150)] public string? Affiliation { get; set; }
    [Range(typeof(bool), "true", "true", ErrorMessage = "You must accept the Terms and Privacy Policy.")] public bool AcceptTerms { get; set; }
}

public sealed record FeedViewModel(IReadOnlyList<FeedItemDto> Posts, IReadOnlyList<StartupCardDto> Stories, IReadOnlyList<InvestorCardDto> Investors,
    MatchList? Recommended, string Tab, PostCategory? Category, int Take, IReadOnlyList<SpotlightDto> Spotlight, int? MyStartupId = null, bool CanPost = false);
public sealed record ProfileSuggestion(int Id, string Kind, string Name, string Detail, string? Photo, bool? Accepting)
{
    public string Href => $"/{Kind}/{Id}";
}
public sealed record ProfileSearchViewModel(string Query, IReadOnlyList<ProfileSuggestion> Results);

public sealed record StartupSearchViewModel(SearchStartupsRequest Query, PagedResult<StartupCardDto> Result);
public sealed record StartupDetailViewModel(StartupDetailDto Startup, ProfilePublications Publications, SpotlightDto? Spotlight = null);
public sealed record ProfilePublications(IReadOnlyList<FeedItemDto> Items, VideoDto? Teaser, bool Reels, string Path);
public sealed record PersonalProfileViewModel(MyProfileDto Profile, ProfilePublications? Publications);

public sealed class DashboardViewModel
{
    public IReadOnlyList<RequestDto> Requests { get; set; } = [];
    public IReadOnlyList<ConversationSummaryDto> Conversations { get; set; } = [];
    public MatchList? Matches { get; set; }
    public WalletDto? Wallet { get; set; }
    public IReadOnlyList<PromotionDto> Promotions { get; set; } = [];
    public IReadOnlyList<TeamAccountDto> Team { get; set; } = [];
    public StartupDetailDto? Startup { get; set; }
    public InvestorDetailDto? Investor { get; set; }
    public int InvitationsUsedToday { get; set; }
}

public sealed record DiscoverViewModel(string Query, bool UsedFallback, IReadOnlyList<MatchItem> Matches, IReadOnlySet<int> Shortlisted, IReadOnlyList<MatchItem> AllMatches);

public sealed class PitchStudioViewModel
{
    public string Tab { get; set; } = "package";
    public PitchPackageDto Package { get; set; } = null!;
    public IReadOnlyList<InvestorCardDto> Investors { get; set; } = [];
    public IReadOnlyList<DraftDto> Drafts { get; set; } = [];
    public IReadOnlyList<SentPitchDto> Sent { get; set; } = [];
    public WalletDto? Wallet { get; set; }
    public bool StartupVerified { get; set; }
    public int? ToInvestorId { get; set; }
    public int? EditDraftId { get; set; }
    public string? SubmittedNote { get; set; }
    public int[]? SelectedDocuments { get; set; }
    public string IdempotencyKey { get; set; } = "";
}

public sealed class RequestsViewModel
{
    public IReadOnlyList<RequestDto> Items { get; set; } = [];
    public int InvitationsUsedToday { get; set; }
    public bool ReceivePitches { get; set; }
}

public sealed record ConversationsViewModel(IReadOnlyList<ConversationSummaryDto> List, ConversationDto? Selected, IReadOnlyList<TeamAccountDto> Team, IReadOnlyList<MeetingDto>? Meetings = null);
public sealed record LibraryViewModel(IReadOnlyList<DocumentDto> Documents, IReadOnlyList<(int InvestorId, string Name)> ConnectedInvestors);
public sealed record MyPostsViewModel(IReadOnlyList<OwnPostDto> Posts, IReadOnlyList<PromotionDto> Promotions, WalletDto? Wallet, int? EditId,
    string Tab, string StartupName, VideoDto? Teaser, bool PublicTeaser, IReadOnlyList<(int Days, int Cost)> Packages);

public sealed record ReelsViewModel(IReadOnlyList<FeedItemDto> Reels, IReadOnlyList<SpotlightDto> Teasers);
public sealed record FinanceOrdersViewModel(IReadOnlyList<OrderDto> Orders, string Query, PaymentStatus? Status, bool IsSimulator);

public sealed record WalletViewModel(WalletDto Wallet, IReadOnlyList<PackageDto> Packages, IReadOnlyList<OrderDto> Orders, RiseUp.Entities.Abstractions.PaymentMode Mode);

public sealed class SettingsViewModel
{
    public StartupDetailDto? Startup { get; set; }
    public InvestorDetailDto? Investor { get; set; }
}

public sealed record AdminOverviewViewModel(AdminRole Role, int PendingVerifications, int OpenTransfers, int OpenReports, int OrdersNeedingReview, int AuditEntries);
public sealed record VerificationQueueViewModel(IReadOnlyList<ApplicationDto> Applications, IReadOnlyList<TransferDto> Transfers);

public sealed record NameChangesViewModel(IReadOnlyList<NameChangeDto> Requests);
