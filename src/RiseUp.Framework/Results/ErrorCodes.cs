namespace RiseUp.Framework.Results;

/// <summary>Stable error codes shared by Facade, Web and tests. Mapped to HTTP status codes in Web.UI.</summary>
public static class ErrorCodes
{
    public const string Validation = "validation";
    public const string NotFound = "not_found";
    public const string Forbidden = "forbidden";
    public const string Unauthenticated = "unauthenticated";
    public const string Conflict = "conflict";

    public const string EmailTaken = "email_taken";
    public const string InvalidCredentials = "invalid_credentials";
    public const string AccountLocked = "account_locked";
    public const string EmailNotVerified = "email_not_verified";
    public const string TokenInvalid = "token_invalid";

    public const string StartupAlreadyExists = "startup_already_exists";
    public const string NotVerified = "not_verified";
    public const string InvestorNotVerified = "investor_not_verified";
    public const string StartupNotVerified = "startup_not_verified";
    public const string AccountSuspended = "account_suspended";
    public const string OwnershipMismatch = "ownership_mismatch";
    public const string ConsentMissing = "consent_missing";

    public const string PitchDisabled = "pitch_disabled";
    public const string PendingRequestExists = "pending_request_exists";
    public const string AlreadyConnected = "already_connected";
    public const string CooldownActive = "cooldown_active";
    public const string Blocked = "blocked";
    public const string InsufficientTokens = "insufficient_tokens";
    public const string InsufficientPurchasedTokens = "insufficient_purchased_tokens";
    public const string InvitationDailyLimit = "invitation_daily_limit";
    public const string NotPending = "not_pending";

    public const string ContentRejected = "content_rejected";
    public const string AiTemporarilyUnavailable = "ai_temporarily_unavailable";

    public const string TeamLimitReached = "team_limit";
    public const string EditWindowClosed = "edit_window_closed";
    public const string MessagingDisabled = "messaging_disabled";
    public const string ForbiddenDocument = "forbidden_document";
    public const string UploadPendingScan = "upload_pending_scan";

    public const string DuplicatePaymentEvent = "duplicate_payment_event";
    public const string InvalidSignature = "invalid_signature";
    public const string NegativeBalance = "negative_balance";
    public const string AlreadyPromoted = "already_promoted";

    public const string NameChangeCooldown = "name_change_cooldown";
    public const string NameChangePending = "name_change_pending";
    public const string InvalidMedia = "invalid_media";
    public const string MediaInfected = "media_infected";
    public const string LimitReached = "limit_reached";
    public const string NotConnected = "not_connected";
}
