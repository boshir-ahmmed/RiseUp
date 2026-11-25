using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using RiseUp.Facade.Conversations;
using RiseUp.Facade.Identity;
using RiseUp.Framework.Constants;
using RiseUp.Web.UI.Infrastructure;

namespace RiseUp.Web.UI.Hubs;

/// <summary>Membership is verified on join AND on every send via the Facade — never trusting client group claims.</summary>
[Authorize(Policy = Policies.ChatParticipant)]
public sealed class ChatHub(IConversationFacade conversations, IAccountFacade accounts) : Hub
{
    public static string Group(int conversationId) => $"conversation:{conversationId}";

    private async Task RequireCurrentSessionAsync()
    {
        if (await TeamSession.IsCurrentAsync(Context.User!, accounts, Context.ConnectionAborted)) return;
        Context.Abort();
        throw new HubException("Please sign in again.");
    }

    public async Task Join(int conversationId)
    {
        await RequireCurrentSessionAsync();
        if (!await conversations.CanAccessAsync(CurrentActor.From(Context.User!), conversationId))
            throw new HubException("Not a participant.");
        await Groups.AddToGroupAsync(Context.ConnectionId, Group(conversationId));
    }

    public async Task Send(int conversationId, string body)
    {
        await RequireCurrentSessionAsync();
        var result = await conversations.SendAsync(CurrentActor.From(Context.User!), conversationId, body);
        if (result.IsFailure) throw new HubException(result.Error.Message);
        await Clients.Group(Group(conversationId)).SendAsync("changed", conversationId);
    }
}
