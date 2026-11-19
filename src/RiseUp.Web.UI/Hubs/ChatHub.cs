using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using RiseUp.Facade.Conversations;
using RiseUp.Framework.Constants;
using RiseUp.Web.UI.Infrastructure;

namespace RiseUp.Web.UI.Hubs;

/// <summary>Membership is verified on join AND on every send via the Facade — never trusting client group claims.</summary>
[Authorize(Policy = Policies.ChatParticipant)]
public sealed class ChatHub(IConversationFacade conversations) : Hub
{
    public static string Group(int conversationId) => $"conversation:{conversationId}";

    public async Task Join(int conversationId)
    {
        if (!await conversations.CanAccessAsync(CurrentActor.From(Context.User!), conversationId))
            throw new HubException("Not a participant.");
        await Groups.AddToGroupAsync(Context.ConnectionId, Group(conversationId));
    }

    public async Task Send(int conversationId, string body)
    {
        var result = await conversations.SendAsync(CurrentActor.From(Context.User!), conversationId, body);
        if (result.IsFailure) throw new HubException(result.Error.Message);
        await Clients.Group(Group(conversationId)).SendAsync("message", new { conversationId, id = result.Value!.Id, author = result.Value.AuthorName, body = result.Value.Body, at = result.Value.SentAtUtc });
    }
}
