namespace RiseUp.Framework.Constants;

/// <summary>Centralised, stable plural routes (same idea as the legacy UrlRewrite class).</summary>
public static class UrlRewrite
{
    public static class Account
    {
        public const string Login = "login";
        public const string Logout = "logout";
        public const string Register = "register";
        public const string RegisterRole = "register/{role}";
        public const string VerifyEmail = "account/verify-email";
        public const string ForgotPassword = "account/forgot-password";
        public const string ResetPassword = "account/reset-password";
        public const string AccessDenied = "account/access-denied";
        public const string AdminLogin = "admin/login";
    }

    public const string Feed = "";
    public const string Startups = "startups";
    public const string StartupDetail = "startups/{id:int}";
    public const string Investors = "investors";
    public const string InvestorDetail = "investors/{id:int}";
    public const string PostDetail = "posts/{id:int}";
    public const string Dashboard = "dashboard";
    public const string Pitches = "pitches";
    public const string Requests = "requests";
    public const string Conversations = "conversations";
    public const string Wallet = "wallet";
    public const string Team = "team";
    public const string Library = "library";
    public const string PaymentWebhook = "payments/webhook";
    public const string Me = "me";
    public const string EditProfile = "me/edit";
    public const string Reels = "reels";
    public const string Shortlist = "shortlist";
    public const string PitchRoom = "pitch-room/{id:int}";

    /// <summary>Stable URL for a stored media asset. Access is re-authorised on every request.</summary>
    public static string Media(int id) => $"/media/{id}";
    public static string Media(int? id) => id is int v ? Media(v) : "";
}
