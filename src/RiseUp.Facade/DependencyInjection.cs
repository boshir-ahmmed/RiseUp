using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using RiseUp.Facade.Common;
using RiseUp.Facade.ConfidentialDocuments;
using RiseUp.Facade.ConnectionRequests;
using RiseUp.Facade.Conversations;
using RiseUp.Facade.Discovery;
using RiseUp.Facade.Identity;
using RiseUp.Facade.Investors;
using RiseUp.Facade.Media;
using RiseUp.Facade.Pitching;
using RiseUp.Facade.Profiles;
using RiseUp.Facade.Moderation;
using RiseUp.Facade.Notifications;
using RiseUp.Facade.Payments;
using RiseUp.Facade.Posts;
using RiseUp.Facade.Promotions;
using RiseUp.Facade.Startups;
using RiseUp.Facade.TeamAccounts;
using RiseUp.Facade.Verification;
using RiseUp.Facade.Wallets;

namespace RiseUp.Facade;

public static class DependencyInjection
{
    /// <summary>Registers use cases only. No DbContext or provider here (fixes legacy AddCrmFacade).</summary>
    public static IServiceCollection AddFacade(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, ServiceLifetime.Singleton);
        services.AddScoped<ActivityRecorder>();
        services.AddScoped<EmailDelivery>();
        services.AddScoped<IAccountFacade, AccountFacade>();
        services.AddScoped<IStartupFacade, StartupFacade>();
        services.AddScoped<IInvestorFacade, InvestorFacade>();
        services.AddScoped<IVerificationFacade, VerificationFacade>();
        services.AddScoped<IMatchingFacade, MatchingFacade>();
        services.AddScoped<IConnectionRequestFacade, ConnectionRequestFacade>();
        services.AddScoped<IConversationFacade, ConversationFacade>();
        services.AddScoped<ITeamAccountFacade, TeamAccountFacade>();
        services.AddScoped<IDocumentFacade, DocumentFacade>();
        services.AddScoped<IPostFacade, PostFacade>();
        services.AddScoped<IWalletFacade, WalletFacade>();
        services.AddScoped<IPaymentFacade, PaymentFacade>();
        services.AddScoped<IPromotionFacade, PromotionFacade>();
        services.AddScoped<IModerationFacade, ModerationFacade>();
        services.AddScoped<INotificationFacade, NotificationFacade>();
        // v3/v4: media, profiles and video pitching
        services.AddScoped<MediaIntake>();
        services.AddScoped<NameDirectory>();
        services.AddScoped<IMediaFacade, MediaFacade>();
        services.AddScoped<IProfileFacade, ProfileFacade>();
        services.AddScoped<PitchPackageFacade>();
        services.AddScoped<IPitchPackageFacade>(sp => sp.GetRequiredService<PitchPackageFacade>());
        services.AddScoped<IPitchRoomFacade, PitchRoomFacade>();
        services.AddScoped<IShortlistFacade, ShortlistFacade>();
        services.AddScoped<IMeetingFacade, MeetingFacade>();
        return services;
    }
}
