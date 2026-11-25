using RiseUp.Facade.Conversations;
using RiseUp.Facade.TeamAccounts;

namespace RiseUp.Web.UI.ViewModels;

public sealed record TeamPageViewModel(IReadOnlyList<TeamAccountDto> Members, IReadOnlyList<ConversationSummaryDto> Groups,
    int? EditingId, CreateTeamAccountRequest Form);
