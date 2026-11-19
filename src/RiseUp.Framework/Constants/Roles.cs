namespace RiseUp.Framework.Constants;

/// <summary>Role names persisted in the database and issued as claims. Never hard-code roles in controllers.</summary>
public static class Roles
{
    public const string Founder = "Founder";
    public const string Investor = "Investor";
    public const string TeamMember = "TeamMember";
    public const string SuperAdmin = "SuperAdmin";
    public const string VerificationAdmin = "VerificationAdmin";
    public const string Moderator = "Moderator";
    public const string FinanceAdmin = "FinanceAdmin";

    public static readonly string[] Admins = [SuperAdmin, VerificationAdmin, Moderator, FinanceAdmin];
}

public static class Policies
{
    public const string Founder = "Founder";
    public const string Investor = "Investor";
    public const string FounderOrInvestor = "FounderOrInvestor";
    public const string ChatParticipant = "ChatParticipant";
    public const string AnyAdmin = "AnyAdmin";
    public const string VerificationAdmin = "VerificationAdmin";
    public const string Moderator = "Moderator";
    public const string FinanceAdmin = "FinanceAdmin";
    public const string SuperAdmin = "SuperAdmin";
}

public static class ClaimNames
{
    public const string AccountId = "riseup:account_id";
    public const string TeamAccountId = "riseup:team_id";
    public const string StartupId = "riseup:startup_id";
    public const string InvestorId = "riseup:investor_id";
    public const string MfaSatisfied = "riseup:mfa";
}
